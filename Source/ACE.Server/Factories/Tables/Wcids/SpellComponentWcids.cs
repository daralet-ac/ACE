using System.Collections.Generic;

using ACE.Common;
using ACE.Database.Models.World;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables.Wcids
{
    public static class SpellComponentWcids
    {
        private static ChanceTable<WeenieClassName> T1_Chances = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.peascarablead,   1.00f ),
        };

        private static ChanceTable<WeenieClassName> T2_Chances = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.peascarablead,   0.50f ),
            ( WeenieClassName.peascarabiron,   0.50f ),
        };

        private static ChanceTable<WeenieClassName> T3_Chances = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.peascarablead,   0.25f ),
            ( WeenieClassName.peascarabiron,   0.50f ),
            ( WeenieClassName.peascarabcopper, 0.25f ),
        };

        private static ChanceTable<WeenieClassName> T4_Chances = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.peascarabiron,   0.25f ),
            ( WeenieClassName.peascarabcopper, 0.50f ),
            ( WeenieClassName.peascarabsilver, 0.25f ),
        };

        private static ChanceTable<WeenieClassName> T5_Chances = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.peascarabcopper, 0.25f ),
            ( WeenieClassName.peascarabsilver, 0.50f ),
            ( WeenieClassName.peascarabgold,   0.25f ),
        };

        private static ChanceTable<WeenieClassName> T6_T8_Chances = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.peascarabsilver, 0.25f ),
            ( WeenieClassName.peascarabgold,   0.50f ),
            ( WeenieClassName.peascarabpyreal, 0.25f ),
        };

        private static readonly List<ChanceTable<WeenieClassName>> peaTiers = new List<ChanceTable<WeenieClassName>>()
        {
            T1_Chances,
            T2_Chances,
            T3_Chances,
            T4_Chances,
            T5_Chances,
            T6_T8_Chances,
            T6_T8_Chances,
            T6_T8_Chances,
        };

        // level 8 spell components have a chance of dropping in t7 / t8
        private static ChanceTable<bool> level8SpellComponentChance = new ChanceTable<bool>()
        {
            ( false, 0.6f ),
            ( true,  0.4f ),
        };

        public static WeenieClassName Roll(TreasureDeath profile)
        {
            return Roll_AllSpellComponents(profile);
        }

        private static ChanceTable<WeenieClassName> Quills = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace37363_quillofinfliction,    0.50f ),   // war/debuff (other)
            ( WeenieClassName.ace37364_quillofintrospection, 0.35f ),   // beneficial (self)
            ( WeenieClassName.ace37365_quillofbenevolence,   0.10f ),   // beneficial (other)
            ( WeenieClassName.ace37362_quillofextraction,    0.05f ),   // drain (other)
        };

        private static ChanceTable<WeenieClassName> Inks = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.ace37353_inkofformation,       0.30f ),   // self (can only be used with introspection quills)
            ( WeenieClassName.ace37360_inkofconveyance,      0.24f ),   // other (can only be used with benevolence, infliction, and extraction quills)
            ( WeenieClassName.ace37355_inkofobjectification, 0.10f ),   // item spells
            ( WeenieClassName.ace37354_inkofnullification,   0.06f ),   // nullify (can only be used with introspection or benevolence quills)
            // war spells
            ( WeenieClassName.ace37356_parabolicink,         0.06f ),   // arc
            ( WeenieClassName.ace37357_inkofpartition,       0.06f ),   // blast
            ( WeenieClassName.ace37358_inkofseparation,      0.06f ),   // volley
            ( WeenieClassName.ace37359_alacritousink,        0.06f ),   // streak
            ( WeenieClassName.ace37361_inkofdirection,       0.06f ),   // bolt
        };

        private static readonly List<WeenieClassName> Glyphs = new List<WeenieClassName>()
        {
            WeenieClassName.ace37343_glyphofalchemy,
            WeenieClassName.ace37344_glyphofarcanelore,
            WeenieClassName.ace37345_glyphofarmor,
            WeenieClassName.ace37346_glyphofarmortinkering,
            WeenieClassName.ace37347_glyphofbludgeoning,
            WeenieClassName.ace37349_glyphofcooking,
            WeenieClassName.ace37350_glyphofcoordination,
            WeenieClassName.ace37342_glyphofcorrosion,
            WeenieClassName.ace37351_glyphofcreatureenchantment,
            WeenieClassName.ace43379_glyphofdamage,
            WeenieClassName.ace37352_glyphofdeception,
            WeenieClassName.ace45370_glyphofdirtyfighting,
            WeenieClassName.ace45371_glyphofdualwield,
            WeenieClassName.ace37300_glyphofendurance,
            WeenieClassName.ace37373_glyphoffinesseweapons,
            WeenieClassName.ace37301_glyphofflame,
            WeenieClassName.ace37302_glyphoffletching,
            WeenieClassName.ace37303_glyphoffocus,
            WeenieClassName.ace37348_glyphoffrost,
            WeenieClassName.ace37304_glyphofhealing,
            WeenieClassName.ace37305_glyphofhealth,
            WeenieClassName.ace37369_glyphofheavyweapons,
            WeenieClassName.ace37309_glyphofitemenchantment,
            WeenieClassName.ace37310_glyphofitemtinkering,
            WeenieClassName.ace37311_glyphofjump,
            WeenieClassName.ace37312_glyphofleadership,
            WeenieClassName.ace37313_glyphoflifemagic,
            WeenieClassName.ace37339_glyphoflightweapons,
            WeenieClassName.ace37314_glyphoflightning,
            WeenieClassName.ace37315_glyphoflockpick,
            WeenieClassName.ace37316_glyphofloyalty,
            WeenieClassName.ace37317_glyphofmagicdefense,
            WeenieClassName.ace38760_glyphofmagicitemtinkering,
            WeenieClassName.ace37318_glyphofmana,
            WeenieClassName.ace37319_glyphofmanaconversion,
            WeenieClassName.ace37321_glyphofmanaregeneration,
            WeenieClassName.ace37323_glyphofmeleedefense,
            WeenieClassName.ace37324_glyphofmissiledefense,
            WeenieClassName.ace37338_glyphofmissileweapons,
            WeenieClassName.ace37325_glyphofmonsterappraisal,
            WeenieClassName.ace43387_glyphofnether,
            WeenieClassName.ace37326_glyphofpersonappraisal,
            WeenieClassName.ace37327_glyphofpiercing,
            WeenieClassName.ace37328_glyphofquickness,
            WeenieClassName.ace45372_glyphofrecklessness,
            WeenieClassName.ace37307_glyphofregeneration,
            WeenieClassName.ace37329_glyphofrun,
            WeenieClassName.ace37330_glyphofsalvaging,
            WeenieClassName.ace37331_glyphofself,
            WeenieClassName.ace45373_glyphofshield,
            WeenieClassName.ace37332_glyphofslashing,
            WeenieClassName.ace45374_glyphofsneakattack,
            WeenieClassName.ace37333_glyphofstamina,
            WeenieClassName.ace37336_glyphofstaminaregeneration,
            WeenieClassName.ace37337_glyphofstrength,
            WeenieClassName.ace49455_glyphofsummoning,
            WeenieClassName.ace41747_glyphoftwohandedcombat,
            WeenieClassName.ace43380_glyphofvoidmagic,
            WeenieClassName.ace37340_glyphofwarmagic,
            WeenieClassName.ace37341_glyphofweapontinkering,
        };

        private static WeenieClassName Roll_Level8SpellComponent(TreasureDeath profile)
        {
            // even chance between quill / ink / glyph
            var type = (Level8_SpellComponentType)ThreadSafeRandom.Next(1, 3);

            switch (type)
            {
                // quality mod?
                case Level8_SpellComponentType.Quill:
                    return Quills.Roll(profile.LootQualityMod);

                case Level8_SpellComponentType.Ink:
                    return Inks.Roll(profile.LootQualityMod);

                case Level8_SpellComponentType.Glyph:

                    // even chance for each glyph
                    var rng = ThreadSafeRandom.Next(0, Glyphs.Count - 1);
                    return Glyphs[rng];
            }

            return WeenieClassName.undef;
        }

        private static List<WeenieClassName> herbs = new List<WeenieClassName>()
        {
            WeenieClassName.amaranth,
            WeenieClassName.bistort,
            WeenieClassName.comfrey,
            WeenieClassName.damiana,
            WeenieClassName.dragonsblood,
            WeenieClassName.eyebright,
            WeenieClassName.frankincense,
            WeenieClassName.ginseng,
            WeenieClassName.hawthorn,
            WeenieClassName.henbane,
            WeenieClassName.hyssop,
            WeenieClassName.mandrake,
            WeenieClassName.mugwort,
            WeenieClassName.myrrh,
            WeenieClassName.saffron,
            WeenieClassName.vervain,
            WeenieClassName.wormwood,
            WeenieClassName.yarrow
        };

        private static List<WeenieClassName> herbsPea = new List<WeenieClassName>()
        {
            WeenieClassName.peaherbamaranth,
            WeenieClassName.peaherbbistort,
            WeenieClassName.peaherbcomfrey,
            WeenieClassName.peaherbdamiana,
            WeenieClassName.peaherbdragonsblood,
            WeenieClassName.peaherbeyebright,
            WeenieClassName.peaherbfrankincense,
            WeenieClassName.peaherbginseng,
            WeenieClassName.peaherbhawthorn,
            WeenieClassName.peaherbhenbane,
            WeenieClassName.peaherbhyssop,
            WeenieClassName.peaherbmandrake,
            WeenieClassName.peaherbmugwort,
            WeenieClassName.peaherbmyrrh,
            WeenieClassName.peaherbsaffron,
            WeenieClassName.peaherbvervain,
            WeenieClassName.peaherbwormwood,
            WeenieClassName.peaherbyarrow
        };

        private static List<WeenieClassName> powderedGems = new List<WeenieClassName>()
        {
            WeenieClassName.agate,
            WeenieClassName.amber,
            WeenieClassName.azurite,
            WeenieClassName.bloodstone,
            WeenieClassName.carnelian,
            WeenieClassName.hematite,
            WeenieClassName.lapislazul,
            WeenieClassName.malachite,
            WeenieClassName.moonstone,
            WeenieClassName.onyx,
            WeenieClassName.quartz,
            WeenieClassName.turquoise
        };

        private static List<WeenieClassName> powderedGemsPea = new List<WeenieClassName>()
        {
            WeenieClassName.peapowderagate,
            WeenieClassName.peapowderamber,
            WeenieClassName.peapowderazurite,
            WeenieClassName.peapowderbloodstone,
            WeenieClassName.peapowdercarnelian,
            WeenieClassName.peapowderhematite,
            WeenieClassName.peapowderlapislazuli,
            WeenieClassName.peapowdermalachite,
            WeenieClassName.peapowdermoonstone,
            WeenieClassName.peapowderonyx,
            WeenieClassName.peapowderquartz,
            WeenieClassName.peapowderturquoise
        };

        private static List<WeenieClassName> alchemicalSubstances = new List<WeenieClassName>()
        {
            WeenieClassName.alchembrimstone,
            WeenieClassName.alchemcadmia,
            WeenieClassName.alchemcinnabar,
            WeenieClassName.alchemcobalt,
            WeenieClassName.alchemcolcothar,
            WeenieClassName.alchemgypsum,
            WeenieClassName.alchemquicksilver,
            WeenieClassName.alchemrealgar,
            WeenieClassName.alchemstibnite,
            WeenieClassName.alchemturpeth,
            WeenieClassName.alchemverdigris,
            WeenieClassName.alchemvitriol
        };

        private static List<WeenieClassName> alchemicalSubstancesPea = new List<WeenieClassName>()
        {
            WeenieClassName.peaalchembrimstone,
            WeenieClassName.peaalchemcadmia,
            WeenieClassName.peaalchemcinnabar,
            WeenieClassName.peaalchemcobalt,
            WeenieClassName.peaalchemcolcothar,
            WeenieClassName.peaalchemgypsum,
            WeenieClassName.peaalchemquicksilver,
            WeenieClassName.peaalchemrealgar,
            WeenieClassName.peaalchemstibnite,
            WeenieClassName.peaalchemturpeth,
            WeenieClassName.peaalchemverdigris,
            WeenieClassName.peaalchemvitriol
        };

        private static List<WeenieClassName> talismans = new List<WeenieClassName>()
        {
            WeenieClassName.aldertalisman,
            WeenieClassName.ashwoodtalisman,
            WeenieClassName.birchtalisman,
            WeenieClassName.blackthorntalisman,
            WeenieClassName.cedartalisman,
            WeenieClassName.ebonytalisman,
            WeenieClassName.eldertalisman,
            WeenieClassName.hazeltalisman,
            WeenieClassName.hemlocktalisman,
            WeenieClassName.oaktalisman,
            WeenieClassName.poplartalisman,
            WeenieClassName.rowantalisman,
            WeenieClassName.willowtalisman,
            WeenieClassName.yewtalisman
        };

        private static List<WeenieClassName> talismansPea = new List<WeenieClassName>()
        {
            WeenieClassName.peatalismanalder,
            WeenieClassName.peatalismanashwood,
            WeenieClassName.peatalismanbirch,
            WeenieClassName.peatalismanblackthorn,
            WeenieClassName.peatalismancedar,
            WeenieClassName.peatalismanebony,
            WeenieClassName.peatalismanelder,
            WeenieClassName.peatalismanhazel,
            WeenieClassName.peatalismanhemlock,
            WeenieClassName.peatalismanoak,
            WeenieClassName.peatalismanpoplar,
            WeenieClassName.peatalismanrowan,
            WeenieClassName.peatalismanwillow,
            WeenieClassName.peatalismanyew
        };

        private static List<WeenieClassName> tapers = new List<WeenieClassName>()
        {
            WeenieClassName.taperblue,
            WeenieClassName.taperbrown,
            WeenieClassName.tapergreen,
            WeenieClassName.tapergrey,
            WeenieClassName.taperindigo,
            WeenieClassName.taperorange,
            WeenieClassName.taperpink,
            WeenieClassName.taperred,
            WeenieClassName.taperviolet,
            WeenieClassName.taperwhite,
            WeenieClassName.taperyellow,
            WeenieClassName.taperturquoise
        };

        private static List<WeenieClassName> tapersPea = new List<WeenieClassName>()
        {
            WeenieClassName.peataperblue,
            WeenieClassName.peataperbrown,
            WeenieClassName.peatapergreen,
            WeenieClassName.peatapergrey,
            WeenieClassName.peataperindigo,
            WeenieClassName.peataperorange,
            WeenieClassName.peataperpink,
            WeenieClassName.peataperred,
            WeenieClassName.peataperviolet,
            WeenieClassName.peataperwhite,
            WeenieClassName.peataperyellow,
            WeenieClassName.peataperturquoise
        };

        private static WeenieClassName Roll_AllSpellComponents(TreasureDeath profile)
        {
            var type = ThreadSafeRandom.Next(1, 6);
            var isPea = false;

            if (profile.Tier > 1)
                isPea = ((ThreadSafeRandom.Next(1, 10) / 10) + profile.LootQualityMod) >= 1;

            switch (type)
            {
                case 1:
                    if(isPea)
                        return herbsPea[ThreadSafeRandom.Next(0, herbsPea.Count - 1)];
                    return herbs[ThreadSafeRandom.Next(0, herbs.Count - 1)];
                case 2:
                    if (isPea)
                        return powderedGemsPea[ThreadSafeRandom.Next(0, powderedGemsPea.Count - 1)];
                    return powderedGems[ThreadSafeRandom.Next(0, powderedGems.Count - 1)];
                case 3:
                    if (isPea)
                        return alchemicalSubstancesPea[ThreadSafeRandom.Next(0, alchemicalSubstancesPea.Count - 1)];
                    return alchemicalSubstances[ThreadSafeRandom.Next(0, alchemicalSubstances.Count - 1)];
                case 4:
                    if (isPea)
                        return talismansPea[ThreadSafeRandom.Next(0, talismansPea.Count - 1)];
                    return talismans[ThreadSafeRandom.Next(0, talismans.Count - 1)];
                case 5:
                    if (isPea)
                        return tapersPea[ThreadSafeRandom.Next(0, tapersPea.Count - 1)];
                    return tapers[ThreadSafeRandom.Next(0, tapers.Count - 1)];
                case 6:
                    var table = peaTiers[profile.Tier - 1];
                    return table.Roll(profile.LootQualityMod);
            }

            return WeenieClassName.undef;
        }

        static SpellComponentWcids()
        {
            T1_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
            {
                ( WeenieClassName.scarablead,      1.0f ),
            };

            T2_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
            {
                ( WeenieClassName.scarablead,     10.0f ),
                ( WeenieClassName.scarabiron,     10.0f ),

                ( WeenieClassName.peascarablead,   1.0f ),
                ( WeenieClassName.peascarabiron,   1.0f ),
            };

            T3_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
            {
                ( WeenieClassName.scarablead,     10.0f ),
                ( WeenieClassName.scarabiron,     20.0f ),
                ( WeenieClassName.scarabcopper,   10.0f ),

                ( WeenieClassName.peascarablead,   1.0f ),
                ( WeenieClassName.peascarabiron,   2.0f ),
                ( WeenieClassName.peascarabcopper, 1.0f ),
            };

            T4_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
            {
                ( WeenieClassName.scarabiron,     10.0f ),
                ( WeenieClassName.scarabcopper,   20.0f ),
                ( WeenieClassName.scarabsilver,   10.0f ),

                ( WeenieClassName.peascarabiron,   1.0f ),
                ( WeenieClassName.peascarabcopper, 2.0f ),
                ( WeenieClassName.peascarabsilver, 1.0f ),
            };

            T5_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
            {
                ( WeenieClassName.scarabcopper,   10.0f ),
                ( WeenieClassName.scarabsilver,   20.0f ),
                ( WeenieClassName.scarabgold,     10.0f ),

                ( WeenieClassName.peascarabcopper, 1.0f ),
                ( WeenieClassName.peascarabsilver, 2.0f ),
                ( WeenieClassName.peascarabgold,   1.0f ),
            };

            T6_T8_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
            {
                ( WeenieClassName.scarabsilver,   10.0f ),
                ( WeenieClassName.scarabgold,     20.0f ),
                ( WeenieClassName.scarabpyreal,   10.0f ),

                ( WeenieClassName.peascarabsilver, 1.0f ),
                ( WeenieClassName.peascarabgold,   2.0f ),
                ( WeenieClassName.peascarabpyreal, 1.0f ),
            };

            peaTiers = new List<ChanceTable<WeenieClassName>>()
            {
                T1_Chances,
                T2_Chances,
                T3_Chances,
                T4_Chances,
                T5_Chances,
                T6_T8_Chances,
                T6_T8_Chances,
                T6_T8_Chances,
            };
        }
    }
}
