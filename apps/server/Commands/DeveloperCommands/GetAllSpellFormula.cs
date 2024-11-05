using System;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class GetAllSpellFormula
{
    /// <summary>
    /// Debug console command to test the GetSpellFormula function.
    /// </summary>
    [CommandHandler(
        "getallspellformula",
        AccessLevel.Developer,
        CommandHandlerFlag.ConsoleInvoke,
        0,
        "Tests spell formula calculation"
    )]
    public static void HandleGetAllSpellFormula(Session session, params string[] parameters)
    {
        if (parameters?.Length != 1)
        {
            Console.WriteLine("getallspellformula <accountname>");
            return;
        }

        var spellTable = DatManager.PortalDat.SpellTable;
        var comps = DatManager.PortalDat.SpellComponentsTable;

        foreach (var entry in spellTable.Spells)
        {
            var spellid = entry.Key;
            Console.WriteLine("Formula for " + spellTable.Spells[spellid].Name + " (" + spellid + ")");

            var formula = SpellTable.GetSpellFormula(DatManager.PortalDat.SpellTable, spellid, parameters[0]);

            for (var i = 0; i < formula.Count; i++)
            {
                Console.WriteLine("Comp " + i + ": " + comps.SpellComponents[formula[i]].Name);
            }

            Console.WriteLine();
        }
    }
}
