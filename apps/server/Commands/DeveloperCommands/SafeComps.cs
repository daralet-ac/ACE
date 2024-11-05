using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class SafeComps
{
    /// <summary>
    /// Enables / disables spell component burning
    /// </summary>
    [CommandHandler(
        "safecomps",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Enables / disables spell component burning",
        "<on/off>"
    )]
    public static void HandleSafeComps(Session session, params string[] parameters)
    {
        var safeComps = true;
        if (parameters.Length > 0 && parameters[0].ToLower().Equals("off"))
        {
            safeComps = false;
        }

        session.Player.SafeSpellComponents = safeComps;

        if (safeComps)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "Your spell components are now safe, and will not be consumed when casting spells.",
                    ChatMessageType.Broadcast
                )
            );
        }
        else
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "Your spell components will now be consumed when casting spells.",
                    ChatMessageType.Broadcast
                )
            );
        }
    }
}
