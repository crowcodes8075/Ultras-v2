//cs_include Scripts/CoreBots.cs
//cs_include Scripts/CoreAdvanced.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Skua.Core.Interfaces;

public class UltraGeneral
{
    public static void EquipPresetClasses(dynamic ultra, IScriptInterface bot, string syncFilePath)
    {
        EquipUltraDailyPresetClasses(ultra, bot, syncFilePath);
    }

    public static void EquipUltraDailyPresetClasses(dynamic ultra, IScriptInterface bot, string syncFilePath, int armySize = 0)
    {
        if (ultra == null || bot == null || string.IsNullOrWhiteSpace(syncFilePath))
            return;

        armySize = armySize > 0
            ? armySize
            : Math.Max(1, bot.Config!.Get<int>("ArmySize"));

        var presetEntries = new[]
        {
            bot.Config!.Get<string>("Class1"),
            bot.Config.Get<string>("Class2"),
            bot.Config.Get<string>("Class3"),
            bot.Config.Get<string>("Class4"),
            bot.Config.Get<string>("Class5"),
            bot.Config.Get<string>("Class6"),
            bot.Config.Get<string>("Class7"),
        }
        .Select(cl => cl?.Trim())
        .Where(cl => !string.IsNullOrEmpty(cl))
        .Select(ParseClassOption)
        .Where(entry => !string.IsNullOrEmpty(entry.ClassName))
        .ToArray();

        if (presetEntries.Length == 0)
            return;

        var presetClasses = presetEntries.Select(entry => entry.ClassName).ToArray();
        var preferredAssignments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in presetEntries)
        {
            if (!string.IsNullOrEmpty(entry.Username) && !preferredAssignments.ContainsKey(entry.Username))
                preferredAssignments[entry.Username] = entry.ClassName;
        }

        bool allowDuplicates = presetClasses.Length < armySize || presetClasses.Distinct(StringComparer.OrdinalIgnoreCase).Count() < presetClasses.Length;
        string[][] classSlots = Enumerable.Range(0, armySize)
            .Select(_ => presetClasses)
            .ToArray();

        ultra.EquipClassSync(
            classSlots,
            armySize,
            syncFilePath,
            allowDuplicates,
            preferredAssignments.Count > 0 ? preferredAssignments : null
        );
    }


    public static void PublishUltraDailyBossNeeds(dynamic ultra, IScriptInterface bot, string syncFilePath, IEnumerable<string> neededBosses)
    {
        if (ultra == null || bot == null || string.IsNullOrWhiteSpace(syncFilePath))
            return;

        string path = ultra.ResolveSyncPath(syncFilePath);
        string username = bot.Player?.Username ?? Guid.NewGuid().ToString();
        string payload = string.Join(",", neededBosses
            .Select(b => b?.Trim())
            .Where(b => !string.IsNullOrEmpty(b)));

        ultra.UpdateEntry(path, username, payload);
    }

    public static void PublishUltraDailyBossStatuses(dynamic ultra, IScriptInterface bot, string syncFilePath, IEnumerable<KeyValuePair<string, bool>> bossStatuses)
    {
        if (ultra == null || bot == null || string.IsNullOrWhiteSpace(syncFilePath))
            return;

        string path = ultra.ResolveSyncPath(syncFilePath);
        string username = bot.Player?.Username ?? Guid.NewGuid().ToString();
        string payload = string.Join(",", bossStatuses.Select(kvp => $"{kvp.Key}={(kvp.Value ? "true" : "false")}"));

        ultra.UpdateEntry(path, username, payload);
    }

    public static int GetUltraDailyBossParticipantCount(dynamic ultra, IScriptInterface bot, string syncFilePath, int defaultCount)
    {
        if (ultra == null || bot == null || string.IsNullOrWhiteSpace(syncFilePath))
            return Math.Max(1, defaultCount);

        string path = ultra.ResolveSyncPath(syncFilePath);
        string[] lines = ultra.ReadLines(path);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const int staleThreshold = 600;

        int count = 0;
        foreach (string line in lines)
        {
            string[] parts = line.Split(':');
            if (parts.Length < 3)
                continue;

            if (!long.TryParse(parts[parts.Length - 1], out long ts))
                continue;

            if (now - ts > staleThreshold)
                continue;

            count++;
        }

        return count > 0
            ? Math.Max(1, count)
            : Math.Max(1, defaultCount);
    }

    public static bool EnsureCompleteDailyQuestSafe(IScriptInterface bot, int questId)
    {
        if (bot == null || questId <= 0)
            return false;

        Quest? quest = bot.Quests.EnsureLoad(questId);
        if (quest == null)
            return false;

        if (bot.Quests.IsDailyComplete(questId))
            return true;

        if (quest.Once && bot.Quests.HasBeenCompleted(questId))
            return true;

        if (!bot.Quests.IsInProgress(questId) && !bot.Quests.CanComplete(questId))
        {
            if (!CoreBots.Instance.EnsureAccept(questId))
                return false;
        }

        return CoreBots.Instance.EnsureComplete(questId);
    }

    public static bool EnsureAcceptOnce(IScriptInterface bot, int questId)
    {
        if (bot == null || questId <= 0)
            return false;

        Quest? quest = bot.Quests.EnsureLoad(questId);
        if (quest == null)
            return false;

        if (bot.Quests.IsDailyComplete(questId))
            return true;

        if (quest.Once && bot.Quests.HasBeenCompleted(questId))
            return true;

        if (bot.Quests.IsInProgress(questId))
            return true;

        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (CoreBots.Instance.EnsureAccept(questId))
                return true;

            if (attempt < maxAttempts)
                bot.Sleep(1000);
        }

        return false;
    }

    public static bool EnsureCompleteOnce(IScriptInterface bot, int questId, int itemId = -1)
    {
        if (bot == null || questId <= 0)
            return false;

        Quest? quest = bot.Quests.EnsureLoad(questId);
        if (quest == null)
            return false;

        if (bot.Quests.IsDailyComplete(questId))
            return true;

        if (quest.Once && bot.Quests.HasBeenCompleted(questId))
            return true;

        if (!bot.Quests.IsInProgress(questId) && !bot.Quests.CanComplete(questId))
        {
            if (!EnsureAcceptOnce(bot, questId))
                return false;
        }

        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (CoreBots.Instance.EnsureComplete(questId, itemId))
                return true;

            if (attempt < maxAttempts)
                bot.Sleep(1000);
        }

        return false;
    }

    public static bool IsWholeArmyDead(dynamic ultra, IScriptInterface bot, string syncFilePath)
    {
        return UpdateAndCheckWholeArmyState(ultra, bot, syncFilePath, !bot.Player.Alive, "1");
    }

    public static bool IsWholeArmyAlive(dynamic ultra, IScriptInterface bot, string syncFilePath)
    {
        return UpdateAndCheckWholeArmyState(ultra, bot, syncFilePath, bot.Player.Alive, "1");
    }

    private static bool UpdateAndCheckWholeArmyState(dynamic ultra, IScriptInterface bot, string syncFilePath, bool localState, string expectedValue)
    {
        if (ultra == null || bot == null || string.IsNullOrWhiteSpace(syncFilePath))
            return false;

        string? username = bot.Player?.Username;
        if (string.IsNullOrWhiteSpace(username))
            return false;

        string key = username.Replace(":", "-");
        string syncFile = ultra.ResolveSyncPath(syncFilePath);
        ultra.UpdateEntry(syncFile, key, localState ? "1" : "0");

        string[] lines = ultra.ReadLines(syncFile);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const int staleThreshold = 600;
        int activeMembers = 0;
        int matchingMembers = 0;

        foreach (string line in lines)
        {
            string[] parts = line.Split(':');
            if (parts.Length < 3)
                continue;
            if (!long.TryParse(parts[2], out long ts))
                continue;
            if (now - ts > staleThreshold)
                continue;

            activeMembers++;
            if (parts[1] == expectedValue)
                matchingMembers++;
        }

        return activeMembers > 0 && activeMembers == matchingMembers;
    }

    private static (string ClassName, string? Username) ParseClassOption(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (string.Empty, null);

        var parts = raw.Split(new[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrEmpty(part))
            .ToArray();

        if (parts.Length == 0)
            return (string.Empty, null);

        return parts.Length == 1
            ? (parts[0], null)
            : (parts[0], parts[1]);
    }

    public static bool IsQuestComplete(IScriptInterface bot, int questId)
    {
        if (bot == null || questId <= 0)
            return false;

        if (bot.Quests.IsDailyComplete(questId))
            return true;

        Quest? quest = bot.Quests.EnsureLoad(questId);
        if (quest == null)
            return false;

        if (quest.Active)
        {
            if (bot.Quests.CanComplete(questId))
                return true;

            if (quest.Slot > 0 && quest.Value > 0)
            {
                int currentValue = bot.Flash.CallGameFunction<int>("world.getQuestValue", quest.Slot);
                return currentValue >= quest.Value;
            }

            return false;
        }

        return quest.Once && bot.Quests.HasBeenCompleted(questId);
    }
}

