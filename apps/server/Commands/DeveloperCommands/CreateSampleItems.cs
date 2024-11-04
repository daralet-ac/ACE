using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class CreateSampleItems
{
    [CommandHandler(
        "inv",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Creates sample items, foci and containers in your inventory."
    )]
    public static void HandleInv(Session session, params string[] parameters)
    {
        var weenieIds = new HashSet<uint> { 44, 45, 46, 136, 5893, 15268, 15269, 15270, 15271, 12748 };

        AddWeeniesToInventory(session, weenieIds);
    }
}
