using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class CreateTestCurrency
{
    [CommandHandler(
        "currency",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Creates some currency items in your inventory for testing."
    )]
    public static void HandleCurrency(Session session, params string[] parameters)
    {
        var weenieIds = new HashSet<uint> { 273, 20630 };

        AddWeeniesToInventory(session, weenieIds);
    }
}
