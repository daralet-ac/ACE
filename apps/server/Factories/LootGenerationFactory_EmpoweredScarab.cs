using System;
using System.Collections.Generic;
using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Factories.Tables;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories;

public static partial class LootGenerationFactory
{
    private static readonly Dictionary<int, uint> IconOverlay2_Level = new Dictionary<int, uint>()
    {
        { 1, 100690996 },
        { 2, 100690997 },
        { 3, 100690998 },
        { 4, 100690999 },
        { 5, 100691000 },
        { 6, 100691001 },
        { 7, 100691002 },
    };

    private static readonly Dictionary<string, uint> Icon_Colors = new Dictionary<string, uint>()
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
    };

    private static readonly Dictionary<string, uint> VulnProt_IconOverlays = new Dictionary<string, uint>()
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

    private static readonly Dictionary<string, int> PaletteTemplate_Colors = new Dictionary<string, int>()
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

    private static readonly Dictionary<string, uint> Vuln_SpellIds = new Dictionary<string, uint>()
    {
        { "black", 39 }, // black
        { "white", 1048 }, // white
        { "purple", 1084 }, // purple
        { "blue", 1060 }, // blue
        { "green", 521 }, // green
        { "yellow", 1151 }, // yellow
        { "orange", 1127 }, // orange
        { "red", 21 } // red
    };

    private static readonly Dictionary<string, string> Vuln_SpellNames = new Dictionary<string, string>()
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

    private static readonly Dictionary<string, uint> Prot_SpellIds = new Dictionary<string, uint>()
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

    private static readonly Dictionary<string, string> Prot_SpellNames = new Dictionary<string, string>()
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

    private static readonly Dictionary<string, string> Duplication_ElementString = new Dictionary<string, string>()
    {
        { "white", " Bludgeoning" }, // white
        { "purple", " Lightning" }, // purple
        { "blue", " Cold" }, // blue
        { "green", "n Acid" }, // green
        { "yellow", " Piercing" }, // yellow
        { "orange", " Slashing" }, // orange
        { "red", " Fire" } // red
    };

    private static readonly Dictionary<string, int> ElementId = new Dictionary<string, int>()
    {
        { "white", 0x4 }, // white - blunt
        { "purple", 0x40 }, // purple - electric
        { "blue", 0x8 }, // blue - cold
        { "green", 0x20 }, // green - acid
        { "yellow", 0x2 }, // yellow - pierce
        { "orange", 0x1 }, // orange - slash
        { "red", 0x10 } // red - fire
    };

    private static readonly Dictionary<string, uint> OverlayIds = new Dictionary<string, uint>()
    {
        { "life", 100689496 },
        { "war", 100689495 },
    };

    private static WorldObject CreateEmpoweredScarab(TreasureDeath profile, bool mutate = true)
    {
        var wcid = EmpoweredScarabWcids.Roll(profile.Tier);

        var wo = WorldObjectFactory.CreateNewWorldObject((uint)wcid);

        if (mutate)
        {
            MutateEmpoweredanaScarab(wo, profile);
        }

        return wo;
    }

    private static void MutateEmpoweredanaScarab(WorldObject wo, TreasureDeath profile)
    {
        var empoweredScarab = wo as EmpoweredScarab;

        string color;

        empoweredScarab.CooldownDuration = GetCooldown(profile);
        empoweredScarab.EmpoweredScarabTriggerChance = GetChance(profile);
        empoweredScarab.EmpoweredScarabManaReserved = GetReservedMana(profile);
        empoweredScarab.EmpoweredScarabMaxLevel = Math.Clamp(profile.Tier - 1, 1, 7);
        empoweredScarab.WieldDifficulty = RollWieldDifficulty(profile.Tier);
        empoweredScarab.ItemMaxLevel = Math.Clamp(profile.Tier - 1, 1, 7);
        empoweredScarab.ItemBaseXp = GetBaseLevelCost(profile);
        empoweredScarab.ItemTotalXp = 0;

        if (empoweredScarab.EmpoweredScarabSchool == (int)MagicSchool.LifeMagic)
        {
            empoweredScarab.EmpoweredScarabEffectId = ThreadSafeRandom.Next(1, 7);
            empoweredScarab.WieldSkillType = (int)Skill.LifeMagic;
        }
        else
        {
            empoweredScarab.EmpoweredScarabEffectId = ThreadSafeRandom.Next(5, 10);
            empoweredScarab.WieldSkillType = (int)Skill.WarMagic;
        }

        // PROT
        if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.CastProt)
        {
            var element = ThreadSafeRandom.Next(0, 7);

            switch (element)
            {
                default:
                case 0:
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

            empoweredScarab.PaletteTemplate = PaletteTemplate_Colors["white"];
            empoweredScarab.IconId = Icon_Colors["white"];
            empoweredScarab.IconOverlayId = VulnProt_IconOverlays[color];

            empoweredScarab.Name += " of Protection";
            empoweredScarab.EmpoweredScarabCastSpellId = Prot_SpellIds[color];
            empoweredScarab.Use =
                $"Whenever the wielder casts a Heal, Revitalize, or Mana Boost spell, "
                + $"there is a chance the target will also gain {Prot_SpellNames[color]}.\n\n"
                + $"The level of the buff is equal to the level of the casted spell, but cannot surpass the Max Level value.";
        }

        // VULN
        if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.CastVuln)
        {
            var element = ThreadSafeRandom.Next(0, 7);

            switch (element)
            {
                default:
                case 0:
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

            empoweredScarab.PaletteTemplate = PaletteTemplate_Colors["black"];
            empoweredScarab.IconId = Icon_Colors["black"];
            empoweredScarab.IconOverlayId = VulnProt_IconOverlays[color];

            empoweredScarab.Name += " of Vulnerability";
            empoweredScarab.EmpoweredScarabCastSpellId = Vuln_SpellIds[color];
            empoweredScarab.Use =
                $"Whenever the wielder casts a Harm, Enfeeble, or Mana Drain spell, "
                + $"there is a chance the target will also be inflicted with {Vuln_SpellNames[color]}.\n\n"
                + $"The level of the debuff is equal to the level of the casted spell, but cannot surpass the Max Level value.";
        }
        // RATE
        else if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.CastVitalRate)
        {
            empoweredScarab.PaletteTemplate = PaletteTemplate_Colors["green"];
            empoweredScarab.IconId = Icon_Colors["green"];

            empoweredScarab.Name += " of Growth";
            empoweredScarab.Use =
                "Whenever the wielder casts a Heal or Harm spell, there is a chance the target will also be affected by Regeneration or Fester, respectively.\n\n"
                + "Whenever the wielder casts a Revitalize or Enfeeble spell, there is a chance the target will also be affected by Rejuvenation or Exhaustion, respectively.\n\n"
                + "Whenever the wielder casts a Mana Boost or Mana Drain spell, there is a chance the target will also be affected by Mana Renewal or Mana Depletion.\n\n"
                + "The level of the buff is equal to the level of the casted spell, but cannot surpass the Max Level value.";
        }
        // ITEM BUFF
        else if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.CastItemBuff)
        {
            empoweredScarab.PaletteTemplate = PaletteTemplate_Colors["purple"];
            empoweredScarab.IconId = Icon_Colors["purple"];

            empoweredScarab.Name += " of Artifice";
            empoweredScarab.Use =
                "Whenever the wielder casts a Heal spell, there is a chance the target will also be affected by Defender Other.\n\n"
                + "Whenever the wielder casts a Revitalize spell, there is a chance the target will also be affected by Blood Drinker.\n\n"
                + "Whenever the wielder casts a Mana Boost spell, there is a chance the target will also be affected by Spirit Drinker.\n\n"
                + "The level of the buff is equal to the level of the casted spell, but cannot surpass the Max Level value.";
        }
        // INTENSITY
        else if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.Intensity)
        {
            empoweredScarab.PaletteTemplate = PaletteTemplate_Colors["red"];
            empoweredScarab.IconId = Icon_Colors["red"];

            empoweredScarab.EmpoweredScarabIntensity = GetIntensity(profile);

            empoweredScarab.Name += " of Intensity";
            empoweredScarab.Use =
                $"Whenever the wielder casts any spell, there is a chance the spell will "
                + $"gain bonus intensity.\n\n"
                + $"Intensity increases damage and restoration amounts.\n\n"
                + $"Only affects spells that are less than or equal to the Max level value.";
        }
        // SHIELD
        else if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.Shield)
        {
            empoweredScarab.PaletteTemplate = PaletteTemplate_Colors["gray"];
            empoweredScarab.IconId = Icon_Colors["gray"];

            empoweredScarab.Name += " of Shielding";
            empoweredScarab.Use =
                $"Whenever the wielder casts any spell, there is a chance they will "
                + $"gain a protective buff, increasing damage reduction rating by 25 for 12 seconds.\n\n"
                + $"Only affects spells that are less than or equal to the Max level value.";
        }
        // MANA REDUCTION
        else if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.ManaReduction)
        {
            empoweredScarab.PaletteTemplate = PaletteTemplate_Colors["blue"];
            empoweredScarab.IconId = Icon_Colors["blue"];

            empoweredScarab.EmpoweredScarabManaReserved = 0;
            empoweredScarab.EmpoweredScarabReductionAmount = GetReductionAmount(profile);
            empoweredScarab.CooldownDuration -= 5;

            empoweredScarab.Name += " of Reduction";
            empoweredScarab.Use =
                $"Whenever the wielder casts any spell, there is a chance that the cost of the spell will be reduced.\n\n"
                + "Only affects spells that are less than or equal to the Max level value.";
        }
        // DUPLICATE
        else if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.Duplicate)
        {
            empoweredScarab.PaletteTemplate = PaletteTemplate_Colors["olive"];
            empoweredScarab.IconId = Icon_Colors["olive"];
            empoweredScarab.Name += " of Duplication";
            empoweredScarab.Use =
                $"Whenever the wielder casts a War spell, there is a chance that the spell will be duplicated and cast against the same target.\n\n"
                + "Only affects spells that are less than or equal to the Max level value.";
        }
        // DETONATE
        else if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.Detonate)
        {
            empoweredScarab.PaletteTemplate = PaletteTemplate_Colors["yellow"];
            empoweredScarab.IconId = Icon_Colors["yellow"];
            empoweredScarab.Name += " of Detonation";
            empoweredScarab.Use =
                $"Whenever the wielder damages a creature with a War spell, there is a chance that the spell will detonate on impact, causing an explosion of the same element.\n\n"
                + "Only affects spells that are less than or equal to the Max level value.";
        }
        // CRUSHING
        else if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.Crit)
        {
            empoweredScarab.PaletteTemplate = PaletteTemplate_Colors["orange"];
            empoweredScarab.IconId = Icon_Colors["orange"];

            empoweredScarab.EmpoweredScarabTriggerChance = 0;

            empoweredScarab.Name += " of Crushing";
            empoweredScarab.Use =
                $"Whenever the wielder performs a critical strike with a War spell, they gain an offensive buff, increasing critical damage rating by 50 for 12 seconds.\n\n"
                + "Only affects spells that are less than or equal to the Max level value.";
        }
    }

    public static readonly double MaxChance = 0.75;
    public static readonly double MinChance = 0.25;

    public static readonly double MaxCooldown = 20.0;
    public static readonly double MinCooldown = 10.0;

    public static readonly double MaxReservedMana = 0.2;
    public static readonly double MinReservedMana = 0.1;

    public static readonly double MaxIntensity = 0.75;
    public static readonly double MinIntensity = 0.25;

    public static readonly double MaxReduction = 0.75;
    public static readonly double MinReduction = 0.25;

    private static double GetChance(TreasureDeath treasureDeath)
    {
        var range = MaxChance - MinChance;
        var roll = range * GetDiminishingRoll(treasureDeath);

        return MinChance + roll;
    }

    private static double GetCooldown(TreasureDeath treasureDeath)
    {
        var range = MaxCooldown - MinCooldown;
        var roll = range * GetDiminishingRoll(treasureDeath);

        return MaxCooldown - roll;
    }

    private static double GetReservedMana(TreasureDeath treasureDeath)
    {
        var range = MaxReservedMana - MinReservedMana;
        var roll = range * GetDiminishingRoll(treasureDeath);

        return MaxReservedMana - roll;
    }

    private static double GetIntensity(TreasureDeath treasureDeath)
    {
        var range = MaxIntensity - MinIntensity;
        var roll = range * GetDiminishingRoll(treasureDeath);

        return MinIntensity + roll;
    }

    private static double GetReductionAmount(TreasureDeath treasureDeath)
    {
        var range = MaxReduction - MinReduction;
        var roll = range * GetDiminishingRoll(treasureDeath);

        return MinReduction + roll;
    }

    private static uint GetBaseLevelCost(TreasureDeath treasureDeath)
    {
        switch (treasureDeath.Tier)
        {
            default:
            case 1:
            case 2:
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
