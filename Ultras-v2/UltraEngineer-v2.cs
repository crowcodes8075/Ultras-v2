/*
name: UltraEngineer
description: Ultra Engineer helper prioritizing drones with army synchronization and consumables.
tags: Ultra
*/

//cs_include Scripts/Ultras-v2/Dependencies/CoreEngine.cs
//cs_include Scripts/Ultras-v2/Dependencies/CoreUltra.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraPotions.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraEnhancements.cs
//cs_include Scripts/CoreBots.cs
//cs_include Scripts/CoreFarms.cs
//cs_include Scripts/CoreAdvanced.cs
//cs_include Scripts/CoreStory.cs
using System.Linq;
using Skua.Core.Interfaces;
using Skua.Core.Options;

public class UltraEngineer
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

    public bool DontPreconfigure = true;
    public string OptionsStorage = "UltraEngineer-v2";
    public List<IOption> Options = new()
    {
        
        new Option<string>("guide-1", "Guide-1", "Set Class1..Class7 to your desired classes. Leave unused entries blank.\nIt will Auto-Equip the class before going into the fight\nPlease make sure that the amount of classes in the text fields reflects\non army size, so if you have 3 set classes, set 3 in the army size.", "Please Click This Text Field"),
        new Option<string>("guide-2", "Guide-2", "Next is as much as possible, make sure you have Rank 10 Alchemy so\nthat the bot does not need to farm for rank 10 alchemy.\nThe bot will use gold to make the process of getting potions faster, \nso make sure you have some gold in your inventory, 5m should do.", "And Read Below"),
        new Option<string>("guide-3", "Guide-3", "The bot is optimized for KE SC LoO AP, it can do all of Ultras, even dage\nthe skills are optimized for KE SC LoO AP but the other classes uses\nthe old Ultras skillset, the potions and enhancements are not optimized as well\nbut figured the most important part of doing ultras when you're botting is\nit can do it, albeit it needs more time.", "Click all 3 for the guide"),
        new Option<bool>("DoEnh", "Do Enhancements",  "Auto-Enhance Gear properly for the fight", true),
        new Option<int>("PotionQuantity", "Potion Quantity", "How many potions to keep stocked.", 10),
        new Option<string>("Class1", "Class 1", "Preset class 1 to auto-equip before the fight.", ""),
        new Option<string>("Class2", "Class 2", "Preset class 2 to auto-equip before the fight.", ""),
        new Option<string>("Class3", "Class 3", "Preset class 3 to auto-equip before the fight.", ""),
        new Option<string>("Class4", "Class 4", "Preset class 4 to auto-equip before the fight.", ""),
        new Option<string>("Class5", "Class 5", "Preset class 5 to auto-equip before the fight.", ""),
        new Option<string>("Class6", "Class 6", "Preset class 6 to auto-equip before the fight.", ""),
        new Option<string>("Class7", "Class 7", "Preset class 7 to auto-equip before the fight.", ""),
        new Option<int>("ArmySize", "Army Size", "How many players are in your army (including yourself).", 4),CoreBots.Instance.SkipOptions,
    };

    public void ScriptMain(IScriptInterface bot)
    {
        if (
            Bot.Config != null
            && Bot.Config.Options.Contains(C.SkipOptions)
            && !Bot.Config.Get<bool>(C.SkipOptions)
        )
            Bot.Config.Configure();

        C.SetOptions();
        Core.Boot();
        Prep();
        Fight();
        C.SetOptions(false);
        Bot.StopSync();
    }

    private void EquipPresetClasses()
    {
        var presetClasses = new[]
        {
            Bot.Config!.Get<string>("Class1"),
            Bot.Config.Get<string>("Class2"),
            Bot.Config.Get<string>("Class3"),
            Bot.Config.Get<string>("Class4"),
            Bot.Config.Get<string>("Class5"),
            Bot.Config.Get<string>("Class6"),
            Bot.Config.Get<string>("Class7"),
        }
        .Select(cl => cl?.Trim())
        .Where(cl => !string.IsNullOrEmpty(cl))
        .ToArray();

        if (presetClasses.Length == 0)
            return;

        int armySize = Math.Max(1, Bot.Config!.Get<int>("ArmySize"));
        bool allowDuplicates = presetClasses.Length < armySize || presetClasses.Distinct(StringComparer.OrdinalIgnoreCase).Count() < presetClasses.Length;
        string[][] classSlots = Enumerable.Range(0, armySize)
            .Select(_ => presetClasses)
            .ToArray();

        Ultra.EquipClassSync(classSlots, armySize, "engineer_class-v2.sync", allowDuplicates);
    }

    void Prep()
    {
        EquipPresetClasses();

        if (Bot.Config!.Get<bool>("DoEnh"))
        {
            DoEnhs();
        }
        int potionQuant = Bot.Config!.Get<int>("PotionQuantity");
        Pots.UseRecommendedPotions(potionQuant);
        EquipPresetClasses();
    }

    void Fight()
    {
        const string map       = "ultraengineer";
        const string boss = "Ultra Engineer";
        const string priority1 = "Defense Drone";
        const string priority2 = "Attack Drone";

        string syncPath = Ultra.ResolveSyncPath("UltraItemCheck.sync");
        Ultra.ClearSyncFile(syncPath);
        Bot.Sleep(2500);
        C.EnsureAccept(8154);
        C.AddDrop("Engineer Insignia");
        Core.Join(map);
        int armySize = Math.Max(1, Bot.Config!.Get<int>("ArmySize"));
        Ultra.WaitForArmy(armySize - 1, "ultra_engineer.sync");
        Core.ChooseBestCell(boss);
        Bot.Player.SetSpawnPoint();
        Core.EnableSkills();

        while (!Bot.ShouldExit)
        {
            // Dead → wait for respawn
            if (!Bot.Player.Alive)
            {
                Bot.Wait.ForTrue(() => Bot.Player.Alive, 20);
                continue;
            }

            // Check if the whole army has finished
            if (Ultra.CheckArmyProgressBool(() => Bot.TempInv.Contains("Ultra Engineer Defeated", 1), syncPath))
            {
                C.Logger("All players finished farm.");
                C.EnsureComplete(8154);
                Ultra.JoinHouse();
                break;
            }
            Ultra.KillWithPriority(boss, 3, priority1, 2, priority2, 1);
            Pots.ActivateEquippedPotion();
        }
    }

    void DoEnhs() => Enh.Apply();
}

