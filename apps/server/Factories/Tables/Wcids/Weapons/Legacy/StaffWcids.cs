using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Factories.Tables.Wcids;

public static class StaffWcids
{
    private static ChanceTable<WeenieClassName> StaffWcids_All = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.quarterstaffnew, 4.0f),
        (WeenieClassName.quarterstaffacidnew, 1.0f),
        (WeenieClassName.quarterstaffelectricnew, 1.0f),
        (WeenieClassName.quarterstaffflamenew, 1.0f),
        (WeenieClassName.quarterstafffrostnew, 1.0f),
        (WeenieClassName.nabutnew, 4.0f),
        (WeenieClassName.nabutacidnew, 1.0f),
        (WeenieClassName.nabutelectricnew, 1.0f),
        (WeenieClassName.nabutfirenew, 1.0f),
        (WeenieClassName.nabutfrostnew, 1.0f),
        (WeenieClassName.jonew, 4.0f),
        (WeenieClassName.joacidnew, 1.0f),
        (WeenieClassName.joelectricnew, 1.0f),
        (WeenieClassName.jofirenew, 1.0f),
        (WeenieClassName.jofrostnew, 1.0f),
        (WeenieClassName.swordstaff, 4.0f),
        (WeenieClassName.swordstaffacid, 1.0f),
        (WeenieClassName.swordstaffelectric, 1.0f),
        (WeenieClassName.swordstafffire, 1.0f),
        (WeenieClassName.swordstafffrost, 1.0f),
    };

    private static ChanceTable<WeenieClassName> StaffWcids_Aluvian = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.quarterstaffnew, 0.40f),
        (WeenieClassName.quarterstaffacidnew, 0.15f),
        (WeenieClassName.quarterstaffelectricnew, 0.15f),
        (WeenieClassName.quarterstaffflamenew, 0.15f),
        (WeenieClassName.quarterstafffrostnew, 0.15f),
    };

    private static ChanceTable<WeenieClassName> StaffWcids_Gharundim = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.nabutnew, 0.40f),
        (WeenieClassName.nabutacidnew, 0.15f),
        (WeenieClassName.nabutelectricnew, 0.15f),
        (WeenieClassName.nabutfirenew, 0.15f),
        (WeenieClassName.nabutfrostnew, 0.15f),
    };

    private static ChanceTable<WeenieClassName> StaffWcids_Sho = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.jonew, 0.20f),
        (WeenieClassName.joacidnew, 0.075f),
        (WeenieClassName.joelectricnew, 0.075f),
        (WeenieClassName.jofirenew, 0.075f),
        (WeenieClassName.jofrostnew, 0.075f),
        (WeenieClassName.swordstaff, 0.20f),
        (WeenieClassName.swordstaffacid, 0.075f),
        (WeenieClassName.swordstaffelectric, 0.075f),
        (WeenieClassName.swordstafffire, 0.075f),
        (WeenieClassName.swordstafffrost, 0.075f),
    };

    public static WeenieClassName Roll(TreasureHeritageGroup heritage, int tier)
    {
        switch (heritage)
        {
            case TreasureHeritageGroup.Aluvian:
                return StaffWcids_Aluvian.Roll();

            case TreasureHeritageGroup.Gharundim:
                return StaffWcids_Gharundim.Roll();

            case TreasureHeritageGroup.Sho:
                return StaffWcids_Sho.Roll();

            default:
                return StaffWcids_All.Roll();
        }
    }

    private static readonly Dictionary<WeenieClassName, TreasureWeaponType> _combined =
        new Dictionary<WeenieClassName, TreasureWeaponType>();

    public static bool TryGetValue(WeenieClassName wcid, out TreasureWeaponType weaponType)
    {
        return _combined.TryGetValue(wcid, out weaponType);
    }
}
