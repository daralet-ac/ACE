using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class CreateSpellComps
{
    [CommandHandler(
        "comps",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Creates spell component items in your inventory for testing."
    )]
    public static void HandleComps(Session session, params string[] parameters)
    {
        var weenieIds = new HashSet<uint>
        {
            686,
            687,
            688,
            689,
            690,
            691,
            740,
            741,
            742,
            743,
            744,
            745,
            746,
            747,
            748,
            749,
            750,
            751,
            752,
            753,
            754,
            755,
            756,
            757,
            758,
            759,
            760,
            761,
            762,
            763,
            764,
            765,
            766,
            767,
            768,
            769,
            770,
            771,
            772,
            773,
            774,
            775,
            776,
            777,
            778,
            779,
            780,
            781,
            782,
            783,
            784,
            785,
            786,
            787,
            788,
            789,
            790,
            791,
            792,
            1643,
            1644,
            1645,
            1646,
            1647,
            1648,
            1649,
            1650,
            1651,
            1652,
            1653,
            1654,
            7299,
            7581,
            8897,
            20631
        };

        AddWeeniesToInventory(session, weenieIds, 1);
    }
}
