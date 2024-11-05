using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands.WorldObjectCommands;

public class PropertyDump
{
    [CommandHandler(
        "propertydump",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Lists all properties for the last world object you examined."
    )]
    public static void HandlePropertyDump(Session session, params string[] parameters)
    {
        var target = CommandHandlerHelper.GetLastAppraisedObject(session);

        if (target != null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"\n{target.DebugOutputString(target)}", ChatMessageType.System)
            );
        }
    }
}
