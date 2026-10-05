using System.Collections.Generic;
using System.Text;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class QuestMutateSim
{
    [CommandHandler(
        "questmutatesim",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        1,
        "Rolls a quest item's pickup mutation many times and reports the spread of its main stats.",
        "<wcid> [rolls, default 1000] [wield requirement, default the item's own]"
    )]
    public static void HandleQuestMutateSim(Session session, params string[] parameters)
    {
        if (!uint.TryParse(parameters[0], out var wcid))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"{parameters[0]} isn't a wcid.");
            return;
        }

        var rolls = 1000;
        if (parameters.Length > 1 && (!int.TryParse(parameters[1], out rolls) || rolls < 1 || rolls > 100000))
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Rolls must be between 1 and 100000.");
            return;
        }

        int? requirement = null;
        if (parameters.Length > 2)
        {
            if (!int.TryParse(parameters[2], out var parsedRequirement))
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"{parameters[2]} isn't a wield requirement.");
                return;
            }

            requirement = parsedRequirement;
        }

        var results = new Dictionary<string, List<double>>();
        var authored = new Dictionary<string, double>();
        string name = null;
        var tier = 0;
        int? tinks = null;

        for (var i = 0; i < rolls; i++)
        {
            var wo = WorldObjectFactory.CreateNewWorldObject(wcid);

            if (wo == null)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Couldn't create {wcid}.");
                return;
            }

            if (!wo.MutableQuestItem)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"{wo.Name} ({wcid}) isn't a mutable quest item.");
                return;
            }

            if (requirement != null)
            {
                wo.WieldDifficulty = requirement;
            }

            if (i == 0)
            {
                name = wo.Name;
                tier = LootGenerationFactory.GetQuestItemTierIndex(wo) + 1;
                Record(authored, wo);
            }

            LootGenerationFactory.MutateQuestItem(wo);
            tinks = wo.QuestItemTinks;

            foreach (var (stat, value) in GetMainStats(wo))
            {
                if (!results.TryGetValue(stat, out var values))
                {
                    results[stat] = values = new List<double>(rolls);
                }

                values.Add(value);
            }
        }

        var report = new StringBuilder($"{name} ({wcid}) at tier {tier}, {rolls} rolls, {tinks ?? 0} tinks baked in:");

        if (results.Count == 0)
        {
            report.Append("\n  no main stats to roll");
        }

        foreach (var (stat, values) in results)
        {
            values.Sort();
            var start = authored.TryGetValue(stat, out var authoredValue) ? Format(stat, authoredValue) : "-";
            report.Append(
                $"\n  {stat}: authored {start} -> min {Format(stat, values[0])}, median {Format(stat, values[values.Count / 2])}, max {Format(stat, values[^1])}"
            );
        }

        CommandHandlerHelper.WriteOutputInfo(session, report.ToString());
    }

    private static void Record(Dictionary<string, double> stats, WorldObject wo)
    {
        foreach (var (stat, value) in GetMainStats(wo))
        {
            stats[stat] = value;
        }
    }

    private static IEnumerable<(string Stat, double Value)> GetMainStats(WorldObject wo)
    {
        if (wo.Damage != null)
        {
            yield return ("Damage", wo.Damage.Value);
        }

        if (wo.DamageMod != null)
        {
            yield return ("DamageMod", wo.DamageMod.Value);
        }

        if (wo.ElementalDamageMod != null)
        {
            yield return ("ElementalDamageMod", wo.ElementalDamageMod.Value);
        }

        if (wo.WeaponRestorationSpellsMod != null)
        {
            yield return ("RestorationMod", wo.WeaponRestorationSpellsMod.Value);
        }

        if (wo.ArmorLevel is > 0)
        {
            yield return ("ArmorLevel", wo.ArmorLevel.Value);
        }

        if (wo.WardLevel is > 0)
        {
            yield return ("WardLevel", wo.WardLevel.Value);
        }
    }

    private static string Format(string stat, double value)
    {
        return stat is "Damage" or "ArmorLevel" or "WardLevel" ? $"{value:0}" : $"{value:0.00}";
    }
}
