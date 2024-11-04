using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class DebugSpellBook
{
    [CommandHandler(
        "debugspellbook",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows the spellbook for the last appraised object"
    )]
    public static void HandleDebugSpellbook(Session session, params string[] parameters)
    {
        var creature = CommandHandlerHelper.GetLastAppraisedObject(session) as Creature;

        if (creature == null || creature.Biota.PropertiesSpellBook == null)
        {
            return;
        }

        var lines = new List<string>();

        foreach (var entry in creature.Biota.PropertiesSpellBook)
        {
            lines.Add($"{(SpellId)entry.Key} - {entry.Value}");
        }

        CommandHandlerHelper.WriteOutputInfo(session, string.Join('\n', lines));
    }
}
