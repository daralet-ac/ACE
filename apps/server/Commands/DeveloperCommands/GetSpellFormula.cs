using System;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class GetSpellFormula
{
    /// <summary>
    /// Debug console command to test the GetSpellFormula function.
    /// </summary>
    [CommandHandler(
        "getspellformula",
        AccessLevel.Developer,
        CommandHandlerFlag.ConsoleInvoke,
        0,
        "Tests spell formula calculation"
    )]
    public static void HandleGetSpellFormula(Session session, params string[] parameters)
    {
        if (parameters?.Length != 2)
        {
            Console.WriteLine("getspellformula <accountname> <spellid>");
            return;
        }

        if (!uint.TryParse(parameters[1], out var spellid))
        {
            Console.WriteLine("getspellformula <accountname> <spellid>");
            return;
        }

        var spellTable = DatManager.PortalDat.SpellTable;
        var comps = DatManager.PortalDat.SpellComponentsTable;

        Console.WriteLine("Formula for " + spellTable.Spells[spellid].Name);
        Console.WriteLine(
            "Spell Words: " + spellTable.Spells[spellid].GetSpellWords(DatManager.PortalDat.SpellComponentsTable)
        );
        Console.WriteLine(spellTable.Spells[spellid].Desc);

        var formula = SpellTable.GetSpellFormula(DatManager.PortalDat.SpellTable, spellid, parameters[0]);

        for (var i = 0; i < formula.Count; i++)
        {
            if (comps.SpellComponents.ContainsKey(formula[i]))
            {
                Console.WriteLine("Comp " + i + ": " + comps.SpellComponents[formula[i]].Name);
            }
            else
            {
                Console.WriteLine("Comp " + i + " : Unknown Component " + formula[i]);
            }
        }

        Console.WriteLine();
    }
}
