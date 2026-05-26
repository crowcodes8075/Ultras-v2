/*
name: UltraDailies
description: Unified Ultra-v2 helper for UltraEzrajal, UltraWarden, UltraEngineer, and UltraAvatarTyndarius.
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

using System;
using System.Collections.Generic;
using System.Linq;
using Skua.Core.Interfaces;
using Skua.Core.Models.Items;
using Skua.Core.Models.Monsters;
using Skua.Core.Options;

public class UltraDailies
{
    private static CoreAdvanced Adv
    {
        get => _Adv ??= new CoreAdvanced();
        set => _Adv = value;
    }
    private static CoreAdvanced _Adv;
    private CoreBots C => CoreBots.Instance;
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

    public IScriptInterface Bot => IScriptInterface.Instance;
    public CoreEngine Core = new();
    public CoreUltra Ultra = new();

    public bool DontPreconfigure = true;
    public string OptionsStorage = "UltraDailies";

    private const double WardenTauntInterval = 12.0;
    private const double WardenTauntWindow = 6.0;
    private DateTime wardenFightStartTime = DateTime.MinValue;
    private double wardenTauntOffsetSeconds = 0;
    private DateTime wardenLastTauntTime = DateTime.MinValue;

    private bool isBall1Taunter;
    private bool isBall2Killer;
    private DateTime tynFightStartTime = DateTime.MinValue;
    private double tynTauntOffset = 0;
    private const double TynTauntInterval = 12.0;
    private const double TynTauntWindow = 6.0;
    private DateTime tynLastTaunt = DateTime.MinValue;

    private bool enhancementsApplied = false;

    private const string BossParticipantSyncFile = "ultra_dailies_participants.sync";

    private const string BossSyncFile = "ultra_dailies_bosses.sync";

    public List<IOption> Options = new()
    {
        // UltraWarden options
        new Option<string>("WardenTaunter1", "Warden Primary Taunter", "Class name for Warden primary taunter (fires at 0s).", ""),
        new Option<string>("WardenTaunter2", "Warden Secondary Taunter", "Class name for Warden secondary taunter (fires at 6s).", ""),

        // UltraAvatarTyndarius options
        new Option<string>("Ball1Taunter", "Ball 1 Taunter", "Class name that taunts Ball 1 (left orb).", "ArchPaladin"),
        new Option<string>("Ball2Killer", "Ball 2 Killer", "Class name that kills Ball 2 (right orb).", "King's Echo"),
        new Option<string>("TynTaunter1", "Tyndarius Taunter 1", "Class name of Tyndarius Taunter 1 (fires at 0s).", "StoneCrusher"),
        new Option<string>("TynTaunter2", "Tyndarius Taunter 2", "Class name of Tyndarius Taunter 2 (fires at 6s).", "Lord Of Order"),

        new Option<string>("Class1", "Class 1", "Preset class 1 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class2", "Class 2", "Preset class 2 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class3", "Class 3", "Preset class 3 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class4", "Class 4", "Preset class 4 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class5", "Class 5", "Preset class 5 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class6", "Class 6", "Preset class 6 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),
        new Option<string>("Class7", "Class 7", "Preset class 7 to auto-equip before the fight. Use format: ClassName,Username. Only type ClassName if you want it to be random.", ""),

        new Option<int>("ArmySize", "Army Size", "How many players are in your army (including yourself).", 4),
        new Option<bool>("DoEnh", "Do Enhancements", "Auto-Enhance Gear properly for the fight", true),
        new Option<int>("PotionQuantity", "Potion Quantity", "How many potions to keep stocked.", 10),
        CoreBots.Instance.SkipOptions,
    };

    public void ScriptMain(IScriptInterface bot)
    {
        string boss = "UltraDailies";

        C.SetOptions();
        Core.Boot();

        switch (NormalizeString(boss))
        {
            case "ultradailies":
            case "all":
                RunBossQueue(new[] { "UltraEzrajal", "UltraWarden", "UltraEngineer", "UltraAvatarTyndarius" });
                break;
            case "ultraezrajal":
            case "ultra ezrajal":
                PrepEzrajal();
                FightEzrajal();
                break;
            case "ultrawarden":
            case "ultra warden":
                PrepWarden();
                FightWarden();
                break;
            case "ultraengineer":
            case "ultra engineer":
                PrepEngineer();
                FightEngineer();
                break;
            case "ultraavatartyndarius":
            case "ultra avatar tyndarius":
                C.Join("whitemap");
                PrepAvatarTyndarius();
                FightAvatarTyndarius();
                break;
            default:
                C.Logger($"Unknown boss selected: {boss}", "Error", true, true);
                break;
        }

        C.SetOptions(false);
        Bot.StopSync();
    }

    private void RunBossQueue(IEnumerable<string> bosses)
    {
        PublishBossCompletionStatuses(bosses);
        var sharedBosses = GetSharedBossQueue(bosses);
        foreach (string boss in sharedBosses)
        {

            switch (boss)
            {
                case "UltraEzrajal":
                    PrepEzrajal();
                    FightEzrajal();
                    break;
                case "UltraWarden":
                    PrepWarden();
                    FightWarden();
                    break;
                case "UltraEngineer":
                    PrepEngineer();
                    FightEngineer();
                    break;
                case "UltraAvatarTyndarius":
                    C.Join("whitemap");
                    PrepAvatarTyndarius();
                    FightAvatarTyndarius();
                    break;
                default:
                    C.Logger($"Unknown boss in queue: {boss}", "Error", true, true);
                    break;
            }
        }
    }

    private IEnumerable<string> GetSharedBossQueue(IEnumerable<string> bosses)
    {
        if (Bot == null)
            return bosses;

        int armySize = Math.Max(1, Bot.Config!.Get<int>("ArmySize"));
        string syncPath = Ultra.ResolveSyncPath(BossSyncFile);
        string user = Bot.Player?.Username ?? Guid.NewGuid().ToString();

        string payload = string.Join(",", bosses.Select(b => $"{b}={(IsBossComplete(b) ? "true" : "false")}"));
        Ultra.UpdateEntry(syncPath, user, payload);
        string participantPath = Ultra.ResolveSyncPath(BossParticipantSyncFile);
        Ultra.UpdateEntry(participantPath, user, "1");

        int lastCount = -1;
        const int staleThreshold = 600;
        DateTime waitStarted = DateTime.UtcNow;
        const int timeoutMs = 60000;

        while (!Bot.ShouldExit)
        {
            int count = 0;
            foreach (string line in Ultra.ReadLines(participantPath))
            {
                string[] parts = line.Split(':');
                if (parts.Length < 3)
                    continue;
                if (!long.TryParse(parts[parts.Length - 1], out long ts))
                    continue;
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts > staleThreshold)
                    continue;
                count++;
            }

            if (count != lastCount)
            {
                lastCount = count;
                C.Logger($"[BossSync] Registered participants: {count}/{armySize}", "Info");
            }

            if (count >= armySize)
                break;

            if ((DateTime.UtcNow - waitStarted).TotalMilliseconds >= timeoutMs && count > 0)
            {
                C.Logger($"[BossSync] Wait timeout reached — proceeding with {count}/{armySize} registered clients.", "Warning");
                break;
            }

            Ultra.UpdateEntry(syncPath, user, payload);
            Ultra.UpdateEntry(participantPath, user, "1");
            Bot.Sleep(500);
        }

        if (Bot.ShouldExit)
            return bosses;

        Dictionary<string, bool> allBossComplete = bosses
            .ToDictionary(b => b, b => true, StringComparer.OrdinalIgnoreCase);

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (string line in Ultra.ReadLines(syncPath))
        {
            string[] parts = line.Split(':');
            if (parts.Length < 3)
                continue;
            if (!long.TryParse(parts[parts.Length - 1], out long ts))
                continue;
            if (now - ts > staleThreshold)
                continue;

            string payloadLine = parts[1];
            var statuses = payloadLine.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split(new[] { '=' }, 2))
                .Where(pair => pair.Length == 2)
                .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim(), StringComparer.OrdinalIgnoreCase);

            foreach (string boss in bosses)
            {
                if (!statuses.TryGetValue(boss, out string? rawValue)
                    || !bool.TryParse(rawValue, out bool complete)
                    || !complete)
                {
                    allBossComplete[boss] = false;
                }
            }
        }

        return bosses.Where(b => !allBossComplete[b]).ToList();
    }

    private bool IsBossComplete(string boss)
    {
        (int id, string name) = boss switch
        {
            "UltraEzrajal" => (8152, "Ultra Ezrajal"),
            "UltraWarden" => (8153, "Ultra Warden"),
            "UltraEngineer" => (8154, "Ultra Engineer"),
            "UltraAvatarTyndarius" => (8245, "Ultra Avatar Tyndarius"),
            _ => (0, string.Empty),
        };

        if (id <= 0)
            return false;

        bool complete = UltraGeneral.IsQuestComplete(Bot, id);
        LogBossQuestStatus(name, id, complete);
        return complete;
    }

    private void LogBossQuestStatus(string name, int questId, bool complete)
    {
        Quest? quest = Bot.Quests.EnsureLoad(questId);
        int slot = quest?.Slot ?? 0;
        int value = quest?.Value ?? 0;
        bool active = quest?.Active ?? false;
        int questValue = slot > 0 ? Bot.Flash.CallGameFunction<int>("world.getQuestValue", slot) : 0;

        C.Logger(
            $"{name} [{questId}] complete={complete} " +
            $"daily={Bot.Quests.IsDailyComplete(questId)} " +
            $"progress={Bot.Quests.IsInProgress(questId)} " +
            $"active={active} " +
            $"everCompleted={Bot.Quests.HasBeenCompleted(questId)} " +
            $"slot={slot} value={value} questValue={questValue}",
            "Info"
        );
    }

    private string GetBossOption()
    {
        string boss = (Bot.Config!.Get<string>("Boss") ?? "").Trim();
        return string.IsNullOrEmpty(boss) ? "UltraDailies" : boss;
    }
    private string NormalizeString(string input) => (input ?? string.Empty).Trim().ToLowerInvariant();
    private bool HasAssignedClass(string assignedClass) =>
        NormalizeString(Bot.Player.CurrentClass?.Name) == NormalizeString(assignedClass);

    private void EquipPresetClasses(string syncFile, int armySize) =>
        UltraGeneral.EquipUltraDailyPresetClasses(Ultra, Bot, syncFile, armySize);

    private void PublishBossCompletionStatuses(IEnumerable<string> bosses)
    {
        if (Bot == null)
            return;

        string syncPath = Ultra.ResolveSyncPath(BossSyncFile);
        string user = Bot.Player?.Username ?? Guid.NewGuid().ToString();
        string payload = string.Join(",", bosses.Select(b => $"{b}={(IsBossComplete(b) ? "true" : "false")}"));

        Ultra.UpdateEntry(syncPath, user, payload);
    }

    private int GetBossParticipantCount(string boss) =>
        UltraGeneral.GetUltraDailyBossParticipantCount(
            Ultra,
            Bot,
            BossParticipantSyncFile,
            Math.Max(1, Bot.Config!.Get<int>("ArmySize"))
        );

    private void DoEnhs() => Enh.Apply();

    private void PrepCommon(bool skipThird = false, string syncFile = "", int armySize = 0)
    {
        if (!string.IsNullOrEmpty(syncFile))
            EquipPresetClasses(syncFile, armySize);

        if (Bot.Config!.Get<bool>("DoEnh") && !enhancementsApplied)
        {
            DoEnhs();
            enhancementsApplied = true;
        }

        int potionQuant = Bot.Config.Get<int>("PotionQuantity");
        Pots.EnsureRecommendedPotions(potionQuant, skipThird: skipThird);

        if (!string.IsNullOrEmpty(syncFile))
            EquipPresetClasses(syncFile, armySize);

        Pots.UseRecommendedPotions(potionQuant, skipThird: skipThird, ensureStock: false);
    }

    private void PrepEzrajal()
    {
        PrepCommon(syncFile: "ezrajal_class-v2.sync", armySize: GetBossParticipantCount("UltraEzrajal"));
    }

    private void FightEzrajal()
    {
        const string map = "ultraezrajal";
        const string boss = "Ultra Ezrajal";
        string syncPath = Ultra.ResolveSyncPath("ultra_ezrajal_done.sync");
        Ultra.ClearSyncFile(syncPath);
        Bot.Sleep(2500);

        if (!UltraGeneral.IsQuestComplete(Bot, 8152))
            UltraGeneral.EnsureAcceptOnce(Bot, 8152);
        C.AddDrop("Ezrajal Insignia");

        Core.Join(map);
        int armySize = GetBossParticipantCount("UltraEzrajal");
        Ultra.WaitForArmy(armySize - 1, "ultra_ezrajal.sync");
        Core.ChooseBestCell(boss);
        Bot.Player.SetSpawnPoint();
        Core.EnableSkills();

        while (!Bot.ShouldExit)
        {
            if (!Bot.Player.Alive)
            {
                Bot.Wait.ForTrue(() => Bot.Player.Alive, 20);
                continue;
            }

            if (Ultra.CheckArmyProgressBool(() => Bot.TempInv.Contains("Ultra Ezrajal Defeated", 1), syncPath))
            {
                C.Logger("All players finished farm.");
                UltraGeneral.EnsureCompleteOnce(Bot, 8152);
                Bot.UltraBossHelper.DisableCounterAttack();
                Ultra.JoinHouse();
                break;
            }

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
            Bot.Sleep(500);
        }
    }

    private void PrepWarden()
    {
        string a = (Bot.Config!.Get<string>("WardenTaunter1") ?? string.Empty).Trim();
        string b = (Bot.Config.Get<string>("WardenTaunter2") ?? string.Empty).Trim();

        PrepCommon(skipThird: IsWardenTaunter(a, b), syncFile: "warden_class-v2.sync", armySize: GetBossParticipantCount("UltraWarden"));

        if (IsWardenTaunter(a, b))
            Ultra.GetScrollOfEnrage();

        string cn = Bot.Player.CurrentClass?.Name ?? string.Empty;
        if (!string.IsNullOrEmpty(a) && cn.Equals(a, StringComparison.OrdinalIgnoreCase))
            wardenTauntOffsetSeconds = 0;
        else if (!string.IsNullOrEmpty(b) && cn.Equals(b, StringComparison.OrdinalIgnoreCase))
            wardenTauntOffsetSeconds = 6;
        else
            wardenTauntOffsetSeconds = 0;

        C.Logger($"Warden taunt offset: {wardenTauntOffsetSeconds}s (class: {cn})");
    }

    private bool IsWardenTaunter(string a, string b)
    {
        string cn = Bot.Player.CurrentClass?.Name ?? string.Empty;
        return (!string.IsNullOrEmpty(a) && cn.Equals(a, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrEmpty(b) && cn.Equals(b, StringComparison.OrdinalIgnoreCase));
    }

    private void FightWarden()
    {
        const string map = "ultrawarden";
        const string boss = "Ultra Warden";

        string a = (Bot.Config!.Get<string>("WardenTaunter1") ?? string.Empty).Trim();
        string b = (Bot.Config.Get<string>("WardenTaunter2") ?? string.Empty).Trim();
        bool isTaunter = IsWardenTaunter(a, b);

        string syncPath = Ultra.ResolveSyncPath("ultra_warden_done.sync");
        Ultra.ClearSyncFile(syncPath);
        Bot.Sleep(2500);

        if (!UltraGeneral.IsQuestComplete(Bot, 8153))
            UltraGeneral.EnsureAcceptOnce(Bot, 8153);
        C.AddDrop("Warden Insignia");
        Core.Join(map);
        int armySize = GetBossParticipantCount("UltraWarden");
        Ultra.WaitForArmy(armySize - 1, "ultra_warden.sync");
        Core.ChooseBestCell(boss);
        Bot.Player.SetSpawnPoint();
        Core.EnableSkills();

        wardenFightStartTime = DateTime.Now;
        C.Logger($"Fight start time synced: {wardenFightStartTime:HH:mm:ss.fff}");

        while (!Bot.ShouldExit)
        {
            if (!Bot.Player.Alive)
            {
                Bot.Wait.ForTrue(() => Bot.Player.Alive, 20);
                wardenLastTauntTime = DateTime.MinValue;
                continue;
            }

            if (Ultra.CheckArmyProgressBool(() => Bot.TempInv.Contains("Ultra Warden Defeated", 1), syncPath))
            {
                C.Jump("Enter", "Spawn");
                C.Logger("All players finished farm.");
                UltraGeneral.EnsureCompleteOnce(Bot, 8153);
                Ultra.JoinHouse();
                break;
            }

            Bot.Combat.Attack("*");

            if (isTaunter && Bot.Player.HasTarget)
            {
                TimeSpan elapsed = DateTime.Now - wardenFightStartTime;
                double currentTime = elapsed.TotalSeconds;
                double timeInCycle = (currentTime - wardenTauntOffsetSeconds) % WardenTauntInterval;
                bool inTauntWindow = timeInCycle >= 0 && timeInCycle < WardenTauntWindow;
                bool cooldownExpired = (DateTime.Now - wardenLastTauntTime).TotalSeconds >= WardenTauntInterval - 1;

                if (inTauntWindow && cooldownExpired)
                {
                    wardenLastTauntTime = DateTime.Now;
                    Bot.Combat.Attack(boss);
                    C.Logger($"Taunt window ({currentTime:F1}s into fight, offset {wardenTauntOffsetSeconds}s) — executing taunt!");

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

    private void PrepEngineer()
    {
        PrepCommon(syncFile: "engineer_class-v2.sync", armySize: GetBossParticipantCount("UltraEngineer"));
    }

    private void FightEngineer()
    {
        const string map = "ultraengineer";
        const string boss = "Ultra Engineer";
        const string priority1 = "Defense Drone";
        const string priority2 = "Attack Drone";

        string syncPath = Ultra.ResolveSyncPath("ultra_engineer_done.sync");
        Ultra.ClearSyncFile(syncPath);
        Bot.Sleep(2500);

        if (!UltraGeneral.IsQuestComplete(Bot, 8154))
            UltraGeneral.EnsureAcceptOnce(Bot, 8154);
        C.AddDrop("Engineer Insignia");
        Core.Join(map);
        int armySize = GetBossParticipantCount("UltraEngineer");
        Ultra.WaitForArmy(armySize - 1, "ultra_engineer.sync");
        Core.ChooseBestCell(boss);
        Bot.Player.SetSpawnPoint();
        Core.EnableSkills();

        while (!Bot.ShouldExit)
        {
            if (!Bot.Player.Alive)
            {
                Bot.Wait.ForTrue(() => Bot.Player.Alive, 20);
                continue;
            }

            if (Ultra.CheckArmyProgressBool(() => Bot.TempInv.Contains("Ultra Engineer Defeated", 1), syncPath))
            {
                C.Logger("All players finished farm.");
                UltraGeneral.EnsureCompleteOnce(Bot, 8154);
                Ultra.JoinHouse();
                break;
            }

            Ultra.KillWithPriority(boss, 3, priority1, 2, priority2, 1);
            Pots.ActivateEquippedPotion();
        }
    }

    private void PrepAvatarTyndarius()
    {
        string ball1 = (Bot.Config!.Get<string>("Ball1Taunter") ?? string.Empty).Trim();
        string ball2 = (Bot.Config.Get<string>("Ball2Killer") ?? string.Empty).Trim();
        string tyn1 = (Bot.Config.Get<string>("TynTaunter1") ?? string.Empty).Trim();
        string tyn2 = (Bot.Config.Get<string>("TynTaunter2") ?? string.Empty).Trim();

        isBall1Taunter = HasAssignedClass(ball1);
        isBall2Killer = HasAssignedClass(ball2);
        bool isTynTaunter = HasAssignedClass(tyn1) || HasAssignedClass(tyn2);

        PrepCommon(skipThird: isBall1Taunter || isTynTaunter, syncFile: "tyndarius_class-v2.sync", armySize: GetBossParticipantCount("UltraAvatarTyndarius"));

        string cn = Bot.Player.CurrentClass?.Name ?? string.Empty;
        if (cn.Equals(tyn1, StringComparison.OrdinalIgnoreCase))
            tynTauntOffset = 0;
        else if (cn.Equals(tyn2, StringComparison.OrdinalIgnoreCase))
            tynTauntOffset = 6;

        C.Logger($"Tyndarius — Ball1: {isBall1Taunter} | Ball2Kill: {isBall2Killer} | TynTaunt: {isTynTaunter} (offset {tynTauntOffset}s)");

        if (isBall1Taunter || isTynTaunter)
            Ultra.GetScrollOfEnrage();
    }

    private void FightAvatarTyndarius()
    {
        const string map = "ultratyndarius";
        const string boss = "Ultra Avatar Tyndarius";

        string tyn1 = (Bot.Config!.Get<string>("TynTaunter1") ?? string.Empty).Trim();
        string tyn2 = (Bot.Config.Get<string>("TynTaunter2") ?? string.Empty).Trim();
        string cn = Bot.Player.CurrentClass?.Name ?? string.Empty;
        bool isTynTaunter = cn.Equals(tyn1, StringComparison.OrdinalIgnoreCase) || cn.Equals(tyn2, StringComparison.OrdinalIgnoreCase);

        string syncPath = Ultra.ResolveSyncPath("ultra_tyndarius_done.sync");
        Ultra.ClearSyncFile(syncPath);
        Bot.Sleep(2500);

        C.AddDrop("Avatar Tyndarius Insignia");
        if (!UltraGeneral.IsQuestComplete(Bot, 8245))
            UltraGeneral.EnsureAcceptOnce(Bot, 8245);
        Core.Join(map);
        int armySize = GetBossParticipantCount("UltraAvatarTyndarius");
        Ultra.WaitForArmy(armySize - 1, "ultra_tyndarius.sync");
        Core.ChooseBestCell(boss);
        Core.EnableSkills();

        while (!Bot.ShouldExit)
        {
            if (!Bot.Player.Alive)
            {
                Bot.Wait.ForTrue(() => Bot.Player.Alive, 20);
                continue;
            }

            if (Ultra.CheckArmyProgressBool(() => Bot.TempInv.Contains("Ultra Avatar Tyndarius Defeated", 1), syncPath))
            {
                C.Jump("Enter", "Spawn");
                C.Logger("All players finished farm.");
                UltraGeneral.EnsureCompleteOnce(Bot, 8245);
                Core.DisableSkills();
                Ultra.JoinHouse();
                break;
            }

            if (Bot.Map.Name != map)
                Core.Join(map);

            if (Bot.Player.Cell != "Boss")
            {
                Bot.Map.Jump("Boss", "Left", autoCorrect: false);
                Bot.Wait.ForCellChange("Boss");
            }

            bool ball1Alive = Bot.Monsters.CurrentAvailableMonsters.Any(x => x != null && x.Alive && x.MapID == 1);
            bool ball2Alive = Bot.Monsters.CurrentAvailableMonsters.Any(x => x != null && x.Alive && x.MapID == 3);
            bool bothDead = !ball1Alive && !ball2Alive;

            if (isBall1Taunter)
            {
                if (bothDead)
                {
                    // fall through to Tyndarius phase
                }
                else if (ball1Alive)
                {
                    Bot.Combat.Attack(1);
                    for (int i = 0; i < 40 && !Bot.ShouldExit; i++)
                    {
                        if (!Bot.Player.Alive) break;
                        if (!Bot.Player.HasTarget) Bot.Combat.Attack(1);
                        Bot.Skills.UseSkill(5);
                        Bot.Sleep(25);
                    }
                }
                else if (ball2Alive)
                {
                    Bot.Combat.Attack(3);
                    Bot.Sleep(500);
                }
            }
            else if (isBall2Killer)
            {
                if (ball2Alive)
                {
                    Bot.Combat.Attack(3);
                    for (int i = 0; i < 40 && !Bot.ShouldExit; i++)
                    {
                        if (!Bot.Player.Alive) break;
                        if (!Bot.Player.HasTarget) Bot.Combat.Attack(3);
                        Bot.Skills.UseSkill(5);
                        Bot.Sleep(25);
                    }
                }
                else if (ball1Alive)
                {
                    Bot.Combat.Attack(1);
                    Bot.Sleep(500);
                }
            }

            if (bothDead || (!isBall1Taunter && !isBall2Killer))
            {
                if (tynFightStartTime == DateTime.MinValue)
                {
                    tynFightStartTime = DateTime.Now;
                    C.Logger("Balls down — Tyndarius timer started.");
                }

                Bot.Combat.Attack(2);

                if (isTynTaunter && Bot.Player.HasTarget)
                {
                    TimeSpan elapsed = DateTime.Now - tynFightStartTime;
                    double currentTime = elapsed.TotalSeconds;
                    double timeInCycle = (currentTime - tynTauntOffset) % TynTauntInterval;
                    bool inTauntWindow = timeInCycle >= 0 && timeInCycle < TynTauntWindow;
                    bool cooldownExpired = (DateTime.Now - tynLastTaunt).TotalSeconds >= TynTauntInterval - 1;

                    if (inTauntWindow && cooldownExpired)
                    {
                        tynLastTaunt = DateTime.Now;
                        C.Logger($"Tyndarius taunt ({currentTime:F1}s, offset {tynTauntOffset}s)");

                        for (int i = 0; i < 40 && !Bot.ShouldExit; i++)
                        {
                            if (!Bot.Player.Alive) break;
                            if (!Bot.Player.HasTarget) Bot.Combat.Attack(2);
                            Bot.Skills.UseSkill(5);
                            Bot.Sleep(25);
                        }

                        C.Logger("Tyndarius taunt done.");
                        Bot.Sleep(300);
                    }
                    else if (ball2Alive)
                    {
                        Bot.Combat.Attack(3);
                        Bot.Sleep(500);
                    }
                    else if (ball1Alive)
                    {
                        Bot.Combat.Attack(1);
                        Bot.Sleep(500);
                    }
                    else
                    {
                        Bot.Sleep(500);
                    }
                }
                else
                {
                    Bot.Sleep(500);
                }
            }

            Pots.ActivateEquippedPotion();
        }
    }
}


