using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class Pk
{
    // pk
    [CommandHandler(
        "pk",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "sets your own PK state.",
        "< npk / pk / pkl / free >\n"
            + "This command sets your current player killer state\n"
            + "This command also expects to be used '@cloak player' to properly reflect to other players\n"
            + "< npk > You can only attack monsters.\n"
            + "< pk > You can attack player killers and monsters.\n"
            + "< pkl > You can attack player killer lites and monsters\n"
            + "< free > You can attack players, player killers, player killer lites, monsters, and npcs"
    )]
    public static void HandlePk(Session session, params string[] parameters)
    {
        // @pk - Toggles or sets your own PK state.

        if (parameters.Length == 0)
        {
            //player.EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(player, PropertyInt.PlayerKillerStatus, (int)player.PlayerKillerStatus));
            var message =
                $"Your current PK state is: {session.Player.PlayerKillerStatus.ToString()}\n"
                + "You can change it to the following:\n"
                + "NPK      = Non-Player Killer\n"
                + "PK       = Player Killer\n"
                + "PKL      = Player Killer Lite\n"
                + "Free     = Can kill anything\n";
            CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.Broadcast);
        }
        else
        {
            switch (parameters[0].ToLower())
            {
                case "npk":
                    session.Player.PlayerKillerStatus = PlayerKillerStatus.NPK;
                    session.Player.PkLevel = PKLevel.NPK;
                    break;
                case "pk":
                    session.Player.PlayerKillerStatus = PlayerKillerStatus.PK;
                    session.Player.PkLevel = PKLevel.PK;
                    break;
                case "pkl":
                    session.Player.PlayerKillerStatus = PlayerKillerStatus.PKLite;
                    session.Player.PkLevel = PKLevel.NPK;
                    break;
                case "free":
                    session.Player.PlayerKillerStatus = PlayerKillerStatus.Free;
                    session.Player.PkLevel = PKLevel.Free;
                    break;
            }
            session.Player.EnqueueBroadcast(
                new GameMessagePublicUpdatePropertyInt(
                    session.Player,
                    PropertyInt.PlayerKillerStatus,
                    (int)session.Player.PlayerKillerStatus
                )
            );
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Your current PK state is now set to: {session.Player.PlayerKillerStatus.ToString()}",
                ChatMessageType.Broadcast
            );

            PlayerManager.BroadcastToAuditChannel(
                session.Player,
                $"{session.Player.Name} changed their PK state to {session.Player.PlayerKillerStatus.ToString()}."
            );
        }
    }
}
