using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class Attackable
{
    // attackable { on | off }
    [CommandHandler(
        "attackable",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Sets whether monsters will attack you or not.",
        "[ on | off ]\n"
        + "This command sets whether monsters will attack you unprovoked.\n When turned on, monsters will attack you as if you are a normal player.\n When turned off, monsters will ignore you."
    )]
    public static void HandleAttackable(Session session, params string[] parameters)
    {
        // usage: @attackable { on,off}
        // This command sets whether monsters will attack you unprovoked.When turned on, monsters will attack you as if you are a normal player.  When turned off, monsters will ignore you.
        // @attackable - Sets whether monsters will attack you or not.

        if (session.Player.IsAdvocate && session.Player.AdvocateLevel < 5)
        {
            return;
        }

        var param = parameters[0];

        switch (param)
        {
            case "off":
                session.Player.UpdateProperty(session.Player, PropertyBool.Attackable, false, true);
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        "Monsters will only attack you if provoked by you first.",
                        ChatMessageType.Broadcast
                    )
                );
                break;
            case "on":
            default:
                session.Player.UpdateProperty(session.Player, PropertyBool.Attackable, true, true);
                session.Network.EnqueueSend(
                    new GameMessageSystemChat("Monsters will attack you normally.", ChatMessageType.Broadcast)
                );
                break;
        }
    }
}
