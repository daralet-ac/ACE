using System.Linq;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.PlayerCommands;

public class Corpses
{
    [CommandHandler(
        "corpses",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        "Shows location of recent player corpses on the landscape"
    )]
    public static void CorpseList(Session session, params string[] parameters)
    {
        if (session.Player.CorpseLog == null || session.Player.CorpseLog.Length == 0)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat("You have no recent corpses.", ChatMessageType.Broadcast)
            );
            return;
        }

        var corpses = session.Player.CorpseLog.Split(';');

        if (corpses != null)
        {
            var rewrittenCorpseLog = "";

            // MAINTENANCE STEP: First we query the DB to see if any of the corpses no longer exist, and remove them from the list if so
            foreach (var corpselog in corpses)
            {
                var corpse = corpselog.Split('|');

                if (corpse.Length > 0)
                {
                    // parse corpse guid, query DB to see if it still exists, if it doesn't do not add to the rewritten string
                    if (uint.TryParse(corpse[0], out var corpseGuid))
                    {
                        var exists = DatabaseManager.Shard.BaseDatabase.GetBiota(corpseGuid);
                        if (exists != null && exists.WeenieType == (int)WeenieType.Corpse)
                        {
                            rewrittenCorpseLog += string.Join("|", corpse) + ";";
                        }
                    }
                }
            }

            session.Player.CorpseLog = rewrittenCorpseLog;

            // check again to see if, after removal, we have any corpses in the log
            if (session.Player.CorpseLog == null || session.Player.CorpseLog.Length == 0)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat("You have no recent corpses.", ChatMessageType.Broadcast)
                );
                return;
            }

            if (parameters.Length > 0)
            {
                // QUERY DROPPED LOOT: @corpses [number]
                if (parameters.Length == 1)
                {
                    if (int.TryParse(parameters[0], out var result))
                    {
                        var corpseLogs = session.Player.CorpseLog.Split(';');
                        if (corpseLogs.Length >= result)
                        {
                            var log = corpseLogs[result - 1];
                            var split = log.Split('|');
                            if (split.Length == 6)
                            {
                                session.Network.EnqueueSend(
                                    new GameMessageSystemChat($"Corpse {result}: {split[5]}", ChatMessageType.Broadcast)
                                );
                                return;
                            }
                        }
                        else
                        {
                            session.Network.EnqueueSend(
                                new GameMessageSystemChat($"Invalid corpse number.", ChatMessageType.Broadcast)
                            );
                            return;
                        }
                    }
                    else
                    {
                        session.Network.EnqueueSend(
                            new GameMessageSystemChat("That is not a valid command.", ChatMessageType.Broadcast)
                        );
                        return;
                    }
                }

                // REMOVE CORPSE FROM LIST: @corpse remove [number]
                if (parameters.Length == 2)
                {
                    if (parameters[0].ToLower() == "remove")
                    {
                        if (int.TryParse(parameters[1], out var result))
                        {
                            var corpseLogs = session.Player.CorpseLog.Split(';');
                            if (corpseLogs.Length >= result)
                            {
                                var newCorpseLog = string.Join(";", corpseLogs.Where(p => p != corpseLogs[result - 1]));
                                session.Player.CorpseLog = newCorpseLog;
                                session.Network.EnqueueSend(
                                    new GameMessageSystemChat(
                                        $"Corpse {result} has been removed, however the corpse still exists and may be retrieved.",
                                        ChatMessageType.Broadcast
                                    )
                                );
                                return;
                            }
                            else
                            {
                                session.Network.EnqueueSend(
                                    new GameMessageSystemChat($"Invalid corpse number.", ChatMessageType.Broadcast)
                                );
                                return;
                            }
                        }
                    }
                }
            }
            // LIST OF CURRENT CORPSES: @corpses
            else if (parameters.Length == 0)
            {
                var count = 1;

                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"---------------------------Current Corpses---------------------------",
                        ChatMessageType.Broadcast
                    )
                );

                var revisedCorpses = session.Player.CorpseLog.Split(";");

                foreach (var corpse in revisedCorpses)
                {
                    if (corpse.Length == 0)
                    {
                        continue;
                    }

                    var split = corpse.Split("|");

                    if (split == null || split.Length < 5)
                    {
                        continue;
                    }

                    // Corpse GUID, Killer Name, DateTime of Death, Location, DateTime Decay, Items
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"{count}:  Killed By - {split[1]}  |  Time of Death - {split[2]}  |  Location - {split[3]}  |   Est. Decay - {split[4]}",
                            ChatMessageType.Broadcast
                        )
                    );

                    count++;
                }

                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Corpses will be removed from this list once opened, or once the decay time expires, though depending on landblock activity they may still exist in the world.",
                        ChatMessageType.Broadcast
                    )
                );
            }
        }
    }
}
