using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Network;
using Microsoft.EntityFrameworkCore;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyLegendaryWieldLevel
{
    [CommandHandler(
        "verify-legendary-wield-level",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any items with legendary cantrips that have less than 180 wield level requirement"
    )]
    public static void HandleVerifyLegendaryWieldLevel(Session session, params string[] parameters)
    {
        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;

        using (var ctx = new ShardDbContext())
        {
            ctx.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            // get all biota spellbooks
            var spellbook = ctx.BiotaPropertiesSpellBook.Where(i => i.Probability == 2.0f).ToList();

            var legendaryItems = new HashSet<uint>();

            foreach (var spell in spellbook)
            {
                if (LootTables.LegendaryCantrips.Contains(spell.Spell))
                {
                    legendaryItems.Add(spell.ObjectId);
                }
            }

            // get wield requirements for these items
            var query =
                from wieldReq in ctx.BiotaPropertiesInt
                join wieldDiff in ctx.BiotaPropertiesInt on wieldReq.ObjectId equals wieldDiff.ObjectId
                where
                    wieldReq.Type.Equals((int)PropertyInt.WieldRequirements)
                    && wieldReq.Value.Equals((int)WieldRequirement.Level)
                    && wieldDiff.Type.Equals((int)PropertyInt.WieldDifficulty)
                    && legendaryItems.Contains(wieldReq.ObjectId)
                select new { WieldReq = wieldReq, WieldDiff = wieldDiff };

            var wieldReq1 = query.ToList();

            query =
                from wieldReq in ctx.BiotaPropertiesInt
                join wieldDiff in ctx.BiotaPropertiesInt on wieldReq.ObjectId equals wieldDiff.ObjectId
                where
                    wieldReq.Type.Equals((int)PropertyInt.WieldRequirements2)
                    && wieldReq.Value.Equals((int)WieldRequirement.Level)
                    && wieldDiff.Type.Equals((int)PropertyInt.WieldDifficulty2)
                    && legendaryItems.Contains(wieldReq.ObjectId)
                select new { WieldReq = wieldReq, WieldDiff = wieldDiff };

            var wieldReq2 = query.ToList();

            var verified = new HashSet<uint>();
            var updated = new HashSet<uint>();

            var hasLevelReq1 = new HashSet<uint>();
            var hasLevelReq2 = new HashSet<uint>();

            var updates = new List<string>();

            foreach (var wieldReq in wieldReq1)
            {
                hasLevelReq1.Add(wieldReq.WieldReq.ObjectId);

                if (wieldReq.WieldDiff.Value < 180)
                {
                    foundIssues = true;
                    updates.Add(
                        $"UPDATE biota_properties_int SET value=180 WHERE object_Id=0x{wieldReq.WieldDiff.ObjectId:X8} AND type={(int)PropertyInt.WieldDifficulty};"
                    );
                }
                else
                {
                    verified.Add(wieldReq.WieldDiff.ObjectId);
                }
            }

            foreach (var wieldReq in wieldReq2)
            {
                hasLevelReq2.Add(wieldReq.WieldReq.ObjectId);

                if (wieldReq.WieldDiff.Value < 180)
                {
                    foundIssues = true;
                    updates.Add(
                        $"UPDATE biota_properties_int SET value=180 WHERE object_Id=0x{wieldReq.WieldDiff.ObjectId:X8} AND type={(int)PropertyInt.WieldDifficulty2};"
                    );
                }
                else
                {
                    verified.Add(wieldReq.WieldDiff.ObjectId);
                }
            }

            /*var hasLevelReq = hasLevelReq1.Union(hasLevelReq2).ToList();

            if (hasLevelReq.Count != legendaryItems.Count)
            {
                foundIssues = true;
                var noReqs = legendaryItems.Except(hasLevelReq).ToList();

                foreach (var noReq in noReqs)
                {
                    if (!hasLevelReq1.Contains(noReq))
                    {
                        updates.Add($"INSERT INTO biota_properties_int SET object_Id=0x{noReq:X8}, `type`={(int)PropertyInt.WieldRequirements}, value={(int)WieldRequirement.Level};");
                        updates.Add($"INSERT INTO biota_properties_int SET object_Id=0x{noReq:X8}, `type`={(int)PropertyInt.WieldSkillType}, value=1;");
                        updates.Add($"INSERT INTO biota_properties_int SET object_Id=0x{noReq:X8}, `type`={(int)PropertyInt.WieldDifficulty}, value=180;");
                    }
                    else
                    {
                        updates.Add($"INSERT INTO biota_properties_int SET object_Id=0x{noReq:X8}, `type`={(int)PropertyInt.WieldRequirements2}, value={(int)WieldRequirement.Level};");
                        updates.Add($"INSERT INTO biota_properties_int SET object_Id=0x{noReq:X8}, `type`={(int)PropertyInt.WieldSkillType2}, value=1;");
                        updates.Add($"INSERT INTO biota_properties_int SET object_Id=0x{noReq:X8}, `type`={(int)PropertyInt.WieldDifficulty2}, value=180;");
                    }
                }
            }*/

            var numIssues = legendaryItems.Count - verified.Count;

            if (numIssues > 0)
            {
                Console.WriteLine($"Found issues for {numIssues:N0} of {legendaryItems.Count:N0} legendary items");
            }

            if (!fix && foundIssues)
            {
                Console.WriteLine($"Dry run completed. Type 'verify-legendary-wield-level fix' to fix any issues.");
            }

            if (fix)
            {
                foreach (var update in updates)
                {
                    Console.WriteLine(update);
                    ctx.Database.ExecuteSqlRaw(update);
                }
            }

            if (!foundIssues)
            {
                Console.WriteLine($"Verified wield levels for {legendaryItems.Count:N0} legendary items");
            }
        }
    }
}
