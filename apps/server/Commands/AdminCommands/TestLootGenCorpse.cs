using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class TestLootGenCorpse
{
    [CommandHandler(
        "testlootgencorpse",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        1,
        "Generates Corpses for testing LootFactories",
        "<DID> <number corpses> <display table - melee, missile, caster, armor, pet, aetheria>"
    )]
    public static void TestLootGeneratorCorpse(Session session, params string[] parameters)
    {
        if (parameters[0] == "-info")
        {
            Console.WriteLine(
                $"Usage: \n"
                    + $"<DID> <number corpses> <(optional)display table - melee, missile, caster, jewelry, armor, pet, aetheria> \n"
                    + $" Example: The following command will generate 50 corpses generated from DeathTreasure DID 998 that shows the caster table\n"
                    + $"testlootgencorpse 998 50 caster \n"
                    + $" Example: The following command will generate 75 corpses generated from DeathTreasure DID 452 that just shows a summary \n"
                    + $"testlootgencorpse 452 75 \n"
            );
            return;
        }

        if (parameters.Length < 2)
        {
            Console.WriteLine($" LootFactory Simulator \n ---------------------\n Need to specify number of coprses\n");
            return;
        }

        if (!uint.TryParse(parameters[0], out var monsterDID))
        {
            Console.WriteLine($" LootFactory Simulator \n ---------------------\n DID specified is not an integer \n");
            return;
        }

        if (!int.TryParse(parameters[1], out var numItems))
        {
            Console.WriteLine(
                $" LootFactory Simulator \n ---------------------\n Invalid Parameter - Must be a number \n"
            );
            return;
        }

        var logStats = false;
        var displayTable = "";

        if (parameters.Length > 2)
        {
            switch (parameters[2].ToLower())
            {
                case "melee":
                case "missile":
                case "caster":
                case "jewelry":
                case "armor":
                case "pet":
                case "aetheria":
                case "all":
                case "cloak":
                    displayTable = parameters[2].ToLower();
                    break;

                case "-log":
                    logStats = true;
                    break;

                default:
                    Console.WriteLine(
                        "Invalid Table Option.  Available Tables to show are melee, missile, caster, jewelry, armor, cloak, pet, aetheria or all."
                    );
                    return;
            }
        }

        if (parameters.Length > 3)
        {
            var logParam = parameters[3].ToLower();

            if (logParam == "-log")
            {
                logStats = true;
            }
            else
            {
                Console.WriteLine("Invalid Option.  To log a file, use option -log");
                return;
            }
        }
        var results = LootGenerationFactory_Test.TestLootGenMonster(monsterDID, numItems, logStats, displayTable);

        Console.WriteLine(results);
    }
}
