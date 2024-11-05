using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class AddSpell
{
    // add <spell>
    [CommandHandler(
        "addspell",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Adds the specified spell to your own spellbook.",
        "<spellid>"
    )]
    public static void HandleAddSpell(Session session, params string[] parameters)
    {
        if (Enum.TryParse(parameters[0], true, out SpellId spellId))
        {
            if (Enum.IsDefined(typeof(SpellId), spellId))
            {
                session.Player.LearnSpellWithNetworking((uint)spellId);
            }
        }
    }
}
