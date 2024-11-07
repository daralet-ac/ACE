using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.DeveloperCommands;

public class AdminVision
{
    private static readonly ILogger _log = Log.ForContext(typeof(AdminVision));

    // adminvision { on | off | toggle | check}
    [CommandHandler(
        "adminvision",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Allows the admin to see admin-only visible items.",
        "{ on | off | toggle | check }\n"
        + "Controls whether or not the admin can see admin-only visible items. Note that if you turn this feature off, you will need to log out and back in before the visible items become invisible."
    )]
    public static void HandleAdminvision(Session session, params string[] parameters)
    {
        // @adminvision { on | off | toggle | check}
        // Controls whether or not the admin can see admin-only visible items. Note that if you turn this feature off, you will need to log out and back in before the visible items become invisible.
        // @adminvision - Allows the admin to see admin - only visible items.

        switch (parameters?[0].ToLower())
        {
            case "1":
            case "on":
                session.Player.HandleAdminvisionToggle(1);
                break;
            case "0":
            case "off":
                session.Player.HandleAdminvisionToggle(0);
                break;
            case "toggle":
                session.Player.HandleAdminvisionToggle(2);
                break;
            case "check":
            default:
                session.Player.HandleAdminvisionToggle(-1);
                break;
        }
    }
}
