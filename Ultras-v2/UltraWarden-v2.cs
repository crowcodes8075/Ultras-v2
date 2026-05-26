/*
name: UltraWarden
description: Ultra Warden helper with timer-based 2-taunter rotation and army synchronization.
tags: Ultra
*/

//cs_include Scripts/Ultras-v2/Dependencies/CoreEngine.cs
//cs_include Scripts/Ultras-v2/Dependencies/CoreUltra.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraPotions.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraGeneral.cs
//cs_include Scripts/Ultras-v2/Dependencies/UltraEnhancements.cs
//cs_include Scripts/CoreFarms.cs
//cs_include Scripts/CoreAdvanced.cs
//cs_include Scripts/CoreStory.cs
using System.Linq;
using Skua.Core.Interfaces;
using Skua.Core.Options;

public class UltraWarden
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

    // Timer-based taunt rotation
    private DateTime fightStartTime = DateTime.MinValue;
    private double tauntOffsetSeconds = 0;
    private const double TauntIntervalSeconds = 12.0; // 2 taunters × 6s slots
    private const double TauntWindowSeconds = 6.0;    // each taunter's window
    private DateTime lastTauntTime = DateTime.MinValue;

    string a, b;
    public bool DontPreconfigure = true;
    public string OptionsStorage = "UltraWarden-v2";
    public List<IOption> Options = new()
    {
        new Option<string>(
        "guide-1",
        "Guide-1",
        "Set Class1..Class7 to your desired classes. Leave unused entries blank.\n" +
        "It will Auto-Equip the class before going into the fight\n" +
        "Please make sure that the amount of classes in the text fields reflect\n" +
        "on army size, so if you have 3 set classes, set 3 in the army size.",
        "Please Click This Text Field"
        ),
        new Option<string>(
        "guide-2",
        "Guide-2",
        "Next is as much as possible, make sure you have Rank 10 Alchemy so\n" +
        "Potent Honor Potion needs Rank 10 good as well, so get those rep farms first.\n" +
        "The bot will use gold to make the process of getting potions faster, \n" +
        "so make sure you have some gold in your inventory, 5m should do.",
        "And Read Below"
        ),
        new Option<string>(
        "guide-3",
        "Guide-3",
        "The bot is optimized for KE SC LoO AP, it can do all of Ultras, even dage\n" +
        "the skills are optimized for KE SC LoO AP but the other classes use\n" +
        "the old Ultras skillset, the potions and enhancements are not optimized as well\n" +
        "but figured the most important part of doing ultras when you're botting is\n" +
        "it can do it, albeit it needs more time.",
        "Click all 3 for the guide"
        ),
        new Option<string>("a", "Taunter Class (Primary)",  "Class name of Taunter 1 (fires at 0s).\nName must be exact including punctuation, spelling, and capitalisation.", ""),
        new Option<string>("b", "Taunter Class (Secondary)", "Class name of Taunter 2 (fires at 6s).\nName must be exact including punctuation, spelling, and capitalisation.", ""),
        new Option<string>("Class1", "Class 1", "Preset class 1 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class2", "Class 2", "Preset class 2 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class3", "Class 3", "Preset class 3 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class4", "Class 4", "Preset class 4 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class5", "Class 5", "Preset class 5 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class6", "Class 6", "Preset class 6 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class7", "Class 7", "Preset class 7 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<bool>("DoEnh", "Do Enhancements",  "Auto-Enhance Gear properly for the fight", true),
        new Option<int>("PotionQuantity", "Potion Quantity", "How many potions to keep stocked.", 10),
        new Option<int>("ArmySize", "Army Size", "How many players are in your army (including yourself).", 4),
        CoreBots.Instance.SkipOptions,
    };

    public void ScriptMain(IScriptInterface bot)
    {
        a = (Bot.Config!.Get<string>("a") ?? "").Trim();
        b = (Bot.Config.Get<string>("b") ?? "").Trim();

        C.SetOptions();
        Core.Boot();
        Prep();
        Fight();
        C.SetOptions(false);
        Bot.StopSync();
    }

    bool IsTaunter() => Core.HasClassEquipped(a) || Core.HasClassEquipped(b);

    void EquipPresetClasses()
    {
        UltraGeneral.EquipPresetClasses(Ultra, Bot, "warden_class-v2.sync");
    }

    void Prep()
    {
        EquipPresetClasses();

        if (Bot.Config!.Get<bool>("DoEnh"))
            Enh.Apply();

        int potionQuant = Bot.Config!.Get<int>("PotionQuantity");
        Pots.EnsureRecommendedPotions(potionQuant, skipThird: IsTaunter());

        EquipPresetClasses();

        Pots.UseRecommendedPotions(potionQuant, skipThird: IsTaunter(), ensureStock: false);

        if (IsTaunter())
            Ultra.GetScrollOfEnrage();

        // Assign taunt offset based on which slot this client fills
        string cn = Bot.Player.CurrentClass?.Name ?? string.Empty;
        if (!string.IsNullOrEmpty(a) && cn.Equals(a, StringComparison.OrdinalIgnoreCase))
            tauntOffsetSeconds = 0;
        else if (!string.IsNullOrEmpty(b) && cn.Equals(b, StringComparison.OrdinalIgnoreCase))
            tauntOffsetSeconds = 6;
        else
            tauntOffsetSeconds = 0; // non-taunter, offset irrelevant

        C.Logger($"Warden taunt offset: {tauntOffsetSeconds}s (class: {cn})");
    }

    void Fight()
    {
        const string map = "ultrawarden";
        const string boss = "Ultra Warden";

        string syncPath = Ultra.ResolveSyncPath("UltraItemCheck.sync");
        Ultra.ClearSyncFile(syncPath);
        Bot.Sleep(2500);
        C.EnsureAccept(8153);
        C.AddDrop("Warden Insignia");
        Core.Join(map);
        int armySize = Math.Max(1, Bot.Config!.Get<int>("ArmySize"));
        Ultra.WaitForArmy(armySize - 1, "ultra_warden.sync");
        Core.ChooseBestCell(boss);
        Bot.Player.SetSpawnPoint();
        Core.EnableSkills();

        // Sync fight start time
        fightStartTime = DateTime.Now;
        C.Logger($"Fight start time synced: {fightStartTime:HH:mm:ss.fff}");

        while (!Bot.ShouldExit)
        {
            // Dead → wait for respawn
            if (!Bot.Player.Alive)
            {
                Bot.Wait.ForTrue(() => Bot.Player.Alive, 20);
                lastTauntTime = DateTime.MinValue;
                continue;
            }

            if (Ultra.CheckArmyProgressBool(() => Bot.TempInv.Contains("Ultra Warden Defeated", 1), syncPath))
            {
                C.Jump("Enter", "Spawn");
                C.Logger("All players finished farm.");
                C.EnsureComplete(8153);
                Ultra.JoinHouse();
                break;
            }

            Bot.Combat.Attack("*");

            // Timer-based taunt rotation — only for taunters
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
                    C.Logger($"Taunt window ({currentTime:F1}s into fight, offset {tauntOffsetSeconds}s) — executing taunt!");

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
}

