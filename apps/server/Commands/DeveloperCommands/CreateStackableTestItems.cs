using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class CreateStackableTestItems
{
    [CommandHandler(
        "splits",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Creates some stackable items in your inventory for testing."
    )]
    public static void HandleSplits(Session session, params string[] parameters)
    {
        var weenieIds = new HashSet<uint> { 300, 690, 20630, 20631, 31198, 37155 };

        AddWeeniesToInventory(session, weenieIds);
    }
}
