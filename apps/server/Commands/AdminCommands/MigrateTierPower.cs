using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ACE.Database;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;
using Microsoft.EntityFrameworkCore;

namespace ACE.Server.Commands.AdminCommands;

public class MigrateTierPower
{
    [CommandHandler(
        "migrate-tier-power",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        0,
        "Moves items rolled before the tier rework onto the new scale. Dry run unless 'fix' is given.",
        "[fix]"
    )]
    public static void HandleMigrateTierPower(Session session, params string[] parameters)
    {
        var fix = parameters.Length > 0 && parameters[0].Equals("fix", StringComparison.OrdinalIgnoreCase);

        // an online player's items are saved from memory, which would undo the migration
        if (fix && PlayerManager.GetAllOnline().Count > 0)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "migrate-tier-power fix must be run with 0 players connected (ie. with the world closed)."
            );
            return;
        }

        CommandHandlerHelper.WriteOutputInfo(session, "Finding items rolled before the tier rework (this may take awhile on large servers) ...");

        var ids = GetCandidateIds();

        CommandHandlerHelper.WriteOutputInfo(session, $"Found {ids.Count:N0} candidate items");

        var logPath = Path.Combine(AppContext.BaseDirectory, $"tier-power-migration-{DateTime.Now:yyyyMMdd-HHmmss}{(fix ? "" : "-dryrun")}.log");
        var migrated = 0;
        var failed = 0;

        using (var log = new StreamWriter(logPath))
        {
            foreach (var id in ids)
            {
                try
                {
                    var biota = DatabaseManager.Shard.BaseDatabase.GetBiota(id, true);

                    if (biota == null)
                    {
                        continue;
                    }

                    var wo = WorldObjectFactory.CreateWorldObject(biota);

                    if (wo == null || wo is Creature)
                    {
                        continue;
                    }

                    var changes = new List<string>();

                    if (!LootGenerationFactory.MigrateToTierPower(wo, changes))
                    {
                        continue;
                    }

                    log.WriteLine($"0x{id:X8} {wo.Name} ({wo.WeenieClassId}), tier {LootGenerationFactory.GetTierPowerMigrationTier(wo)}: {string.Join(", ", changes)}");

                    if (fix && !DatabaseManager.Shard.BaseDatabase.SaveBiota(wo.Biota, wo.BiotaDatabaseLock, true))
                    {
                        log.WriteLine($"0x{id:X8} - failed to save");
                        failed++;
                        continue;
                    }

                    migrated++;
                }
                catch (Exception ex)
                {
                    log.WriteLine($"0x{id:X8} - {ex.Message}");
                    failed++;
                }
            }
        }

        var willBe = fix ? "" : " will be";

        CommandHandlerHelper.WriteOutputInfo(session, $"{migrated:N0} items{willBe} migrated, {failed:N0} failed. Log: {logPath}");

        if (!fix)
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Dry run completed. Type 'migrate-tier-power fix' with the world closed to migrate.");
        }
    }

    /// <summary>
    /// Loot items of tier 2 and up, rolled quest items and ammo, that haven't been migrated
    /// </summary>
    private static List<uint> GetCandidateIds()
    {
        using (var ctx = new ShardDbContext())
        {
            ctx.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            var tiered = ctx
                .BiotaPropertiesInt.Where(i => i.Type == (ushort)PropertyInt.Tier && i.Value >= 2)
                .Select(i => i.ObjectId)
                .ToList();

            var questItems = ctx
                .BiotaPropertiesFloat.Where(i => i.Type == (ushort)PropertyFloat.QuestItemRollQuality)
                .Select(i => i.ObjectId)
                .ToList();

            var ammo = ctx.Biota.Where(i => i.WeenieType == (int)WeenieType.Ammunition).Select(i => i.Id).ToList();

            var done = ctx
                .BiotaPropertiesBool.Where(i => i.Type == (ushort)PropertyBool.TierPowerMigrated && i.Value)
                .Select(i => i.ObjectId)
                .ToHashSet();

            return tiered.Concat(questItems).Concat(ammo).Distinct().Where(i => !done.Contains(i)).OrderBy(i => i).ToList();
        }
    }
}
