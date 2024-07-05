using System.Collections.Generic;
using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Factories.Tables.Spells;

public static class MissileSpells
{
    private static readonly ILogger _log = Log.ForContext(typeof(MissileSpells));

    private static readonly List<SpellId> spells = new List<SpellId>()
    {
        SpellId.BloodDrinkerSelf1,
        SpellId.HeartSeekerSelf1,
        SpellId.DefenderSelf1,
        SpellId.SwiftKillerSelf1,
    };

    private static readonly int NumTiers = 8;

    // original api
    public static readonly SpellId[][] Table = new SpellId[spells.Count][];
    public static readonly List<SpellId> CreatureLifeTable = new List<SpellId>();

    static MissileSpells()
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
                _log.Error($"MissileSpells - couldn't find {spell}");
                continue;
            }

            if (spellLevels.Count != NumTiers)
            {
                _log.Error($"MissileSpells - expected {NumTiers} levels for {spell}, found {spellLevels.Count}");
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
                case SpellId.HeartSeekerSelf1:
                case SpellId.DefenderSelf1:
                case SpellId.SwiftKillerSelf1:
                    break;

                default:
                    CreatureLifeTable.Add(spell);
                    break;
            }
        }
    }

    // alt

    private static readonly List<(SpellId spellId, float chance)> weaponMissileSpells = new List<(SpellId, float)>()
    {
        (SpellId.SwiftKillerSelf1, 1.00f),
        (SpellId.DefenderSelf1, 1.00f),
        (SpellId.HeartSeekerSelf1, 1.00f),
        (SpellId.BloodDrinkerSelf1, 1.00f),
    };

    public static ChanceTable<SpellId> missileProcsLife = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

    public static ChanceTable<SpellId> missileProcsWarSlash = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.WhirlingBlade1, 1.0f),
    };

    public static ChanceTable<SpellId> missileProcsWarPierce = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.ForceBolt1, 1.0f),
    };

    public static ChanceTable<SpellId> missileProcsWarBludgeon = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.ShockWave1, 1.0f),
    };

    public static ChanceTable<SpellId> missileProcsWarAcid = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.AcidStream1, 1.0f),
    };

    public static ChanceTable<SpellId> missileProcsWarFire = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.FlameBolt1, 1.0f),
    };

    public static ChanceTable<SpellId> missileProcsWarCold = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.FrostBolt1, 1.0f),
    };

    public static ChanceTable<SpellId> missileProcsWarElectric = new ChanceTable<SpellId>(ChanceTableType.Weight)
    {
        (SpellId.LightningBolt1, 1.0f),
    };

    public static List<SpellId> Roll(TreasureDeath treasureDeath)
    {
        var spells = new List<SpellId>();

        foreach (var spell in weaponMissileSpells)
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
            return missileProcsLife.Roll(treasureDeath.LootQualityMod);
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
                    return missileProcsWarSlash.Roll(treasureDeath.LootQualityMod);
                }
                else
                {
                    return missileProcsWarPierce.Roll(treasureDeath.LootQualityMod);
                }

            case DamageType.Slash:
                return missileProcsWarSlash.Roll(treasureDeath.LootQualityMod);
            case DamageType.Pierce:
                return missileProcsWarPierce.Roll(treasureDeath.LootQualityMod);
            case DamageType.Bludgeon:
                return missileProcsWarBludgeon.Roll(treasureDeath.LootQualityMod);
            case DamageType.Acid:
                return missileProcsWarAcid.Roll(treasureDeath.LootQualityMod);
            case DamageType.Fire:
                return missileProcsWarFire.Roll(treasureDeath.LootQualityMod);
            case DamageType.Cold:
                return missileProcsWarCold.Roll(treasureDeath.LootQualityMod);
            case DamageType.Electric:
                return missileProcsWarElectric.Roll(treasureDeath.LootQualityMod);
        }
    }
}
