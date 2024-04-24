using System;
using System.Collections.Generic;

using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;

namespace ACE.Server.Factories.Tables
{
    /// <summary>
    /// Defines which spells can be found on item types
    /// </summary>
    public static class SpellSelectionTable
    {
        // thanks to Sapphire Knight and Butterflygolem for helping to figure this part out!

        // gems
        private static ChanceTable<SpellId> spellSelectionGroup1 = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CANTRIPSTRENGTH1,               2.0f ),
            ( SpellId.CANTRIPENDURANCE1,              2.0f ),
            ( SpellId.CANTRIPCOORDINATION1,           2.0f ),
            ( SpellId.CANTRIPQUICKNESS1,              2.0f ),
            ( SpellId.CANTRIPFOCUS1,                  2.0f ),
            ( SpellId.CANTRIPWILLPOWER1,              2.0f ),

            ( SpellId.CANTRIPARCANEPROWESS1,          1.0f ),

            ( SpellId.CANTRIPMAGICRESISTANCE1,        1.0f ),
            //( SpellId.CANTRIPIMPREGNABILITY1,         1.0f ),
            ( SpellId.CANTRIPINVULNERABILITY1,        1.0f ),
            ( SpellId.CANTRIPDECEPTIONPROWESS1,       1.0f ),
            ( SpellId.CANTRIPMONSTERATTUNEMENT1,      1.0f ),
            ( SpellId.CANTRIPJUMPINGPROWESS1,         1.0f ),
            ( SpellId.CANTRIPSPRINT1,                 1.0f ),
            ( SpellId.CANTRIPHEALINGPROWESS1,         1.0f ),
            //( SpellId.CANTRIPLEADERSHIP1,           2.0f ),
            //( SpellId.CANTRIPFEALTY1,               2.0f ),

            ( SpellId.CANTRIPALCHEMICALPROWESS1,      1.0f ),
            ( SpellId.CANTRIPCOOKINGPROWESS1,         1.0f ),
            ( SpellId.CANTRIPFLETCHINGPROWESS1,       1.0f ),
            ( SpellId.CANTRIPLOCKPICKPROWESS1,        1.0f ),
            //( SpellId.CantripSalvaging1,            1.0f ),

            ( SpellId.CANTRIPLIGHTWEAPONSAPTITUDE1,    1.0f ), // AxeMasteryOther1
            ( SpellId.CANTRIPFINESSEWEAPONSAPTITUDE1,  1.0f ), // DaggerMasteryOther1
            //( SpellId.MaceMasteryOther1,             0.5f ),
            ( SpellId.CANTRIPSPEARAPTITUDE1,           1.0f ),
            //( SpellId.StaffMasteryOther1,            0.5f ),
            ( SpellId.CANTRIPHEAVYWEAPONSAPTITUDE1,    1.0f ), // SwordMasteryOther1
            ( SpellId.CANTRIPUNARMEDAPTITUDE1,         1.0f ),
            ( SpellId.CANTRIPMISSILEWEAPONSAPTITUDE1,  1.0f ), // BowMasteryOther1
            //( SpellId.CrossbowMasteryOther1,         0.5f ),
            ( SpellId.CANTRIPTHROWNAPTITUDE1,          1.0f ),
            ( SpellId.CantripShieldAptitude1,          1.0f ),
            ( SpellId.CantripDualWieldAptitude1,       1.0f ),
            ( SpellId.CANTRIPTWOHANDEDAPTITUDE1,       1.0f ),
            ( SpellId.CANTRIPWARMAGICAPTITUDE1,        1.0f ),
            ( SpellId.CANTRIPLIFEMAGICAPTITUDE1,       1.0f ),

            ( SpellId.CANTRIPARMOR1,                   1.0f ),
            ( SpellId.CANTRIPACIDWARD1,                1.0f ),
            ( SpellId.CANTRIPBLUDGEONINGWARD1,         1.0f ),
            ( SpellId.CANTRIPFROSTWARD1,               1.0f ),
            ( SpellId.CANTRIPSTORMWARD1,               1.0f ),
            ( SpellId.CANTRIPFLAMEWARD1,               1.0f ),
            ( SpellId.CANTRIPSLASHINGWARD1,            1.0f ),
            ( SpellId.CANTRIPPIERCINGWARD1,            1.0f ),
            ( SpellId.CANTRIPMANAGAIN1,             3.0f ),
            ( SpellId.CANTRIPSTAMINAGAIN1,          3.0f ),
            ( SpellId.CANTRIPHEALTHGAIN1,           3.0f ),

            ( SpellId.CANTRIPBLOODTHIRST1,          1.0f ),
            ( SpellId.CANTRIPHEARTTHIRST1,          1.0f ),
            ( SpellId.CANTRIPDEFENDER1,             1.0f ),
            ( SpellId.CANTRIPSWIFTHUNTER1,          1.0f ),

            ( SpellId.CantripSpiritThirst1,         1.0f ),
            ( SpellId.CANTRIPDEFENDER1,             1.0f ),
        };

        // jewelry
        private static ChanceTable<SpellId> spellSelectionGroup2 = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CANTRIPSTRENGTH1,               2.0f ),
            ( SpellId.CANTRIPENDURANCE1,              2.0f ),
            ( SpellId.CANTRIPCOORDINATION1,           2.0f ),
            ( SpellId.CANTRIPQUICKNESS1,              2.0f ),
            ( SpellId.CANTRIPFOCUS1,                  2.0f ),
            ( SpellId.CANTRIPWILLPOWER1,              2.0f ),
            ( SpellId.CANTRIPLIGHTWEAPONSAPTITUDE1,    1.0f ), // AxeMasteryOther1
            ( SpellId.CANTRIPFINESSEWEAPONSAPTITUDE1,  1.0f ), // DaggerMasteryOther1
            //( SpellId.MaceMasteryOther1,             0.5f ),
            ( SpellId.CANTRIPSPEARAPTITUDE1,           1.0f ),
            //( SpellId.StaffMasteryOther1,            0.5f ),
            ( SpellId.CANTRIPHEAVYWEAPONSAPTITUDE1,    1.0f ), // SwordMasteryOther1
            ( SpellId.CANTRIPUNARMEDAPTITUDE1,         1.0f ),
            ( SpellId.CANTRIPMISSILEWEAPONSAPTITUDE1,  1.0f ), // BowMasteryOther1
            //( SpellId.CrossbowMasteryOther1,         0.5f ),
            ( SpellId.CANTRIPTHROWNAPTITUDE1,          1.0f ),
            ( SpellId.CantripShieldAptitude1,          1.0f ),
            ( SpellId.CantripDualWieldAptitude1,       1.0f ),
            ( SpellId.CANTRIPTWOHANDEDAPTITUDE1,       1.0f ),
            ( SpellId.CANTRIPWARMAGICAPTITUDE1,        1.0f ),
            ( SpellId.CANTRIPLIFEMAGICAPTITUDE1,       1.0f ),
        };

        // crowns
        private static ChanceTable<SpellId> spellSelectionGroup3 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        // orbs
        private static ChanceTable<SpellId> spellSelectionGroup4 = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CantripSpiritThirst1,         1.0f ),
            ( SpellId.CantripHermeticLink1,         1.0f ),
            ( SpellId.CANTRIPDEFENDER1,             1.0f ),
        };

        // wands, staffs, sceptres, batons
        private static ChanceTable<SpellId> spellSelectionGroup5 = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CantripSpiritThirst1,         1.0f ),
            ( SpellId.CantripHermeticLink1,         1.0f ),
            ( SpellId.CANTRIPDEFENDER1,             1.0f ),
        };

        // one-handed melee weapons
        private static ChanceTable<SpellId> spellSelectionGroup6 = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CANTRIPBLOODTHIRST1,          1.0f ),
            ( SpellId.CANTRIPHEARTTHIRST1,          1.0f ),
            ( SpellId.CANTRIPDEFENDER1,             1.0f ),
            ( SpellId.CANTRIPSWIFTHUNTER1,          1.0f ),
        };

        // bracers, breastplates, coats, cuirasses, girths, hauberks, pauldrons, chest armor, sleeves
        private static ChanceTable<SpellId> spellSelectionGroup7 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        // shields
        private static ChanceTable<SpellId> spellSelectionGroup8 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        // gauntlets
        private static ChanceTable<SpellId> spellSelectionGroup9 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        // helms, basinets, helmets, coifs, cowls, heaumes, kabutons
        private static ChanceTable<SpellId> spellSelectionGroup10 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        // boots, chiran sandals, sollerets
        private static ChanceTable<SpellId> spellSelectionGroup11 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        // breeches, jerkins, shirts, pants, tunics, doublets, trousers, pantaloons
        private static ChanceTable<SpellId> spellSelectionGroup12 = new ChanceTable<SpellId>()
        {
        };

        // caps, qafiyas, turbans, fezs, berets
        private static ChanceTable<SpellId> spellSelectionGroup13 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        // cloth gloves (1 entry?)
        private static ChanceTable<SpellId> spellSelectionGroup14 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        // greaves, leggings, tassets, leather pants
        private static ChanceTable<SpellId> spellSelectionGroup15 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        // dinnerware
        private static ChanceTable<SpellId> spellSelectionGroup16 = new ChanceTable<SpellId>()
        {
        };

        // added

        // missile weapons, two-handed weapons
        private static ChanceTable<SpellId> spellSelectionGroup17 = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CANTRIPBLOODTHIRST1,          1.0f ),
            ( SpellId.CANTRIPHEARTTHIRST1,          1.0f ),
            ( SpellId.CANTRIPDEFENDER1,             1.0f ),
            ( SpellId.CANTRIPSWIFTHUNTER1,          1.0f ),
        };

        // shoes, loafers, slippers, sandals
        private static ChanceTable<SpellId> spellSelectionGroup18 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        // nether caster
        private static ChanceTable<SpellId> spellSelectionGroup19 = new ChanceTable<SpellId>(ChanceTableType.Weight)
        {
            ( SpellId.CantripSpiritThirst1,         1.0f ),
            ( SpellId.CantripHermeticLink1,         1.0f ),
            ( SpellId.CANTRIPDEFENDER1,             1.0f ),
        };

        // leather cap (1 entry?)
        private static ChanceTable<SpellId> spellSelectionGroup20 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        private static ChanceTable<SpellId> spellSelectionGroup21 = new ChanceTable<SpellId>(ChanceTableType.Weight)
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

        /// <summary>
        /// Key is (PropertyInt.TsysMutationData >> 24) - 1
        /// </summary>
        private static readonly List<ChanceTable<SpellId>> spellSelectionGroup = new List<ChanceTable<SpellId>>()
        {
            spellSelectionGroup1,
            spellSelectionGroup2,
            spellSelectionGroup3,
            spellSelectionGroup4,
            spellSelectionGroup5,
            spellSelectionGroup6,
            spellSelectionGroup7,
            spellSelectionGroup8,
            spellSelectionGroup9,
            spellSelectionGroup10,
            spellSelectionGroup11,
            spellSelectionGroup12,
            spellSelectionGroup13,
            spellSelectionGroup14,
            spellSelectionGroup15,
            spellSelectionGroup16,
            spellSelectionGroup17,
            spellSelectionGroup18,
            spellSelectionGroup19,
            spellSelectionGroup20,
        };

        /// <summary>
        /// Rolls for a creature / life spell for an item
        /// </summary>
        /// <param name="spellCode">the SpellCode from WorldObject</param>
        public static SpellId Roll(int spellCode)
        {
            return spellSelectionGroup[spellCode - 1].Roll();
        }
    }
}
