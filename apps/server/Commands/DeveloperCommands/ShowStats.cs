using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class ShowStats
{
    [CommandHandler(
        "showstats",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Shows a list of a creature's current attribute/skill levels",
        "showstats"
    )]
    public static void HandleShowStats(Session session, params string[] parameters)
    {
        // get the last appraised object
        var item = CommandHandlerHelper.GetLastAppraisedObject(session) as Creature;
        if (item == null)
        {
            session.Player.SendMessage("ERROR: You must appraise a creature or player to use this function.");
            return;
        }

        var creature = (Creature)item;
        var output = "Strength: " + creature.Strength.Current;
        output += "\nEndurance: " + creature.Endurance.Current;
        output += "\nCoordination: " + creature.Coordination.Current;
        output += "\nQuickness: " + creature.Quickness.Current;
        output += "\nFocus: " + creature.Focus.Current;
        output += "\nSelf: " + creature.Self.Current;

        output += "\n\nHealth: " + creature.Health.Current + "/" + creature.Health.MaxValue;
        output += "\nStamina: " + creature.Stamina.Current + "/" + creature.Stamina.MaxValue;
        output += "\nMana: " + creature.Mana.Current + "/" + creature.Mana.MaxValue;

        var specialized = creature
            .Skills.Values.Where(s => s.AdvancementClass == SkillAdvancementClass.Specialized)
            .OrderBy(s => s.Skill.ToString());
        var trained = creature
            .Skills.Values.Where(s => s.AdvancementClass == SkillAdvancementClass.Trained)
            .OrderBy(s => s.Skill.ToString());
        var untrained = creature
            .Skills.Values.Where(s => s.AdvancementClass == SkillAdvancementClass.Untrained && s.IsUsable)
            .OrderBy(s => s.Skill.ToString());
        var unusable = creature
            .Skills.Values.Where(s => s.AdvancementClass == SkillAdvancementClass.Untrained && !s.IsUsable)
            .OrderBy(s => s.Skill.ToString());

        if (specialized.Count() > 0)
        {
            output += "\n\n== Specialized ==";
            foreach (var skill in specialized)
            {
                output += "\n" + skill.Skill + ": " + skill.Current;
            }
        }

        if (trained.Count() > 0)
        {
            output += "\n\n== Trained ==";
            foreach (var skill in trained)
            {
                output += "\n" + skill.Skill + ": " + skill.Current;
            }
        }

        if (untrained.Count() > 0)
        {
            output += "\n\n== Untrained ==";
            foreach (var skill in untrained)
            {
                output += "\n" + skill.Skill + ": " + skill.Current;
            }
        }

        if (unusable.Count() > 0)
        {
            output += "\n\n== Unusable ==";
            foreach (var skill in unusable)
            {
                output += "\n" + skill.Skill + ": " + skill.Current;
            }
        }

        session.Player.SendMessage(output);
    }
}
