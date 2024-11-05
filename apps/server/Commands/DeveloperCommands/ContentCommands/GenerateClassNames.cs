using System.Collections.Generic;
using System.IO;
using System.Linq;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class GenerateClassNames
{
    [CommandHandler(
        "generate-classnames",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        "Generates WeenieClassName.cs from current world database"
    )]
    public static void HandleGenerateClassNames(Session session, params string[] parameters)
    {
        var lines = new List<string>();

        var replaceChars = new Dictionary<string, string>()
        {
            { " ", "_" },
            { "-", "_" },
            { "!", "" },
            { "#", "" },
            { "?", "" },
        };

        using (var ctx = new WorldDbContext())
        {
            var weenies = ctx.Weenie.OrderBy(i => i.ClassId);

            lines.Add("namespace ACE.Server.Factories.Enum");
            lines.Add("{");
            lines.Add("    public enum WeenieClassName");
            lines.Add("    {");
            lines.Add("        undef = 0,");

            foreach (var weenie in weenies)
            {
                var className = weenie.ClassName;

                foreach (var kvp in replaceChars)
                {
                    className = className.Replace(kvp.Key, kvp.Value);
                }

                if (className[0] >= '0' && className[0] <= '9')
                {
                    className = $"_{className}";
                }

                lines.Add($"        {className} = {weenie.ClassId},");
            }

            lines.Add("    }");
            lines.Add("}");
        }

        var filename = "WeenieClassName.cs";
        var sep = Path.DirectorySeparatorChar;
        var path = $"..{sep}..{sep}..{sep}..{sep}Factories{sep}Enum{sep}{filename}";
        if (!File.Exists(path))
        {
            path = filename;
        }

        File.WriteAllLines(path, lines);

        CommandHandlerHelper.WriteOutputInfo(session, $"Wrote {path}");
    }
}
