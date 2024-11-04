using System;
using System.Globalization;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class MonsterSpell
{
    [CommandHandler(
        "monsterspell",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "The last appraised creature casts a spell. For targeted spells, defaults to the current player.",
        "optional target guid"
    )]
    public static void HandleMonsterProj(Session session, params string[] parameters)
    {
        if (!Enum.TryParse(parameters[0], out SpellId spellId))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Invalid SpellId {spellId}");
            return;
        }
        var spell = new Spell(spellId);
        if (spell.NotFound)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Couldn't find SpellId {spellId}");
            return;
        }

        var attackTarget = session.Player as Creature;

        if (parameters.Length > 1)
        {
            if (!uint.TryParse(parameters[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var targetGuid))
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Invalid target guid: {parameters[1]}");
                return;
            }

            attackTarget = session.Player.FindObject(targetGuid, Player.SearchLocations.Landblock) as Creature;

            if (attackTarget == null)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Couldn't find attack target {targetGuid:X8}");
                return;
            }
        }
        var monster = CommandHandlerHelper.GetLastAppraisedObject(session) as Creature;

        if (monster == null)
        {
            return;
        }

        var prevAttackTarget = monster.AttackTarget;
        monster.AttackTarget = attackTarget;

        monster.CastSpell(spell);

        monster.AttackTarget = prevAttackTarget;
    }
}
