using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class RequireComps
{
    [CommandHandler(
        "requirecomps",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Sets whether spell components are required to cast spells.",
        "[ on | off ]\n"
        + "This command sets whether spell components are required to cast spells..\n When turned on, spell components are required.\n When turned off, spell components are ignored."
    )]
    public static void HandleRequireComps(Session session, params string[] parameters)
    {
        var param = parameters[0];

        switch (param)
        {
            case "off":
                session.Player.SpellComponentsRequired = false;
                session.Player.EnqueueBroadcast(
                    new GameMessagePublicUpdatePropertyBool(
                        session.Player,
                        PropertyBool.SpellComponentsRequired,
                        session.Player.SpellComponentsRequired
                    )
                );
                session.Network.EnqueueSend(
                    new GameMessageSystemChat("You can now cast spells without components.", ChatMessageType.Broadcast)
                );
                break;
            case "on":
            default:
                session.Player.SpellComponentsRequired = true;
                session.Player.EnqueueBroadcast(
                    new GameMessagePublicUpdatePropertyBool(
                        session.Player,
                        PropertyBool.SpellComponentsRequired,
                        session.Player.SpellComponentsRequired
                    )
                );
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        "You can no longer cast spells without components.",
                        ChatMessageType.Broadcast
                    )
                );
                break;
        }
    }
}
