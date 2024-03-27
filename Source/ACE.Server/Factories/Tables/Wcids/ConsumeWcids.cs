using System;
using System.Collections.Generic;
using ACE.Database.Models.World;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables.Wcids
{
    public static class ConsumeWcids
    {
        private static ChanceTable<WeenieClassName> T1_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.apple,           1.0f ),
            ( WeenieClassName.bread,           1.0f ),
            ( WeenieClassName.cabbage,         1.0f ),
            ( WeenieClassName.cheese,          1.0f ),
            ( WeenieClassName.chicken,         1.0f ),
            ( WeenieClassName.egg,             1.0f ),
            ( WeenieClassName.fish,            1.0f ),
            ( WeenieClassName.grapes,          1.0f ),
            ( WeenieClassName.beefside,        1.0f ),
            ( WeenieClassName.mushroom,        1.0f ),
            ( WeenieClassName.healthDraught,   2.0f ),
            ( WeenieClassName.manaDraught,     2.0f ),
            ( WeenieClassName.staminaDraught,  2.0f ),
        };

        private static ChanceTable<WeenieClassName> T2_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.healthDraught,   1.0f ),
            ( WeenieClassName.manaDraught,     1.0f ),
            ( WeenieClassName.staminaDraught,  1.0f ),
        };

        private static ChanceTable<WeenieClassName> T3_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.healthDraught,   2.0f ),
            ( WeenieClassName.manaDraught,     2.0f ),
            ( WeenieClassName.staminaDraught,  2.0f ),
            ( WeenieClassName.healthPotion,    1.0f ),
            ( WeenieClassName.manaPotion,      1.0f ),
            ( WeenieClassName.staminaPotion,   1.0f ),
        };

        private static ChanceTable<WeenieClassName> T4_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.healthDraught,   1.0f ),
            ( WeenieClassName.manaDraught,     1.0f ),
            ( WeenieClassName.staminaDraught,  1.0f ),
            ( WeenieClassName.healthPotion,    2.0f ),
            ( WeenieClassName.manaPotion,      2.0f ),
            ( WeenieClassName.staminaPotion,   2.0f ),
        };

        private static ChanceTable<WeenieClassName> T5_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.healthPotion,    2.0f ),
            ( WeenieClassName.manaPotion,      2.0f ),
            ( WeenieClassName.staminaPotion,   2.0f ),
            ( WeenieClassName.healthTonic,     1.0f ),
            ( WeenieClassName.manaTonic,       1.0f ),
            ( WeenieClassName.staminaTonic,    1.0f ),
        };

        private static ChanceTable<WeenieClassName> T6_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.healthPotion,    1.0f ),
            ( WeenieClassName.manaPotion,      1.0f ),
            ( WeenieClassName.staminaPotion,   1.0f ),
            ( WeenieClassName.healthTonic,     2.0f ),
            ( WeenieClassName.manaTonic,       2.0f ),
            ( WeenieClassName.staminaTonic,    2.0f ),
        };

        private static ChanceTable<WeenieClassName> T7_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.healthTonic,     2.0f ),
            ( WeenieClassName.manaTonic,       2.0f ),
            ( WeenieClassName.staminaTonic,    2.0f ),
            ( WeenieClassName.healthTincture,  1.0f ),
            ( WeenieClassName.manaTincture,    1.0f ),
            ( WeenieClassName.staminaTincture, 1.0f ),
        };

        private static ChanceTable<WeenieClassName> T8_Chances = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.healthTincture,  1.0f ),
            ( WeenieClassName.manaTincture,    1.0f ),
            ( WeenieClassName.staminaTincture, 1.0f ),
            // ( WeenieClassName.healthElixir,  1.0f ),
            // ( WeenieClassName.manaElixir,    1.0f ),
            // ( WeenieClassName.staminaElixir, 1.0f ),
        };

        private static readonly List<ChanceTable<WeenieClassName>> consumeTiers = new List<ChanceTable<WeenieClassName>>()
        {
            T1_Chances,
            T2_Chances,
            T3_Chances,
            T4_Chances,
            T5_Chances,
            T6_Chances,
            T7_Chances,
            T8_Chances,
        };
        
        static ConsumeWcids()
        {

        }
        
        public static WeenieClassName Roll(TreasureDeath profile)
        {
            // todo: verify t7 / t8 chances
            var table = consumeTiers[profile.Tier - 1];

            // quality mod?
            return table.Roll(profile.LootQualityMod);
        }
    }
}
