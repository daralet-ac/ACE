using System;
using System.Collections.Generic;

using ACE.Common;
using ACE.Database.Models.World;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Factories.Tables.Wcids
{
    public static class ArmorWcids
    {
        private static ChanceTable<WeenieClassName> LeatherWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            // shield
            ( WeenieClassName.buckler,                1f ),

            // headgear
            ( WeenieClassName.capleather,              0.2f ),
            ( WeenieClassName.cowlleather,             0.2f ),
            ( WeenieClassName.basinetleather,          0.2f ),
            ( WeenieClassName.cowlleathernew,          0.2f ),
            ( WeenieClassName.basinetleathernew,       0.2f ),

            // handwear
            ( WeenieClassName.gauntletsleather,        0.33f ),
            ( WeenieClassName.gauntletsleathernew,     0.33f ),
            ( WeenieClassName.longgauntletsleathernew, 0.33f ),

            // footwear
            ( WeenieClassName.bootsleather,            0.5f ),
            ( WeenieClassName.bootsleathernew,         0.5f ),

            // single slot 0.25f to account for multislot overlap + old and new types
            ( WeenieClassName.breastplateleather,      0.25f ),
            ( WeenieClassName.girthleather,            0.25f ),
            ( WeenieClassName.tassetsleather,          0.25f ),
            ( WeenieClassName.greavesleather,          0.25f ),
            ( WeenieClassName.pauldronsleather,        0.25f ),
            ( WeenieClassName.bracersleather,          0.25f ),

            ( WeenieClassName.girthleathernew,         0.25f ),
            ( WeenieClassName.pauldronsleathernew,     0.25f ),
            ( WeenieClassName.bracersleathernew,       0.25f ),
            ( WeenieClassName.tassetsleathernew,       0.25f ),
            ( WeenieClassName.greavesleathernew,       0.25f ),

            // multislot
            ( WeenieClassName.coatleather,             0.125f ),
            ( WeenieClassName.cuirassleather,          0.125f ),
            ( WeenieClassName.shirtleather,            0.125f ),
            ( WeenieClassName.leggingsleather,         0.125f ),
            ( WeenieClassName.sleevesleather,          0.125f ),
            
            ( WeenieClassName.coatleathernew,          0.125f ),
            ( WeenieClassName.cuirassleathernew,       0.125f ),
            ( WeenieClassName.shirtleathernew,         0.125f ),
            ( WeenieClassName.breastplateleathernew,   0.125f ),
            ( WeenieClassName.shortsleathernew,        0.125f ),
            ( WeenieClassName.pantsleathernew,         0.125f ),
            ( WeenieClassName.leggingsleathernew,      0.125f ),
            ( WeenieClassName.sleevesleathernew,       0.125f ),
            
        };

        private static ChanceTable<WeenieClassName> StuddedLeatherWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.buckler,              0.25f ),
            // headgear
            ( WeenieClassName.cowlstuddedleather,        0.5f ),
            ( WeenieClassName.basinetstuddedleather,     0.5f ),

            // handwear
            ( WeenieClassName.gauntletsstuddedleather,   1.0f ),

            // footwear
            ( WeenieClassName.bootsreinforcedleather,    1.0f ),

            // single slot
            ( WeenieClassName.breastplatestuddedleather, 0.25f ),
            ( WeenieClassName.girthstuddedleather,       0.5f ),
            ( WeenieClassName.tassetsstuddedleather,     0.5f ),
            ( WeenieClassName.greavesstuddedleather,     0.5f ),
            ( WeenieClassName.pauldronsstuddedleather,   0.5f ),
            ( WeenieClassName.bracersstuddedleather,     0.5f ),

            // multislot
            ( WeenieClassName.coatstuddedleather,        0.25f ),
            ( WeenieClassName.shirtstuddedleather,       0.25f ),
            ( WeenieClassName.cuirassstuddedleather,     0.25f ),
            ( WeenieClassName.leggingsstuddedleather,    0.25f ),
            ( WeenieClassName.sleevesstuddedleather,     0.25f ),

            
        };

        private static ChanceTable<WeenieClassName> YoroiWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.buckler,              0.25f ),
            // headgear
            ( WeenieClassName.kabuton,              1.0f ),
            // handwear
            ( WeenieClassName.gauntletsleather,      1.0f ),
            // footwear
            ( WeenieClassName.bootssteeltoe,        1.0f ),

            // even spread of ms coverage, so 0.5f for all
            ( WeenieClassName.breastplateyoroi,     0.5f ),
            ( WeenieClassName.cuirassyoroi,         0.5f ),
            ( WeenieClassName.girthyoroi,           0.5f ),

            ( WeenieClassName.leggingsyoroi,        0.5f ),
            ( WeenieClassName.tassetsyoroi,        0.5f ),
            ( WeenieClassName.greavesyoroi,        0.5f ),

            ( WeenieClassName.sleevesyoroi,        0.5f ),
            ( WeenieClassName.pauldronsyoroi,      0.5f ),
            ( WeenieClassName.kote,                0.5f ),
        };

        private static ChanceTable<WeenieClassName> KoujiaWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
             ( WeenieClassName.buckler,              0.25f ),
            // headgear
            ( WeenieClassName.kabuton,              1.0f ),
            // handwear
            ( WeenieClassName.gauntletsleather,      1.0f ),
            // footwear
            ( WeenieClassName.bootssteeltoe,        1.0f ),

            ( WeenieClassName.breastplatekoujia, 1.0f ),
            ( WeenieClassName.leggingskoujia,    1.0f ),
            ( WeenieClassName.sleeveskoujia,     1.0f ),
        };

        private static ChanceTable<WeenieClassName> LoricaWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
         {
            ( WeenieClassName.helmlorica,        1.0f ),
            ( WeenieClassName.bootslorica,       1.0f ),
            ( WeenieClassName.gauntletslorica,   1.0f ),
            ( WeenieClassName.breastplatelorica, 1.0f ),
            ( WeenieClassName.leggingslorica,    1.0f ),
            ( WeenieClassName.sleeveslorica,     1.0f ),
         };

        private static ChanceTable<WeenieClassName> OlthoiKoujiaWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace37190_olthoikoujiagauntlets,   0.20f ),
            ( WeenieClassName.ace37198_olthoikoujiakabuton,     0.20f ),
            ( WeenieClassName.ace37203_olthoikoujialeggings,    0.20f ),
            ( WeenieClassName.ace37206_olthoikoujiasleeves,     0.20f ),
            ( WeenieClassName.ace37215_olthoikoujiabreastplate, 0.20f ),
        };




        private static ChanceTable<WeenieClassName> ChainmailWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            // shields
            ( WeenieClassName.shieldkite,           0.25f ),
            ( WeenieClassName.shieldround,          0.25f ),
            
            // headgear
            ( WeenieClassName.capmetal,             0.33f ),
            ( WeenieClassName.mailcoif,             0.33f ),
            ( WeenieClassName.basinetchainmail,     0.33f ),

            // handwear
            ( WeenieClassName.gauntletschainmail,   1.0f ),

            // footwear
            ( WeenieClassName.sollerets,            1.0f ),

            // single slot
            ( WeenieClassName.breastplatechainmail, 0.5f ),
            ( WeenieClassName.pauldronschainmail,   0.5f ),
            ( WeenieClassName.bracerschainmail,     0.5f ),
            ( WeenieClassName.girthchainmail,       0.5f ),
            ( WeenieClassName.tassetschainmail,     0.5f ),
            ( WeenieClassName.greaveschainmail,     0.5f ),

            // multislot
            ( WeenieClassName.shirtchainmail,       0.25f ),
            ( WeenieClassName.hauberkchainmail,     0.25f ),
            ( WeenieClassName.leggingschainmail,    0.25f ),
            ( WeenieClassName.sleeveschainmail,     0.25f ),

        };


        private static ChanceTable<WeenieClassName> ScalemailWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            //shields
            ( WeenieClassName.shieldkite,           0.25f ),
            ( WeenieClassName.shieldround,          0.25f ),

            // headgear
            ( WeenieClassName.baigha,               0.33f ),
            ( WeenieClassName.basinetscalemail,     0.33f ),
            ( WeenieClassName.coifscale,            0.33f ),

            // handwear
            ( WeenieClassName.gauntletsscalemail,   1.0f ),

            // footwear
            ( WeenieClassName.sollerets,            1.0f ),

            // single slot
            ( WeenieClassName.breastplatescalemail, 1.0f ),
            ( WeenieClassName.girthscalemail,       1.0f ),
            ( WeenieClassName.tassetsscalemail,     1.0f ),
            ( WeenieClassName.greavesscalemail,     1.0f ),
            ( WeenieClassName.pauldronsscalemail,   1.0f ),
            ( WeenieClassName.bracersscalemail,     1.0f ),

            // multislot
            ( WeenieClassName.cuirassscalemail,     1.0f ),
            ( WeenieClassName.hauberkscalemail,     1.0f ),
            ( WeenieClassName.shirtscalemail,       1.0f ),
            ( WeenieClassName.leggingsscalemail,    1.0f ),
            ( WeenieClassName.sleevesscalemail,     1.0f ),

        };

        private static ChanceTable<WeenieClassName> PlatemailWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            //shields
            ( WeenieClassName.shieldkitelarge,      0.25f ),
            ( WeenieClassName.shieldroundlarge,     0.25f ),
            ( WeenieClassName.shieldtower,          0.2f ),

            // headgear
            ( WeenieClassName.armet,                1.0f ),
            ( WeenieClassName.heaume,               1.0f ),
            ( WeenieClassName.heaumenew,            1.0f ),

            // handwear
            ( WeenieClassName.gauntletsplatemail,   1.0f ),

            // footwear
            ( WeenieClassName.sollerets,            1.0f ),

            // single slot
            ( WeenieClassName.breastplateplatemail, 0.5f ),
            ( WeenieClassName.tassetsplatemail,     0.5f ),
            ( WeenieClassName.greavesplatemail,     0.5f ),
            ( WeenieClassName.pauldronsplatemail,   0.5f ),
            ( WeenieClassName.vambracesplatemail,   0.5f ),
            ( WeenieClassName.girthplatemail,       0.5f ),

            // multislot
            ( WeenieClassName.hauberkplatemail,     0.25f ),
            ( WeenieClassName.cuirassplatemail,     0.25f ),
            ( WeenieClassName.leggingsplatemail,    0.25f ),
            ( WeenieClassName.sleevesplatemail,     0.25f ),

        };

        private static ChanceTable<WeenieClassName> CeldonWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        { 
            // headgear
            ( WeenieClassName.helmet,               0.5f ),
            ( WeenieClassName.helmhorned,           0.5f ),

            // handwear
            ( WeenieClassName.gauntletsplatemail,   1.0f ),

            // footwear
            ( WeenieClassName.sollerets,         1.0f ),

            ( WeenieClassName.girthceldon,       1.0f ),
            ( WeenieClassName.breastplateceldon, 1.0f ),
            ( WeenieClassName.leggingsceldon,    1.0f ),
            ( WeenieClassName.sleevesceldon,     1.0f ),
        };

        private static ChanceTable<WeenieClassName> CovenantWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.shieldcovenant,      0.5f ),

            ( WeenieClassName.helmcovenant,        1.0f ),
            ( WeenieClassName.gauntletscovenant,   1.0f ),
            ( WeenieClassName.bootscovenant,       1.0f ),
            ( WeenieClassName.breastplatecovenant, 1.0f ),
            ( WeenieClassName.girthcovenant,       1.0f ),
            ( WeenieClassName.tassetscovenant,     1.0f ),
            ( WeenieClassName.greavescovenant,     1.0f ),
            ( WeenieClassName.pauldronscovenant,   1.0f ),
            ( WeenieClassName.bracerscovenant,     1.0f ),
        };

     private static ChanceTable<WeenieClassName> NariyidWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.helmnariyid,        1.0f ),

            ( WeenieClassName.bootsnariyid,       1.0f ),
            ( WeenieClassName.gauntletsnariyid,   1.0f ),

            ( WeenieClassName.breastplatenariyid, 1.0f ),

            ( WeenieClassName.girthnariyid,       1.0f ),

            ( WeenieClassName.leggingsnariyid,    1.0f ),

            ( WeenieClassName.sleevesnariyid,     1.0f ),
        };

        private static ChanceTable<WeenieClassName> OlthoiCeldonWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.ace37189_olthoiceldongauntlets,   1.0f ),
            ( WeenieClassName.ace37192_olthoiceldongirth,        1.0f ),
            ( WeenieClassName.ace37197_olthoiceldonhelm,        1.0f ),
            ( WeenieClassName.ace37202_olthoiceldonleggings,     1.0f ),
            ( WeenieClassName.ace37205_olthoiceldonsleeves,      1.0f ),
            ( WeenieClassName.ace37209_olthoiceldonsollerets,    1.0f ),
            ( WeenieClassName.ace37214_olthoiceldonbreastplate,  1.0f ),
        };

                private static ChanceTable<WeenieClassName> ClothWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            // headgear
            ( WeenieClassName.capcloth,             0.25f),
            ( WeenieClassName.cowlcloth,            0.25f),
            ( WeenieClassName.capfez,               0.25f),
            ( WeenieClassName.capsho,                 0.25f),

            // handwear
            ( WeenieClassName.glovescloth,          1.0f ),

            //footwear - none because no non-robe body cloth drops in early tiers

            // robes - higher % so they continue to drop frequently in higher tiers
            ( WeenieClassName.robeshohood,          0.67f ),
            ( WeenieClassName.robeshonohood,        0.67f ),
            ( WeenieClassName.robegharundimhood,    0.67f ),
            ( WeenieClassName.robegharundimnohood,  0.67f ),
            ( WeenieClassName.robealuvianhood,      0.67f ),
            ( WeenieClassName.robealuviannohood,    0.67f ),

        };

        private static ChanceTable<WeenieClassName> AmuliWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            // headgear
            ( WeenieClassName.qafiya,               0.33f ),
            ( WeenieClassName.turban,               0.33f ),
            ( WeenieClassName.hood,                 0.33f ),
            // handwear
            ( WeenieClassName.glovescloth,          1.0f ),
            // footwear
            ( WeenieClassName.sandals,              0.33f ),
            ( WeenieClassName.shoes,                0.33f ),
            ( WeenieClassName.slippers,             0.33f ),

            ( WeenieClassName.coatamullian,     1.0f ),
            ( WeenieClassName.leggingsamullian, 1.0f ),
        };

        private static ChanceTable<WeenieClassName> ChiranWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.helmchiran,      1.0f ),
            ( WeenieClassName.sandalschiran,   1.0f ),
            ( WeenieClassName.gauntletschiran, 1.0f ),
            ( WeenieClassName.coatchiran,      1.0f ),
            ( WeenieClassName.leggingschiran,  1.0f ),
        };

        private static ChanceTable<WeenieClassName> OlthoiAmuliWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace37188_olthoiamuligauntlets, 0.20f ),
            ( WeenieClassName.ace37196_olthoiamulihelm,      0.20f ),
            ( WeenieClassName.ace37201_olthoiamulileggings,  0.20f ),
            ( WeenieClassName.ace37208_olthoiamulisollerets, 0.20f ),
            ( WeenieClassName.ace37299_olthoiamulicoat,      0.20f ),
        };

        

        // ToD+

        // viamontian platemail
        // introduced 07-2005 - throne of destiny
        // equivalent to platemail / scalemail / yoroi
        private static ChanceTable<WeenieClassName> DiforsaWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.shieldtower,        0.08f ),
            ( WeenieClassName.helmdiforsa,        0.02f ),
            ( WeenieClassName.helmet,             0.02f ),
            ( WeenieClassName.armet,              0.02f ),
            ( WeenieClassName.heaumenew,          0.02f ),
            ( WeenieClassName.solleretsdiforsa,   0.04f ),
            ( WeenieClassName.sollerets,          0.04f ),
            ( WeenieClassName.bracersdiforsa,     0.06f ),
            ( WeenieClassName.breastplatediforsa, 0.08f ),
            ( WeenieClassName.cuirassdiforsa,     0.08f ),
            ( WeenieClassName.gauntletsdiforsa,   0.08f ),
            ( WeenieClassName.girthdiforsa,       0.05f ),
            ( WeenieClassName.greavesdiforsa,     0.07f ),
            ( WeenieClassName.tassetsdiforsa,     0.07f ),
            ( WeenieClassName.hauberkdiforsa,     0.06f ),
            ( WeenieClassName.leggingsdiforsa,    0.08f ),
            ( WeenieClassName.pauldronsdiforsa,   0.05f ),
            ( WeenieClassName.sleevesdiforsa,     0.08f ),
        };

        // viamontian heritage low
        // introduced 07-2005 - throne of destiny
        // equivalent to celdon / amuli / koujia
        private static ChanceTable<WeenieClassName> TenassaWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.breastplatetenassa, 0.33f ),
            ( WeenieClassName.leggingstenassa,    0.34f ),
            ( WeenieClassName.sleevestenassa,     0.33f ),
        };

        // viamontian heritage high
        // introduced 07-2005 - throne of destiny
        // equivalent to lorica / nariyid / chiran
        private static ChanceTable<WeenieClassName> AlduressaWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.bootsalduressa,     0.20f ),
            ( WeenieClassName.gauntletsalduressa, 0.20f ),
            ( WeenieClassName.helmalduressa,      0.20f ),
            ( WeenieClassName.coatalduressa,      0.20f ),
            ( WeenieClassName.leggingsalduressa,  0.20f ),
        };

        // olthoi armor, t7+
        // introduced 08-2008 - ancient powers
        private static ChanceTable<WeenieClassName> OlthoiWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace37191_olthoigauntlets,   0.10f ),
            ( WeenieClassName.ace37193_olthoigirth,       0.10f ),
            ( WeenieClassName.ace37194_olthoigreaves,     0.10f ),
            ( WeenieClassName.ace37199_olthoihelm,        0.10f ),
            ( WeenieClassName.ace37204_olthoipauldrons,   0.10f ),
            ( WeenieClassName.ace37211_olthoisollerets,   0.10f ),
            ( WeenieClassName.ace37212_olthoitassets,     0.10f ),
            ( WeenieClassName.ace37213_olthoibracers,     0.10f ),
            ( WeenieClassName.ace37216_olthoibreastplate, 0.10f ),
            ( WeenieClassName.ace37291_olthoishield,      0.10f ),
        };

        // olthoi heritage armor, t7+
        // introduced 08-2008 - ancient powers




        private static ChanceTable<WeenieClassName> OlthoiAlduressaWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace37187_olthoialduressagauntlets, 0.20f ),
            ( WeenieClassName.ace37195_olthoialduressahelm,      0.20f ),
            ( WeenieClassName.ace37200_olthoialduressaleggings,  0.20f ),
            ( WeenieClassName.ace37207_olthoialduressaboots,     0.20f ),
            ( WeenieClassName.ace37217_olthoialduressacoat,      0.20f ),
        };

        // society armor
        // introduced: 08-2008 - ancient powers
        private static ChanceTable<WeenieClassName> CelestialHandWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace38463_celestialhandbreastplate, 0.34f ),
            ( WeenieClassName.ace38464_celestialhandgauntlets,   0.33f ),
            ( WeenieClassName.ace38465_celestialhandgirth,       0.33f ),
        };

        private static ChanceTable<WeenieClassName> EldrytchWebWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace38472_eldrytchwebbreastplate, 0.34f ),
            ( WeenieClassName.ace38473_eldrytchwebgauntlets,   0.33f ),
            ( WeenieClassName.ace38474_eldrytchwebgirth,       0.33f ),
        };

        private static ChanceTable<WeenieClassName> RadiantBloodWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace38481_radiantbloodbreastplate, 0.34f ),
            ( WeenieClassName.ace38482_radiantbloodgauntlets,   0.33f ),
            ( WeenieClassName.ace38483_radiantbloodgirth,       0.33f ),
        };

        // empyrean, tier 6+
        // introduced 05-2010 - celebration
        private static ChanceTable<WeenieClassName> HaebreanWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace42749_haebreanbreastplate, 0.11f ),
            ( WeenieClassName.ace42750_haebreangauntlets,   0.11f ),
            ( WeenieClassName.ace42751_haebreangirth,       0.11f ),
            ( WeenieClassName.ace42752_haebreangreaves,     0.11f ),
            ( WeenieClassName.ace42753_haebreanhelm,        0.12f ),
            ( WeenieClassName.ace42754_haebreanpauldrons,   0.11f ),
            ( WeenieClassName.ace42755_haebreanboots,       0.11f ),
            ( WeenieClassName.ace42756_haebreantassets,     0.11f ),
            ( WeenieClassName.ace42757_haebreanvambraces,   0.11f ),
        };

        // empyrean, tier 6+
        // introduced 07-2010 - filling in the blanks
        private static ChanceTable<WeenieClassName> KnorrAcademyWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace43048_knorracademybreastplate, 0.12f ),
            ( WeenieClassName.ace43049_knorracademygauntlets,   0.11f ),
            ( WeenieClassName.ace43050_knorracademygirth,       0.11f ),
            ( WeenieClassName.ace43051_knorracademygreaves,     0.11f ),
            ( WeenieClassName.ace43052_knorracademypauldrons,   0.11f ),
            ( WeenieClassName.ace43053_knorracademyboots,       0.11f ),
            ( WeenieClassName.ace43054_knorracademytassets,     0.11f ),
            ( WeenieClassName.ace43055_knorracademyvambraces,   0.11f ),
            ( WeenieClassName.ace43068_knorracademyhelm,        0.11f ),
        };

        // tier 6+ leather
        // introduced: 03-2011 - hidden in shadows
        private static ChanceTable<WeenieClassName> SedgemailLeatherWcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace43828_sedgemailleathervest,      0.17f ),
            ( WeenieClassName.ace43829_sedgemailleathercowl,      0.17f ),
            ( WeenieClassName.ace43830_sedgemailleathergauntlets, 0.16f ),
            ( WeenieClassName.ace43831_sedgemailleatherpants,     0.17f ),
            ( WeenieClassName.ace43832_sedgemailleathershoes,     0.16f ),
            ( WeenieClassName.ace43833_sedgemailleathersleeves,   0.17f ),
        };

        // over-robes
        // introduced: 10-2011 - cloak of darkness
        private static ChanceTable<WeenieClassName> OverRobe_T3_T5_Wcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace44799_faranoverrobe,      0.25f ),     // aluvian? t3+
            ( WeenieClassName.ace44800_dhovestandoverrobe, 0.25f ),     // gharu'ndim? t3+
            ( WeenieClassName.ace44801_suikanoverrobe,     0.25f ),     // sho? t3+
            ( WeenieClassName.ace44802_vestirioverrobe,    0.25f ),     // viamontian? t3+
        };

        private static ChanceTable<WeenieClassName> OverRobe_T6_T8_Wcids = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace44799_faranoverrobe,      0.20f ),     // aluvian? t3+
            ( WeenieClassName.ace44800_dhovestandoverrobe, 0.20f ),     // gharu'ndim? t3+
            ( WeenieClassName.ace44801_suikanoverrobe,     0.20f ),     // sho? t3+
            ( WeenieClassName.ace44802_vestirioverrobe,    0.20f ),     // viamontian? t3+
            ( WeenieClassName.ace44803_empyreanoverrobe,   0.20f ),     // empyrean? t6+?
        };

        private static ChanceTable<WeenieClassName> ClothAluvianWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.robealuvianhood,      1.0f ),
            ( WeenieClassName.robealuviannohood,    1.0f ),
            ( WeenieClassName.glovescloth,          1.0f ),
            ( WeenieClassName.cowlcloth,            0.25f ),
            ( WeenieClassName.capcloth,             0.25f ),
            ( WeenieClassName.hood,                 0.25f ),
            ( WeenieClassName.crown,                0.25f ),
            ( WeenieClassName.sandals,              0.5f ),
            ( WeenieClassName.shoes,                0.5f ),
        };

        private static ChanceTable<WeenieClassName> ClothGharuWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.robegharundimhood,    1.0f ),
            ( WeenieClassName.robegharundimnohood,  1.0f ),
            ( WeenieClassName.glovescloth,          1.0f ),
            ( WeenieClassName.sandals,              0.5f ),
            ( WeenieClassName.shoes,                0.5f ),
            ( WeenieClassName.qafiya,               0.25f ),
            ( WeenieClassName.turban,               0.25f ),
            ( WeenieClassName.capfez,               0.25f ),
            ( WeenieClassName.crown,                0.25f ),
        };

        private static ChanceTable<WeenieClassName> ClothShoWcids = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.robeshohood,      1.0f ),
            ( WeenieClassName.robeshonohood,    1.0f ),
            ( WeenieClassName.glovescloth,      1.0f ),
            ( WeenieClassName.hood,             0.33f ),
            ( WeenieClassName.capsho,           0.33f ),
            ( WeenieClassName.crown,            0.33f ),
            ( WeenieClassName.sandals,          0.5f ),
            ( WeenieClassName.shoes,            0.5f ),
        };



        public static WeenieClassName Roll(TreasureDeath treasureDeath, TreasureRoll treasureRoll)
        {
            switch (treasureRoll.ArmorType)
            {
                case TreasureArmorType.Cloth:
                    return ClothWcids.Roll();

                case TreasureArmorType.Leather:
                    return LeatherWcids.Roll();

                case TreasureArmorType.StuddedLeather:
                    return StuddedLeatherWcids.Roll();

                case TreasureArmorType.Chainmail:
                    return ChainmailWcids.Roll();

                case TreasureArmorType.Scalemail:
                    return ScalemailWcids.Roll();

                case TreasureArmorType.Platemail:
                    return PlatemailWcids.Roll();

                case TreasureArmorType.Yoroi:
                    return YoroiWcids.Roll();

                case TreasureArmorType.Celdon:
                    return CeldonWcids.Roll();

                case TreasureArmorType.Koujia:
                    return KoujiaWcids.Roll();

                case TreasureArmorType.Amuli:
                    return AmuliWcids.Roll();

                case TreasureArmorType.Covenant:
                    return CovenantWcids.Roll();

                case TreasureArmorType.Nariyid:
                    return NariyidWcids.Roll();

                case TreasureArmorType.Lorica:
                    return LoricaWcids.Roll();

                case TreasureArmorType.Chiran:
                    return ChiranWcids.Roll();

                case TreasureArmorType.OlthoiCeldon:
                    return OlthoiCeldonWcids.Roll();

                case TreasureArmorType.OlthoiKoujia:
                    return OlthoiKoujiaWcids.Roll();

                case TreasureArmorType.OlthoiAmuli:
                    return OlthoiAmuliWcids.Roll();

                case TreasureArmorType.HeritageLow:
                    return RollHeritageLowWcid(treasureDeath, treasureRoll);

                case TreasureArmorType.HeritageHigh:
                    return RollHeritageHighWcid(treasureDeath, treasureRoll);

                case TreasureArmorType.Olthoi:
                    return OlthoiWcids.Roll();

                case TreasureArmorType.OlthoiHeritage:
                    return RollOlthoiHeritageWcid(treasureDeath, treasureRoll);

                //case TreasureArmorType.Society:
                //    return RollSocietyArmor(ref treasureRoll.ArmorType);

                //case TreasureArmorType.Haebrean:
                //    return HaebreanWcids.Roll();

                //case TreasureArmorType.KnorrAcademy:
                //    return KnorrAcademyWcids.Roll();

                //case TreasureArmorType.Sedgemail:
                //    return SedgemailLeatherWcids.Roll();

                //case TreasureArmorType.Overrobe:
                //    return RollOverRobeWcid(treasureDeath);
            }
            return WeenieClassName.undef;
        }

        public static TreasureHeritageGroup RollHeritage(TreasureDeath treasureDeath, TreasureRoll treasureRoll)
        {
            return HeritageChance.Roll(treasureDeath.UnknownChances, treasureRoll, true);
        }

        public static WeenieClassName RollPlatemailWcid(TreasureDeath treasureDeath, TreasureRoll treasureRoll)
        {
            var heritage = RollHeritage(treasureDeath, treasureRoll);
            switch (heritage)
            {
                case TreasureHeritageGroup.Aluvian:
                    treasureRoll.ArmorType = TreasureArmorType.Platemail;
                    return PlatemailWcids.Roll();

                case TreasureHeritageGroup.Gharundim:
                    treasureRoll.ArmorType = TreasureArmorType.Scalemail;
                    return ScalemailWcids.Roll();

                case TreasureHeritageGroup.Sho:
                    treasureRoll.ArmorType = TreasureArmorType.Yoroi;
                    return YoroiWcids.Roll();

                case TreasureHeritageGroup.Viamontian:
                    treasureRoll.ArmorType = TreasureArmorType.Diforsa;
                    return DiforsaWcids.Roll();
            }
            return WeenieClassName.undef;
        }

        public static WeenieClassName RollHeritageLowWcid(TreasureDeath treasureDeath, TreasureRoll treasureRoll)
        {
            var heritage = RollHeritage(treasureDeath, treasureRoll);

            switch (heritage)
            {
                case TreasureHeritageGroup.Aluvian:
                    treasureRoll.ArmorType = TreasureArmorType.Celdon;
                    return CeldonWcids.Roll();

                case TreasureHeritageGroup.Gharundim:
                    treasureRoll.ArmorType = TreasureArmorType.Amuli;
                    return AmuliWcids.Roll();

                case TreasureHeritageGroup.Sho:
                    treasureRoll.ArmorType = TreasureArmorType.Koujia;
                    return KoujiaWcids.Roll();

                //case TreasureHeritageGroup.Viamontian:
                //    treasureRoll.ArmorType = TreasureArmorType.Tenassa;
                //    return TenassaWcids.Roll();

                default:
                    var rng = ThreadSafeRandom.Next(1, 3);
                    switch (rng)
                    {
                        case 1:
                            treasureRoll.ArmorType = TreasureArmorType.Celdon;
                            return CeldonWcids.Roll();
                        case 2:
                            treasureRoll.ArmorType = TreasureArmorType.Amuli;
                            return AmuliWcids.Roll();
                        case 3:
                        default:
                            treasureRoll.ArmorType = TreasureArmorType.Koujia;
                            return KoujiaWcids.Roll();
                    }
            }
        }

        public static WeenieClassName RollHeritageHighWcid(TreasureDeath treasureDeath, TreasureRoll treasureRoll)
        {
            var heritage = RollHeritage(treasureDeath, treasureRoll);

            switch (heritage)
            {
                case TreasureHeritageGroup.Aluvian:
                    treasureRoll.ArmorType = TreasureArmorType.Lorica;
                    return LoricaWcids.Roll();

                case TreasureHeritageGroup.Gharundim:
                    treasureRoll.ArmorType = TreasureArmorType.Nariyid;
                    return NariyidWcids.Roll();

                case TreasureHeritageGroup.Sho:
                    treasureRoll.ArmorType = TreasureArmorType.Chiran;
                    return ChiranWcids.Roll();

                //case TreasureHeritageGroup.Viamontian:
                //    treasureRoll.ArmorType = TreasureArmorType.Alduressa;
                //    return AlduressaWcids.Roll();

                default:
                    var rng = ThreadSafeRandom.Next(1, 3);
                    switch (rng)
                    {
                        case 1:
                            treasureRoll.ArmorType = TreasureArmorType.Lorica;
                            return LoricaWcids.Roll();
                        case 2:
                            treasureRoll.ArmorType = TreasureArmorType.Nariyid;
                            return NariyidWcids.Roll();
                        case 3:
                        default:
                            treasureRoll.ArmorType = TreasureArmorType.Chiran;
                            return ChiranWcids.Roll();
                    }
            }
        }

        public static WeenieClassName RollOlthoiHeritageWcid(TreasureDeath treasureDeath, TreasureRoll treasureRoll)
        {
            var heritage = RollHeritage(treasureDeath, treasureRoll);

            switch (heritage)
            {
                case TreasureHeritageGroup.Aluvian:
                    treasureRoll.ArmorType = TreasureArmorType.OlthoiCeldon;
                    return OlthoiCeldonWcids.Roll();

                case TreasureHeritageGroup.Gharundim:
                    treasureRoll.ArmorType = TreasureArmorType.OlthoiAmuli;
                    return OlthoiAmuliWcids.Roll();

                case TreasureHeritageGroup.Sho:
                    treasureRoll.ArmorType = TreasureArmorType.OlthoiKoujia;
                    return OlthoiKoujiaWcids.Roll();

                //case TreasureHeritageGroup.Viamontian:
                //    treasureRoll.ArmorType = TreasureArmorType.OlthoiAlduressa;
                //    return OlthoiAlduressaWcids.Roll();

                default:
                    var rng = ThreadSafeRandom.Next(1, 3);
                    switch (rng)
                    {
                        case 1:
                            treasureRoll.ArmorType = TreasureArmorType.OlthoiCeldon;
                            return OlthoiCeldonWcids.Roll();
                        case 2:
                            treasureRoll.ArmorType = TreasureArmorType.OlthoiAmuli;
                            return OlthoiAmuliWcids.Roll();
                        case 3:
                        default:
                            treasureRoll.ArmorType = TreasureArmorType.OlthoiKoujia;
                            return OlthoiKoujiaWcids.Roll();
                    }
            }
        }

        public static WeenieClassName RollSocietyArmor(ref TreasureArmorType armorType)
        {
            // no heritage, even chance?
            var rng = ThreadSafeRandom.Next(1, 3);

            switch (rng)
            {
                case 1:
                    armorType = TreasureArmorType.CelestialHand;
                    return CelestialHandWcids.Roll();
                case 2:
                    armorType = TreasureArmorType.EldrytchWeb;
                    return EldrytchWebWcids.Roll();
                case 3:
                    armorType = TreasureArmorType.RadiantBlood;
                    return RadiantBloodWcids.Roll();
            }
            return WeenieClassName.undef;
        }

        public static WeenieClassName RollOverRobeWcid(TreasureDeath treasureDeath)
        {
            if (treasureDeath.Tier < 6)
                return OverRobe_T3_T5_Wcids.Roll();
            else
                return OverRobe_T6_T8_Wcids.Roll();
        }

        public static WeenieClassName RollClothWcid(TreasureDeath treasureDeath, TreasureRoll treasureRoll)
        {
            var heritage = RollHeritage(treasureDeath, treasureRoll);

            switch (heritage)
            {
                case TreasureHeritageGroup.Aluvian:
                    return ClothAluvianWcids.Roll();

                case TreasureHeritageGroup.Gharundim:
                    return ClothGharuWcids.Roll();

                case TreasureHeritageGroup.Sho:
                    return ClothShoWcids.Roll();

                default:
                    return ClothWcids.Roll();
            }
        }

        private static readonly Dictionary<WeenieClassName, TreasureArmorType> _combined = new Dictionary<WeenieClassName, TreasureArmorType>();

        static ArmorWcids()
        {
            BuildCombined(LeatherWcids, TreasureArmorType.Leather);
            BuildCombined(StuddedLeatherWcids, TreasureArmorType.StuddedLeather);
            BuildCombined(ChainmailWcids, TreasureArmorType.Chainmail);
            BuildCombined(PlatemailWcids, TreasureArmorType.Platemail);
            BuildCombined(ScalemailWcids, TreasureArmorType.Scalemail);
            BuildCombined(YoroiWcids, TreasureArmorType.Yoroi);
            BuildCombined(CeldonWcids, TreasureArmorType.Celdon);
            BuildCombined(AmuliWcids, TreasureArmorType.Amuli);
            BuildCombined(KoujiaWcids, TreasureArmorType.Koujia);
            BuildCombined(CovenantWcids, TreasureArmorType.Covenant);
            BuildCombined(LoricaWcids, TreasureArmorType.Lorica);
            BuildCombined(NariyidWcids, TreasureArmorType.Nariyid);
            BuildCombined(ChiranWcids, TreasureArmorType.Chiran);
            //BuildCombined(DiforsaWcids, TreasureArmorType.Diforsa);
            //BuildCombined(TenassaWcids, TreasureArmorType.Tenassa);
            //BuildCombined(AlduressaWcids, TreasureArmorType.Alduressa);
            BuildCombined(OlthoiWcids, TreasureArmorType.Olthoi);
            BuildCombined(OlthoiCeldonWcids, TreasureArmorType.OlthoiCeldon);
            BuildCombined(OlthoiAmuliWcids, TreasureArmorType.OlthoiAmuli);
            BuildCombined(OlthoiKoujiaWcids, TreasureArmorType.OlthoiKoujia);
            //BuildCombined(OlthoiAlduressaWcids, TreasureArmorType.OlthoiAlduressa);
            //BuildCombined(CelestialHandWcids, TreasureArmorType.CelestialHand);   // handled in SocietyArmor
            //BuildCombined(EldrytchWebWcids, TreasureArmorType.EldrytchWeb);
            //BuildCombined(RadiantBloodWcids, TreasureArmorType.RadiantBlood);
            //BuildCombined(HaebreanWcids, TreasureArmorType.Haebrean);
            //BuildCombined(KnorrAcademyWcids, TreasureArmorType.KnorrAcademy);
            //BuildCombined(SedgemailLeatherWcids, TreasureArmorType.Sedgemail);
            //BuildCombined(OverRobe_T3_T5_Wcids, TreasureArmorType.Overrobe);
            ////BuildCombined(OverRobe_T6_T8_Wcids, TreasureArmorType.Overrobe);
            //BuildCombined(ClothAluvianWcids, TreasureArmorType.Cloth);
            //BuildCombined(ClothGharuWcids, TreasureArmorType.Cloth);
            //BuildCombined(ClothShoWcids, TreasureArmorType.Cloth);
            BuildCombined(ClothWcids, TreasureArmorType.Cloth);
        }

        private static void BuildCombined(ChanceTable<WeenieClassName> wcids, TreasureArmorType armorType)
        {
            foreach (var entry in wcids)
                _combined.TryAdd(entry.result, armorType);
        }

        public static bool TryGetValue(WeenieClassName wcid, out TreasureArmorType armorType)
        {
            return _combined.TryGetValue(wcid, out armorType);
        }
    }
}
