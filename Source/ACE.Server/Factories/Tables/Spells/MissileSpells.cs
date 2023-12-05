using System.Collections.Generic;
using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using Serilog;

namespace ACE.Server.Factories.Tables.Spells
{
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
                Table[i] = new SpellId[NumTiers];

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
                    Table[i][j] = spellLevels[j];

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
            ( SpellId.SwiftKillerSelf1,  1.00f ),
            ( SpellId.DefenderSelf1,     1.00f ),
            ( SpellId.HeartSeekerSelf1,  1.00f ),
            ( SpellId.BloodDrinkerSelf1, 1.00f ),
        };

        public static ChanceTable<SpellId> missileProcs = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.Undef,              150.0f ),

            ( SpellId.StaminaToManaSelf1,   2.0f ),
            ( SpellId.ManaToStaminaSelf1,   2.0f ),
            ( SpellId.ManaToHealthSelf1,    2.0f ),

            ( SpellId.DrainMana1,           2.0f ),
            ( SpellId.DrainStamina1,        2.0f ),
            ( SpellId.DrainHealth1,         2.0f ),

            ( SpellId.ManaBoostSelf1,       1.0f ),
            ( SpellId.RevitalizeSelf1,      1.0f ),
            ( SpellId.HealSelf1,            1.0f ),
        };

        private static ChanceTable<SpellId> missileProcsCertain = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.StaminaToManaSelf1,   2.0f ),
            ( SpellId.ManaToStaminaSelf1,   2.0f ),
            ( SpellId.ManaToHealthSelf1,    2.0f ),

            ( SpellId.DrainMana1,           2.0f ),
            ( SpellId.DrainStamina1,        2.0f ),
            ( SpellId.DrainHealth1,         2.0f ),

            ( SpellId.ManaBoostSelf1,       1.0f ),
            ( SpellId.RevitalizeSelf1,      1.0f ),
            ( SpellId.HealSelf1,            1.0f ),
        };

        public static List<SpellId> Roll(TreasureDeath treasureDeath)
        {
            var spells = new List<SpellId>();

            foreach (var spell in weaponMissileSpells)
            {
                var rng = ThreadSafeRandom.NextInterval(treasureDeath.LootQualityMod);

                if (rng < spell.chance)
                    spells.Add(spell.spellId);
            }
            return spells;
        }

        public static SpellId RollProc(TreasureDeath treasureDeath)
        {
            float lootQualityMod = 0.0f;
            if (treasureDeath != null)
                lootQualityMod = treasureDeath.LootQualityMod;
            return missileProcs.Roll(lootQualityMod);
        }

        public static SpellId PseudoRandomRollProc(int seed)
        {
            return missileProcsCertain.PseudoRandomRoll(seed);
        }
    }
}
