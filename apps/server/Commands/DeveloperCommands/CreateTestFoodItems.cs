using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class CreateTestFoodItems
{
    [CommandHandler(
        "food",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Creates some food items in your inventory for testing."
    )]
    public static void HandleFood(Session session, params string[] parameters)
    {
        var weenieIds = new HashSet<uint> { 259, 259, 260, 377, 378, 379 };

        AddWeeniesToInventory(session, weenieIds);
    }
}
