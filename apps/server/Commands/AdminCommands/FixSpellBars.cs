using System;
using System.Linq;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.AdminCommands;

public class FixSpellBars
{
    private static readonly ILogger _log = Log.ForContext(typeof(FixSpellBars));

    [CommandHandler(
        "fix-spell-bars",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Fixes the players spell bars.",
        "<execute>"
    )]
    public static void HandleFixSpellBars(Session session, params string[] parameters)
    {
        Console.WriteLine();

        Console.WriteLine(
            "This command will attempt to fix player spell bars. Unless explictly indicated, command will dry run only"
        );
        Console.WriteLine(
            "You must have executed 2020-04-11-00-Update-Character-SpellBars.sql script first before running this command"
        );

        Console.WriteLine();

        var execute = false;

        if (parameters.Length < 1)
        {
            Console.WriteLine(
                "This will be a dry run and show which characters that would be affected. To perform fix, please use command: fix-spell-bars execute"
            );
        }
        else if (parameters[0].ToLower() == "execute")
        {
            execute = true;
        }
        else
        {
            Console.WriteLine("Please use command fix-spell-bars execute");
        }

        if (!execute)
        {
            Console.WriteLine();
            Console.WriteLine("Press enter to start.");
            Console.ReadLine();
        }

        var numberOfRecordsFixed = 0;

        _log.Information("Starting FixSpellBarsPR2918 process. This could take a while...");

        using (var context = new ShardDbContext())
        {
            var characterSpellBarsNotFixed = context
                .CharacterPropertiesSpellBar.Where(c => c.SpellBarNumber == 0)
                .ToList();

            if (characterSpellBarsNotFixed.Count > 0)
            {
                _log.Warning(
                    "2020-04-11-00-Update-Character-SpellBars.sql patch not yet applied. Please apply this patch ASAP! Skipping FixSpellBarsPR2918 for now..."
                );
                _log.Fatal(
                    "2020-04-11-00-Update-Character-SpellBars.sql patch not yet applied. You must apply this patch before proceeding further..."
                );
                return;
            }

            var characterSpellBars = context
                .CharacterPropertiesSpellBar.OrderBy(c => c.CharacterId)
                .ThenBy(c => c.SpellBarNumber)
                .ThenBy(c => c.SpellBarIndex)
                .ToList();

            uint characterId = 0;
            uint spellBarNumber = 0;
            uint spellBarIndex = 0;

            foreach (var entry in characterSpellBars)
            {
                if (entry.CharacterId != characterId)
                {
                    characterId = entry.CharacterId;
                    spellBarIndex = 0;
                }

                if (entry.SpellBarNumber != spellBarNumber)
                {
                    spellBarNumber = entry.SpellBarNumber;
                    spellBarIndex = 0;
                }

                spellBarIndex++;

                if (entry.SpellBarIndex != spellBarIndex)
                {
                    Console.WriteLine(
                        $"FixSpellBarsPR2918: Character 0x{entry.CharacterId:X8}, SpellBarNumber = {entry.SpellBarNumber} | SpellBarIndex = {entry.SpellBarIndex:000}; Fixed - {spellBarIndex:000}"
                    );
                    entry.SpellBarIndex = spellBarIndex;
                    numberOfRecordsFixed++;
                }
                else
                {
                    Console.WriteLine(
                        $"FixSpellBarsPR2918: Character 0x{entry.CharacterId:X8}, SpellBarNumber = {entry.SpellBarNumber} | SpellBarIndex = {entry.SpellBarIndex:000}; OK"
                    );
                }
            }

            // Save
            if (execute)
            {
                Console.WriteLine("Saving changes...");
                context.SaveChanges();
                _log.Information("Fixed {RecordsFixed:N0} CharacterPropertiesSpellBar records.", numberOfRecordsFixed);
            }
            else
            {
                Console.WriteLine($"{numberOfRecordsFixed:N0} CharacterPropertiesSpellBar records need to be fixed!");
                Console.WriteLine("dry run completed. Use fix-spell-bars execute to actually run command");
            }
        }
    }
}
