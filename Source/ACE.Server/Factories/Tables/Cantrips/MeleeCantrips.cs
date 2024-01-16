using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using Serilog;

namespace ACE.Server.Factories.Tables.Cantrips
{
    public static class MeleeCantrips
    {
        private static readonly ILogger _log = Log.ForContext(typeof(MeleeCantrips));

        private static readonly List<SpellId> spells = new List<SpellId>()
        {
            //SpellId.CANTRIPSTRENGTH1,
            //SpellId.CANTRIPENDURANCE1,
            //SpellId.CANTRIPCOORDINATION1,
            //SpellId.CANTRIPQUICKNESS1,      // added, according to spellSelectionGroup6

            SpellId.CANTRIPBLOODTHIRST1,
            SpellId.CANTRIPHEARTTHIRST1,
            SpellId.CANTRIPDEFENDER1,
            SpellId.CANTRIPSWIFTHUNTER1,

            //SpellId.CantripDualWieldAptitude1,
            //SpellId.CantripDirtyFightingProwess1,
            //SpellId.CantripRecklessnessProwess1,
            //SpellId.CantripSneakAttackProwess1,
        };

        private static readonly int NumLevels = 4;

        // original api
        public static readonly SpellId[][] Table = new SpellId[spells.Count][];

        static MeleeCantrips()
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
                    _log.Error($"MeleeCantrips - couldn't find {spell}");
                    continue;
                }

                if (spellLevels.Count != NumLevels)
                {
                    _log.Error($"MeleeCantrips - expected {NumLevels} levels for {spell}, found {spellLevels.Count}");
                    continue;
                }

                for (var j = 0; j < NumLevels; j++)
                    Table[i][j] = spellLevels[j];
            }
        }

        private static ChanceTable<SpellId> meleeCantrips = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CANTRIPBLOODTHIRST1,          1.0f ),
            ( SpellId.CANTRIPDEFENDER1,             1.0f ),
            ( SpellId.CANTRIPHEARTTHIRST1,          1.0f ),
            ( SpellId.CANTRIPSWIFTHUNTER1,          1.0f )
        };

        public static SpellId Roll()
        {
            return meleeCantrips.Roll();
        }
    }
}
