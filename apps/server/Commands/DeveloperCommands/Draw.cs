using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.DeveloperCommands;

public class Draw
{
    private static readonly ILogger _log = Log.ForContext(typeof(Draw));

    // draw
    [CommandHandler("draw", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleDraw(Session session, params string[] parameters)
    {
        // @draw - Draws undrawable things.

        // TODO: output
    }
}
