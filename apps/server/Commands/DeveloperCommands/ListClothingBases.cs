using System;
using System.Collections.Generic;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class ListClothingBases
{
    /// <summary>
    /// List all clothing bases which are compatible with setup
    /// </summary>
    [CommandHandler(
        "listcb",
        AccessLevel.Developer,
        CommandHandlerFlag.ConsoleInvoke,
        "List Clothing Tables available"
    )]
    public static void HandleShowCompatibleClothingBases(Session session, params string[] parameters)
    {
        uint.TryParse(parameters[0], out var setupId);

        uint cbStart = 0x10000001;
        uint cbEnd = 0x1000086c;

        var compatibleCBs = new List<uint>();

        for (var i = cbStart; i < cbEnd; i++)
        {
            var cbToTest = DatManager.PortalDat.ReadFromDat<ClothingTable>(i);

            if (cbToTest.ClothingBaseEffects.ContainsKey(setupId))
            {
                compatibleCBs.Add(i);
            }
        }

        Console.WriteLine($"There are {compatibleCBs.Count} compatible clothingbase tables for setup {setupId}");
        Console.WriteLine("");
        Console.WriteLine($"{string.Join("\n", compatibleCBs.ToArray())}");
    }
}
