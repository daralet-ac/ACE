using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using Serilog;

namespace ACE.Server.Factories.Tables.Cantrips
{
    public static class JewelryCantrips
    {
        private static readonly ILogger _log = Log.ForContext(typeof(JewelryCantrips));

        private static readonly List<SpellId> spells = new List<SpellId>()
        {
            SpellId.CANTRIPHEAVYWEAPONSAPTITUDE1,
            SpellId.CANTRIPLIGHTWEAPONSAPTITUDE1,
            SpellId.CANTRIPFINESSEWEAPONSAPTITUDE1,
            SpellId.CANTRIPMISSILEWEAPONSAPTITUDE1,
            SpellId.CANTRIPTWOHANDEDAPTITUDE1,

            SpellId.CANTRIPINVULNERABILITY1,
            SpellId.CANTRIPIMPREGNABILITY1,
            SpellId.CANTRIPMAGICRESISTANCE1,

            SpellId.CANTRIPCREATUREENCHANTMENTAPTITUDE1,
            SpellId.CANTRIPITEMENCHANTMENTAPTITUDE1,
            SpellId.CANTRIPLIFEMAGICAPTITUDE1,
            SpellId.CANTRIPWARMAGICAPTITUDE1,
            SpellId.CantripVoidMagicAptitude1,
            SpellId.CantripSummoningProwess1,

            SpellId.CANTRIPARCANEPROWESS1,
            SpellId.CANTRIPDECEPTIONPROWESS1,
            SpellId.CANTRIPHEALINGPROWESS1,
            SpellId.CANTRIPLOCKPICKPROWESS1,
            SpellId.CANTRIPJUMPINGPROWESS1,
            SpellId.CANTRIPMANACONVERSIONPROWESS1,
            SpellId.CANTRIPSPRINT1,

            SpellId.CantripDualWieldAptitude1,
            SpellId.CantripDirtyFightingProwess1,
            SpellId.CantripRecklessnessProwess1,    // was in original twice
            SpellId.CantripSneakAttackProwess1,

            SpellId.CantripShieldAptitude1,

            SpellId.CANTRIPALCHEMICALPROWESS1,
            SpellId.CANTRIPCOOKINGPROWESS1,
            SpellId.CANTRIPFLETCHINGPROWESS1,

            SpellId.CANTRIPLEADERSHIP1,
            SpellId.CANTRIPFEALTY1,

            SpellId.CantripSalvaging1,
            SpellId.CANTRIPARMOREXPERTISE1,
            SpellId.CANTRIPITEMEXPERTISE1,
            SpellId.CANTRIPMAGICITEMEXPERTISE1,
            SpellId.CANTRIPWEAPONEXPERTISE1,

            SpellId.CANTRIPMONSTERATTUNEMENT1,
            SpellId.CANTRIPPERSONATTUNEMENT1,

            // life

            SpellId.CANTRIPARMOR1,      // was in original twice
            SpellId.CANTRIPACIDWARD1,
            SpellId.CANTRIPBLUDGEONINGWARD1,
            SpellId.CANTRIPFROSTWARD1,
            SpellId.CANTRIPSTORMWARD1,
            SpellId.CANTRIPFLAMEWARD1,
            SpellId.CANTRIPSLASHINGWARD1,
            SpellId.CANTRIPPIERCINGWARD1,
        };

        private static readonly int NumLevels = 4;

        // original api
        public static readonly SpellId[][] Table = new SpellId[spells.Count][];

        static JewelryCantrips()
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
                    _log.Error($"JewelryCantrips - couldn't find {spell}");
                    continue;
                }

                if (spellLevels.Count != NumLevels)
                {
                    _log.Error($"JewelryCantrips - expected {NumLevels} levels for {spell}, found {spellLevels.Count}");
                    continue;
                }

                for (var j = 0; j < NumLevels; j++)
                    Table[i][j] = spellLevels[j];
            }
        }

        private static ChanceTable<SpellId> jewelryCantrips = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CANTRIPLIGHTWEAPONSAPTITUDE1,        2.0f ), // CANTRIPAXEAPTITUDE1
            ( SpellId.CANTRIPFINESSEWEAPONSAPTITUDE1,      2.0f ), // CANTRIPDAGGERAPTITUDE1
            //( SpellId.CANTRIPMACEAPTITUDE1,                2.0f ),
            ( SpellId.CANTRIPSPEARAPTITUDE1,               2.0f ),
            //( SpellId.CANTRIPSTAFFAPTITUDE1,               2.0f ),
            ( SpellId.CANTRIPHEAVYWEAPONSAPTITUDE1,        2.0f ), // CANTRIPSWORDAPTITUDE1
            ( SpellId.CANTRIPUNARMEDAPTITUDE1,             2.0f ),
            ( SpellId.CANTRIPMISSILEWEAPONSAPTITUDE1,      2.0f ), // CANTRIPBOWAPTITUDE1
            //( SpellId.CANTRIPCROSSBOWAPTITUDE1,            2.0f ),
            ( SpellId.CANTRIPTHROWNAPTITUDE1,              2.0f ),

            ( SpellId.CANTRIPIMPREGNABILITY1,              2.0f ),
            ( SpellId.CANTRIPINVULNERABILITY1,             2.0f ),
            ( SpellId.CANTRIPMAGICRESISTANCE1,             2.0f ),

            ( SpellId.CANTRIPLIFEMAGICAPTITUDE1,           2.0f ),
            ( SpellId.CANTRIPWARMAGICAPTITUDE1,            2.0f ),

            ( SpellId.CANTRIPALCHEMICALPROWESS1,           2.0f ),
            ( SpellId.CANTRIPARCANEPROWESS1,               2.0f ),
            ( SpellId.CANTRIPCOOKINGPROWESS1,              2.0f ),
            ( SpellId.CANTRIPDECEPTIONPROWESS1,            2.0f ),
            //( SpellId.CANTRIPFEALTY1,                      2.0f ),
            ( SpellId.CANTRIPFLETCHINGPROWESS1,            2.0f ),
            ( SpellId.CANTRIPHEALINGPROWESS1,              2.0f ),
            ( SpellId.CANTRIPJUMPINGPROWESS1,              2.0f ),
            //( SpellId.CANTRIPLEADERSHIP1,                  2.0f ),
            ( SpellId.CANTRIPLOCKPICKPROWESS1,             2.0f ),
            ( SpellId.CANTRIPMANACONVERSIONPROWESS1,       2.0f ),
            ( SpellId.CANTRIPMONSTERATTUNEMENT1,           2.0f ),
            ( SpellId.CANTRIPSPRINT1,                      2.0f ),

            //( SpellId.CantripSalvaging1,                   2.0f ),
            ( SpellId.CantripShieldAptitude1,              2.0f ),
            ( SpellId.CantripDualWieldAptitude1,           2.0f ),
            ( SpellId.CANTRIPTWOHANDEDAPTITUDE1,           2.0f ),
            //( SpellId.CantripArmorAptitude1,               2.0f ),
        };

        public static SpellId Roll()
        {
            return jewelryCantrips.Roll();
        }
    }
}
