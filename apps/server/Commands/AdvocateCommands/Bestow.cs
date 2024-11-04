using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdvocateCommands;

public class Bestow
{
    // bestow <name> <level>
    [CommandHandler(
        "bestow",
        AccessLevel.Advocate,
        CommandHandlerFlag.RequiresWorld,
        2,
        "Sets a character's Advocate Level.",
        "<name> <level>\nAdvocates can bestow any level less than their own."
    )]
    public static void HandleBestow(Session session, params string[] parameters)
    {
        var charName = string.Join(" ", parameters).Trim();

        var level = parameters[parameters.Length - 1];

        if (!int.TryParse(level, out var advocateLevel) || advocateLevel < 1 || advocateLevel > 7)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"{level} is not a valid advocate level.", ChatMessageType.Broadcast)
            );
            return;
        }

        var advocateName = charName.TrimEnd((" " + level).ToCharArray());

        var playerToFind = PlayerManager.FindByName(advocateName);

        if (playerToFind != null)
        {
            if (playerToFind is Player player)
            {
                //if (!Advocate.IsAdvocate(player))
                //{
                //    session.Network.EnqueueSend(new GameMessageSystemChat($"{playerToFind.Name} is not an Advocate.", ChatMessageType.Broadcast));
                //    return;
                //}

                if (player.IsPK || PropertyManager.GetBool("pk_server").Item)
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"{playerToFind.Name} in a Player Killer and cannot be an Advocate.",
                            ChatMessageType.Broadcast
                        )
                    );
                    return;
                }

                if (session.Player.AdvocateLevel <= player.AdvocateLevel)
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"You cannot change {playerToFind.Name}'s Advocate status because they are equal to or out rank you.",
                            ChatMessageType.Broadcast
                        )
                    );
                    return;
                }

                if (advocateLevel >= session.Player.AdvocateLevel && !session.Player.IsAdmin)
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"You cannot bestow {playerToFind.Name}'s Advocate rank to {advocateLevel} because that is equal to or higher than your rank.",
                            ChatMessageType.Broadcast
                        )
                    );
                    return;
                }

                if (advocateLevel == player.AdvocateLevel)
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"{playerToFind.Name}'s Advocate rank is already at level {advocateLevel}.",
                            ChatMessageType.Broadcast
                        )
                    );
                    return;
                }

                if (!Advocate.CanAcceptAdvocateItems(player, advocateLevel))
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"You cannot change {playerToFind.Name}'s Advocate status because they do not have capacity for the advocate items.",
                            ChatMessageType.Broadcast
                        )
                    );
                    return;
                }

                if (Advocate.Bestow(player, advocateLevel))
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"{playerToFind.Name} is now an Advocate, level {advocateLevel}.",
                            ChatMessageType.Broadcast
                        )
                    );
                }
                else
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Advocate bestowal of {playerToFind.Name} failed.",
                            ChatMessageType.Broadcast
                        )
                    );
                }
            }
            else
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"{playerToFind.Name} is not online. Cannot complete bestowal process.",
                        ChatMessageType.Broadcast
                    )
                );
            }
        }
        else
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"{advocateName} was not found in the database.", ChatMessageType.Broadcast)
            );
        }
    }
}
