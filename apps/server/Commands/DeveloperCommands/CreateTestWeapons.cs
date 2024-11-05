using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class CreateTestWeapons
{
    [CommandHandler(
        "weapons",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Creates testing items in your inventory."
    )]
    public static void HandleWeapons(Session session, params string[] parameters)
    {
        var weenieIds = new HashSet<uint> { 93, 148, 300, 307, 311, 326, 338, 348, 350, 7765, 12748, 12463, 31812 };

        AddWeeniesToInventory(session, weenieIds);
    }
}
