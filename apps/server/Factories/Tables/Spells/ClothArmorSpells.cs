using System.Collections.Generic;
using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using Serilog;

namespace ACE.Server.Factories.Tables.Spells
{
    public static class ClothArmorSpells
    {
        private static readonly ILogger _log = Log.ForContext(typeof(ClothArmorSpells));

        private static readonly List<SpellId> spells = new List<SpellId>()
        {
             SpellId.CANTRIPARMOR1,               
             SpellId.CANTRIPACIDWARD1,            
             SpellId.CANTRIPBLUDGEONINGWARD1,     
             SpellId.CANTRIPFROSTWARD1,           
             SpellId.CANTRIPSTORMWARD1,           
             SpellId.CANTRIPFLAMEWARD1,           
             SpellId.CANTRIPSLASHINGWARD1,        
             SpellId.CANTRIPPIERCINGWARD1,        
             SpellId.CANTRIPMANAGAIN1,            
             SpellId.CANTRIPSTAMINAGAIN1,         
             SpellId.CANTRIPHEALTHGAIN1,          
        };

        private static readonly int NumTiers = 8;

        // original api
        public static readonly SpellId[][] Table = new SpellId[spells.Count][];

        static ClothArmorSpells()
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
                    _log.Error($"ClothArmorSpells - couldn't find {spell}");
                    continue;
                }

                if (spellLevels.Count != NumTiers)
                {
                    _log.Error($"ClothArmorSpells - expected {NumTiers} levels for {spell}, found {spellLevels.Count}");
                    continue;
                }

                for (var j = 0; j < NumTiers; j++)
                    Table[i][j] = spellLevels[j];
            }
        }

        // alt

        // this table also applies to clothing w/ AL

        private static readonly List<(SpellId spellId, float chance)> clothArmorSpells = new List<(SpellId, float)>()
        {
            ( SpellId.CANTRIPARMOR1,                5.0f ),
            ( SpellId.CANTRIPACIDWARD1,             5.0f ),
            ( SpellId.CANTRIPBLUDGEONINGWARD1,      5.0f ),
            ( SpellId.CANTRIPFROSTWARD1,            5.0f ),
            ( SpellId.CANTRIPSTORMWARD1,            5.0f ),
            ( SpellId.CANTRIPFLAMEWARD1,            5.0f ),
            ( SpellId.CANTRIPSLASHINGWARD1,         5.0f ),
            ( SpellId.CANTRIPPIERCINGWARD1,         5.0f ),
            ( SpellId.CANTRIPMANAGAIN1,             3.0f ),
            ( SpellId.CANTRIPSTAMINAGAIN1,          3.0f ),
            ( SpellId.CANTRIPHEALTHGAIN1,           3.0f ),
        };

        public static List<SpellId> Roll(TreasureDeath treasureDeath)
        {
            var spells = new List<SpellId>();

            foreach (var spell in clothArmorSpells)
            {
                var rng = ThreadSafeRandom.NextInterval(treasureDeath.LootQualityMod);

                if (rng < spell.chance)
                    spells.Add(spell.spellId);
            }
            return spells;
        }
    }
}
