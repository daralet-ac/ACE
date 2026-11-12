using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;

namespace ACE.Server.Commands.PlayerCommands;

public class RestedCommand
{
    // rested
    [CommandHandler(
        "rested",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        "Shows your rested experience: how much is left, the bonus it gives your kills, and whether you're resting now.",
        ""
    )]
    public static void HandleRested(Session session, params string[] parameters)
    {
        // the same reply the Daralet Toolkit's XP bar reads (@playerui xp)
        foreach (var line in RestedXpRules.Lines(session.Player.GetPlayerUiRested()))
        {
            CommandHandlerHelper.WriteOutputInfo(session, line, ChatMessageType.Broadcast);
        }
    }
}
