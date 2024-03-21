using System.Collections.Generic;

using ACE.Entity.Enum;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories.Tables
{
    public static class VendorBaseItems
    {
        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> HealerItems = new List<(int, uint, int, double, int)>()
        {
            { (0, 1053958, 1, 1.0, 100) },  // health draught
            { (1, 1053962, 1, 1.0, 100) },  // health potion
            { (0, 1053959, 1, 1.0, 100) },  // stamina draught
            { (1, 1053963, 1, 1.0, 100) },  // stamina potion
            { (0, 1053957, 1, 1.0, 100) },  // mana draught
            { (1, 1053961, 1, 1.0, 100) },  // mana potion
            { (0, 628, 1, 1.0, 0) },        // Crude Healing Kit
            { (1, 629, 1, 1.0, 0) },        // Adept Healing Kit
            //{ 2, (1, 1, 1.0) },
            //{ 3, (1, 1, 1.0) },
            //{ 4, (1, 1, 1.0) },
            //{ 5, (1, 1, 1.0) },
            //{ 6, (1, 1, 1.0) },
            //{ 7, (1, 1, 1.0) },
        };
    }
}
