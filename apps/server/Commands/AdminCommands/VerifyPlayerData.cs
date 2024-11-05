using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyPlayerData
{
    [CommandHandler(
        "verify-player-data",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any bugs with player data. Runs all of the verify* commands."
    )]
    public static void HandleVerifyAll(Session session, params string[] parameters)
    {
        VerifyPlayerAttributes.HandleVerifyAttributes(session, parameters);
        VerifyPlayerVitals.HandleVerifyVitals(session, parameters);

        VerifyPlayerSkills.HandleVerifySkills(session, parameters);

        VerifyPlayerSkillCredits.HandleVerifySkillCredits(session, parameters);

        VerifyPlayerHeritageAugs.HandleVerifyHeritageAugs(session, parameters);
        VerifyPlayerMaxAugs.HandleVerifyMaxAugs(session, parameters);

        VerifyPlayerXp.HandleVerifyExperience(session, parameters);
    }
}
