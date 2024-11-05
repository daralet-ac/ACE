using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class LostTest
{
    [CommandHandler(
        "lostest",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Tests for direct visibilty with latest appraised object"
    )]
    public static void HandleVisible(Session session, params string[] parameters)
    {
        // get the last appraised object
        var targetID = session.Player.CurrentAppraisalTarget;
        if (targetID == null)
        {
            CommandHandlerHelper.WriteOutputInfo(session, "ERROR: no appraisal target");
            return;
        }
        var targetGuid = new ObjectGuid(targetID.Value);
        var target = session.Player.CurrentLandblock?.GetObject(targetGuid);
        if (target == null)
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Couldn't find " + targetGuid);
            return;
        }

        var visible = session.Player.IsDirectVisible(target);
        CommandHandlerHelper.WriteOutputInfo(session, "Visible: " + visible);
    }
}
