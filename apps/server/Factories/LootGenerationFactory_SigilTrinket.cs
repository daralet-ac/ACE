using System;
using System.Collections.Generic;
using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Factories.Tables.Wcids;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories;

public static partial class LootGenerationFactory
{
    // NOTE:
    // Static data (icon ids, palette colors, per-effect mappings) has been moved to
    // Config/sigil_trinket_config.json and is loaded by SigilTrinketConfig.
    // This factory now reads configuration from SigilTrinketConfig to keep data and logic separated.

    // --- Creation and mutation logic uses SigilTrinketConfig for lookups ---

    public static WorldObject CreateSigilTrinket(TreasureDeath profile, SigilTrinketType sigilTrinketType, bool mutate = true, int? effectId = null, int? wieldSkillRng = null, uint? wcidOverride = null)
    {
        var wcid = SigilTrinketWcids.Roll(profile.Tier, sigilTrinketType);

        var actualWcid = wcidOverride ?? (uint)wcid;

        var wo = WorldObjectFactory.CreateNewWorldObject(actualWcid);

        if (mutate)
        {
            MutateSigilTrinket(wo, profile, effectId, wieldSkillRng);
        }

        return wo;
    }

    private static void MutateSigilTrinket(WorldObject wo, TreasureDeath profile, int? effectId = null, int? forcedWieldSkillRng = null)
    {
        if (wo is not SigilTrinket sigilTrinket)
        {
            return;
        }

        sigilTrinket.CooldownDuration = GetCooldown(profile);
        sigilTrinket.SigilTrinketTriggerChance = GetChance(profile);
        sigilTrinket.SigilTrinketMaxTier = Math.Clamp(profile.Tier - 1, 1, 7);
        sigilTrinket.WieldRequirements = WieldRequirement.Training;
        sigilTrinket.WieldDifficulty = 3; // Specialized
        sigilTrinket.ItemMaxLevel = Math.Clamp(profile.Tier - 1, 1, 7);
        sigilTrinket.ItemBaseXp = GetBaseLevelCost(profile);
        sigilTrinket.ItemTotalXp = 0;

        // Icon overlay id comes from config tier icon ids
        if (SigilTrinketConfig.TierIconIds.TryGetValue(Math.Clamp(profile.Tier - 1, 1, 7), out var overlayId))
        {
            sigilTrinket.IconOverlayId = overlayId;
        }

        sigilTrinket.SigilTrinketHealthReserved = 0;
        sigilTrinket.SigilTrinketStaminaReserved = 0;
        sigilTrinket.SigilTrinketManaReserved = 0;

        // allow emote to override the wield skill rng (0 or 1). If not provided, use a random 0/1.
        var wieldSkillRng = forcedWieldSkillRng ?? ThreadSafeRandom.Next(0, 1);

        switch (sigilTrinket.SigilTrinketType)
        {
            case (int)SigilTrinketType.Compass:
                sigilTrinket.SigilTrinketHealthReserved = GetReservedVital(profile, false, true);
                sigilTrinket.WieldSkillType = wieldSkillRng == 0 ? (int)Skill.Shield : (int)Skill.TwoHandedCombat;

                if (sigilTrinket.WieldSkillType == (int)Skill.Shield)
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxShieldEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxShieldEffectId);

                    ApplyConfigMap(profile, sigilTrinket, SigilTrinketConfig.ShieldCompass);
                }
                else
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxTwohandedCombatEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxTwohandedCombatEffectId);

                    ApplyConfigMap(profile, sigilTrinket, SigilTrinketConfig.TwohandedCompass);
                }
                break;
            case (int)SigilTrinketType.PuzzleBox:
                sigilTrinket.SigilTrinketHealthReserved = GetReservedVital(profile, true, true);
                sigilTrinket.SigilTrinketStaminaReserved = GetReservedVital(profile, true);
                sigilTrinket.WieldSkillType = wieldSkillRng == 0 ? (int)Skill.DualWield : (int)Skill.Thievery;

                if (sigilTrinket.WieldSkillType == (int)Skill.DualWield)
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxDualWieldEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxDualWieldEffectId);

                    ApplyConfigMap(profile, sigilTrinket, SigilTrinketConfig.DualWieldPuzzleBox);
                }
                else
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxThieveryEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxThieveryEffectId);

                    ApplyConfigMap(profile, sigilTrinket, SigilTrinketConfig.ThieveryPuzzleBox);
                }
                break;
            case (int)SigilTrinketType.Scarab:
                sigilTrinket.SigilTrinketHealthReserved = GetReservedVital(profile, true, true);
                sigilTrinket.SigilTrinketManaReserved = GetReservedVital(profile, true);
                sigilTrinket.WieldSkillType = wieldSkillRng == 0 ? (int)Skill.LifeMagic : (int)Skill.WarMagic;

                if (sigilTrinket.WieldSkillType == (int)Skill.LifeMagic)
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxLifeMagicEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxLifeMagicEffectId);

                    ApplyConfigMap(profile, sigilTrinket, SigilTrinketConfig.LifeMagicScarab);
                }
                else
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxWarMagicEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxWarMagicEffectId);

                    ApplyConfigMap(profile, sigilTrinket, SigilTrinketConfig.WarMagicScarab);
                }
                break;
            case (int)SigilTrinketType.PocketWatch:
                sigilTrinket.SigilTrinketStaminaReserved = GetReservedVital(profile);
                sigilTrinket.WieldSkillType = (int)Skill.PhysicalDefense;
                sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxPhysicalDefenseEffectId
                    ? effectId.Value
                    : ThreadSafeRandom.Next(0, SigilTrinket.MaxPhysicalDefenseEffectId);

                ApplyConfigMap(profile, sigilTrinket, SigilTrinketConfig.PhysicalDefensePocketWatch);
                break;
            case (int)SigilTrinketType.Top:
                sigilTrinket.SigilTrinketManaReserved = GetReservedVital(profile);
                sigilTrinket.WieldSkillType = (int)Skill.MagicDefense;
                sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxMagicDefenseEffectId
                    ? effectId.Value
                    : ThreadSafeRandom.Next(0, SigilTrinket.MaxMagicDefenseEffectId);

                ApplyConfigMap(profile, sigilTrinket, SigilTrinketConfig.MagicDefenseTop);
                break;
            case (int)SigilTrinketType.Goggles:
                sigilTrinket.SigilTrinketStaminaReserved = GetReservedVital(profile, true);
                sigilTrinket.SigilTrinketManaReserved = GetReservedVital(profile, true);
                sigilTrinket.WieldSkillType = wieldSkillRng == 0 ? (int)Skill.Perception : (int)Skill.Deception;

                if (sigilTrinket.WieldSkillType == (int)Skill.Perception)
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxPerceptionEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxPerceptionEffectId);

                    ApplyConfigMap(profile, sigilTrinket, SigilTrinketConfig.PerceptionGoggles);
                }
                else
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxDeceptionEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxDeceptionEffectId);

                    ApplyConfigMap(profile, sigilTrinket, SigilTrinketConfig.DeceptionGoggles);
                }
                break;
        }
    }

    private static void ApplyConfigMap(TreasureDeath profile, SigilTrinket sigilTrinket, IReadOnlyDictionary<int, SigilStatConfig> map)
    {
        if (!sigilTrinket.SigilTrinketEffectId.HasValue)
        {
            return;
        }

        var effectId = sigilTrinket.SigilTrinketEffectId.Value;
        if (!map.TryGetValue(effectId, out var cfg))
        {
            return;
        }

        // Palette / Icon
        if (cfg.PaletteKey is not null && SigilTrinketConfig.PaletteTemplateColors.TryGetValue(cfg.PaletteKey, out var palette))
        {
            sigilTrinket.PaletteTemplate = palette;
        }

        if (cfg.IconColorKey is not null && SigilTrinketConfig.IconColorIds.Count > 0)
        {
            var typeIndex = Math.Clamp(sigilTrinket.SigilTrinketType ?? 0, 0, SigilTrinketConfig.IconColorIds.Count - 1);
            var iconMap = SigilTrinketConfig.IconColorIds[typeIndex];
            if (iconMap.TryGetValue(cfg.IconColorKey, out var iconId))
            {
                sigilTrinket.IconId = iconId;
            }
        }

        // Name suffix
        sigilTrinket.Name += cfg.NameSuffix ?? string.Empty;

        // Intensity / Reduction / ManaReservation / Cooldown / TriggerChance
        if (cfg.SetIntensity)
        {
            sigilTrinket.SigilTrinketIntensity = GetIntensity(profile);
        }

        if (cfg.SetReduction)
        {
            sigilTrinket.SigilTrinketManaReserved = 0;
            sigilTrinket.SigilTrinketReductionAmount = GetReductionAmount(profile);
        }

        if (cfg.SetManaReservedZero)
        {
            sigilTrinket.SigilTrinketManaReserved = 0;
        }

        if (cfg.CooldownDelta != 0.0)
        {
            sigilTrinket.CooldownDuration += cfg.CooldownDelta;
        }

        if (cfg.ZeroTriggerChance)
        {
            sigilTrinket.SigilTrinketTriggerChance = 0;
        }

        // Use text - support {wieldReq} placeholder
        if (!string.IsNullOrEmpty(cfg.UseText))
        {
            var useText = cfg.UseText;
            if (useText.Contains("{wieldReq}", StringComparison.Ordinal))
            {
                var wieldReq = GetWieldDifficultyPerTier((sigilTrinket.SigilTrinketMaxTier ?? 1) + 1);
                useText = useText.Replace("{wieldReq}", wieldReq.ToString(), StringComparison.Ordinal);
            }

            sigilTrinket.Use = useText;
        }
    }

    private const double MaxChance = 0.75;
    private const double MinChance = 0.25;

    private const double MaxCooldown = 20.0;
    private const double MinCooldown = 10.0;

    private const double MaxReservedVital = 0.2;
    private const double MinReservedVital = 0.1;

    private const double MaxIntensity = 0.75;
    private const double MinIntensity = 0.25;

    private const double MaxReduction = 0.75;
    private const double MinReduction = 0.25;

    private static double GetChance(TreasureDeath treasureDeath)
    {
        const double range = MaxChance - MinChance;
        var roll = range * GetDiminishingRoll(treasureDeath);

        return MinChance + roll;
    }

    private static double GetCooldown(TreasureDeath treasureDeath)
    {
        const double range = MaxCooldown - MinCooldown;
        var roll = range * GetDiminishingRoll(treasureDeath);

        return MaxCooldown - roll;
    }

    /// <summary>
    /// Roll for reserved vital amount. 50% less reserved for hybrid trinkets and 50% less reserved for health vitals.
    /// </summary>
    private static double GetReservedVital(TreasureDeath treasureDeath, bool hybrid = false, bool health = false)
    {
        const double range = MaxReservedVital - MinReservedVital;
        var hybridMultiplier = hybrid ? 0.5 : 1.0;
        var healthMultiplier = health ? 0.5 : 1.0;
        var roll = range * hybridMultiplier * healthMultiplier * GetDiminishingRoll(treasureDeath);

        return MaxReservedVital * hybridMultiplier * healthMultiplier - roll;
    }

    private static double GetIntensity(TreasureDeath treasureDeath)
    {
        const double range = MaxIntensity - MinIntensity;
        var roll = range * GetDiminishingRoll(treasureDeath);

        return MinIntensity + roll;
    }

    private static double GetReductionAmount(TreasureDeath treasureDeath)
    {
        const double range = MaxReduction - MinReduction;
        var roll = range * GetDiminishingRoll(treasureDeath);

        return MinReduction + roll;
    }

    private static uint GetBaseLevelCost(TreasureDeath treasureDeath)
    {
        switch (treasureDeath.Tier)
        {
            default:
                return 10000;
            case 3:
                return 50000;
            case 4:
                return 100000;
            case 5:
                return 250000;
            case 6:
                return 500000;
            case 7:
                return 1000000;
            case 8:
                return 2000000;
        }
    }
}
