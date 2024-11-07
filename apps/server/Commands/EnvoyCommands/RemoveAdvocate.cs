using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.EnvoyCommands;

public class RemoveAdvocate
{
    // remove <name>
    [CommandHandler(
        "remove",
        AccessLevel.Envoy,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Removes the specified character from the Advocate ranks.",
        "<character name>\nAdvocates can remove Advocate status for any Advocate of lower level than their own."
    )]
    public static void HandleRemove(Session session, params string[] parameters)
    {
        var charName = string.Join(" ", parameters).Trim();

        var playerToFind = PlayerManager.FindByName(charName);

        if (playerToFind != null)
        {
            if (playerToFind is Player player)
            {
                if (!Advocate.IsAdvocate(player))
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat($"{playerToFind.Name} is not an Advocate.", ChatMessageType.Broadcast)
                    );
                    return;
                }

                if (session.Player.AdvocateLevel < player.AdvocateLevel)
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"You cannot remove {playerToFind.Name}'s Advocate status because they out rank you.",
                            ChatMessageType.Broadcast
                        )
                    );
                    return;
                }

                if (Advocate.Remove(player))
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"{playerToFind.Name} is no longer an Advocate.",
                            ChatMessageType.Broadcast
                        )
                    );
                }
                else
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Advocate removal of {playerToFind.Name} failed.",
                            ChatMessageType.Broadcast
                        )
                    );
                }
            }
            else
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"{playerToFind.Name} is not online. Cannot complete removal process.",
                        ChatMessageType.Broadcast
                    )
                );
            }
        }
        else
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"{charName} was not found in the database.", ChatMessageType.Broadcast)
            );
        }
    }
}
