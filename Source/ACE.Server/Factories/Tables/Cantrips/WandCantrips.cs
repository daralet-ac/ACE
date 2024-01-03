using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using Serilog;

namespace ACE.Server.Factories.Tables.Cantrips
{
    public static class WandCantrips
    {
        private static readonly ILogger _log = Log.ForContext(typeof(WandCantrips));

        private static readonly List<SpellId> spells = new List<SpellId>()
        {
            SpellId.CANTRIPFOCUS1,
            SpellId.CANTRIPWILLPOWER1,

            SpellId.CANTRIPCREATUREENCHANTMENTAPTITUDE1,
            SpellId.CANTRIPITEMENCHANTMENTAPTITUDE1,
            SpellId.CANTRIPLIFEMAGICAPTITUDE1,
            SpellId.CANTRIPWARMAGICAPTITUDE1,
            SpellId.CantripVoidMagicAptitude1,      // missing from original

            SpellId.CANTRIPARCANEPROWESS1,
            SpellId.CANTRIPMANACONVERSIONPROWESS1,

            SpellId.CantripSneakAttackProwess1,

            SpellId.CANTRIPDEFENDER1,
            SpellId.CantripHermeticLink1,
            SpellId.CantripSpiritThirst1,
        };

        private static readonly int NumLevels = 4;

        // original api
        public static readonly SpellId[][] Table = new SpellId[spells.Count][];

        static WandCantrips()
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
                    _log.Error($"WandCantrips - couldn't find {spell}");
                    continue;
                }

                if (spellLevels.Count != NumLevels)
                {
                    _log.Error($"WandCantrips - expected {NumLevels} levels for {spell}, found {spellLevels.Count}");
                    continue;
                }

                for (var j = 0; j < NumLevels; j++)
                    Table[i][j] = spellLevels[j];
            }
        }

        // retail appears to have been bugged here for WarMagicAptitude and VoidMagicAptitude
        // orbs: war magic aptitude
        // non-elemental casters: void magic aptitude
        // war elemental casters: void magic aptitude
        // void elemental casters: war magic aptitude

        private static ChanceTable<SpellId> casterCantrips = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CantripSpiritThirst1,                   1.0f ),
            ( SpellId.CANTRIPDEFENDER1,                       1.0f ),
            ( SpellId.CantripHermeticLink1,                   1.0f )
        };

        public static SpellId Roll()
        {
            return casterCantrips.Roll();
        }
    }
}
