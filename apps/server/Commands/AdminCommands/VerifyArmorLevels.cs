using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Network;
using Microsoft.EntityFrameworkCore;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyArmorLevels
{
    [CommandHandler(
        "verify-armor-levels",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any existing armor levels above the top of their tier's range, plus tinks"
    )]
    public static void HandleFixArmorLevel(Session session, params string[] parameters)
    {
        Console.WriteLine($"Fetching shard armors (this may take awhile on large servers) ...");

        var resistMagic = GetResistMagic();
        var numTimesTinkered = GetNumTimesTinkered();
        var imbuedEffects = GetImbuedEffect();
        var armorStyles = GetIntProperty(PropertyInt.ArmorStyle);
        var armorPatches = GetIntProperty(PropertyInt.ArmorPatchAmount);
        var tiers = GetIntProperty(PropertyInt.Tier);

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

                numTimesTinkered.TryGetValue(armorItem.Guid, out var numTinkers);
                imbuedEffects.TryGetValue(armorItem.Guid, out var imbuedEffect);

                armorStyles.TryGetValue(armorItem.Guid, out var armorStyle);
                armorPatches.TryGetValue(armorItem.Guid, out var armorPatch);
                var tier = tiers.TryGetValue(armorItem.Guid, out var itemTier) ? itemTier : 8;

                var numArmorTinkerStr = numTinkers > 0 ? $" ({numTinkers})" : "";

                var equipMask = (EquipMask)armorItem.ValidLocs;

                var newArmorLevel = GetArmorLevel(
                    armorItem.ArmorLevel,
                    equipMask,
                    armorStyle,
                    tier,
                    armorPatch,
                    numTinkers,
                    imbuedEffect
                );

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

    public static Dictionary<uint, int> GetIntProperty(PropertyInt property)
    {
        using (var ctx = new ShardDbContext())
        {
            return ctx
                .BiotaPropertiesInt.Where(i => i.Type == (int)property)
                .ToDictionary(i => i.ObjectId, i => i.Value);
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

    /// <summary>
    /// The most Armor Level a tink can add: the largest share of the untinkered Armor Level any material adds
    /// </summary>
    public const double MaxArmorTinkPercent = 0.10;

    /// <summary>
    /// Caps an item's Armor Level at the top of its style's range in its tier (TierPower; T8 for items with no tier),
    /// plus the most each tink could add (a share of that top, never less than 1) and any armor patch. Every tink but
    /// an imbue counts as an armor tink, so a capped item is never lowered below what tinkering could have made it.
    /// </summary>
    public static int GetArmorLevel(
        int armorLevel,
        EquipMask equipMask,
        int armorStyle,
        int tier,
        int armorPatch,
        int numTinkers,
        int imbuedEffect
    )
    {
        var baseArmorLevel = LootTables.GetArmorStyleBaseArmorLevel(armorStyle == 0 ? null : armorStyle);
        var (_, topOfTier) = (equipMask & EquipMask.Shield) != 0
            ? TierPower.GetShieldArmorRollRange(baseArmorLevel, tier)
            : TierPower.GetArmorRollRange(baseArmorLevel, tier);

        var armorTinks = Math.Max(0, imbuedEffect != 0 ? numTinkers - 1 : numTinkers);
        var maxTinkBonus = Math.Max(1, (int)(topOfTier * MaxArmorTinkPercent));
        var maxArmorLevel = (int)topOfTier + armorTinks * maxTinkBonus + armorPatch;

        return Math.Min(armorLevel, maxArmorLevel);
    }
}
