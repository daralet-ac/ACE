using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.DeveloperCommands;

public class AdminUi
{
    private static readonly ILogger _log = Log.ForContext(typeof(AdminUi));

    // adminui
    [CommandHandler("adminui", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleAdminui(Session session, params string[] parameters)
    {
        // usage: @adminui
        // This command toggles whether the Admin UI is visible.

        // just a placeholder, probably not needed or should be handled by a decal plugin to replicate the admin ui
    }
}
