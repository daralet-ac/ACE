using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Builds the character stats text shown at the bottom of an owned Combat Focus appraisal.
/// Values are totals from equipped gear and active buffs.
/// </summary>
partial class Player
{
    private enum StatsSheetJewelValue
    {
        Flat,
        Percent,
        UpToPercent,
        Protection
    }

    private static readonly (PropertyInt Rating, StatsSheetJewelValue Value, string Effect, string SecondaryEffect)[] StatsSheetJewelEffects =
    [
        (PropertyInt.GearStrength, StatsSheetJewelValue.Flat, "Strength", null),
        (PropertyInt.GearEndurance, StatsSheetJewelValue.Flat, "Endurance", null),
        (PropertyInt.GearCoordination, StatsSheetJewelValue.Flat, "Coordination", null),
        (PropertyInt.GearQuickness, StatsSheetJewelValue.Flat, "Quickness", null),
        (PropertyInt.GearFocus, StatsSheetJewelValue.Flat, "Focus", null),
        (PropertyInt.GearSelf, StatsSheetJewelValue.Flat, "Self", null),
        (PropertyInt.GearToughness, StatsSheetJewelValue.Flat, "Physical Defense", null),
        (PropertyInt.GearResistance, StatsSheetJewelValue.Flat, "Magic Defense", null),

        (PropertyInt.GearSlashBane, StatsSheetJewelValue.Protection, "slashing protection on armor", null),
        (PropertyInt.GearPierceBane, StatsSheetJewelValue.Protection, "piercing protection on armor", null),
        (PropertyInt.GearBludgeonBane, StatsSheetJewelValue.Protection, "bludgeoning protection on armor", null),
        (PropertyInt.GearFireBane, StatsSheetJewelValue.Protection, "fire protection on armor", null),
        (PropertyInt.GearFrostBane, StatsSheetJewelValue.Protection, "cold protection on armor", null),
        (PropertyInt.GearAcidBane, StatsSheetJewelValue.Protection, "acid protection on armor", null),
        (PropertyInt.GearLightningBane, StatsSheetJewelValue.Protection, "lightning protection on armor", null),

        (PropertyInt.GearPhysicalWard, StatsSheetJewelValue.Percent, "reduced physical damage taken", null),
        (PropertyInt.GearElementalWard, StatsSheetJewelValue.Percent, "reduced elemental damage taken", null),
        (PropertyInt.GearHardenedDefense, StatsSheetJewelValue.UpToPercent, "reduced physical damage taken", null),
        (PropertyInt.GearNullification, StatsSheetJewelValue.UpToPercent, "reduced magic damage taken", null),
        (PropertyInt.GearBlock, StatsSheetJewelValue.Percent, "block chance", null),
        (PropertyInt.GearThorns, StatsSheetJewelValue.Percent, "of blocked damage reflected", null),
        (PropertyInt.GearReprisal, StatsSheetJewelValue.Percent, "chance to evade critical hits", null),
        (PropertyInt.GearFamiliarity, StatsSheetJewelValue.UpToPercent, "evade and resist chance vs. your target", null),
        (PropertyInt.GearHealthToStamina, StatsSheetJewelValue.Percent, "chance to gain damage taken as stamina", null),
        (PropertyInt.GearHealthToMana, StatsSheetJewelValue.Percent, "chance to gain damage taken as mana", null),

        (PropertyInt.GearSelfHarm, StatsSheetJewelValue.Percent, "increased damage", null),
        (PropertyInt.GearBravado, StatsSheetJewelValue.UpToPercent, "attack skill vs. your target", null),
        (PropertyInt.GearRedFury, StatsSheetJewelValue.UpToPercent, "damage as health drops", null),
        (PropertyInt.GearYellowFury, StatsSheetJewelValue.UpToPercent, "physical damage as stamina drops", null),
        (PropertyInt.GearBlueFury, StatsSheetJewelValue.UpToPercent, "magic damage as mana drops", null),
        (PropertyInt.GearBludgeon, StatsSheetJewelValue.UpToPercent, "critical damage", null),
        (PropertyInt.GearPierce, StatsSheetJewelValue.UpToPercent, "piercing resistance penetration", null),
        (PropertyInt.GearSlash, StatsSheetJewelValue.Percent, "chance to cleave an extra target", null),
        (PropertyInt.GearFire, StatsSheetJewelValue.Percent, "fire damage", "chance to ignite the ground"),
        (PropertyInt.GearFrost, StatsSheetJewelValue.Percent, "cold damage", "chance to create chilling mist"),
        (PropertyInt.GearAcid, StatsSheetJewelValue.Percent, "acid damage", "chance to create acidic mist"),
        (PropertyInt.GearLightning, StatsSheetJewelValue.Percent, "lightning damage", "chance to electrify the ground"),
        (PropertyInt.GearElementalist, StatsSheetJewelValue.UpToPercent, "war magic damage", null),
        (PropertyInt.GearWardPen, StatsSheetJewelValue.UpToPercent, "ward penetration", null),
        (PropertyInt.GearLifesteal, StatsSheetJewelValue.Percent, "chance to steal health on hit", null),
        (PropertyInt.GearStaminasteal, StatsSheetJewelValue.Percent, "chance to steal stamina on hit", null),
        (PropertyInt.GearManasteal, StatsSheetJewelValue.Percent, "chance to steal mana on hit", null),

        (PropertyInt.GearHealBubble, StatsSheetJewelValue.Percent, "restoration spell bonus", "chance to create a healing sphere"),
        (PropertyInt.GearVitalsTransfer, StatsSheetJewelValue.Percent, "vitals transfer bonus", null),
        (PropertyInt.GearSelflessness, StatsSheetJewelValue.Percent, "restoration bonus on others", null),
        (PropertyInt.GearThreatGain, StatsSheetJewelValue.Percent, "increased threat", null),
        (PropertyInt.GearThreatReduction, StatsSheetJewelValue.Percent, "reduced threat", null),
        (PropertyInt.GearItemManaUsage, StatsSheetJewelValue.Percent, "reduced item mana usage", null),
        (PropertyInt.GearCompBurn, StatsSheetJewelValue.Percent, "reduced component burn chance", null),
        (PropertyInt.GearExperienceGain, StatsSheetJewelValue.Percent, "experience from kills", null),
        (PropertyInt.GearMagicFind, StatsSheetJewelValue.Percent, "loot quality", null),
        (PropertyInt.GearPyrealFind, StatsSheetJewelValue.Percent, "chance for an extra item", null),
    ];

    private static readonly (DamageType DamageType, ResistanceType ResistanceType, string Name)[] StatsSheetDamageTypes =
    [
        (DamageType.Slash, ResistanceType.Slash, "Slashing"),
        (DamageType.Pierce, ResistanceType.Pierce, "Piercing"),
        (DamageType.Bludgeon, ResistanceType.Bludgeon, "Bludgeoning"),
        (DamageType.Fire, ResistanceType.Fire, "Fire"),
        (DamageType.Cold, ResistanceType.Cold, "Cold"),
        (DamageType.Acid, ResistanceType.Acid, "Acid"),
        (DamageType.Electric, ResistanceType.Electric, "Lightning"),
        (DamageType.Nether, ResistanceType.Nether, "Nether"),
    ];

    private static readonly BodyPart[] StatsSheetBodyParts =
    [
        BodyPart.Head,
        BodyPart.Chest,
        BodyPart.Abdomen,
        BodyPart.UpperArm,
        BodyPart.LowerArm,
        BodyPart.Hand,
        BodyPart.UpperLeg,
        BodyPart.LowerLeg,
        BodyPart.Foot
    ];

    /// <summary>
    /// Returns the character stats text for the Combat Focus appraisal
    /// </summary>
    public string GetStatsSheetText()
    {
        var sb = new StringBuilder();

        sb.Append("---------- Character Stats ----------\n");

        AppendStatsSheetGearMods(sb);
        AppendStatsSheetRatings(sb);
        AppendStatsSheetWard(sb);
        AppendStatsSheetArmor(sb);
        AppendStatsSheetResistances(sb);
        AppendStatsSheetJewels(sb);

        return sb.ToString();
    }

    private void AppendStatsSheetGearMods(StringBuilder sb)
    {
        var lines = new List<string>();

        // armor attack mod + the wielded weapon's attack mod (including weapon aura buffs),
        // shown even out of combat mode, where the weapon bonus is normally inactive
        var attackMod = (GetArmorAttackMod() ?? 0) + (GetWeaponOffenseModifier(this, true) - 1.0f);

        AddStatsSheetMod(lines, "Attack Skill", attackMod);
        AddStatsSheetMod(lines, "Physical Defense", GetArmorPhysicalDefMod() ?? 0);
        AddStatsSheetMod(lines, "Magic Defense", GetArmorMagicDefMod() ?? 0);
        AddStatsSheetMod(lines, "War Magic", GetGearSkillMod(Skill.WarMagic));
        AddStatsSheetMod(lines, "Life Magic", GetGearSkillMod(Skill.LifeMagic));
        AddStatsSheetMod(lines, "Dual Wield", GetArmorDualWieldMod() ?? 0);
        AddStatsSheetMod(lines, "Two-handed Combat", GetArmorTwohandedCombatMod() ?? 0);
        AddStatsSheetMod(lines, "Shield", GetArmorShieldMod() ?? 0);
        AddStatsSheetMod(lines, "Perception", GetArmorPerceptionMod() ?? 0);
        AddStatsSheetMod(lines, "Deception", GetArmorDeceptionMod() ?? 0);
        AddStatsSheetMod(lines, "Thievery", GetArmorThieveryMod() ?? 0);
        AddStatsSheetMod(lines, "Run", GetArmorRunMod() ?? 0);

        AddStatsSheetMod(lines, "Maximum Health", GetArmorHealthMod() ?? 0);
        AddStatsSheetMod(lines, "Maximum Stamina", GetArmorStaminaMod() ?? 0);
        AddStatsSheetMod(lines, "Maximum Mana", GetArmorManaMod() ?? 0);
        AddStatsSheetMod(lines, "Health Regen", GetArmorHealthRegenMod());
        AddStatsSheetMod(lines, "Stamina Regen", GetArmorStaminaRegenMod());
        AddStatsSheetMod(lines, "Mana Regen", GetArmorManaRegenMod());
        AddStatsSheetMod(lines, "Restoration Spells", GetWeaponLifeMagicVitalMod() ?? 0);

        AddStatsSheetMod(lines, "Weapon Physical Defense", GetWeaponPhysicalDefenseModifier(this, true) - 1.0f);
        AddStatsSheetMod(lines, "Weapon Magic Defense", GetWeaponMagicDefenseModifier(this, true) - 1.0f);

        AddStatsSheetMod(lines, "Stamina/Mana Usage Penalty", GetArmorResourcePenalty() ?? 0);

        var frigidResistance = GetEquippedAndActivatedItemRatingSum(PropertyInt.GearFrigidProtection);
        if (frigidResistance != 0)
        {
            lines.Add($"Frigid Resistance: {frigidResistance}");
        }

        AppendStatsSheetSection(sb, "Gear Mods", lines);
    }

    private void AppendStatsSheetRatings(StringBuilder sb)
    {
        var lines = new List<string>();

        AddStatsSheetRating(lines, "Damage", GetDamageRating());
        AddStatsSheetRating(lines, "Damage Resist", GetDamageResistRating());
        AddStatsSheetRating(lines, "Critical Chance", GetCritRating());
        AddStatsSheetRating(lines, "Critical Damage", GetCritDamageRating());
        AddStatsSheetRating(lines, "Critical Resist", GetCritResistRating());
        AddStatsSheetRating(lines, "Critical Damage Resist", GetCritDamageResistRating());
        AddStatsSheetRating(lines, "Healing Boost", GetHealingBoostRating());
        AddStatsSheetRating(lines, "Max Health", GetGearMaxHealth());

        AppendStatsSheetSection(sb, "Ratings", lines);
    }

    private void AppendStatsSheetWard(StringBuilder sb)
    {
        var wardLevel = (int)Math.Round(
            (GetWardLevel() + EnchantmentManager.GetWardAdditiveMod()) * EnchantmentManager.GetWardMultiplicativeMod()
        );

        AppendStatsSheetSection(sb, "Ward", [$"Ward Level: {wardLevel}"]);
    }

    /// <summary>
    /// Armor level vs. each damage type: the average across all body parts,
    /// with the lowest and highest body part in parentheses
    /// </summary>
    private void AppendStatsSheetArmor(StringBuilder sb)
    {
        var lines = new List<string>();
        var bodyArmorMod = EnchantmentManager.GetBodyArmorMod();

        foreach (var (damageType, _, name) in StatsSheetDamageTypes)
        {
            if (damageType == DamageType.Nether)
            {
                continue;
            }

            var bodyPartArmor = new List<float>();

            foreach (var bodyPart in StatsSheetBodyParts)
            {
                var coverageMask = BodyParts.GetCoverageMask(bodyPart);
                var layers = EquippedObjects.Values.Where(e => e is Clothing && (e.ClothingPriority & coverageMask) != 0);

                bodyPartArmor.Add(layers.Sum(armor => GetArmorMod(armor, damageType, false)) + bodyArmorMod);
            }

            var average = (int)Math.Round(bodyPartArmor.Average());
            var lowest = (int)Math.Round(bodyPartArmor.Min());
            var highest = (int)Math.Round(bodyPartArmor.Max());

            lines.Add($"{name}: {average}  ({lowest} - {highest})");
        }

        AppendStatsSheetSection(sb, "Armor Level (average, lowest - highest)", lines);
    }

    /// <summary>
    /// Percent of damage taken for each damage type, before armor
    /// </summary>
    private void AppendStatsSheetResistances(StringBuilder sb)
    {
        var lines = new List<string>();

        // every damage type includes the natural resistance from Strength + Endurance
        var naturalResistance = GetNaturalResistance(DamageType.Undef);
        lines.Add($"Natural (Strength + Endurance): {Math.Round(naturalResistance * 100)}%");

        var physicalWard = 1.0f - Jewel.GetJewelEffectMod(this, PropertyInt.GearPhysicalWard);
        var elementalWard = 1.0f - Jewel.GetJewelEffectMod(this, PropertyInt.GearElementalWard);

        foreach (var (damageType, resistanceType, name) in StatsSheetDamageTypes)
        {
            var wardMod = 1.0f;

            if ((damageType & DamageType.Physical) != 0)
            {
                wardMod = physicalWard;
            }
            else if ((damageType & DamageType.Elemental) != 0)
            {
                wardMod = elementalWard;
            }

            var damageTaken = wardMod * GetResistanceMod(resistanceType);

            lines.Add($"{name}: {Math.Round(damageTaken * 100)}%");
        }

        AppendStatsSheetSection(sb, "Resistances (damage taken)", lines);
    }

    /// <summary>
    /// Totals for every jewel effect (and matching gear rating) on equipped items.
    /// Ramping effects show their maximum.
    /// </summary>
    private void AppendStatsSheetJewels(StringBuilder sb)
    {
        var lines = new List<string>();

        foreach (var (ratingProperty, value, effect, secondaryEffect) in StatsSheetJewelEffects)
        {
            var rating = GetEquippedAndActivatedItemRatingSum(ratingProperty);

            if (rating <= 0 || !Jewel.JewelTypeToMaterial.TryGetValue(ratingProperty, out var material))
            {
                continue;
            }

            if (
                !Jewel.JewelEffectInfoAlternate.TryGetValue(material, out var info)
                || info.PropertyName != ratingProperty
            )
            {
                info = Jewel.JewelEffectInfoMain[material];
            }

            var amount = info.BasePrimary + info.BonusPrimary * rating;

            var text = value switch
            {
                StatsSheetJewelValue.Flat => $"+{Math.Round(amount, 2)} {effect}",
                StatsSheetJewelValue.Protection => $"+{amount / 100:0.00} {effect}",
                StatsSheetJewelValue.UpToPercent => $"up to {Math.Round(amount, 2)}% {effect}",
                _ => $"{Math.Round(amount, 2)}% {effect}",
            };

            if (secondaryEffect != null)
            {
                var secondaryAmount = info.BaseSecondary + info.BonusSecondary * rating;
                text += $", {Math.Round(secondaryAmount, 2)}% {secondaryEffect}";
            }

            lines.Add($"{info.Name} ({rating}): {text}");
        }

        AppendStatsSheetSection(sb, "Jewel Effects", lines);
    }

    private static void AddStatsSheetRating(List<string> lines, string name, int rating)
    {
        if (rating == 0)
        {
            return;
        }

        lines.Add($"{name}: {rating}");
    }

    private static void AddStatsSheetMod(List<string> lines, string name, double mod)
    {
        if (Math.Abs(mod) < 0.0005)
        {
            return;
        }

        lines.Add($"{name}: {FormatStatsSheetPercent(mod)}");
    }

    private static void AppendStatsSheetSection(StringBuilder sb, string header, List<string> lines)
    {
        sb.Append($"\n{header}:\n");

        if (lines.Count == 0)
        {
            sb.Append("   None\n");
            return;
        }

        foreach (var line in lines)
        {
            sb.Append($"   {line}\n");
        }
    }

    private static string FormatStatsSheetPercent(double mod)
    {
        var percent = Math.Round(mod * 100, 1);

        return percent >= 0 ? $"+{percent}%" : $"{percent}%";
    }
}
