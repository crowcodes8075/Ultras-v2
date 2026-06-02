/*
name: UltraEzrajal
description: Ultra Ezrajal helper handling Counter Attack windows with army sync.
tags: Ultra
*/

//cs_include Scripts/Ultras-v2/Dependencies/CoreEngine.cs
//cs_include Scripts/Ultras-v2/Dependencies/CoreUltra.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraPotions.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraGeneral.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraEnhancements.cs
//cs_include Scripts/CoreBots.cs
//cs_include Scripts/CoreFarms.cs
//cs_include Scripts/CoreAdvanced.cs
//cs_include Scripts/CoreStory.cs

using System.Linq;
using Skua.Core.Interfaces;
using Skua.Core.Options;

public class UltraEzrajal
{
    private static CoreAdvanced Adv
    {
        get => _Adv ??= new CoreAdvanced();
        set => _Adv = value;
    }
    private CoreBots C => CoreBots.Instance;
    private static CoreAdvanced _Adv;
    public IScriptInterface Bot => IScriptInterface.Instance;
    public CoreEngine Core = new();
    public CoreUltra Ultra = new();

    private static UltraPotions Pots
    {
        get => _Pots ??= new UltraPotions();
        set => _Pots = value;
    }
    private static UltraPotions _Pots;

    private static UltraEnhancements Enh
    {
        get => _Enh ??= new UltraEnhancements();
        set => _Enh = value;
    }
    private static UltraEnhancements _Enh;

    public string OptionsStorage = "UltraEzrajal-v2";
    public bool DontPreconfigure = true;
    public List<IOption> Options = new()
    {
        new Option<int>("ArmySize", "Army Size", "How many players are in your army (including yourself).", 4),
        new Option<bool>("DoEnh", "Do Enhancements",  "Auto-Enhance Gear properly for the fight", true),
        new Option<int>("PotionQuantity", "Potion Quantity", "How many potions to keep stocked.", 10),
        new Option<string>("Class1", "Class 1", "Preset class 1 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", "ArchPaladin"),
        new Option<string>("Class2", "Class 2", "Preset class 2 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", "StoneCrusher"),
        new Option<string>("Class3", "Class 3", "Preset class 3 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", "Lord Of Order"),
        new Option<string>("Class4", "Class 4", "Preset class 4 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", "King's Echo"),
        new Option<string>("Class5", "Class 5", "Preset class 5 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", ""),
        new Option<string>("Class6", "Class 6", "Preset class 6 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", ""),
        new Option<string>("Class7", "Class 7", "Preset class 7 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", ""),
        CoreBots.Instance.SkipOptions,
    };

    public void ScriptMain(IScriptInterface bot)
    {
        C.SetOptions();
        Core.Boot();
        Bot.UltraBossHelper.EnableCounterAttack();
        try
        {
            Prep();
            Fight();
        }
        finally
        {
            C.SetOptions(false);
            Bot.StopSync();
        }
    }

    private void EquipPresetClasses()
    {
        UltraGeneral.EquipPresetClasses(Ultra, Bot, "ezrajal_class-v2.sync");
    }

    void Prep()
    {
        EquipPresetClasses();

        if (Bot.Config!.Get<bool>("DoEnh"))
        {
            DoEnhs();
        }
        int potionQuant = Bot.Config!.Get<int>("PotionQuantity");
        Pots.EnsureRecommendedPotions(potionQuant);
        EquipPresetClasses();

        Pots.UseRecommendedPotions(potionQuant, ensureStock: false);
    }

    void Fight()
    {
        const string map = "ultraezrajal";
        const string boss = "Ultra Ezrajal";

        string syncPath = Ultra.ResolveSyncPath("UltraItemCheck.sync");
        string wipeDeadSyncPath = Ultra.ResolveSyncPath("UltraEzrajalWipeDead.sync");
        string wipeAliveSyncPath = Ultra.ResolveSyncPath("UltraEzrajalWipeAlive.sync");
        Ultra.ClearSyncFile(syncPath);
        Ultra.ClearSyncFile(wipeDeadSyncPath);
        Ultra.ClearSyncFile(wipeAliveSyncPath);
        Bot.Sleep(2500);

        C.EnsureAccept(8152);
        C.AddDrop("Ezrajal Insignia");

        // ---------------------------
        // MAP SETUP
        // ---------------------------
        Core.Join(map);
        int armySize = Math.Max(1, Bot.Config!.Get<int>("ArmySize"));
        Ultra.WaitForArmy(armySize - 1, "ultra_ezrajal.sync");
        Core.ChooseBestCell(boss);
        Bot.Player.SetSpawnPoint();
        Core.EnableSkills();

        // ---------------------------
        // MAIN COMBAT LOOP
        // ---------------------------
        bool armyWipeDetected = false;

        while (!Bot.ShouldExit)
        {
            bool allDead = UltraGeneral.IsWholeArmyDead(Ultra, Bot, wipeDeadSyncPath);
            if (allDead)
            {
                if (!armyWipeDetected)
                    C.Logger("Army wipe detected — all clients dead.");
                armyWipeDetected = true;
            }

            if (armyWipeDetected)
            {
                bool allAlive = UltraGeneral.IsWholeArmyAlive(Ultra, Bot, wipeAliveSyncPath);
                if (allAlive)
                {
                    C.Logger("Army wipe recovered — all clients alive again.");
                    Ultra.ClearSyncFile(wipeDeadSyncPath);
                    Ultra.ClearSyncFile(wipeAliveSyncPath);
                    Bot.Combat.CancelTarget();
                    armyWipeDetected = false;
                    C.Logger("Army wipe recovered — resuming fight.");
                    continue;
                }

                Bot.Combat.CancelTarget();
                C.Logger("Army wipe active — waiting for everyone to respawn before fighting.");
                Bot.Sleep(250);
                continue;
            }

            // Dead → wait for respawn
            if (!Bot.Player.Alive)
            {
                Bot.Wait.ForTrue(() => Bot.Player.Alive, 20);
                continue;
            }

            // Check if the whole army has finished
            if (Ultra.CheckArmyProgressBool(() => Bot.TempInv.Contains("Ultra Ezrajal Defeated", 1), syncPath))
            {
                C.Logger("All players finished farm.");
                C.EnsureComplete(8152);
                Bot.UltraBossHelper.DisableCounterAttack();
                Ultra.JoinHouse();
                break;
            }

            // ---------------------------
            // COUNTER ATTACK HANDLER
            // ---------------------------
            if (
                Bot.Player.HasTarget
                && Bot.Target?.Auras?.Any(a => a != null && a?.Name == "Counter Attack") == true
            )
            {
                Bot.Combat.CancelAutoAttack();

                Bot.Sleep(6300);
            }
            else
            {
                Bot.Combat.Attack(boss);
            }

            Pots.ActivateEquippedPotion();
            Bot.Sleep(500); // slightly lower, smoother attacks
        }
    }

    void DoEnhs() => Enh.Apply();
}



