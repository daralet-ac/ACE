using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.EnvoyCommands;

public class Cloak
{
    // cloak < on / off / player / creature >
    [CommandHandler(
        "cloak",
        AccessLevel.Envoy,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Sets your cloaking state.",
        "< on / off / player / creature >\n"
            + "This command sets your current cloaking state\n"
            + "< on > You will be completely invisible to players.\n"
            + "< off > You will show up as a normal.\n"
            + "< player > You will appear as a player. (No + and a white radar dot.)\n"
            + "< creature > You will appear as a creature. (No + and an orange radar dot.)"
    )]
    public static void HandleCloak(Session session, params string[] parameters)
    {
        // Please specify if you want cloaking on or off.usage: @cloak < on / off / player / creature >
        // This command sets your current cloaking state.
        // < on > You will be completely invisible to players.
        // < off > You will show up as a normal.
        // < player > You will appear as a player. (No + and a white radar dot.)
        // < creature > You will appear as a creature. (No + and an orange radar dot.)
        // @cloak - Sets your cloaking state.

        // TODO: investigate translucensy/visbility of other cloaked admins.

        switch (parameters?[0].ToLower())
        {
            case "off":
                if (session.Player.CloakStatus == CloakStatus.Off)
                {
                    return;
                }

                session.Player.DeCloak();

                session.Player.SetProperty(PropertyInt.CloakStatus, (int)CloakStatus.Off);

                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"You are no longer cloaked, can no longer pass through doors and will appear as an admin.",
                    ChatMessageType.Broadcast
                );
                break;
            case "on":
                if (session.Player.CloakStatus == CloakStatus.On)
                {
                    return;
                }

                session.Player.HandleCloak();

                session.Player.SetProperty(PropertyInt.CloakStatus, (int)CloakStatus.On);

                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"You are now cloaked.\nYou are now ethereal and can pass through doors.",
                    ChatMessageType.Broadcast
                );
                break;
            case "player":
                if (session.AccessLevel > AccessLevel.Envoy)
                {
                    if (session.Player.CloakStatus == CloakStatus.Player)
                    {
                        return;
                    }

                    session.Player.SetProperty(PropertyInt.CloakStatus, (int)CloakStatus.Player);

                    session.Player.DeCloak();
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"You will now appear as a player.",
                        ChatMessageType.Broadcast
                    );
                }
                else
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"You do not have permission to do that state",
                        ChatMessageType.Broadcast
                    );
                }

                break;
            case "creature":
                if (session.AccessLevel > AccessLevel.Envoy)
                {
                    if (session.Player.CloakStatus == CloakStatus.Creature)
                    {
                        return;
                    }

                    session.Player.SetProperty(PropertyInt.CloakStatus, (int)CloakStatus.Creature);
                    session.Player.Attackable = true;

                    session.Player.DeCloak();
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"You will now appear as a creature.\nUse @pk free to be allowed to attack all living things.",
                        ChatMessageType.Broadcast
                    );
                }
                else
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"You do not have permission to do that state",
                        ChatMessageType.Broadcast
                    );
                }

                break;
            default:
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        "Please specify if you want cloaking on or off.",
                        ChatMessageType.Broadcast
                    )
                );
                break;
        }
    }
}
