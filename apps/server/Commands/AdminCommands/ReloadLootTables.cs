using System.IO;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories.Entity;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class ReloadLootTables
{
    [CommandHandler(
        "reload-loot-tables",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        "reloads the latest data from the loot tables",
        "optional profile folder"
    )]
    public static void HandleReloadLootTables(Session session, params string[] parameters)
    {
        var sep = Path.DirectorySeparatorChar;

        var folder = $"..{sep}..{sep}..{sep}..{sep}Factories{sep}Tables{sep}";
        if (parameters.Length > 0)
        {
            folder = parameters[0];
        }

        var di = new DirectoryInfo(folder);

        if (!di.Exists)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"{folder} not found");
            return;
        }
        LootSwap.UpdateTables(folder);
    }
}
