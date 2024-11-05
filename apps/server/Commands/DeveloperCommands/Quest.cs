using System;
using System.Globalization;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class Quest
{
    // qst
    [CommandHandler(
        "qst",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Query, stamp, and erase quests on the targeted player",
        "(fellow) [list | bestow | erase]\n"
            + "qst list [filter] - List the quest flags for the targeted player\n"
            + "qst bestow - Stamps the specific quest flag on the targeted player. If this fails, it's probably because you spelled the quest flag wrong.\n"
            + "qst stamp - Stamps the specific quest flag on the targeted player the specified number of times. If this fails, it's probably because you spelled the quest flag wrong.\n"
            + "qst erase - Erase the specific quest flag from the targeted player. If no quest flag is given, it erases the entire quest table for the targeted player.\n"
    )]
    public static void Handleqst(Session session, params string[] parameters)
    {
        // fellow bestow  stamp erase
        // @qst list [filter]-List the quest flags for the targeted player, if a filter is provided, you will only get quest flags back that have the filter as a substring of the quest name.
        // @qst erase <quest flag> -Erase the specific quest flag from the targeted player.If no quest flag is given, it erases the entire quest table for the targeted player.
        // @qst erase fellow <quest flag> -Erase a fellowship quest flag.
        // @qst bestow <quest flag> -Stamps the specific quest flag on the targeted player.If this fails, it's probably because you spelled the quest flag wrong.
        // @qst - Query, stamp, and erase quests on the targeted player.
        if (parameters.Length == 0)
        {
            // todo: display help screen
            return;
        }

        var objectId = new ObjectGuid();

        if (session.Player.HealthQueryTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);
        }
        else if (session.Player.ManaQueryTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.ManaQueryTarget);
        }
        else if (session.Player.CurrentAppraisalTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);
        }

        var wo = session.Player.CurrentLandblock?.GetObject(objectId);

        if (wo != null && wo is Creature creature)
        {
            if (parameters[0].Equals("list"))
            {
                var filter = "";
                if (parameters.Length > 1)
                {
                    filter = parameters[1].ToLower();
                }

                var questsHdr = $"Quest Registry for {creature.Name} (0x{creature.Guid}):\n";
                questsHdr += "================================================\n";
                session.Player.SendMessage(questsHdr);

                var quests = creature.QuestManager.GetQuests();

                if (quests.Count == 0)
                {
                    session.Player.SendMessage("No quests found.");
                    return;
                }

                foreach (var quest in quests)
                {
                    if (filter != "" && !quest.QuestName.ToLower().Contains(filter))
                    {
                        continue;
                    }

                    var questEntry = "";
                    questEntry +=
                        $"Quest Name: {quest.QuestName}\nCompletions: {quest.NumTimesCompleted} | Last Completion: {quest.LastTimeCompleted} ({Time.GetDateTimeFromTimestamp(quest.LastTimeCompleted).ToLocalTime()})\n";
                    var nextSolve = creature.QuestManager.GetNextSolveTime(quest.QuestName);

                    if (nextSolve == TimeSpan.MinValue)
                    {
                        questEntry += "Can Solve: Immediately\n";
                    }
                    else if (nextSolve == TimeSpan.MaxValue)
                    {
                        questEntry += "Can Solve: Never again\n";
                    }
                    else
                    {
                        questEntry +=
                            $"Can Solve: In {nextSolve:%d} days, {nextSolve:%h} hours, {nextSolve:%m} minutes and, {nextSolve:%s} seconds. ({(DateTime.UtcNow + nextSolve).ToLocalTime()})\n";
                    }

                    questEntry += "--====--\n";
                    session.Player.SendMessage(questEntry);
                }
                return;
            }

            if (parameters[0].Equals("bestow"))
            {
                if (parameters.Length < 2)
                {
                    // delete all quests?
                    // seems unsafe, maybe a confirmation?
                    return;
                }
                var questName = parameters[1];
                if (creature.QuestManager.HasQuest(questName))
                {
                    session.Player.SendMessage($"{creature.Name} already has {questName}");
                    return;
                }

                var canSolve = creature.QuestManager.CanSolve(questName);
                if (canSolve)
                {
                    creature.QuestManager.Update(questName);
                    session.Player.SendMessage($"{questName} bestowed on {creature.Name}");
                    return;
                }
                else
                {
                    session.Player.SendMessage($"Couldn't bestow {questName} on {creature.Name}");
                    return;
                }
            }

            if (parameters[0].Equals("erase"))
            {
                if (parameters.Length < 2)
                {
                    // delete all quests?
                    // seems unsafe, maybe a confirmation?
                    session.Player.SendMessage(
                        $"You must specify a quest to erase, if you want to erase all quests use the following command: /qst erase *"
                    );
                    return;
                }
                var questName = parameters[1];

                if (questName == "*")
                {
                    creature.QuestManager.EraseAll();
                    session.Player.SendMessage($"All quests erased.");
                    return;
                }

                if (!creature.QuestManager.HasQuest(questName))
                {
                    session.Player.SendMessage($"{questName} not found.");
                    return;
                }
                creature.QuestManager.Erase(questName);
                session.Player.SendMessage($"{questName} erased.");
                return;
            }

            if (parameters[0].Equals("stamp"))
            {
                var numCompletions = int.MinValue;

                if (parameters.Length > 2 && !int.TryParse(parameters[2], out numCompletions))
                {
                    session.Player.SendMessage($"{parameters[2]} is not a valid int");
                    return;
                }
                var questName = parameters[1];

                if (numCompletions != int.MinValue)
                {
                    creature.QuestManager.SetQuestCompletions(questName, numCompletions);
                }
                else
                {
                    creature.QuestManager.Update(questName);
                }

                var quest = creature.QuestManager.GetQuest(questName);
                if (quest != null)
                {
                    var numTimesCompleted = quest.NumTimesCompleted;
                    session.Player.SendMessage($"{questName} stamped with {numTimesCompleted} completions.");
                }
                else
                {
                    session.Player.SendMessage($"Couldn't stamp {questName} on {creature.Name}");
                }
                return;
            }

            if (parameters[0].Equals("bits"))
            {
                if (parameters.Length < 2)
                {
                    var msg = "@qst - Query, stamp, and erase quests on the targeted player\n";
                    msg += "Usage: @qst bits [on | off | show] <questname> <bits>\n";
                    msg +=
                        "qst bits on  - Stamps the specific quest flag on the targeted player with specified bits ON. If this fails, it's probably because you spelled the quest flag wrong.\n";
                    msg +=
                        "qst bits off - Stamps the specific quest flag on the targeted player with specified bits OFF. If this fails, it's probably because you spelled the quest flag wrong.\n";
                    msg += "qst bits show - List the specific quest flag bits for the targeted player.\n";
                    session.Player.SendMessage(msg);
                    return;
                }

                if (parameters[1].Equals("on"))
                {
                    if (parameters.Length < 3)
                    {
                        session.Player.SendMessage($"You must specify bits to turn on or off.");
                        return;
                    }
                    if (parameters.Length < 2)
                    {
                        session.Player.SendMessage($"You must specify a quest to set its bits.");
                        return;
                    }

                    var questName = parameters[2];

                    var questBits = parameters[3];

                    if (
                        !uint.TryParse(
                            questBits.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? questBits[2..] : questBits,
                            NumberStyles.HexNumber,
                            CultureInfo.CurrentCulture,
                            out var bits
                        )
                    )
                    {
                        session.Player.SendMessage($"{parameters[3]} is not a valid hex number");
                        return;
                    }

                    if (creature.QuestManager.HasQuestBits(questName, (int)bits))
                    {
                        session.Player.SendMessage(
                            $"{creature.Name} already has set 0x{bits:X} bits to ON for {questName}"
                        );
                        return;
                    }

                    creature.QuestManager.SetQuestBits(questName, (int)bits);
                    session.Player.SendMessage($"{creature.Name} has set 0x{bits:X} bits to ON for {questName}");
                    return;
                }

                if (parameters[1].Equals("off"))
                {
                    if (parameters.Length < 3)
                    {
                        session.Player.SendMessage($"You must specify bits to turn on or off.");
                        return;
                    }
                    if (parameters.Length < 2)
                    {
                        session.Player.SendMessage($"You must specify a quest to set its bits.");
                        return;
                    }

                    var questName = parameters[2];

                    var questBits = parameters[3];

                    if (
                        !uint.TryParse(
                            questBits.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? questBits[2..] : questBits,
                            NumberStyles.HexNumber,
                            CultureInfo.CurrentCulture,
                            out var bits
                        )
                    )
                    {
                        session.Player.SendMessage($"{parameters[3]} is not a valid uint");
                        return;
                    }

                    if (creature.QuestManager.HasNoQuestBits(questName, (int)bits))
                    {
                        session.Player.SendMessage(
                            $"{creature.Name} already has set 0x{bits:X} bits to OFF for {questName}"
                        );
                        return;
                    }

                    creature.QuestManager.SetQuestBits(questName, (int)bits, false);
                    session.Player.SendMessage($"{creature.Name} has set 0x{bits:X} bits to OFF for {questName}");
                    return;
                }

                if (parameters[1].Equals("show"))
                {
                    if (parameters.Length < 2)
                    {
                        session.Player.SendMessage($"You must specify a quest to show its bits.");
                        return;
                    }

                    var questName = parameters[2];

                    var questsHdr = $"Quest Bits Registry for {creature.Name} (0x{creature.Guid}):\n";
                    questsHdr += "================================================\n";

                    var quest = creature.QuestManager.GetQuest(questName);

                    if (quest == null)
                    {
                        session.Player.SendMessage($"{questName} not found.");
                        return;
                    }

                    var maxSolves = creature.QuestManager.GetMaxSolves(questName);
                    var maxSolvesBinary = Convert.ToString(maxSolves, 2);

                    var questEntry = "";
                    questEntry += $"Quest Name: {quest.QuestName}\n";
                    questEntry += $"Current Set Bits: 0x{quest.NumTimesCompleted:X}\n";
                    questEntry += $"Allowed Max Bits: 0x{maxSolves:X}\n";
                    questEntry +=
                        $"Last Set On: {quest.LastTimeCompleted} ({Time.GetDateTimeFromTimestamp(quest.LastTimeCompleted).ToLocalTime()})\n";

                    //var nextSolve = creature.QuestManager.GetNextSolveTime(quest.QuestName);

                    //if (nextSolve == TimeSpan.MinValue)
                    //    questEntry += "Can Solve: Immediately\n";
                    //else if (nextSolve == TimeSpan.MaxValue)
                    //    questEntry += "Can Solve: Never again\n";
                    //else
                    //    questEntry += $"Can Solve: In {nextSolve:%d} days, {nextSolve:%h} hours, {nextSolve:%m} minutes and, {nextSolve:%s} seconds. ({(DateTime.UtcNow + nextSolve).ToLocalTime()})\n";

                    questEntry +=
                        $"-= Binary String Representation =-\n  C: {Convert.ToString(quest.NumTimesCompleted, 2).PadLeft(maxSolvesBinary.Length, '0')}\n  A: {Convert.ToString(maxSolves, 2)}\n";

                    questEntry += "--====--\n";
                    session.Player.SendMessage(questsHdr + questEntry);
                }
            }

            if (parameters[0].Equals("fellow"))
            {
                if (creature is Player player)
                {
                    var fellowship = player.Fellowship;

                    if (fellowship == null)
                    {
                        session.Player.SendMessage($"Selected player {wo.Name} (0x{objectId}) is not in a fellowship.");
                        return;
                    }

                    if (parameters.Length < 2)
                    {
                        var msg = "@qst - Query, stamp, and erase quests on the targeted player\n";
                        msg += "Usage: @qst fellow [list | bestow | erase]\n";
                        msg += "qst fellow list - List the quest flags for the Fellowship of targeted player\n";
                        msg +=
                            "qst fellow bestow - Stamps the specific quest flag on the Fellowship of targeted player. If this fails, it's probably because you spelled the quest flag wrong.\n";
                        msg +=
                            "qst fellow stamp - Stamps the specific quest flag on the Fellowship of targeted player the specified number of times. If this fails, it's probably because you spelled the quest flag wrong.\n";
                        msg +=
                            "qst fellow erase - Erase the specific quest flag from the Fellowship of targeted player. If no quest flag is given, it erases the entire quest table for the Fellowship of targeted player.\n";
                        session.Player.SendMessage(msg);
                        return;
                    }

                    if (parameters[1].Equals("list"))
                    {
                        var questsHdr = $"Quest Registry for Fellowship of {creature.Name} (0x{creature.Guid}):\n";
                        questsHdr += "================================================\n";
                        session.Player.SendMessage(questsHdr);

                        var quests = fellowship.QuestManager.GetQuests();

                        if (quests.Count == 0)
                        {
                            session.Player.SendMessage("No quests found.");
                            return;
                        }

                        foreach (var quest in quests)
                        {
                            var questEntry = "";
                            questEntry +=
                                $"Quest Name: {quest.QuestName}\nCompletions: {quest.NumTimesCompleted} | Last Completion: {quest.LastTimeCompleted} ({Time.GetDateTimeFromTimestamp(quest.LastTimeCompleted).ToLocalTime()})\n";
                            var nextSolve = fellowship.QuestManager.GetNextSolveTime(quest.QuestName);

                            if (nextSolve == TimeSpan.MinValue)
                            {
                                questEntry += "Can Solve: Immediately\n";
                            }
                            else if (nextSolve == TimeSpan.MaxValue)
                            {
                                questEntry += "Can Solve: Never again\n";
                            }
                            else
                            {
                                questEntry +=
                                    $"Can Solve: In {nextSolve:%d} days, {nextSolve:%h} hours, {nextSolve:%m} minutes and, {nextSolve:%s} seconds. ({(DateTime.UtcNow + nextSolve).ToLocalTime()})\n";
                            }

                            questEntry += "--====--\n";
                            session.Player.SendMessage(questEntry);
                        }
                        return;
                    }

                    if (parameters[1].Equals("bestow"))
                    {
                        if (parameters.Length < 3)
                        {
                            // delete all quests?
                            // seems unsafe, maybe a confirmation?
                            return;
                        }
                        var questName = parameters[2];
                        if (fellowship.QuestManager.HasQuest(questName))
                        {
                            session.Player.SendMessage($"Fellowship of {creature.Name} already has {questName}");
                            return;
                        }

                        var canSolve = fellowship.QuestManager.CanSolve(questName);
                        if (canSolve)
                        {
                            fellowship.QuestManager.Update(questName);
                            session.Player.SendMessage($"{questName} bestowed on Fellowship of {creature.Name}");
                            return;
                        }
                        else
                        {
                            session.Player.SendMessage($"Couldn't bestow {questName} on Fellowship of {creature.Name}");
                            return;
                        }
                    }

                    if (parameters[1].Equals("erase"))
                    {
                        if (parameters.Length < 3)
                        {
                            // delete all quests?
                            // seems unsafe, maybe a confirmation?
                            session.Player.SendMessage(
                                $"You must specify a quest to erase, if you want to erase all quests use the following command: /qst fellow erase *"
                            );
                            return;
                        }
                        var questName = parameters[2];

                        if (questName == "*")
                        {
                            fellowship.QuestManager.EraseAll();
                            session.Player.SendMessage($"All quests erased.");
                            return;
                        }

                        if (!fellowship.QuestManager.HasQuest(questName))
                        {
                            session.Player.SendMessage($"{questName} not found.");
                            return;
                        }
                        fellowship.QuestManager.Erase(questName);
                        session.Player.SendMessage($"{questName} erased.");
                        return;
                    }

                    if (parameters[1].Equals("stamp"))
                    {
                        var numCompletions = int.MinValue;

                        if (parameters.Length > 3 && !int.TryParse(parameters[3], out numCompletions))
                        {
                            session.Player.SendMessage($"{parameters[3]} is not a valid int");
                            return;
                        }
                        var questName = parameters[2];

                        if (numCompletions != int.MinValue)
                        {
                            fellowship.QuestManager.SetQuestCompletions(questName, numCompletions);
                        }
                        else
                        {
                            fellowship.QuestManager.Update(questName);
                        }

                        var quest = fellowship.QuestManager.GetQuest(questName);
                        if (quest != null)
                        {
                            var numTimesCompleted = quest.NumTimesCompleted;
                            session.Player.SendMessage($"{questName} stamped with {numTimesCompleted} completions.");
                        }
                        else
                        {
                            session.Player.SendMessage($"Couldn't stamp {questName} on {creature.Name}");
                        }
                        return;
                    }
                }
                else
                {
                    session.Player.SendMessage(
                        $"Selected object {wo.Name} (0x{objectId}) is not a player and cannot have a fellowship."
                    );
                }
            }
        }
        else
        {
            if (wo == null)
            {
                session.Player.SendMessage($"Selected object (0x{objectId}) not found.");
            }
            else
            {
                session.Player.SendMessage($"Selected object {wo.Name} (0x{objectId}) is not a creature.");
            }
        }
    }
}
