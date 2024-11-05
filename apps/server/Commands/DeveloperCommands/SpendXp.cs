using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class SpendXp
{
    // spendxp
    [CommandHandler("spendxp", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 2)]
    public static void HandleSpendxp(Session session, params string[] parameters)
    {
        // @spendxp - Allows you to more quickly spend your available xp into the specified skill.

        // TODO: output
    }
}
