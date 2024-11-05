using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using Microsoft.EntityFrameworkCore;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyArmorLevels
{
    [CommandHandler(
        "verify-armor-levels",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any existing armor levels above AL cap"
    )]
    public static void HandleFixArmorLevel(Session session, params string[] parameters)
    {
        Console.WriteLine($"Fetching shard armors (this may take awhile on large servers) ...");

        var resistMagic = GetResistMagic();
        var tinkerLogs = GetTinkerLogs();
        var numTimesTinkered = GetNumTimesTinkered();
        var imbuedEffects = GetImbuedEffect();

        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";

        // get all loot-generated items on server with armor level
        using (var ctx = new ShardDbContext())
        {
            ctx.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            var query =
                from armor in ctx.BiotaPropertiesInt
                join workmanship in ctx.BiotaPropertiesInt on armor.ObjectId equals workmanship.ObjectId
                join name in ctx.BiotaPropertiesString on armor.ObjectId equals name.ObjectId
                join validLocations in ctx.BiotaPropertiesInt on armor.ObjectId equals validLocations.ObjectId
                where
                    armor.Type == (int)PropertyInt.ArmorLevel
                    && workmanship.Type == (int)PropertyInt.ItemWorkmanship
                    && name.Type == (int)PropertyString.Name
                    && validLocations.Type == (int)PropertyInt.ValidLocations
                orderby armor.Value descending
                select new
                {
                    Guid = armor.ObjectId,
                    ArmorLevel = armor.Value,
                    Name = name.Value,
                    ValidLocs = validLocations.Value,
                    Armor = armor
                };

            var armorItems = query.ToList();

            var adjusted = 0;

            foreach (var armorItem in armorItems)
            {
                // ignore unenchantable
                if (resistMagic.TryGetValue(armorItem.Guid, out var resist) && resist == 9999)
                {
                    continue;
                }

                TinkerLog tinkerLog = null;
                if (tinkerLogs.TryGetValue(armorItem.Guid, out var _tinkerLog))
                {
                    tinkerLog = new TinkerLog(_tinkerLog);
                }

                numTimesTinkered.TryGetValue(armorItem.Guid, out var numTinkers);
                imbuedEffects.TryGetValue(armorItem.Guid, out var imbuedEffect);

                var numArmorTinkers = tinkerLog != null ? tinkerLog.NumTinkers(MaterialType.Steel) : 0;

                var numArmorTinkerStr = numArmorTinkers > 0 ? $" ({numArmorTinkers})" : "";

                var equipMask = (EquipMask)armorItem.ValidLocs;

                var newArmorLevel = GetArmorLevel(armorItem.ArmorLevel, equipMask, tinkerLog, numTinkers, imbuedEffect);

                if (newArmorLevel != armorItem.ArmorLevel)
                {
                    if (fix)
                    {
                        armorItem.Armor.Value = newArmorLevel;
                    }

                    Console.WriteLine(
                        $"{armorItem.Name}, {armorItem.ArmorLevel}{numArmorTinkerStr} => {newArmorLevel}{fixStr}"
                    );

                    adjusted++;
                }
            }
            if (fix)
            {
                ctx.SaveChanges();
            }

            var willBe = fix ? " " : " will be ";

            if (adjusted > 0)
            {
                Console.WriteLine($"Found {armorItems.Count:N0} armors, {adjusted:N0}{willBe}adjusted");

                if (!fix)
                {
                    Console.WriteLine($"Dry run completed. Type 'verify-armor-levels fix' to fix any issues.");
                }
            }
            else
            {
                Console.WriteLine($"Verified {armorItems.Count:N0} armors.");
            }
        }
    }

    public static Dictionary<uint, int> GetResistMagic()
    {
        using (var ctx = new ShardDbContext())
        {
            var resistMagic = ctx
                .BiotaPropertiesInt.Where(i => i.Type == (int)PropertyInt.ResistMagic)
                .ToDictionary(i => i.ObjectId, i => i.Value);

            return resistMagic;
        }
    }

    public static Dictionary<uint, string> GetTinkerLogs()
    {
        using (var ctx = new ShardDbContext())
        {
            var tinkerLogs = ctx
                .BiotaPropertiesString.Where(i => i.Type == (int)PropertyString.TinkerLog)
                .ToDictionary(i => i.ObjectId, i => i.Value);

            return tinkerLogs;
        }
    }

    public static Dictionary<uint, int> GetNumTimesTinkered()
    {
        using (var ctx = new ShardDbContext())
        {
            var numTimesTinkered = ctx
                .BiotaPropertiesInt.Where(i => i.Type == (int)PropertyInt.NumTimesTinkered)
                .ToDictionary(i => i.ObjectId, i => i.Value);

            return numTimesTinkered;
        }
    }

    public static Dictionary<uint, int> GetImbuedEffect()
    {
        using (var ctx = new ShardDbContext())
        {
            var imbuedEffect = ctx
                .BiotaPropertiesInt.Where(i => i.Type == (int)PropertyInt.ImbuedEffect)
                .ToDictionary(i => i.ObjectId, i => i.Value);

            return imbuedEffect;
        }
    }

    // head / hands / feet
    public const int MaxArmorLevel_Extremity = 345;

    // everything else
    public const int MaxArmorLevel_NonExtremity = 315;

    public static int GetArmorLevel(
        int armorLevel,
        EquipMask equipMask,
        TinkerLog tinkerLog,
        int numTinkers,
        int imbuedEffect
    )
    {
        var maxArmorLevel =
            (equipMask & EquipMask.Extremity) != 0 ? MaxArmorLevel_Extremity : MaxArmorLevel_NonExtremity;

        if (tinkerLog != null && tinkerLog.Tinkers.Count == numTinkers)
        {
            // full tinkering log available
            maxArmorLevel += tinkerLog.NumTinkers(MaterialType.Steel) * 20;
        }
        else if (numTinkers > 0)
        {
            // partial or no tinkering log available
            var rngMax = numTinkers;
            if (imbuedEffect != 0)
            {
                rngMax--;
            }

            if (rngMax > 0)
            {
                // prevent further iterations on multiple re-runs
                if (armorLevel <= maxArmorLevel + rngMax * 20)
                {
                    return armorLevel;
                }

                var rng = ThreadSafeRandom.Next(0, rngMax);
                maxArmorLevel += rng * 20;
            }
        }
        return Math.Min(armorLevel, maxArmorLevel);
    }
}
