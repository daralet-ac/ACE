using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class HouseBarrierTest
{
    [CommandHandler(
        "barrier-test",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows debug information for house barriers"
    )]
    public static void HandleBarrierTest(Session session, params string[] parameters)
    {
        var cell = session.Player.Location.Cell;
        Console.WriteLine($"CurCell: {cell:X8}");

        if (session.Player.CurrentLandblock.IsDungeon)
        {
            Console.WriteLine($"Dungeon landblock");

            if (!HouseManager.ApartmentBlocks.ContainsKey(session.Player.Location.Landblock))
            {
                return;
            }
        }
        else
        {
            cell = session.Player.Location.GetOutdoorCell();
            Console.WriteLine($"OutdoorCell: {cell:X8}");
        }

        var barrier = HouseCell.HouseCells.ContainsKey(cell);
        Console.WriteLine($"Barrier: {barrier}");
    }
}
