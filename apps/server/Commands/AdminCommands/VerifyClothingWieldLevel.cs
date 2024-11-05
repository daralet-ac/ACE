using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Network;
using Microsoft.EntityFrameworkCore;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyClothingWieldLevel
{
    [CommandHandler(
        "verify-clothing-wield-level",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any t7/t8 clothing that is missing a wield level requirement"
    )]
    public static void HandleVerifyClothingWieldLevel(Session session, params string[] parameters)
    {
        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;

        using (var ctx = new ShardDbContext())
        {
            ctx.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            // get all shard clothing
            var _clothing = ctx.Biota.Where(i => i.WeenieType == (int)WeenieType.Clothing).ToList();

            // get all shard armor levels
            var armorLevels = ctx
                .BiotaPropertiesInt.Where(i => i.Type == (ushort)PropertyInt.ArmorLevel)
                .ToDictionary(i => i.ObjectId, i => i.Value);

            // filter clothing to actual clothing
            var clothing = new Dictionary<uint, Database.Models.Shard.Biota>();
            foreach (var item in _clothing)
            {
                if (!armorLevels.TryGetValue(item.Id, out var armorLevel) || armorLevel == 0)
                {
                    clothing.Add(item.Id, item);
                    //Console.WriteLine($"{item.Id:X8} - {(Factories.Enum.WeenieClassName)item.WeenieClassId}");
                }
            }

            // get shard spells
            var _spells = ctx
                .BiotaPropertiesSpellBook.AsEnumerable()
                .Where(i => clothing.ContainsKey(i.ObjectId))
                .ToList();

            // filter clothing to those with epics/legendaries
            var highTierClothing = new Dictionary<uint, int>();

            foreach (var s in _spells)
            {
                var cantripLevel = 0;

                if (LootTables.EpicCantrips.Contains(s.Spell))
                {
                    cantripLevel = 3;
                }
                else if (LootTables.LegendaryCantrips.Contains(s.Spell))
                {
                    cantripLevel = 4;
                }

                if (cantripLevel == 0)
                {
                    continue;
                }

                if (highTierClothing.ContainsKey(s.ObjectId))
                {
                    highTierClothing[s.ObjectId] = Math.Max(highTierClothing[s.ObjectId], cantripLevel);
                }
                else
                {
                    highTierClothing[s.ObjectId] = cantripLevel;
                }
            }

            // get wield level for these items
            var wieldLevels = ctx
                .BiotaPropertiesInt.AsEnumerable()
                .Where(i => i.Type == (ushort)PropertyInt.WieldDifficulty && highTierClothing.ContainsKey(i.ObjectId))
                .Select(i => i.ObjectId)
                .ToHashSet();

            foreach (var kvp in highTierClothing)
            {
                var objectId = kvp.Key;
                var maxCantripLevel = kvp.Value;

                if (wieldLevels.Contains(objectId))
                {
                    continue;
                }

                if (!foundIssues)
                {
                    Console.WriteLine($"Missing wield difficulty:");
                    foundIssues = true;
                }

                if (fix)
                {
                    var wieldLevel = 150;

                    if (maxCantripLevel > 3)
                    {
                        var rng = ThreadSafeRandom.Next(0.0f, 1.0f);
                        if (rng < 0.9f)
                        {
                            wieldLevel = 180;
                        }
                    }

                    ctx.Database.ExecuteSqlInterpolated(
                        $"insert into biota_properties_int set object_Id={objectId}, `type`={(ushort)PropertyInt.WieldRequirements}, value={(int)WieldRequirement.Level};"
                    );
                    ctx.Database.ExecuteSqlInterpolated(
                        $"insert into biota_properties_int set object_Id={objectId}, `type`={(ushort)PropertyInt.WieldSkillType}, value=1;"
                    );
                    ctx.Database.ExecuteSqlInterpolated(
                        $"insert into biota_properties_int set object_Id={objectId}, `type`={(ushort)PropertyInt.WieldDifficulty}, value={wieldLevel};"
                    );
                }

                var item = clothing[objectId];

                Console.WriteLine(
                    $"{item.Id:X8} - {(Factories.Enum.WeenieClassName)item.WeenieClassId} - {maxCantripLevel}{fixStr}"
                );
            }

            if (!fix && foundIssues)
            {
                Console.WriteLine($"Dry run completed. Type 'verify-clothing-wield-level fix' to fix any issues.");
            }

            if (!foundIssues)
            {
                Console.WriteLine($"Verified wield levels for {highTierClothing.Count:N0} pieces of t7 / t8 clothing");
            }
        }
    }
}
