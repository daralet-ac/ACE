using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ACE.Database;
using ACE.Database.Models.Shard;
using Serilog;

namespace ACE.Server.Managers;

/// <summary>
/// Account quests: quests stamped on an account rather than a character, so every character on the account shares one
/// entry, including characters made later. A quest is an account quest when its name starts with ACCOUNT_; content
/// stamps and checks it like any other quest and QuestManager sends it here.
///
/// They are kept in the shard database (account_quest_registry). An account's quests are read when one of its
/// characters enters the world, shared by its characters while any is online, and every change is written straight
/// through the shard database queue.
/// </summary>
public static class AccountQuestManager
{
    private static readonly ILogger _log = Log.ForContext(typeof(AccountQuestManager));

    public const string Prefix = "ACCOUNT_";

    public static bool IsAccountQuest(string questName)
    {
        return questName != null && questName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly ConcurrentDictionary<uint, AccountQuests> Loaded = new();

    /// <summary>
    /// Reads an account's quests from the database, unless they are already loaded. Call this from the shard database
    /// queue (e.g. a GetPossessedBiotasInParallel callback), so it reads back any saves still queued ahead of it.
    /// </summary>
    public static void Load(uint accountId)
    {
        if (Loaded.ContainsKey(accountId))
        {
            return;
        }

        var quests = Read(accountId);

        if (quests != null)
        {
            Loaded.TryAdd(accountId, quests);
        }
    }

    /// <summary>
    /// An account's quests: the loaded ones, or read now if they aren't loaded yet.
    /// If they can't be read, this returns an empty set that isn't saved, so the account's real quests are never overwritten.
    /// </summary>
    public static AccountQuests Get(uint accountId)
    {
        if (Loaded.TryGetValue(accountId, out var quests))
        {
            return quests;
        }

        quests = Read(accountId);

        if (quests == null)
        {
            return new AccountQuests(accountId, [], persist: false);
        }

        return Loaded.GetOrAdd(accountId, quests);
    }

    /// <summary>
    /// Forgets an account's quests once none of its characters are online; they are read again at the next login.
    /// </summary>
    public static void Unload(uint accountId)
    {
        Loaded.TryRemove(accountId, out _);
    }

    private static AccountQuests Read(uint accountId)
    {
        try
        {
            return new AccountQuests(
                accountId,
                DatabaseManager.Shard.BaseDatabase.GetAccountQuests(accountId),
                persist: true
            );
        }
        catch (Exception ex)
        {
            _log.Error(
                ex,
                "[QUESTS] Couldn't read account quests for account {AccountId}; its ACCOUNT_ quests are missing and won't be saved until it logs in again. Has the 2026-10-05-00-Account-Quest-Registry.sql update run?",
                accountId
            );
            return null;
        }
    }

    internal static void ReportSaveFailure(uint accountId, string questName, Exception ex)
    {
        _log.Error(
            ex,
            "[QUESTS] Couldn't save account quest {QuestName} for account {AccountId}.",
            questName,
            accountId
        );
    }
}

/// <summary>
/// The quests stamped on one account. QuestManager changes an entry, then calls Save with it.
/// </summary>
public class AccountQuests
{
    public uint AccountId { get; }

    private readonly bool _persist;

    private readonly Dictionary<string, AccountQuestRegistry> _quests = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    /// <summary>
    /// persist: false for a set that is never written to the database (e.g. the account's quests couldn't be read).
    /// </summary>
    public AccountQuests(uint accountId, IEnumerable<AccountQuestRegistry> quests, bool persist)
    {
        AccountId = accountId;
        _persist = persist;

        foreach (var quest in quests)
        {
            _quests[quest.QuestName] = quest;
        }
    }

    public List<AccountQuestRegistry> GetQuests()
    {
        lock (_lock)
        {
            return _quests.Values.ToList();
        }
    }

    public AccountQuestRegistry GetQuest(string questName)
    {
        lock (_lock)
        {
            return _quests.GetValueOrDefault(questName);
        }
    }

    /// <summary>
    /// A new entry is not saved until Save is called with it.
    /// </summary>
    public AccountQuestRegistry GetOrCreateQuest(string questName, out bool questRegistryWasCreated)
    {
        lock (_lock)
        {
            if (_quests.TryGetValue(questName, out var quest))
            {
                questRegistryWasCreated = false;
                return quest;
            }

            quest = new AccountQuestRegistry { AccountId = AccountId, QuestName = questName };
            _quests[questName] = quest;

            questRegistryWasCreated = true;
            return quest;
        }
    }

    /// <summary>
    /// Writes an entry's current completion time and count to the database.
    /// </summary>
    public void Save(AccountQuestRegistry quest)
    {
        if (!_persist)
        {
            return;
        }

        // the values as they are now; the queue may run after they change again
        var questName = quest.QuestName;

        DatabaseManager.Shard.SaveAccountQuest(
            AccountId,
            questName,
            quest.LastTimeCompleted,
            quest.NumTimesCompleted,
            ex => AccountQuestManager.ReportSaveFailure(AccountId, questName, ex)
        );
    }

    /// <summary>
    /// Returns false if the account didn't have the quest.
    /// </summary>
    public bool EraseQuest(string questName)
    {
        AccountQuestRegistry quest;

        lock (_lock)
        {
            if (!_quests.Remove(questName, out quest))
            {
                return false;
            }
        }

        if (_persist)
        {
            DatabaseManager.Shard.RemoveAccountQuest(
                AccountId,
                quest.QuestName,
                ex => AccountQuestManager.ReportSaveFailure(AccountId, quest.QuestName, ex)
            );
        }

        return true;
    }
}
