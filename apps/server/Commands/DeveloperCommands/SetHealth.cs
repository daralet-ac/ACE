using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class SetHealth
{
    [CommandHandler(
        "sethealth",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "sets your current health to a specific value.",
        "ushort"
    )]
    public static void HandleSetHealth(Session session, params string[] parameters)
    {
        if (parameters?.Length > 0)
        {
            if (ushort.TryParse(parameters[0], out var health))
            {
                session.Player.Health.Current = health;
                var updatePlayersHealth = new GameMessagePrivateUpdateAttribute2ndLevel(
                    session.Player,
                    Vital.Health,
                    session.Player.Health.Current
                );
                var message = new GameMessageSystemChat(
                    $"Attempting to set health to {health}...",
                    ChatMessageType.Broadcast
                );
                session.Network.EnqueueSend(updatePlayersHealth, message);
                return;
            }
        }

        ChatPacket.SendServerMessage(session, "Usage: /sethealth 200 (max Max Health)", ChatMessageType.Broadcast);
    }
}
