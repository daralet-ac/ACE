using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class RemoveVitae
{
    [CommandHandler(
        "remove-vitae",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Removes vitae from last appraised player"
    )]
    public static void HandleRemoveVitae(Session session, params string[] parameters)
    {
        var player = CommandHandlerHelper.GetLastAppraisedObject(session) as Player;

        if (player == null)
        {
            player = session.Player;
        }

        player.EnchantmentManager.RemoveVitae();

        if (player != session.Player)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat("Removed vitae for {player.Name}", ChatMessageType.Broadcast)
            );
        }
    }
}
