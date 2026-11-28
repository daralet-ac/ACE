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
        "Shows your rested experience: how much is left, the bonus it gives your kills, and whether you're resting now. Also shows the XP bonus from a campfire's Rested buff.",
        ""
    )]
    public static void HandleRested(Session session, params string[] parameters)
    {
        // the same reply the Daralet Toolkit's XP bar reads (@playerui xp)
        foreach (var line in RestedXpRules.Lines(session.Player.GetPlayerUiRested()))
        {
            CommandHandlerHelper.WriteOutputInfo(session, line, ChatMessageType.Broadcast);
        }

        // the campfire's Rested buff is a separate bonus, worth more from a fire placed by a ranked player
        var rest = session.Player.GetCampfireRest();
        var campfireLine =
            rest == null
                ? CampfireRules.NotRestedLine
                : CampfireRules.RestedLine(rest.Value.BuffXp, rest.Value.RankXp, rest.Value.SecondsLeft);

        CommandHandlerHelper.WriteOutputInfo(session, campfireLine, ChatMessageType.Broadcast);
    }
}
