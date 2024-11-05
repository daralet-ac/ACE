using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class TeleDungeon
{
    /// <summary>
    /// Teleports directly to a dungeon by name or landblock
    /// </summary>
    [CommandHandler(
        "teledungeon",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Teleport to a dungeon",
        "<dungeon name or landblock>"
    )]
    public static void HandleTeleDungeon(Session session, params string[] parameters)
    {
        var isBlock = true;
        var param = parameters[0];
        if (parameters.Length > 1)
        {
            isBlock = false;
        }

        var landblock = 0u;
        if (isBlock)
        {
            try
            {
                landblock = Convert.ToUInt32(param, 16);

                if (landblock >= 0xFFFF)
                {
                    landblock = landblock >> 16;
                }
            }
            catch (Exception)
            {
                isBlock = false;
            }
        }

        // teleport to dungeon landblock
        if (isBlock)
        {
            HandleTeleDungeonBlock(session, landblock);
        }
        // teleport to dungeon by name
        else
        {
            HandleTeleDungeonName(session, parameters);
        }
    }
}
