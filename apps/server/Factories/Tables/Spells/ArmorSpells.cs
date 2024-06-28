using System.Collections.Generic;
using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using Serilog;

namespace ACE.Server.Factories.Tables.Spells
{
    public static class ArmorSpells
    {
        private static readonly ILogger _log = Log.ForContext(typeof(ArmorSpells));

        private static readonly List<SpellId> spells = new List<SpellId>()
        {
            // life buffs
            SpellId.CANTRIPARMOR1,

            SpellId.CANTRIPSLASHINGWARD1,
            SpellId.CANTRIPPIERCINGWARD1,
            SpellId.CANTRIPBLUDGEONINGWARD1,

            SpellId.CANTRIPFLAMEWARD1,
            SpellId.CANTRIPFROSTWARD1,
            SpellId.CANTRIPACIDWARD1,
            SpellId.CANTRIPSTORMWARD1,
        };

        private static readonly int NumTiers = 8;

        // original api
        public static readonly SpellId[][] Table = new SpellId[spells.Count][];
        public static readonly List<SpellId> CreatureLifeTable = new List<SpellId>();

        static ArmorSpells()
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
                    _log.Error($"ArmorSpells - couldn't find {spell}");
                    continue;
                }

                if (spellLevels.Count != NumTiers)
                {
                    _log.Error($"ArmorSpells - expected {NumTiers} levels for {spell}, found {spellLevels.Count}");
                    continue;
                }

                for (var j = 0; j < NumTiers; j++)
                    Table[i][j] = spellLevels[j];

                // build a version of this table w/out item spells
                switch (spell)
                {
                    case SpellId.Impenetrability1:
                    case SpellId.BladeBane1:
                    case SpellId.PiercingBane1:
                    case SpellId.BludgeonBane1:
                    case SpellId.FlameBane1:
                    case SpellId.FrostBane1:
                    case SpellId.AcidBane1:
                    case SpellId.LightningBane1:
                        break;

                    default:
                        CreatureLifeTable.Add(spell);
                        break;
                }
            }
        }

        // alt

        // this table also applies to clothing w/ AL

        private static readonly List<(SpellId spellId, float chance)> armorSpells = new List<(SpellId, float)>()
        {
            ( SpellId.PiercingBane1,    0.15f ),
            ( SpellId.FlameBane1,       0.15f ),
            ( SpellId.FrostBane1,       0.15f ),
            ( SpellId.Impenetrability1, 1.00f ),
            ( SpellId.AcidBane1,        0.15f ),
            ( SpellId.BladeBane1,       0.15f ),
            ( SpellId.LightningBane1,   0.15f ),
            ( SpellId.BludgeonBane1,    0.15f ),
        };

        public static List<SpellId> Roll(TreasureDeath treasureDeath)
        {
            // this roll also applies to clothing w/ AL!
            // ie., shirts and pants would never have item spells on them,
            // but cloth gloves would

            // thanks to Sapphire Knight and Butterflygolem for helping to figure this part out!

            var spells = new List<SpellId>();

            foreach (var spell in armorSpells)
            {
                var rng = ThreadSafeRandom.NextInterval(treasureDeath.LootQualityMod);

                if (rng < spell.chance)
                    spells.Add(spell.spellId);
            }
            return spells;
        }
    }
}
