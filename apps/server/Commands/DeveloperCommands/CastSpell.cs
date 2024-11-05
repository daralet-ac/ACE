using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class CastSpell
{
    [CommandHandler(
        "castspell",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Casts a spell on the last appraised object",
        "spell id"
    )]
    public static void HandleCastSpell(Session session, params string[] parameters)
    {
        if (!uint.TryParse(parameters[0], out var spellId))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Invalid spell id {parameters[0]}");
            return;
        }

        var spell = new Spell(spellId);

        if (spell.NotFound)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Spell {spellId} not found");
            return;
        }

        WorldObject target = null;

        if (spell.NonComponentTargetType != ItemType.None)
        {
            target = CommandHandlerHelper.GetLastAppraisedObject(session);

            if (target == null)
            {
                return;
            }
        }

        session.Player.TryCastSpell(spell, target, tryResist: false);
    }
}
