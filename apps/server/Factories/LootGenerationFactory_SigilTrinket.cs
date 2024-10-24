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

        new Dictionary<string, uint>() // 1 - Goggles
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

        new Dictionary<string, uint>() // 4 - Top
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

        new Dictionary<string, uint>() // 5 - Puzzle Box
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
        }

    ];

    private static readonly Dictionary<string, uint> VulnProtIconOverlayIds = new()
    {
        { "black", 100689523 }, // black
        { "white", 100689533 }, // white
        { "purple", 100689536 }, // purple
        { "blue", 100689535 }, // blue
        { "green", 100689531 }, // green
        { "yellow", 100689537 }, // yellow
        { "orange", 100689532 }, // orange
        { "red", 100689534 } // red
    };

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

    private static readonly Dictionary<string, uint> VulnSpellIds = new()
    {
        { "black", 25 }, // black
        { "white", 1048 }, // white
        { "purple", 1084 }, // purple
        { "blue", 1060 }, // blue
        { "green", 521 }, // green
        { "yellow", 1151 }, // yellow
        { "orange", 1127 }, // orange
        { "red", 21 } // red
    };

    private static readonly Dictionary<string, string> VulnSpellNames = new()
    {
        { "black", "Imperil Other" }, // black
        { "white", "Bludgeoning Vulnerability Other" }, // white
        { "purple", "Lightning Vulnerability Other" }, // purple
        { "blue", "Cold Vulnerability Other" }, // blue
        { "green", "Acid Vulnerability Other" }, // green
        { "yellow", "Piercing Vulnerability Other" }, // yellow
        { "orange", "Slashing Vulnerability Other" }, // orange
        { "red", "Fire Vulnerability Other" } // red
    };

    private static readonly Dictionary<string, uint> ProtSpellIds = new()
    {
        { "black", 23 }, // black
        { "white", 1024 }, // white
        { "purple", 1072 }, // purple
        { "blue", 1036 }, // blue
        { "green", 509 }, // green
        { "yellow", 1139 }, // yellow
        { "orange", 1115 }, // orange
        { "red", 19 } // red
    };

    private static readonly Dictionary<string, string> ProtSpellNames = new()
    {
        { "black", "Armor Other" }, // black
        { "white", "Bludgeoning Protection Other" }, // white
        { "purple", "Lightning Protection Other" }, // purple
        { "blue", "Cold Protection Other" }, // blue
        { "green", "Acid Protection Other" }, // green
        { "yellow", "Piercing Protection Other" }, // yellow
        { "orange", "Slashing Protection Other" }, // orange
        { "red", "Fire Protection Other" } // red
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

    private static WorldObject CreateSigilTrinket(TreasureDeath profile, SigilTrinketType sigilTrinketType,  bool mutate = true)
    {
        var wcid = SigilTrinketWcids.Roll(profile.Tier, sigilTrinketType);

        var wo = WorldObjectFactory.CreateNewWorldObject((uint)wcid);

        if (mutate)
        {
            MutateSigilTrinket(wo, profile);
        }

        return wo;
    }

    private static void MutateSigilTrinket(WorldObject wo, TreasureDeath profile)
    {
        string color;

        if (wo is not SigilTrinket sigilTrinket)
        {
            return;
        }

        sigilTrinket.CooldownDuration = GetCooldown(profile);
        sigilTrinket.SigilTrinketTriggerChance = GetChance(profile);
        sigilTrinket.SigilTrinketMaxLevel = Math.Clamp(profile.Tier - 1, 1, 7);
        sigilTrinket.WieldDifficulty = RollWieldDifficulty(profile.Tier);
        sigilTrinket.ItemMaxLevel = Math.Clamp(profile.Tier - 1, 1, 7);
        sigilTrinket.ItemBaseXp = GetBaseLevelCost(profile);
        sigilTrinket.ItemTotalXp = 0;

        switch (sigilTrinket.SigilTrinketType)
        {
            case (int)SigilTrinketType.Compass:
                sigilTrinket.SigilTrinketHealthReserved = GetReservedVital(profile);
                break;
            case (int)SigilTrinketType.Goggles:
                sigilTrinket.SigilTrinketStaminaReserved = GetReservedVital(profile);
                break;
            case (int)SigilTrinketType.Scarab:
                sigilTrinket.SigilTrinketManaReserved = GetReservedVital(profile);
                break;
            case (int)SigilTrinketType.PocketWatch:
                sigilTrinket.SigilTrinketHealthReserved = GetReservedVital(profile, true);
                sigilTrinket.SigilTrinketStaminaReserved = GetReservedVital(profile, true);
                break;
            case (int)SigilTrinketType.Top:
                sigilTrinket.SigilTrinketHealthReserved = GetReservedVital(profile, true);
                sigilTrinket.SigilTrinketManaReserved = GetReservedVital(profile, true);
                break;
            case (int)SigilTrinketType.PuzzleBox:
                sigilTrinket.SigilTrinketStaminaReserved = GetReservedVital(profile, true);
                sigilTrinket.SigilTrinketManaReserved = GetReservedVital(profile, true);
                break;
        }
        sigilTrinket.SigilTrinketManaReserved = GetReservedVital(profile);

        switch (sigilTrinket.WieldSkillType)
        {
            case (int)Skill.LifeMagic:
                sigilTrinket.SigilTrinketEffectId = ThreadSafeRandom.Next(1, 7);
                break;
            case (int)Skill.WarMagic:
                sigilTrinket.SigilTrinketEffectId = ThreadSafeRandom.Next(5, 10);
                break;
        }

        switch (sigilTrinket.SigilTrinketEffectId)
        {
            // PROT
            case (int)SigilTrinketEffect.ScarabCastProt:
            {
                var element = ThreadSafeRandom.Next(0, 7);

                switch (element)
                {
                    default:
                        color = "black";
                        break;
                    case 1:
                        color = "white";
                        break;
                    case 2:
                        color = "purple";
                        break;
                    case 3:
                        color = "blue";
                        break;
                    case 4:
                        color = "green";
                        break;
                    case 5:
                        color = "yellow";
                        break;
                    case 6:
                        color = "orange";
                        break;
                    case 7:
                        color = "red";
                        break;
                }

                sigilTrinket.PaletteTemplate = PaletteTemplateColors["white"];
                sigilTrinket.IconId = IconColorIds[(int)SigilTrinketType.Scarab]["white"];
                sigilTrinket.IconOverlayId = VulnProtIconOverlayIds[color];

                sigilTrinket.Name += " of Protection";
                sigilTrinket.SigilTrinketCastSpellId = ProtSpellIds[color];
                sigilTrinket.Use =
                    $"Whenever the wielder casts a Heal, Revitalize, or Mana Boost spell, "
                    + $"there is a chance the target will also gain {ProtSpellNames[color]}.\n\n"
                    + $"The level of the buff is equal to the level of the casted spell, but cannot surpass the Max Level value.";
                break;
            }
            // VULN
            case (int)SigilTrinketEffect.ScarabCastVuln:
            {
                var element = ThreadSafeRandom.Next(0, 7);

                switch (element)
                {
                    default:
                        color = "black";
                        break;
                    case 1:
                        color = "white";
                        break;
                    case 2:
                        color = "purple";
                        break;
                    case 3:
                        color = "blue";
                        break;
                    case 4:
                        color = "green";
                        break;
                    case 5:
                        color = "yellow";
                        break;
                    case 6:
                        color = "orange";
                        break;
                    case 7:
                        color = "red";
                        break;
                }

                sigilTrinket.PaletteTemplate = PaletteTemplateColors["black"];
                sigilTrinket.IconId = IconColorIds[(int)SigilTrinketType.Scarab]["black"];
                sigilTrinket.IconOverlayId = VulnProtIconOverlayIds[color];

                sigilTrinket.Name += " of Vulnerability";
                sigilTrinket.SigilTrinketCastSpellId = VulnSpellIds[color];
                sigilTrinket.Use =
                    $"Whenever the wielder casts a Harm, Enfeeble, or Mana Drain spell, "
                    + $"there is a chance the target will also be inflicted with {VulnSpellNames[color]}.\n\n"
                    + $"The level of the debuff is equal to the level of the casted spell, but cannot surpass the Max Level value.";
                break;
            }
            // RATE
            case (int)SigilTrinketEffect.ScarabCastVitalRate:
                sigilTrinket.PaletteTemplate = PaletteTemplateColors["green"];
                sigilTrinket.IconId = IconColorIds[(int)SigilTrinketType.Scarab]["green"];

                sigilTrinket.Name += " of Growth";
                sigilTrinket.Use =
                    "Whenever the wielder casts a Heal or Harm spell, there is a chance the target will also be affected by Regeneration or Fester, respectively.\n\n"
                    + "Whenever the wielder casts a Revitalize or Enfeeble spell, there is a chance the target will also be affected by Rejuvenation or Exhaustion, respectively.\n\n"
                    + "Whenever the wielder casts a Mana Boost or Mana Drain spell, there is a chance the target will also be affected by Mana Renewal or Mana Depletion.\n\n"
                    + "The level of the buff is equal to the level of the casted spell, but cannot surpass the Max Level value.";
                break;
            // ITEM BUFF
            case (int)SigilTrinketEffect.ScarabCastItemBuff:
                sigilTrinket.PaletteTemplate = PaletteTemplateColors["purple"];
                sigilTrinket.IconId = IconColorIds[(int)SigilTrinketType.Scarab]["purple"];

                sigilTrinket.Name += " of Artifice";
                sigilTrinket.Use =
                    "Whenever the wielder casts a Heal spell, there is a chance the target will also be affected by Defender Other.\n\n"
                    + "Whenever the wielder casts a Revitalize spell, there is a chance the target will also be affected by Blood Drinker.\n\n"
                    + "Whenever the wielder casts a Mana Boost spell, there is a chance the target will also be affected by Spirit Drinker.\n\n"
                    + "The level of the buff is equal to the level of the casted spell, but cannot surpass the Max Level value.";
                break;
            // INTENSITY
            case (int)SigilTrinketEffect.ScarabIntensity:
                sigilTrinket.PaletteTemplate = PaletteTemplateColors["red"];
                sigilTrinket.IconId = IconColorIds[(int)SigilTrinketType.Scarab]["red"];

                sigilTrinket.SigilTrinketIntensity = GetIntensity(profile);

                sigilTrinket.Name += " of Intensity";
                sigilTrinket.Use =
                    $"Whenever the wielder casts any spell, there is a chance the spell will "
                    + $"gain bonus intensity.\n\n"
                    + $"Intensity increases damage and restoration amounts.\n\n"
                    + $"Only affects spells that are less than or equal to the Max level value.";
                break;
            // SHIELD
            case (int)SigilTrinketEffect.ScarabShield:
                sigilTrinket.PaletteTemplate = PaletteTemplateColors["gray"];
                sigilTrinket.IconId = IconColorIds[(int)SigilTrinketType.Scarab]["gray"];

                sigilTrinket.Name += " of Shielding";
                sigilTrinket.Use =
                    $"Whenever the wielder casts any spell, there is a chance they will "
                    + $"gain a protective buff, increasing damage reduction rating by 25 for 12 seconds.\n\n"
                    + $"Only affects spells that are less than or equal to the Max level value.";
                break;
            // MANA REDUCTION
            case (int)SigilTrinketEffect.ScarabManaReduction:
                sigilTrinket.PaletteTemplate = PaletteTemplateColors["blue"];
                sigilTrinket.IconId = IconColorIds[(int)SigilTrinketType.Scarab]["blue"];

                sigilTrinket.SigilTrinketManaReserved = 0;
                sigilTrinket.SigilTrinketReductionAmount = GetReductionAmount(profile);
                sigilTrinket.CooldownDuration -= 5;

                sigilTrinket.Name += " of Reduction";
                sigilTrinket.Use =
                    $"Whenever the wielder casts any spell, there is a chance that the cost of the spell will be reduced.\n\n"
                    + "Only affects spells that are less than or equal to the Max level value.";
                break;
            // DUPLICATE
            case (int)SigilTrinketEffect.ScarabDuplicate:
                sigilTrinket.PaletteTemplate = PaletteTemplateColors["olive"];
                sigilTrinket.IconId = IconColorIds[(int)SigilTrinketType.Scarab]["olive"];
                sigilTrinket.Name += " of Duplication";
                sigilTrinket.Use =
                    $"Whenever the wielder casts a War spell, there is a chance that the spell will be duplicated and cast against the same target.\n\n"
                    + "Only affects spells that are less than or equal to the Max level value.";
                break;
            // DETONATE
            case (int)SigilTrinketEffect.ScarabDetonate:
                sigilTrinket.PaletteTemplate = PaletteTemplateColors["yellow"];
                sigilTrinket.IconId = IconColorIds[(int)SigilTrinketType.Scarab]["yellow"];
                sigilTrinket.Name += " of Detonation";
                sigilTrinket.Use =
                    $"Whenever the wielder damages a creature with a War spell, there is a chance that the spell will detonate on impact, causing an explosion of the same element.\n\n"
                    + "Only affects spells that are less than or equal to the Max level value.";
                break;
            // CRUSHING
            case (int)SigilTrinketEffect.ScarabCrit:
                sigilTrinket.PaletteTemplate = PaletteTemplateColors["orange"];
                sigilTrinket.IconId = IconColorIds[(int)SigilTrinketType.Scarab]["orange"];

                sigilTrinket.SigilTrinketTriggerChance = 0;

                sigilTrinket.Name += " of Crushing";
                sigilTrinket.Use =
                    $"Whenever the wielder performs a critical strike with a War spell, they gain an offensive buff, increasing critical damage rating by 50 for 12 seconds.\n\n"
                    + "Only affects spells that are less than or equal to the Max level value.";
                break;
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

    private static double GetReservedVital(TreasureDeath treasureDeath, bool hybrid = false)
    {
        const double range = MaxReservedVital - MinReservedVital;
        var hybridMultiplier = hybrid ? 0.5 : 1.0;
        var roll = range * hybridMultiplier * GetDiminishingRoll(treasureDeath);

        return MaxReservedVital * hybridMultiplier - roll;
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
