using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using Serilog;

namespace ACE.Server.Factories.Tables.Cantrips
{
    public static class ClothArmorCantrips 
    {
        private static readonly ILogger _log = Log.ForContext(typeof(ClothArmorCantrips));

        private static readonly List<SpellId> spells = new List<SpellId>()
        {
                 SpellId.CANTRIPARMOR1,                         
                 SpellId.CANTRIPACIDWARD1,                      
                 SpellId.CANTRIPBLUDGEONINGWARD1,               
                 SpellId.CANTRIPFLAMEWARD1,                     
                 SpellId.CANTRIPFROSTWARD1,                     
                 SpellId.CANTRIPPIERCINGWARD1,                  
                 SpellId.CANTRIPSLASHINGWARD1,                  
                 SpellId.CANTRIPSTORMWARD1,                      
        };

        private static readonly int NumLevels = 4;

        // original api
        public static readonly SpellId[][] Table = new SpellId[spells.Count][];

        static ClothArmorCantrips()
        {
            // takes ~0.3ms
            BuildSpells();
        }

        private static void BuildSpells()
        {
            for (var i = 0; i < spells.Count; i++)
                Table[i] = new SpellId[NumLevels];

            for (var i = 0; i < spells.Count; i++)
            {
                var spell = spells[i];

                var spellLevels = SpellLevelProgression.GetSpellLevels(spell);

                if (spellLevels == null)
                {
                    _log.Error($"ClothArmorCantrips - couldn't find {spell}");
                    continue;
                }

                if (spellLevels.Count != NumLevels)
                {
                    _log.Error($"ClothArmorCantrips - expected {NumLevels} levels for {spell}, found {spellLevels.Count}");
                    continue;
                }

                for (var j = 0; j < NumLevels; j++)
                    Table[i][j] = spellLevels[j];
            }
        }

        private static ChanceTable<SpellId> clothArmorCantrips = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CANTRIPARMOR1,                          1.0f ),
            ( SpellId.CANTRIPACIDWARD1,                       1.0f ),
            ( SpellId.CANTRIPBLUDGEONINGWARD1,                1.0f ),
            ( SpellId.CANTRIPFLAMEWARD1,                      1.0f ),
            ( SpellId.CANTRIPFROSTWARD1,                      1.0f ),
            ( SpellId.CANTRIPPIERCINGWARD1,                   1.0f ),
            ( SpellId.CANTRIPSLASHINGWARD1,                   1.0f ),
            ( SpellId.CANTRIPSTORMWARD1,                      1.0f )
        };

        public static SpellId Roll()
        {
            return clothArmorCantrips.Roll();
        }
    }
}
