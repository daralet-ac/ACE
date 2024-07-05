using System.Collections.Generic;
using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Factories.Tables.Spells;

public static class MeleeSpells
{
    private static readonly ILogger _log = Log.ForContext(typeof(MeleeSpells));

    private static readonly List<SpellId> spells = new List<SpellId>()
    {
        SpellId.BloodDrinkerSelf1,
        SpellId.DefenderSelf1,
        SpellId.HeartSeekerSelf1,
        SpellId.SwiftKillerSelf1,
    };

    private static readonly int NumTiers = 8;

    // original api
    public static readonly SpellId[][] Table = new SpellId[spells.Count][];
    public static readonly List<SpellId> CreatureLifeTable = new List<SpellId>();

    static MeleeSpells()
    {
        // takes ~0.3ms
        BuildSpells();
    }

    private static void BuildSpells()
    {
        for (var i = 0; i < spells.Count; i++)
        {
            Table[i] = new SpellId[NumTiers];
        }

        for (var i = 0; i < spells.Count; i++)
        {
            var spell = spells[i];

            var spellLevels = SpellLevelProgression.GetSpellLevels(spell);

            if (spellLevels == null)
            {
                _log.Error($"MeleeSpells - couldn't find {spell}");
                continue;
            }

            if (spellLevels.Count != NumTiers)
            {
                _log.Error($"MeleeSpells - expected {NumTiers} levels for {spell}, found {spellLevels.Count}");
                continue;
            }

            for (var j = 0; j < NumTiers; j++)
            {
                Table[i][j] = spellLevels[j];
            }

            // build a version of this table w/out item spells
            switch (spell)
            {
                case SpellId.BloodDrinkerSelf1:
                case SpellId.DefenderSelf1:
                case SpellId.HeartSeekerSelf1:
                case SpellId.SwiftKillerSelf1:
                    break;

                default:
                    CreatureLifeTable.Add(spell);
                    break;
            }
        }
    }

    // alt

    private static readonly List<(SpellId spellId, float chance)> weaponMeleeSpells = new List<(SpellId, float)>()
    {
        (SpellId.DefenderSelf1, 1.00f),
        (SpellId.BloodDrinkerSelf1, 1.00f),
        (SpellId.SwiftKillerSelf1, 1.00f),
        (SpellId.HeartSeekerSelf1, 1.00f),
    };

    public static ChanceTable<SpellId> meleeProcsLife = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.StaminaToManaSelf1, 1.0f),
        (SpellId.ManaToStaminaSelf1, 1.0f),
        (SpellId.ManaToHealthSelf1, 1.0f),
        (SpellId.DrainMana1, 1.0f),
        (SpellId.DrainStamina1, 1.0f),
        (SpellId.DrainHealth1, 1.0f),
        (SpellId.ManaBoostSelf1, 1.0f),
        (SpellId.RevitalizeSelf1, 1.0f),
        (SpellId.HealSelf1, 1.0f),
        (SpellId.HarmOther1, 1.0f),
        (SpellId.ExhaustionOther1, 1.0f),
        (SpellId.ManaDrainOther1, 1.0f),
    };

    public static ChanceTable<SpellId> meleeProcsWarSlash = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.WhirlingBlade1, 1.0f),
    };

    public static ChanceTable<SpellId> meleeProcsWarPierce = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.ForceBolt1, 1.0f),
    };

    public static ChanceTable<SpellId> meleeProcsWarBludgeon = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.ShockWave1, 1.0f),
    };

    public static ChanceTable<SpellId> meleeProcsWarAcid = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.AcidStream1, 1.0f),
    };

    public static ChanceTable<SpellId> meleeProcsWarFire = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.FlameBolt1, 1.0f),
    };

    public static ChanceTable<SpellId> meleeProcsWarCold = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.FrostBolt1, 1.0f),
    };

    public static ChanceTable<SpellId> meleeProcsWarElectric = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.LightningBolt1, 1.0f),
    };

    public static List<SpellId> Roll(TreasureDeath treasureDeath)
    {
        var spells = new List<SpellId>();

        foreach (var spell in weaponMeleeSpells)
        {
            var rng = ThreadSafeRandom.NextInterval(treasureDeath.LootQualityMod);

            if (rng < spell.chance)
            {
                spells.Add(spell.spellId);
            }
        }
        return spells;
    }

    public static SpellId RollProc(WorldObject wo, TreasureDeath treasureDeath, bool warSpell)
    {
        if (warSpell)
        {
            return WarSpellProc(wo, treasureDeath);
        }
        else
        {
            return meleeProcsLife.Roll(treasureDeath.LootQualityMod);
        }
    }

    private static SpellId WarSpellProc(WorldObject wo, TreasureDeath treasureDeath)
    {
        switch (wo.W_DamageType)
        {
            default:
            case DamageType.SlashPierce:
                var rng = ThreadSafeRandom.Next(0, 1) == 0 ? true : false;
                if (rng)
                {
                    return meleeProcsWarSlash.Roll(treasureDeath.LootQualityMod);
                }
                else
                {
                    return meleeProcsWarPierce.Roll(treasureDeath.LootQualityMod);
                }

            case DamageType.Slash:
                return meleeProcsWarSlash.Roll(treasureDeath.LootQualityMod);
            case DamageType.Pierce:
                return meleeProcsWarPierce.Roll(treasureDeath.LootQualityMod);
            case DamageType.Bludgeon:
                return meleeProcsWarBludgeon.Roll(treasureDeath.LootQualityMod);
            case DamageType.Acid:
                return meleeProcsWarAcid.Roll(treasureDeath.LootQualityMod);
            case DamageType.Fire:
                return meleeProcsWarFire.Roll(treasureDeath.LootQualityMod);
            case DamageType.Cold:
                return meleeProcsWarCold.Roll(treasureDeath.LootQualityMod);
            case DamageType.Electric:
                return meleeProcsWarElectric.Roll(treasureDeath.LootQualityMod);
        }
    }
}
