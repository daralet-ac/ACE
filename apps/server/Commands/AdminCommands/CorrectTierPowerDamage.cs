using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class CorrectTierPowerDamage
{
    [CommandHandler(
        "correct-tier-power-damage",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        1,
        "Puts back the weapon damage migrate-tier-power placed too low, from an export of the shard backup taken before it "
            + "(Database/Optional/Shard/2026-11-03-00-Tier-Power-Correction-Export.sql). Dry run unless 'fix' is given.",
        "<export file> [fix]"
    )]
    public static void HandleCorrectTierPowerDamage(Session session, params string[] parameters)
    {
        var path = parameters[0];
        var fix = parameters.Length > 1 && parameters[1].Equals("fix", StringComparison.OrdinalIgnoreCase);

        if (!File.Exists(path))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Can't find {path}");
            return;
        }

        // an online player's items are saved from memory, which would undo the correction
        if (fix && PlayerManager.GetAllOnline().Count > 0)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "correct-tier-power-damage fix must be run with 0 players connected (ie. with the world closed)."
            );
            return;
        }

        List<LootGenerationFactory.TierPowerBackupWeapon> weapons;
        try
        {
            weapons = ReadExport(path);
        }
        catch (Exception ex)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Couldn't read {path}: {ex.Message}");
            return;
        }

        CommandHandlerHelper.WriteOutputInfo(session, $"Read {weapons.Count:N0} weapons from the backup, checking them (this may take awhile) ...");

        // next to the export, so it survives the server container being recreated
        var logPath = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(path)) ?? AppContext.BaseDirectory,
            $"tier-power-correction-{DateTime.Now:yyyyMMdd-HHmmss}{(fix ? "" : "-dryrun")}.log"
        );

        var results = new Dictionary<LootGenerationFactory.TierPowerCorrection, int>();
        var missing = 0;
        var failed = 0;

        using (var log = new StreamWriter(logPath))
        {
            foreach (var backup in weapons)
            {
                try
                {
                    var biota = DatabaseManager.Shard.BaseDatabase.GetBiota(backup.Id, true);

                    if (biota == null)
                    {
                        missing++;
                        continue;
                    }

                    var wo = WorldObjectFactory.CreateWorldObject(biota);

                    if (wo == null || wo is Creature)
                    {
                        missing++;
                        continue;
                    }

                    var changes = new List<string>();
                    var result = LootGenerationFactory.CorrectTierPowerDamage(wo, backup, changes);
                    results[result] = results.GetValueOrDefault(result) + 1;

                    if (changes.Count > 0)
                    {
                        log.WriteLine($"{result,-21} 0x{backup.Id:X8} {wo.Name} ({wo.WeenieClassId}), tier {backup.Tier}: {string.Join(", ", changes)}");
                    }

                    if (result != LootGenerationFactory.TierPowerCorrection.Corrected || !fix)
                    {
                        continue;
                    }

                    if (!DatabaseManager.Shard.BaseDatabase.SaveBiota(wo.Biota, wo.BiotaDatabaseLock, true))
                    {
                        log.WriteLine($"0x{backup.Id:X8} - failed to save");
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    log.WriteLine($"0x{backup.Id:X8} - {ex.Message}");
                    failed++;
                }
            }

            log.WriteLine();
            log.WriteLine(Summarize(results, missing, failed, fix));
        }

        CommandHandlerHelper.WriteOutputInfo(session, Summarize(results, missing, failed, fix));
        CommandHandlerHelper.WriteOutputInfo(session, $"Log: {logPath}");

        if (!fix)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Dry run completed. Type 'correct-tier-power-damage {path} fix' with the world closed to correct them."
            );
        }
    }

    private static string Summarize(
        Dictionary<LootGenerationFactory.TierPowerCorrection, int> results,
        int missing,
        int failed,
        bool fix
    )
    {
        var counts = string.Join(", ", results.OrderBy(r => r.Key).Select(r => $"{r.Key} {r.Value:N0}"));

        return $"{(fix ? "Done" : "Dry run")}: {counts}, no longer exists {missing:N0}, failed {failed:N0}";
    }

    /// <summary>
    /// The export's rows, tab separated with a header (mariadb -B): id, wcid, tier, damage, base_damage, damage_mod,
    /// base_damage_mod, created (Unix time), NULL where the backup has none
    /// </summary>
    internal static List<LootGenerationFactory.TierPowerBackupWeapon> ReadExport(string path)
    {
        var weapons = new List<LootGenerationFactory.TierPowerBackupWeapon>();

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var columns = line.Split('\t');
            if (columns.Length < 8)
            {
                throw new FormatException($"expected 8 columns (export it again with the latest query): {line}");
            }

            weapons.Add(
                new LootGenerationFactory.TierPowerBackupWeapon(
                    uint.Parse(columns[0], CultureInfo.InvariantCulture),
                    uint.Parse(columns[1], CultureInfo.InvariantCulture),
                    int.Parse(columns[2], CultureInfo.InvariantCulture),
                    ParseInt(columns[3]),
                    ParseInt(columns[4]),
                    ParseDouble(columns[5]),
                    ParseDouble(columns[6]),
                    ParseInt(columns[7])
                )
            );
        }

        return weapons;
    }

    private static int? ParseInt(string value)
    {
        return value == "NULL" || value.Length == 0 ? null : int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static double? ParseDouble(string value)
    {
        return value == "NULL" || value.Length == 0 ? null : double.Parse(value, CultureInfo.InvariantCulture);
    }
}
