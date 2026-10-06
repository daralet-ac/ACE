using ACE.Database.Models.Shard;
using ACE.Server.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class AccountQuestTests
{
    [TestMethod]
    public void IsAccountQuest_MatchesTheAccountPrefixInAnyCase()
    {
        Assert.IsTrue(AccountQuestManager.IsAccountQuest("ACCOUNT_BankIntroSeen"));
        Assert.IsTrue(AccountQuestManager.IsAccountQuest("account_BetaLevel10"));
        Assert.IsFalse(AccountQuestManager.IsAccountQuest("BankAutoSort"));
        Assert.IsFalse(AccountQuestManager.IsAccountQuest("MyACCOUNT_Quest"));
        Assert.IsFalse(AccountQuestManager.IsAccountQuest(null));
    }

    [TestMethod]
    public void GetOrCreateQuest_FindsExistingQuestsInAnyCase()
    {
        var quests = new AccountQuests(
            7,
            [
                new AccountQuestRegistry
                {
                    AccountId = 7,
                    QuestName = "ACCOUNT_BetaLevel10",
                    NumTimesCompleted = 2
                }
            ],
            persist: false
        );

        var quest = quests.GetOrCreateQuest("account_betalevel10", out var created);

        Assert.IsFalse(created);
        Assert.AreEqual(2, quest.NumTimesCompleted);
        Assert.AreSame(quest, quests.GetQuest("ACCOUNT_BETALEVEL10"));
    }

    [TestMethod]
    public void GetOrCreateQuest_AddsNewQuestsForTheAccount()
    {
        var quests = new AccountQuests(7, [], persist: false);

        var quest = quests.GetOrCreateQuest("ACCOUNT_BankIntroSeen", out var created);

        Assert.IsTrue(created);
        Assert.AreEqual(7u, quest.AccountId);
        Assert.AreEqual("ACCOUNT_BankIntroSeen", quest.QuestName);
        Assert.AreEqual(1, quests.GetQuests().Count);

        // an unsaved set never reaches the database
        quests.Save(quest);
    }

    [TestMethod]
    public void EraseQuest_RemovesOnlyQuestsTheAccountHas()
    {
        var quests = new AccountQuests(
            7,
            [
                new AccountQuestRegistry
                {
                    AccountId = 7,
                    QuestName = "ACCOUNT_BankCommandsUsed",
                    NumTimesCompleted = 1
                }
            ],
            persist: false
        );

        Assert.IsFalse(quests.EraseQuest("ACCOUNT_BankIntroSeen"));
        Assert.IsTrue(quests.EraseQuest("account_bankcommandsused"));
        Assert.IsNull(quests.GetQuest("ACCOUNT_BankCommandsUsed"));
        Assert.AreEqual(0, quests.GetQuests().Count);
    }
}
