using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class TeleXYZ
{
    /// <summary>
    /// telexyz cell x y z qx qy qz qw
    /// </summary>
    [CommandHandler(
        "telexyz",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        8,
        "Teleport to a location.",
        "cell x y z qx qy qz qw\n" + "all parameters must be specified and cell must be in decimal form"
    )]
    public static void HandleDebugTeleportXYZ(Session session, params string[] parameters)
    {
        if (!uint.TryParse(parameters[0], out var cell))
        {
            return;
        }

        var positionData = new float[7];

        for (var i = 0u; i < 7u; i++)
        {
            if (!float.TryParse(parameters[i + 1], out var position))
            {
                return;
            }

            positionData[i] = position;
        }

        session.Player.Teleport(
            new Position(
                cell,
                positionData[0],
                positionData[1],
                positionData[2],
                positionData[3],
                positionData[4],
                positionData[5],
                positionData[6]
            )
        );
    }
}
