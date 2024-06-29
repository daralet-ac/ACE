using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using Serilog;

namespace ACE.Server.Factories.Tables.Cantrips
{
    public static class ArmorCantrips
    {
        private static readonly ILogger _log = Log.ForContext(typeof(ArmorCantrips));

        private static readonly List<SpellId> spells = new List<SpellId>()
        {
            // creature cantrips

            SpellId.CANTRIPSTRENGTH1,
            SpellId.CANTRIPENDURANCE1,
            SpellId.CANTRIPCOORDINATION1,
            SpellId.CANTRIPQUICKNESS1,
            SpellId.CANTRIPFOCUS1,
            SpellId.CANTRIPWILLPOWER1,

            SpellId.CANTRIPHEAVYWEAPONSAPTITUDE1,   // Martial Weapons
            SpellId.CANTRIPLIGHTWEAPONSAPTITUDE1,   // Staff
            SpellId.CANTRIPFINESSEWEAPONSAPTITUDE1, // Dagger
            SpellId.CANTRIPUNARMEDAPTITUDE1,
            SpellId.CANTRIPMISSILEWEAPONSAPTITUDE1, // Bows
            SpellId.CANTRIPTHROWNAPTITUDE1,

            SpellId.CANTRIPTWOHANDEDAPTITUDE1,
            SpellId.CantripDualWieldAptitude1,
            SpellId.CantripShieldAptitude1,

            SpellId.CANTRIPINVULNERABILITY1,
            //SpellId.CANTRIPIMPREGNABILITY1,
            SpellId.CANTRIPMAGICRESISTANCE1,

            SpellId.CANTRIPLIFEMAGICAPTITUDE1,
            SpellId.CANTRIPWARMAGICAPTITUDE1,
            SpellId.CANTRIPMANACONVERSIONPROWESS1,
            //SpellId.CANTRIPCREATUREENCHANTMENTAPTITUDE1,
            //SpellId.CANTRIPITEMENCHANTMENTAPTITUDE1,
            //SpellId.CantripVoidMagicAptitude1,
            //SpellId.CantripSummoningProwess1,
            
            SpellId.CANTRIPLOCKPICKPROWESS1,    // Thievery
            SpellId.CANTRIPMONSTERATTUNEMENT1,
            SpellId.CANTRIPARCANEPROWESS1,
            SpellId.CANTRIPDECEPTIONPROWESS1,
            SpellId.CANTRIPHEALINGPROWESS1,
            SpellId.CANTRIPJUMPINGPROWESS1,         // missing from original cantrips, but was in spells
                                                    // should a separate lower armor cantrips table be added for this?
            SpellId.CANTRIPSPRINT1,                 // missing from original cantrips, but was in spells
                                                    // should a separate lower armor cantrips table be added for this?

            //SpellId.CantripDirtyFightingProwess1,
            //SpellId.CantripRecklessnessProwess1,    // was in original twice
            //SpellId.CantripSneakAttackProwess1,

            //SpellId.CANTRIPLEADERSHIP1,
            //SpellId.CANTRIPFEALTY1,

            //SpellId.CantripSalvaging1,
            //SpellId.CANTRIPARMOREXPERTISE1,
            //SpellId.CANTRIPITEMEXPERTISE1,
            //SpellId.CANTRIPMAGICITEMEXPERTISE1,
            //SpellId.CANTRIPWEAPONEXPERTISE1,
            //SpellId.CANTRIPFLETCHINGPROWESS1,
            //SpellId.CANTRIPALCHEMICALPROWESS1,
            //SpellId.CANTRIPCOOKINGPROWESS1,


            // life cantrips

            //SpellId.CANTRIPARMOR1,      // was in original twice
            //SpellId.CANTRIPACIDWARD1,
            //SpellId.CANTRIPBLUDGEONINGWARD1,
            //SpellId.CANTRIPFROSTWARD1,
            //SpellId.CANTRIPSTORMWARD1,
            //SpellId.CANTRIPFLAMEWARD1,
            //SpellId.CANTRIPSLASHINGWARD1,
            //SpellId.CANTRIPPIERCINGWARD1,

            // item cantrips

            //SpellId.CANTRIPIMPENETRABILITY1,
            //SpellId.CANTRIPSLASHINGBANE1,
            //SpellId.CANTRIPACIDBANE1,
            //SpellId.CANTRIPBLUDGEONINGBANE1,
            //SpellId.CANTRIPFROSTBANE1,
            //SpellId.CANTRIPSTORMBANE1,
            //SpellId.CANTRIPFLAMEBANE1,
            //SpellId.CANTRIPPIERCINGBANE1,
        };

        private static readonly int NumLevels = 4;

        // original api
        public static readonly SpellId[][] Table = new SpellId[spells.Count][];

        static ArmorCantrips()
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
                    _log.Error($"ArmorCantrips - couldn't find {spell}");
                    continue;
                }

                if (spellLevels.Count != NumLevels)
                {
                    _log.Error($"ArmorCantrips - expected {NumLevels} levels for {spell}, found {spellLevels.Count}");
                    continue;
                }

                for (var j = 0; j < NumLevels; j++)
                    Table[i][j] = spellLevels[j];
            }
        }

        private static ChanceTable<SpellId> armorCantrips = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CANTRIPSTRENGTH1,                    1.0f ),
            ( SpellId.CANTRIPENDURANCE1,                   1.0f ),
            ( SpellId.CANTRIPCOORDINATION1,                1.0f ),
            ( SpellId.CANTRIPQUICKNESS1,                   1.0f ),
            ( SpellId.CANTRIPFOCUS1,                       1.0f ),
            ( SpellId.CANTRIPWILLPOWER1,                   1.0f ),

            ( SpellId.CANTRIPHEAVYWEAPONSAPTITUDE1,        1.0f ),
            ( SpellId.CANTRIPLIGHTWEAPONSAPTITUDE1,        1.0f ),
            ( SpellId.CANTRIPFINESSEWEAPONSAPTITUDE1,      1.0f ),
            ( SpellId.CANTRIPMISSILEWEAPONSAPTITUDE1,      1.0f ),
            ( SpellId.CANTRIPTWOHANDEDAPTITUDE1,           1.0f ),

            //( SpellId.CANTRIPIMPREGNABILITY1,              1.0f ),
            ( SpellId.CANTRIPINVULNERABILITY1,             1.0f ),
            ( SpellId.CANTRIPMAGICRESISTANCE1,             1.0f ),

            //( SpellId.CANTRIPCREATUREENCHANTMENTAPTITUDE1, 1.0f ),
            //( SpellId.CANTRIPITEMENCHANTMENTAPTITUDE1,     1.0f ),
            ( SpellId.CANTRIPLIFEMAGICAPTITUDE1,           1.0f ),
            ( SpellId.CANTRIPWARMAGICAPTITUDE1,            1.0f ),
            //( SpellId.CantripVoidMagicAptitude1,           1.0f ),

            //( SpellId.CANTRIPIMPENETRABILITY1,             1.0f ),
            //( SpellId.CANTRIPACIDBANE1,                    1.0f ),
            //( SpellId.CANTRIPBLUDGEONINGBANE1,             1.0f ),
            //( SpellId.CANTRIPFLAMEBANE1,                   1.0f ),
            //( SpellId.CANTRIPFROSTBANE1,                   1.0f ),
            //( SpellId.CANTRIPPIERCINGBANE1,                1.0f ),
            //( SpellId.CANTRIPSLASHINGBANE1,                1.0f ),
            //( SpellId.CANTRIPSTORMBANE1,                   1.0f ),

            ( SpellId.CANTRIPARMOR1,                       1.0f ),
            ( SpellId.CANTRIPACIDWARD1,                    1.0f ),
            ( SpellId.CANTRIPBLUDGEONINGWARD1,             1.0f ),
            ( SpellId.CANTRIPFLAMEWARD1,                   1.0f ),
            ( SpellId.CANTRIPFROSTWARD1,                   1.0f ),
            ( SpellId.CANTRIPPIERCINGWARD1,                1.0f ),
            ( SpellId.CANTRIPSLASHINGWARD1,                1.0f ),
            ( SpellId.CANTRIPSTORMWARD1,                   1.0f ),

            //( SpellId.CANTRIPALCHEMICALPROWESS1,           0.01f ),
            //( SpellId.CANTRIPARCANEPROWESS1,               0.01f ),
            //( SpellId.CANTRIPARMOREXPERTISE1,              0.01f ),
            //( SpellId.CANTRIPCOOKINGPROWESS1,              0.01f ),
            //( SpellId.CANTRIPDECEPTIONPROWESS1,            0.01f ),
            //( SpellId.CANTRIPFEALTY1,                      0.01f ),
            //( SpellId.CANTRIPFLETCHINGPROWESS1,            0.01f ),
            //( SpellId.CANTRIPHEALINGPROWESS1,              0.01f ),
            //( SpellId.CANTRIPITEMEXPERTISE1,               0.01f ),
            ( SpellId.CANTRIPJUMPINGPROWESS1,              1.0f ),
            //( SpellId.CANTRIPLEADERSHIP1,                  0.01f ),
            ( SpellId.CANTRIPLOCKPICKPROWESS1,             1.0f ),
            //( SpellId.CANTRIPMAGICITEMEXPERTISE1,          0.01f ),
            //( SpellId.CANTRIPMANACONVERSIONPROWESS1,       0.01f ),
            ( SpellId.CANTRIPMONSTERATTUNEMENT1,           1.0f ),
            //( SpellId.CANTRIPPERSONATTUNEMENT1,            0.005f ),
            //( SpellId.CantripSalvaging1,                   0.01f ),
            ( SpellId.CANTRIPSPRINT1,                      1.0f ),
            //( SpellId.CANTRIPWEAPONEXPERTISE1,             0.01f ),

            //( SpellId.CantripDirtyFightingProwess1,        1.0f ),
            ( SpellId.CantripDualWieldAptitude1,           1.0f ),
            //( SpellId.CantripRecklessnessProwess1,         1.0f ),
            ( SpellId.CantripShieldAptitude1,              1.0f ),
            //( SpellId.CantripSneakAttackProwess1,          1.0f ),
            //( SpellId.CantripSummoningProwess1,            1.0f ),
        };

        public static SpellId Roll()
        {
            return armorCantrips.Roll();
        }
    }
}
