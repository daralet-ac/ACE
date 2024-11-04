using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class SetAdvancementClass
{
    // setadvclass
    [CommandHandler("setadvclass", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 2)]
    public static void Handlesetadvclass(Session session, params string[] parameters)
    {
        // @setadvclass - Sets the advancement class of one of your own skills.

        // TODO: output
    }
}
