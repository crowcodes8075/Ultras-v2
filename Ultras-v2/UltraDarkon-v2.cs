/*
name: UltraDarkon
description: Ultra Darkon with class-based taunters and UltraPotions support.
tags: ultra, darkon, taunt, Ultra Darkon, ultra darkon
*/

//cs_include Scripts/Ultras-v2/Dependencies/CoreEngine.cs
//cs_include Scripts/Ultras-v2/Dependencies/CoreUltra.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraGeneral.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraPotions.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraEnhancements.cs
//cs_include Scripts/CoreBots.cs
//cs_include Scripts/CoreFarms.cs
//cs_include Scripts/CoreAdvanced.cs
//cs_include Scripts/CoreStory.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Skua.Core.Interfaces;
using Skua.Core.Options;

public class UltraDarkon
{
    private static CoreAdvanced Adv
    {
        get => _Adv ??= new CoreAdvanced();
        set => _Adv = value;
    }

    private static CoreAdvanced _Adv;

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

    private CoreBots C => CoreBots.Instance;

    public IScriptInterface Bot => IScriptInterface.Instance;

    public CoreEngine Core = new();

    public CoreUltra Ultra = new();

    public bool DontPreconfigure = true;

    public string OptionsStorage = "UltraDarkon-v2";

    bool isPrimaryTaunter;
    bool isSecondaryTaunter;
    bool isTertiaryTaunter;

    // Timer-based taunt rotation
    private DateTime fightStartTime = DateTime.MinValue;
    private double tauntOffsetSeconds = 0;
    private const double TauntIntervalSeconds = 12.0; // 3 taunters × 4s slots
    private const double TauntWindowSeconds = 4.0;
    private DateTime lastTauntTime = DateTime.MinValue;

    public List<IOption> Options = new()
    {
        new Option<int>("ArmySize", "Army Size", "How many players are in your army (including yourself).", 4),

        new Option<string>(
            "PrimaryTaunter",
            "Primary Taunter Class",
            "Class name of the primary taunter (fires at 0s).",
            "StoneCrusher"
        ),

        new Option<string>(
            "SecondaryTaunter",
            "Secondary Taunter Class",
            "Class name of the secondary taunter (fires at 4s).",
            "Lord Of Order"
        ),

        new Option<string>(
            "TertiaryTaunter",
            "Tertiary Taunter Class",
            "Class name of the tertiary taunter (fires at 8s).",
            "ArchPaladin"
        ),

        new Option<string>("Class1", "Class 1", "Preset class 1 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", "ArchPaladin"),
        new Option<string>("Class2", "Class 2", "Preset class 2 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", "StoneCrusher"),
        new Option<string>("Class3", "Class 3", "Preset class 3 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", "Lord Of Order"),
        new Option<string>("Class4", "Class 4", "Preset class 4 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", "King's Echo"),
        new Option<string>("Class5", "Class 5", "Preset class 5 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", ""),
        new Option<string>("Class6", "Class 6", "Preset class 6 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", ""),
        new Option<string>("Class7", "Class 7", "Preset class 7 to auto-equip before the fight. \nUse format: ClassName,Username. \nOnly type ClassName if you want it to be random.", ""),

        new Option<bool>(
            "DoEnh",
            "Do Enhancements",
            "Auto-Enhance Gear properly for the fight",
            true
        ),

        new Option<int>(
            "PotionQuantity",
            "Potion Quantity",
            "How many potions to keep stocked.",
            10
        ),

        CoreBots.Instance.SkipOptions,
    };

    private string NormalizeString(string input) =>
        (input ?? "").Trim().ToLower();

    bool HasAssignedClass(string assignedClass) =>
        NormalizeString(Bot.Player.CurrentClass?.Name)
        == NormalizeString(assignedClass);

    bool IsTaunter() => isPrimaryTaunter || isSecondaryTaunter || isTertiaryTaunter;

    private bool HasBossTarget(string boss) =>
        Bot.Player.HasTarget &&
        string.Equals(Bot.Player.Target?.Name, boss, StringComparison.OrdinalIgnoreCase);

    private void EquipPresetClasses()
    {
        UltraGeneral.EquipPresetClasses(Ultra, Bot, "darkon_class-v2.sync");
    }

    public void ScriptMain(IScriptInterface bot)
    {
        C.Logger("Ultra Darkon loaded.");

        C.SetOptions();

        Core.Boot();
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

    void Prep()
    {
        EquipPresetClasses();

        string primaryTaunter =
            Bot.Config!.Get<string>("PrimaryTaunter");

        string secondaryTaunter =
            Bot.Config!.Get<string>("SecondaryTaunter");

        string tertiaryTaunter =
            Bot.Config!.Get<string>("TertiaryTaunter");

        isPrimaryTaunter = HasAssignedClass(primaryTaunter);

        isSecondaryTaunter = HasAssignedClass(secondaryTaunter);
        isTertiaryTaunter = HasAssignedClass(tertiaryTaunter);

        if (Bot.Config!.Get<bool>("DoEnh"))
            DoEnhs();

        // Assign taunt offset after enhancements so the class is final
        string cn = Bot.Player?.CurrentClass?.Name ?? string.Empty;
        if (!string.IsNullOrEmpty(primaryTaunter) && cn.Equals(primaryTaunter, StringComparison.OrdinalIgnoreCase))
            tauntOffsetSeconds = 0;
        else if (!string.IsNullOrEmpty(secondaryTaunter) && cn.Equals(secondaryTaunter, StringComparison.OrdinalIgnoreCase))
            tauntOffsetSeconds = 4;
        else if (!string.IsNullOrEmpty(tertiaryTaunter) && cn.Equals(tertiaryTaunter, StringComparison.OrdinalIgnoreCase))
            tauntOffsetSeconds = 8;
        else
            tauntOffsetSeconds = 0; // non-taunter, offset irrelevant

        C.Logger($"Taunter [{(IsTaunter() ? "Yes" : "No")}] | Darkon taunt offset: {tauntOffsetSeconds}s (class: {cn})");

        int potionQuant = Bot.Config!.Get<int>("PotionQuantity");
        Pots.EnsureRecommendedPotions(potionQuant, skipThird: IsTaunter());

        EquipPresetClasses();

        Pots.UseRecommendedPotions(potionQuant, skipThird: IsTaunter(), ensureStock: false);

        if (IsTaunter())
            Ultra.GetScrollOfEnrage();

        Bot.Sleep(2500);
    }

    void Fight()
    {
        const string boss = "Darkon the Conductor";

        if (!C.isCompletedBefore(8733))
        {
            C.Logger("Quest 8733 (\"The World\") not completed.");
            Bot.Quests.UpdateQuest(8733);
        }

        string syncPath = Ultra.ResolveSyncPath("UltraItemCheck.sync");
        string wipeDeadSyncPath = Ultra.ResolveSyncPath("UltraDarkonWipeDead.sync");
        string wipeAliveSyncPath = Ultra.ResolveSyncPath("UltraDarkonWipeAlive.sync");

        Ultra.ClearSyncFile(syncPath);
        Ultra.ClearSyncFile(wipeDeadSyncPath);
        Ultra.ClearSyncFile(wipeAliveSyncPath);

        Bot.Sleep(2500);

        C.EnsureAccept(8746);

        C.AddDrop("Darkon Insignia");

        Core.Join("ultradarkon");
        int armySize = Math.Max(1, Bot.Config!.Get<int>("ArmySize"));
        Ultra.WaitForArmy(armySize - 1, "Ultra_Darkon.sync");

        Core.ChooseBestCell(boss);

        Core.EnableSkills();

        // Sync fight start time
        fightStartTime = DateTime.Now;
        C.Logger($"Fight start time synced: {fightStartTime:HH:mm:ss.fff}");

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
                    fightStartTime = DateTime.MinValue;
                    lastTauntTime = DateTime.MinValue;
                    armyWipeDetected = false;
                    C.Logger("Army wipe recovered — resetting taunt timing and resuming fight.");
                    continue;
                }

                Bot.Combat.CancelTarget();
                C.Logger("Army wipe active — waiting for everyone to respawn before fighting.");
                Bot.Sleep(250);
                continue;
            }

            // Dead → wait for respawn.
            if (!Bot.Player.Alive)
            {
                Bot.Wait.ForTrue(() => Bot.Player.Alive, 20);
                lastTauntTime = DateTime.MinValue;
                continue;
            }

            if (Ultra.CheckArmyProgressBool(() => Bot.TempInv.Contains("Darkon the Conductor Defeated", 1), syncPath))
            {
                C.Jump("Enter", "Spawn");
                C.Logger("All players finished farm.");
                C.EnsureComplete(8746);
                Bot.Wait.ForPickup("Darkon Insignia");
                Ultra.JoinHouse();
                break;
            }

            // Dynamic targeting — all players focus the sword first, then Darkon.
            if (Bot.Monsters.MapMonsters.Any(x => x != null && x.MapID == 1 && x.HP > 0))
            {
                if (Bot.Player.Target?.MapID != 1)
                    Bot.Combat.Attack(1);
            }
            else if (!HasBossTarget(boss))
            {
                Bot.Combat.Attack(boss);
            }

            // Timer-based taunt rotation — only for taunters.
            if (IsTaunter() && Bot.Player.HasTarget)
            {
                TimeSpan timeSinceFightStart = DateTime.Now - fightStartTime;
                double currentTime = timeSinceFightStart.TotalSeconds;
                double timeInCycle = (currentTime - tauntOffsetSeconds) % TauntIntervalSeconds;
                bool inTauntWindow = timeInCycle >= 0 && timeInCycle < TauntWindowSeconds;
                bool cooldownExpired = (DateTime.Now - lastTauntTime).TotalSeconds >= TauntIntervalSeconds - 1;

                if (inTauntWindow && cooldownExpired)
                {
                    lastTauntTime = DateTime.Now;
                    Bot.Combat.Attack(boss);
                    C.Logger($"Taunt window ({currentTime:F1}s into fight, offset {tauntOffsetSeconds}s) — taunting!");

                    for (int i = 0; i < 40 && !Bot.ShouldExit; i++)
                    {
                        if (!Bot.Player.Alive) break;
                        if (!Bot.Player.HasTarget)
                            Bot.Combat.Attack(boss);
                        Bot.Skills.UseSkill(5);
                        Bot.Sleep(25);
                    }

                    C.Logger("Taunt done — 60 presses complete.");
                    Bot.Sleep(300);
                }
            }

            Pots.ActivateEquippedPotion();

            Bot.Sleep(250);
        }
    }

    void DoEnhs() => Enh.ApplyNoVainglory();
}



