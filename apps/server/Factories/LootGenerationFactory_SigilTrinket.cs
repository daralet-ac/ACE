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
    private static readonly List<Dictionary<string, uint>> IconColorIds =
    [
        new Dictionary<string, uint>() // 0 - Compass
        {
            { "black", 100690601 }, // black
            { "gray", 100690594 },
            { "olive", 100690602 }, // brown
            { "white", 100690596 }, // white
            { "purple", 100690600 }, // purple
            { "blue", 100690595 }, // blue
            { "green", 100690598 }, // green
            { "yellow", 100690566 }, // yellow
            { "orange", 100690602 }, // orange
            { "red", 100690597 }, // red
            { "iron", 100690599 } //
        },

        new Dictionary<string, uint>() // 1 - Puzzle Box
        {
            { "black", 100690665 }, // black
            { "gray", 100690658 }, // gray
            { "olive", 100690661 }, // teal
            { "white", 100690664 }, // white
            { "purple", 100690663 }, // purple
            { "blue", 100690657 }, // blue
            { "green", 100690660 }, // green
            { "yellow", 100690662 }, // yellow
            { "orange", 100690666 }, // brown
            { "red", 100690659 }, // red
            { "iron", 0 } // iron
        },

        new Dictionary<string, uint>() // 2 - Scarabs
        {
            { "black", 100690698 }, // black
            { "gray", 100690701 }, // gray
            { "olive", 100690705 }, // olive
            { "white", 100690704 }, // white
            { "purple", 100690707 }, // purple
            { "blue", 100690706 }, // blue
            { "green", 100690700 }, // green
            { "yellow", 100690699 }, // yellow
            { "orange", 100690702 }, // orange
            { "red", 100693226 }, // red
            { "iron", 100690703 } // iron
        },

        new Dictionary<string, uint>() // 3 - Pocket Watch
        {
            { "black", 100690620 }, // black
            { "gray", 0 },
            { "olive", 100690619 }, // brown
            { "white", 100690614 }, // white
            { "purple", 100690618 }, // purple
            { "blue", 100690613 }, // blue
            { "green", 100690616 }, // green
            { "yellow", 100690592 }, // yellow
            { "orange", 100690593 }, // orange
            { "red", 100690615 }, // red
            { "iron", 100690617 } //
        },

        new Dictionary<string, uint>() // 4 - Top
        {
            { "black", 100690676 }, // black
            { "gray", 100690669 }, // gray
            { "olive", 100690672 }, // teal
            { "white", 100690675 }, // white
            { "purple", 100690674 }, // purple
            { "blue", 100690668 }, // blue
            { "green", 100690671 }, // green
            { "yellow", 100690673 }, // yellow
            { "orange", 100690677 }, // brown
            { "red", 100690670 }, // red
            { "iron", 0 } // iron
        },

        new Dictionary<string, uint>() // 5 - Goggles
        {
        { "black", 100690611 }, // black
        { "gray", 100690610 }, // gray
        { "olive", 100690612 }, // brown
        { "white", 100690604 }, // white
        { "purple", 100690609 }, // purple
        { "blue", 100690603 }, // blue
        { "green", 100690606 }, // green
        { "yellow", 100690608 }, // yellow
        { "orange", 100690607 }, // orange
        { "red", 100690605 }, // red
        { "iron", 100690703 } // iron
        },
    ];

    private static readonly Dictionary<string, int> PaletteTemplateColors = new()
    {
        { "black", 2 }, // black (lead)
        { "gray", 20 }, // gray (silver)
        { "olive", 18 }, // olive (yellowBrown)
        { "white", 61 }, // white
        { "purple", 39 }, // purple (black)
        { "blue", 77 }, // blue (blueGreen)
        { "green", 8 }, // green
        { "yellow", 21 }, // yellow (gold)
        { "orange", 19 }, // orange (copper)
        { "red", 14 }, // red
        { "iron", 82 } // iron (pinkPurple)
    };

    private static readonly Dictionary<string, string> DuplicationElementString = new()
    {
        { "white", " Bludgeoning" }, // white
        { "purple", " Lightning" }, // purple
        { "blue", " Cold" }, // blue
        { "green", "n Acid" }, // green
        { "yellow", " Piercing" }, // yellow
        { "orange", " Slashing" }, // orange
        { "red", " Fire" } // red
    };

    private static readonly Dictionary<string, int> ElementId = new()
    {
        { "white", 0x4 }, // white - blunt
        { "purple", 0x40 }, // purple - electric
        { "blue", 0x8 }, // blue - cold
        { "green", 0x20 }, // green - acid
        { "yellow", 0x2 }, // yellow - pierce
        { "orange", 0x1 }, // orange - slash
        { "red", 0x10 } // red - fire
    };

    private static readonly Dictionary<string, uint> OverlayIds = new()
    {
        { "life", 100689496 },
        { "war", 100689495 },
    };

    private static readonly Dictionary<int, uint> TierIconIds = new()
    {
        {1, 100690996 },
        {2, 100690997 },
        {3, 100690998 },
        {4, 100690999 },
        {5, 100691000 },
        {6, 100691001 },
        {7, 100691002 },
    };

    // --- Refactor: Centralize effect -> stat mapping for each sigil category ---
    private sealed record SigilStatConfig(
        string PaletteKey,
        string IconColorKey,
        string NameSuffix,
        Func<TreasureDeath, SigilTrinket, string> UseBuilder = null,
        bool SetIntensity = false,
        bool SetReduction = false,
        bool SetManaReservedZero = false,
        double CooldownDelta = 0.0,
        bool ZeroTriggerChance = false
    );

    private static readonly IReadOnlyDictionary<int, SigilStatConfig> LifeMagicScarabMap = new Dictionary<int, SigilStatConfig>
    {
        {
            (int)SigilTrinketLifeMagicEffect.ScarabCastProt,
            new SigilStatConfig(
                "white",
                "white",
                " of Protection",
                (profile, trinket) =>
                    "Whenever the wielder casts a Heal, Revitalize, or Mana Boost spell, " +
                    "there is a chance the target will also gain elemental protection against each type of damage they took within the last 10 seconds.\n\n" +
                    "The level of the buff is equal to the level of the casted spell, but cannot surpass the Max Level value."
            )
        },
        {
            (int)SigilTrinketLifeMagicEffect.ScarabCastVuln,
            new SigilStatConfig(
                "black",
                "black",
                " of Vulnerability",
                (profile, trinket) =>
                    "Whenever the wielder casts a Harm, Enfeeble, or Mana Drain spell, " +
                    "there is a chance the target will also be inflicted with an elemental vulnerability, based on their greatest weakness.\n\n" +
                    "The level of the debuff is equal to the level of the casted spell, but cannot surpass the Max Level value."
            )
        },
        {
            (int)SigilTrinketLifeMagicEffect.ScarabCastVitalRate,
            new SigilStatConfig(
                "green",
                "green",
                " of Growth",
                (profile, trinket) =>
                    "Whenever the wielder casts a Heal or Harm spell, there is a chance the target will also be affected by Regeneration or Fester, respectively.\n\n" +
                    "Whenever the wielder casts a Revitalize or Enfeeble spell, there is a chance the target will also be affected by Rejuvenation or Exhaustion, respectively.\n\n" +
                    "Whenever the wielder casts a Mana Boost or Mana Drain spell, there is a chance the target will also be affected by Mana Renewal or Mana Depletion.\n\n" +
                    "The level of the buff is equal to the level of the casted spell, but cannot surpass the Max Level value."
            )
        },
        {
            (int)SigilTrinketLifeMagicEffect.ScarabCastItemBuff,
            new SigilStatConfig(
                "purple",
                "purple",
                " of Artifice",
                (profile, trinket) =>
                    "Whenever the wielder casts a Heal spell, there is a chance the target will also be affected by Defender Other.\n\n" +
                    "Whenever the wielder casts a Revitalize spell, there is a chance the target will also be affected by Blood Drinker.\n\n" +
                    "Whenever the wielder casts a Mana Boost spell, there is a chance the target will also be affected by Spirit Drinker.\n\n" +
                    "The level of the buff is equal to the level of the casted spell, but cannot surpass the Max Level value."
            )
        },
        {
            (int)SigilTrinketLifeMagicEffect.ScarabIntensity,
            new SigilStatConfig("red", "red", " of Intensity",
                (profile, trinket) =>
                    "Whenever the wielder casts any spell, there is a chance the spell will " +
                    "gain intensity. Intensity increases damage and restoration amounts.\n\n" +
                    "Only affects spells that are less than or equal to the Max level value.",
                SetIntensity: true
            )
        },
        {
            (int)SigilTrinketLifeMagicEffect.ScarabShield,
            new SigilStatConfig(
                "gray",
                "gray",
                " of Shielding",
                (profile, trinket) =>
                    "Whenever the wielder casts any spell, there is a chance they will " +
                    "gain a protective buff, increasing damage reduction rating by 25 for 12 seconds.\n\n" +
                    "Only affects spells that are less than or equal to the Max level value."
            )
        },
        {
            (int)SigilTrinketLifeMagicEffect.ScarabManaReduction,
            new SigilStatConfig("blue", "blue", " of Reduction",
                (profile, trinket) =>
                    "Whenever the wielder casts any spell, there is a chance that the cost of the spell will be reduced.\n\n" +
                    "Only affects spells that are less than or equal to the Max level value.",
                SetReduction: true, SetManaReservedZero: true, CooldownDelta: -5.0
            )
        }
    };

    private static readonly IReadOnlyDictionary<int, SigilStatConfig> WarMagicScarabMap = new Dictionary<int, SigilStatConfig>
    {
        {
            (int)SigilTrinketWarMagicEffect.ScarabIntensity,
            new SigilStatConfig("red", "red", " of Intensity",
                (profile, trinket) =>
                    "Whenever the wielder casts any spell, there is a chance the spell will " +
                    "gain intensity. Intensity increases damage and restoration amounts.\n\n" +
                    "Only affects spells that are less than or equal to the Max level value.",
                SetIntensity: true)
        },
        {
            (int)SigilTrinketWarMagicEffect.ScarabShield,
            new SigilStatConfig("gray", "gray", " of Shielding",
                (profile, trinket) =>
                    "Whenever the wielder casts any spell, there is a chance they will " +
                    "gain a protective buff, increasing damage reduction rating by 25 for 12 seconds.\n\n" +
                    "Only affects spells that are less than or equal to the Max level value.")
        },
        {
            (int)SigilTrinketWarMagicEffect.ScarabManaReduction,
            new SigilStatConfig("blue", "blue", " of Reduction",
                (profile, trinket) =>
                    "Whenever the wielder casts any spell, there is a chance that the cost of the spell will be reduced.\n\n" +
                    "Only affects spells that are less than or equal to the Max level value.",
                SetReduction: true, SetManaReservedZero: true, CooldownDelta: -5.0)
        },
        {
            (int)SigilTrinketWarMagicEffect.ScarabDuplicate,
            new SigilStatConfig("olive", "olive", " of Duplication",
                (profile, trinket) =>
                    "Whenever the wielder casts a War spell, there is a chance that the spell will be duplicated and cast against the same target.\n\n" +
                    "Only affects spells that are less than or equal to the Max level value.")
        },
        {
            (int)SigilTrinketWarMagicEffect.ScarabDetonate,
            new SigilStatConfig("yellow", "yellow", " of Detonation",
                (profile, trinket) =>
                    "Whenever the wielder damages a creature with a War spell, there is a chance that the spell will detonate on impact, causing an explosion of the same element.\n\n" +
                    "Only affects spells that are less than or equal to the Max level value.")
        },
        {
            (int)SigilTrinketWarMagicEffect.ScarabCrit,
            new SigilStatConfig("orange", "orange", " of Crushing",
                (profile, trinket) =>
                    "Whenever the wielder damages a creature with a War spell, there is a chance that the spell will detonate on impact, causing an explosion of the same element.\n\n" +
                    "Only affects spells that are less than or equal to the Max level value.",
                SetIntensity: false, SetReduction: false, SetManaReservedZero: false, CooldownDelta: 0.0, ZeroTriggerChance: true)
        }
    };

    private static readonly IReadOnlyDictionary<int, SigilStatConfig> ShieldCompassMap = new Dictionary<int, SigilStatConfig>
    {
        {
            (int)SigilTrinketShieldEffect.Might,
            new SigilStatConfig("red", "red", " of Might",
                (profile, trinket) =>
                {
                    var wieldReq = GetWieldDifficultyPerTier((trinket.SigilTrinketMaxTier ?? 1) + 1);
                    return $"Whenever the wielder attacks with more than 50% power, there is a chance that a normal hit will be converted into a critical hit.\n\nCan only occur while using a shield, with a wield requirement of up to {wieldReq}.";
                })
        },
        {
            (int)SigilTrinketShieldEffect.Aggression,
            new SigilStatConfig("olive", "olive", " of Aggression",
                (profile, trinket) =>
                {
                    var wieldReq = GetWieldDifficultyPerTier((trinket.SigilTrinketMaxTier ?? 1) + 1);
                    return $"Whenever the wielder attacks with more than 50% power, they have a chance to generate double threat towards that enemy.\n\nCan only occur while using a shield, with a wield requirement of up to {wieldReq}.";
                })
        }
    };

    private static readonly IReadOnlyDictionary<int, SigilStatConfig> TwohandedCompassMap = new Dictionary<int, SigilStatConfig>
    {
        {
            (int)SigilTrinketTwohandedCombatEffect.Might,
            new SigilStatConfig("red", "red", " of Might",
                (profile, trinket) =>
                {
                    var wieldReq = GetWieldDifficultyPerTier((trinket.SigilTrinketMaxTier ?? 1) + 1);
                    return $"Whenever the wielder attacks with more than 50% power, there is a chance that a normal hit will be converted into a critical hit.\n\nCan only occur while using a two-handed weapon, with a wield requirement of up to {wieldReq}.";
                })
        },
        {
            (int)SigilTrinketTwohandedCombatEffect.Aggression,
            new SigilStatConfig("olive", "olive", " of Aggression",
                (profile, trinket) =>
                {
                    var wieldReq = GetWieldDifficultyPerTier((trinket.SigilTrinketMaxTier ?? 1) + 1);
                    return $"Whenever the wielder attacks with more than 50% power, they have a chance to generate increased threat towards that enemy.\n\nCan only occur while using a two-handed weapon, with a wield requirement of up to {wieldReq}.";
                })
        }
    };

    private static readonly IReadOnlyDictionary<int, SigilStatConfig> DualWieldPuzzleBoxMap = new Dictionary<int, SigilStatConfig>
    {
        {
            (int)SigilTrinketDualWieldEffect.Assailment,
            new SigilStatConfig("red", "red", " of Assailment",
                (profile, trinket) =>
                {
                    var wieldReq = GetWieldDifficultyPerTier((trinket.SigilTrinketMaxTier ?? 1) + 1);
                    return $"Whenever the wielder performs an critical hit on an enemy, they have a chance to gain a 50% critical damage buff, additively, for 10 seconds.\n\nCan only occur while dual-wielding, with a wield requirement of up to {wieldReq}.";
                })
        }
    };

    private static readonly IReadOnlyDictionary<int, SigilStatConfig> ThieveryPuzzleBoxMap = new Dictionary<int, SigilStatConfig>
    {
        {
            (int)SigilTrinketThieveryEffect.Treachery,
            new SigilStatConfig("green", "green", " of Treachery",
                (profile, trinket) =>
                {
                    var wieldReq = GetWieldDifficultyPerTier((trinket.SigilTrinketMaxTier ?? 1) + 1);
                    return $"Whenever the wielder performs a sneak attack critical hit on an enemy, they have a chance to deal double critical damage.\n\nCan only occur while performing sneak attacks, using a weapon with a wield requirement of up to {wieldReq}.";
                })
        }
    };

    private static readonly IReadOnlyDictionary<int, SigilStatConfig> PocketWatchMap = new Dictionary<int, SigilStatConfig>
    {
        {
            (int)SigilTrinketPhysicalDefenseEffect.Evasion,
            new SigilStatConfig("yellow", "yellow", " of Evasion",
                (profile, trinket) =>
                {
                    var wieldReq = GetWieldDifficultyPerTier((trinket.SigilTrinketMaxTier ?? 1) + 1);
                    return $"Whenever the wielder receives a glancing blow, it has a chance to become a full evade.\n\nCan only occur while wielding a weapon with a wield requirement of up to {wieldReq}.";
                })
        }
    };

    private static readonly IReadOnlyDictionary<int, SigilStatConfig> TopMagicMap = new Dictionary<int, SigilStatConfig>
    {
        {
            (int)SigilTrinketMagicDefenseEffect.Absorption,
            new SigilStatConfig("purple", "purple", " of Absorption",
                (profile, trinket) =>
                {
                    var wieldReq = GetWieldDifficultyPerTier((trinket.SigilTrinketMaxTier ?? 1) + 1);
                    return $"Whenever the wielder is damaged by a spell, they have a chance to prevent half of the damage and convert it into mana gained.\n\nCan only occur while wielding a weapon with a wield requirement of up to {wieldReq}.";
                })
        }
    };

    private static readonly IReadOnlyDictionary<int, SigilStatConfig> PerceptionGogglesMap = new Dictionary<int, SigilStatConfig>
    {
        {
            (int)SigilTrinketPerceptionEffect.Exposure,
            new SigilStatConfig("red", "red", " of Exposure",
                (profile, trinket) =>
                {
                    var wieldReq = GetWieldDifficultyPerTier((trinket.SigilTrinketMaxTier ?? 1) + 1);
                    return $"Whenever the wielder successfully uses the Expose Weakness ability, they have a chance to gain a 25% damage boost for 10 seconds.\n\nCan only occur while wielding a weapon with a wield requirement of up to {wieldReq}.";
                })
        }
    };

    private static readonly IReadOnlyDictionary<int, SigilStatConfig> DeceptionGogglesMap = new Dictionary<int, SigilStatConfig>
    {
        {
            (int)SigilTrinketDeceptionEffect.Avoidance,
            new SigilStatConfig("black", "black", " of Avoidance",
                (profile, trinket) =>
                {
                    var wieldReq = GetWieldDifficultyPerTier((trinket.SigilTrinketMaxTier ?? 1) + 1);
                    return $"Whenever the wielder performs an attack with more than 50% power, they have a chance for the attack to generate no threat towards the target.\n\nCan only occur while using a weapon, with a wield requirement of up to {wieldReq}.";
                })
        }
    };

    // --- Creation and mutation logic remains, but Set* methods now use maps above ---

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
        sigilTrinket.IconOverlayId = TierIconIds[Math.Clamp(profile.Tier - 1, 1, 7)];

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

                    ApplyConfigMap(profile, sigilTrinket, ShieldCompassMap);
                }
                else
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxTwohandedCombatEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxTwohandedCombatEffectId);

                    ApplyConfigMap(profile, sigilTrinket, TwohandedCompassMap);
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

                    ApplyConfigMap(profile, sigilTrinket, DualWieldPuzzleBoxMap);
                }
                else
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxThieveryEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxThieveryEffectId);

                    ApplyConfigMap(profile, sigilTrinket, ThieveryPuzzleBoxMap);
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

                    ApplyConfigMap(profile, sigilTrinket, LifeMagicScarabMap);
                }
                else
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxWarMagicEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxWarMagicEffectId);

                    ApplyConfigMap(profile, sigilTrinket, WarMagicScarabMap);
                }
                break;
            case (int)SigilTrinketType.PocketWatch:
                sigilTrinket.SigilTrinketStaminaReserved = GetReservedVital(profile);
                sigilTrinket.WieldSkillType = (int)Skill.PhysicalDefense;
                sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxPhysicalDefenseEffectId
                    ? effectId.Value
                    : ThreadSafeRandom.Next(0, SigilTrinket.MaxPhysicalDefenseEffectId);

                ApplyConfigMap(profile, sigilTrinket, PocketWatchMap);
                break;
            case (int)SigilTrinketType.Top:
                sigilTrinket.SigilTrinketManaReserved = GetReservedVital(profile);
                sigilTrinket.WieldSkillType = (int)Skill.MagicDefense;
                sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxMagicDefenseEffectId
                    ? effectId.Value
                    : ThreadSafeRandom.Next(0, SigilTrinket.MaxMagicDefenseEffectId);

                ApplyConfigMap(profile, sigilTrinket, TopMagicMap);
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

                    ApplyConfigMap(profile, sigilTrinket, PerceptionGogglesMap);
                }
                else
                {
                    sigilTrinket.SigilTrinketEffectId = effectId.HasValue && effectId.Value >= 0 && effectId.Value < SigilTrinket.MaxDeceptionEffectId
                        ? effectId.Value
                        : ThreadSafeRandom.Next(0, SigilTrinket.MaxDeceptionEffectId);

                    ApplyConfigMap(profile, sigilTrinket, DeceptionGogglesMap);
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
        if (cfg.PaletteKey is not null && PaletteTemplateColors.TryGetValue(cfg.PaletteKey, out var palette))
        {
            sigilTrinket.PaletteTemplate = palette;
        }

        if (cfg.IconColorKey is not null)
        {
            var typeIndex = Math.Clamp(sigilTrinket.SigilTrinketType ?? 0, 0, IconColorIds.Count - 1);
            var iconMap = IconColorIds[typeIndex];
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

        // Use text
        if (cfg.UseBuilder != null)
        {
            sigilTrinket.Use = cfg.UseBuilder(profile, sigilTrinket);
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
