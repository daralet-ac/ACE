using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class DebugMove
{
    [CommandHandler(
        "debugmove",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Toggles movement debugging for the last appraised monster",
        "<on/off>"
    )]
    public static void ToggleMovementDebug(Session session, params string[] parameters)
    {
        // get the last appraised object
        var creature = CommandHandlerHelper.GetLastAppraisedObject(session) as Creature;

        if (creature == null)
        {
            return;
        }

        var enabled = true;
        if (parameters.Length > 0 && parameters[0].Equals("off"))
        {
            enabled = false;
        }

        creature.DebugMove = enabled;
    }
}
