using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class TrainSkill
{
    // trainskill
    [CommandHandler("trainskill", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 1)]
    public static void Handletrainskill(Session session, params string[] parameters)
    {
        // @trainskill - Attempts to train the specified skill by spending skill credits on it.

        // TODO: output
    }
}
