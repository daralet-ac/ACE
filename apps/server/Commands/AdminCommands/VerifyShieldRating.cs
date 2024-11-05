using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Microsoft.EntityFrameworkCore;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyShieldRating
{
    [CommandHandler(
        "verify-shield-rating",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any lootgen shields with incorrectly assigned CD/CDR"
    )]
    public static void HandleRemoveShieldRatings(Session session, params string[] parameters)
    {
        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;

        using (var ctx = new ShardDbContext())
        {
            ctx.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            // get items with GearCritDamage
            var critDamage = ctx
                .BiotaPropertiesInt.Where(i => i.Type == (ushort)PropertyInt.GearCritDamage)
                .ToDictionary(i => i.ObjectId, i => i.Value);

            // get items with GearCritDamageResist
            var critDamageResist = ctx
                .BiotaPropertiesInt.Where(i => i.Type == (ushort)PropertyInt.GearCritDamageResist)
                .ToDictionary(i => i.ObjectId, i => i.Value);

            // get lootgen shields
            var query =
                from biota in ctx.Biota
                join workmanship in ctx.BiotaPropertiesInt on biota.Id equals workmanship.ObjectId
                join combatUse in ctx.BiotaPropertiesInt on biota.Id equals combatUse.ObjectId
                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                where
                    workmanship.Type == (ushort)PropertyInt.ItemWorkmanship
                    && combatUse.Type == (ushort)PropertyInt.CombatUse
                    && combatUse.Value == (int)CombatUse.Shield
                    && name.Type == (ushort)PropertyString.Name
                select new { Biota = biota, Name = name };

            var results = query.ToDictionary(i => i.Biota.Id, i => i);

            // generate list of remove fields
            var critDamageRemove = new Dictionary<uint, int>();
            var critDamageResistRemove = new Dictionary<uint, int>();

            foreach (var result in results.Keys)
            {
                if (critDamage.TryGetValue(result, out var critDamageResult))
                {
                    critDamageRemove.Add(result, critDamageResult);
                }

                if (critDamageResist.TryGetValue(result, out var critDamageResistResult))
                {
                    critDamageResistRemove.Add(result, critDamageResistResult);
                }
            }

            var numIssues = critDamageRemove.Count + critDamageResistRemove.Count;

            var sqlLines = new List<string>();

            if (numIssues > 0)
            {
                foundIssues = true;

                Console.WriteLine($"Found {numIssues:N0} bugged shields:");

                foreach (var critDamageValue in critDamageRemove)
                {
                    var shield = results[critDamageValue.Key];

                    Console.WriteLine(
                        $"{shield.Biota.Id:X8} - {shield.Name.Value} (CD: {critDamageValue.Value}){fixStr}"
                    );

                    sqlLines.Add(
                        $"delete from biota_properties_int where object_Id=0x{shield.Biota.Id:X8} and `type`={(int)PropertyInt.GearCritDamage};"
                    );
                }

                foreach (var critDamageResistValue in critDamageResistRemove)
                {
                    var shield = results[critDamageResistValue.Key];

                    Console.WriteLine(
                        $"{shield.Biota.Id:X8} - {shield.Name.Value} (CDR: {critDamageResistValue.Value}){fixStr}"
                    );

                    sqlLines.Add(
                        $"delete from biota_properties_int where object_Id=0x{shield.Biota.Id:X8} and `type`={(int)PropertyInt.GearCritDamageResist};"
                    );
                }
            }

            if (!fix && foundIssues)
            {
                Console.WriteLine($"Dry run completed. Type 'verify-shield-rating fix' to fix any issues.");
            }

            if (fix)
            {
                foreach (var sqlLine in sqlLines)
                {
                    ctx.Database.ExecuteSqlRaw(sqlLine);
                }
            }

            if (!foundIssues)
            {
                Console.WriteLine($"Verified {results.Count:N0} shields");
            }
        }
    }
}
