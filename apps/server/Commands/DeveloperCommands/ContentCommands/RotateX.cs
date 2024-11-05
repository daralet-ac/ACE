using System.Numerics;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.ContentCommands.ContentCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class RotateX
{
    [CommandHandler(
        "rotate-x",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        1,
        "Adjusts the rotation of a landblock instance along the x-axis",
        "<degrees>"
    )]
    public static void HandleRotateX(Session session, params string[] parameters)
    {
        HandleRotateAxis(session, Vector3.UnitX, parameters);
    }
}
