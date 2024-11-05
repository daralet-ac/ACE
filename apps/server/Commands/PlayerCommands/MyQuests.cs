using System;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.PlayerCommands;

public class MyQuests
{
    // quest info (uses GDLe formatting to match plugin expectations)
    [CommandHandler("myquests", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Shows your quest log")]
    public static void HandleQuests(Session session, params string[] parameters)
    {
        if (!PropertyManager.GetBool("quest_info_enabled").Item)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "The command \"myquests\" is not currently enabled on this server.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        var quests = session.Player.QuestManager.GetQuests();

        if (quests.Count == 0)
        {
            session.Network.EnqueueSend(new GameMessageSystemChat("Quest list is empty.", ChatMessageType.Broadcast));
            return;
        }

        foreach (var playerQuest in quests)
        {
            var text = "";
            var questName = QuestManager.GetQuestName(playerQuest.QuestName);
            var quest = DatabaseManager.World.GetCachedQuest(questName);
            if (quest == null)
            {
                Console.WriteLine($"Couldn't find quest {playerQuest.QuestName}");
                continue;
            }

            var minDelta = quest.MinDelta;
            if (QuestManager.CanScaleQuestMinDelta(quest))
            {
                minDelta = (uint)(quest.MinDelta * PropertyManager.GetDouble("quest_mindelta_rate").Item);
            }

            text +=
                $"{playerQuest.QuestName.ToLower()} - {playerQuest.NumTimesCompleted} solves ({playerQuest.LastTimeCompleted})";
            text += $"\"{quest.Message}\" {quest.MaxSolves} {minDelta}";

            session.Network.EnqueueSend(new GameMessageSystemChat(text, ChatMessageType.Broadcast));
        }
    }
}
