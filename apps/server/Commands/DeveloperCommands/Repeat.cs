using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class Repeat
{
    // repeat < Num > < Command >
    [CommandHandler("repeat", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 2)]
    public static void HandleRepeat(Session session, params string[] parameters)
    {
        // @repeat < Num > < Command > -Repeat a command a number of times.
        // EX: "@repeat 5 @say Hi" - say Hi 5 times.
        // @repeat < Num > < Command > -Repeat a command a number of times.

        // TODO: output
    }
}
