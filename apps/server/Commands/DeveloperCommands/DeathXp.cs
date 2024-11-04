using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class DeathXp
{
    /// <summary>
    /// Displays how much experience the last appraised creature is worth when killed.
    /// </summary>
    [CommandHandler(
        "deathxp",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Displays how much experience the last appraised creature is worth when killed."
    )]
    public static void HandleDeathxp(Session session, params string[] parameters)
    {
        var creature = CommandHandlerHelper.GetLastAppraisedObject(session);
        if (creature == null)
        {
            return;
        }

        CommandHandlerHelper.WriteOutputInfo(session, $"{creature.Name} XP: {creature.XpOverride}");
    }
}
