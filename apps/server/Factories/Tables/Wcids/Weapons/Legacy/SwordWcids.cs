using System.Collections.Generic;

using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Factories.Tables.Wcids
{
    public static class SwordWcids
    {

        private static ChanceTable<WeenieClassName> SwordWcids_All = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.swordshort,         4.0f ),
            ( WeenieClassName.swordshortacid,     1.0f ),
            ( WeenieClassName.swordshortelectric, 1.0f ),
            ( WeenieClassName.swordshortfire,     1.0f ),
            ( WeenieClassName.swordshortfrost,    1.0f ),

            ( WeenieClassName.swordlong,          4.0f ),
            ( WeenieClassName.swordlongacid,      1.0f ),
            ( WeenieClassName.swordlongelectric,  1.0f ),
            ( WeenieClassName.swordlongfire,      1.0f ),
            ( WeenieClassName.swordlongfrost,     1.0f ),

            ( WeenieClassName.swordrapier,        4.0f ),

            ( WeenieClassName.ace40618_spadone,          4.0f ), // Greatsword 
            ( WeenieClassName.ace40619_acidspadone,      1.0f ),
            ( WeenieClassName.ace40620_lightningspadone, 1.0f ),
            ( WeenieClassName.ace40621_flamingspadone,   1.0f ),
            ( WeenieClassName.ace40622_frostspadone,     1.0f ),
        };

        private static ChanceTable<WeenieClassName> SwordWcids_Aluvian_T1 = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.swordshort,           3.0f ),
            ( WeenieClassName.swordlong,            0.5f ),
            ( WeenieClassName.swordbroad,           0.5f ),

            ( WeenieClassName.swordrapier,          0.5f ),
            ( WeenieClassName.ace40618_spadone,     0.5f ),
        };

        private static ChanceTable<WeenieClassName> SwordWcids_Aluvian = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.swordshort,         4.0f ),
            ( WeenieClassName.swordshortacid,     1.0f ),
            ( WeenieClassName.swordshortelectric, 1.0f ),
            ( WeenieClassName.swordshortfire,     1.0f ),
            ( WeenieClassName.swordshortfrost,    1.0f ),

            ( WeenieClassName.swordlong,          4.0f ),
            ( WeenieClassName.swordlongacid,      1.0f ),
            ( WeenieClassName.swordlongelectric,  1.0f ),
            ( WeenieClassName.swordlongfire,      1.0f ),
            ( WeenieClassName.swordlongfrost,     1.0f ),

            ( WeenieClassName.swordrapier,        4.0f ),

            ( WeenieClassName.ace40618_spadone,          4.0f ), // Greatsword 
            ( WeenieClassName.ace40619_acidspadone,      1.0f ),
            ( WeenieClassName.ace40620_lightningspadone, 1.0f ),
            ( WeenieClassName.ace40621_flamingspadone,   1.0f ),
            ( WeenieClassName.ace40622_frostspadone,     1.0f ),
        };

        private static ChanceTable<WeenieClassName> SwordWcids_Gharundim_T1 = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.simi,                 3.0f ),
            ( WeenieClassName.kaskara,              0.5f ),
            ( WeenieClassName.takuba,               0.5f ),
            ( WeenieClassName.scimitar,             0.5f ),

            ( WeenieClassName.swordrapier,          0.5f ),
            ( WeenieClassName.ace41067_shashqa,     0.5f ),
        };

        private static ChanceTable<WeenieClassName> SwordWcids_Gharundim = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.simi,             4.0f ),
            ( WeenieClassName.simiacid,         1.0f ),
            ( WeenieClassName.simielectric,     1.0f ),
            ( WeenieClassName.simifire,         1.0f ),
            ( WeenieClassName.simifrost,        1.0f ),

            ( WeenieClassName.kaskara,          4.0f ),
            ( WeenieClassName.kaskaraacid,      1.0f ),
            ( WeenieClassName.kaskaraelectric,  1.0f ),
            ( WeenieClassName.kaskarafire,      1.0f ),
            ( WeenieClassName.kaskarafrost,     1.0f ),

            ( WeenieClassName.scimitar,          4.0f ),
            ( WeenieClassName.scimitaracid,      1.0f ),
            ( WeenieClassName.scimitarelectric,  1.0f ),
            ( WeenieClassName.scimitarfire,      1.0f ),
            ( WeenieClassName.scimitarfrost,     1.0f ),

            ( WeenieClassName.takuba,           4.0f ),
            ( WeenieClassName.takubaacid,       1.0f ),
            ( WeenieClassName.takubaelectric,   1.0f ),
            ( WeenieClassName.takubafire,       1.0f ),
            ( WeenieClassName.takubafrost,      1.0f ),

            ( WeenieClassName.swordrapier,      4.0f ),

            ( WeenieClassName.ace41067_shashqa,          4.0f ),
            ( WeenieClassName.ace41068_acidshashqa,      1.0f ),
            ( WeenieClassName.ace41069_lightningshashqa, 1.0f ),
            ( WeenieClassName.ace41070_flamingshashqa,   1.0f ),
            ( WeenieClassName.ace41071_frostshashqa,     1.0f ),
        };

        private static ChanceTable<WeenieClassName> SwordWcids_Sho_T1 = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.yaoji,                3.0f ),
            ( WeenieClassName.tachi,                0.5f ),

            ( WeenieClassName.swordrapier,          0.5f ),
            ( WeenieClassName.ace40760_nodachi,     0.5f ),
        };

        private static ChanceTable<WeenieClassName> SwordWcids_Sho = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.yaoji,             4.0f ),
            ( WeenieClassName.yaojiacid,         1.0f ),
            ( WeenieClassName.yaojielectric,     1.0f ),
            ( WeenieClassName.yaojifire,         1.0f ),
            ( WeenieClassName.yaojifrost,        1.0f ),

            ( WeenieClassName.tachi,           4.0f ),
            ( WeenieClassName.tachiacid,       1.0f ),
            ( WeenieClassName.tachielectric,   1.0f ),
            ( WeenieClassName.tachifire,       1.0f ),
            ( WeenieClassName.tachifrost,      1.0f ),

            ( WeenieClassName.swordrapier,      4.0f ),

            ( WeenieClassName.ace40760_nodachi,          4.0f ),
            ( WeenieClassName.ace40761_acidnodachi,      1.0f ),
            ( WeenieClassName.ace40762_lightningnodachi, 1.0f ),
            ( WeenieClassName.ace40763_flamingnodachi,   1.0f ),
            ( WeenieClassName.ace40764_frostnodachi,     1.0f ),
        };

        public static WeenieClassName Roll(TreasureHeritageGroup heritage, int tier, out TreasureWeaponType weaponType)
        {
            WeenieClassName weapon;

            switch (heritage)
            {
                case TreasureHeritageGroup.Aluvian:
                    if (tier > 1)
                        weapon = SwordWcids_Aluvian.Roll();
                    else
                        weapon = SwordWcids_Aluvian_T1.Roll();
                    break;
                case TreasureHeritageGroup.Gharundim:
                    if (tier > 1)
                        weapon = SwordWcids_Gharundim.Roll();
                    else
                        weapon = SwordWcids_Gharundim_T1.Roll();
                    break;
                case TreasureHeritageGroup.Sho:
                    if (tier > 1)
                        weapon = SwordWcids_Sho.Roll();
                    else
                        weapon = SwordWcids_Sho_T1.Roll();
                    break;
                default:
                    weapon = SwordWcids_All.Roll();
                    break;

            }

            weaponType = TreasureWeaponType.Sword;

            return weapon;
        }

        private static readonly Dictionary<WeenieClassName, TreasureWeaponType> _combined = new Dictionary<WeenieClassName, TreasureWeaponType>();

        static SwordWcids()
        {
            foreach (var entry in SwordWcids_Aluvian_T1)
                _combined.TryAdd(entry.result, TreasureWeaponType.Sword);
            foreach (var entry in SwordWcids_Aluvian)
                _combined.TryAdd(entry.result, TreasureWeaponType.Sword);

            foreach (var entry in SwordWcids_Gharundim_T1)
                _combined.TryAdd(entry.result, TreasureWeaponType.Sword);
            foreach (var entry in SwordWcids_Gharundim)
                _combined.TryAdd(entry.result, TreasureWeaponType.Sword);

            foreach (var entry in SwordWcids_Sho_T1)
                _combined.TryAdd(entry.result, TreasureWeaponType.Sword);
            foreach (var entry in SwordWcids_Sho)
                _combined.TryAdd(entry.result, TreasureWeaponType.Sword);
        }

        public static bool TryGetValue(WeenieClassName wcid, out TreasureWeaponType weaponType)
        {
            return _combined.TryGetValue(wcid, out weaponType);
        }
    }
}
