using System.Collections.Generic;

namespace ACE.Server.Factories.Tables
{
    public static class VendorBaseItems
    {
        private const int HeritageAny = 0, HeritageAluvian = 1, HeritageGharundim = 2, HeritageSho = 3;

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> ArchmageItems = new List<(int, int, uint, int, double, int)>()
        {
            // Brown
            { (0, HeritageAny, 1055030, 0, 0.0, 1) }, // Small Component Pouch
            { (2, HeritageAny, 1055040, 0, 0.0, 1) }, // Component Pouch
            { (4, HeritageAny, 1055050, 0, 0.0, 1) }, // Large Component Pouch

            // Yellow
            { (0, HeritageGharundim, 1055031, 0, 0.0, 1) }, // Small Component Pouch
            { (2, HeritageGharundim, 1055041, 0, 0.0, 1) }, // Component Pouch
            { (4, HeritageGharundim, 1055051, 0, 0.0, 1) }, // Large Component Pouch

            // Green
            { (0, HeritageAluvian, 1055032, 0, 0.0, 1) }, // Small Component Pouch
            { (2, HeritageAluvian, 1055042, 0, 0.0, 1) }, // Component Pouch
            { (4, HeritageAluvian, 1055052, 0, 0.0, 1) }, // Large Component Pouch

            // Teal
            { (0, HeritageSho, 1055033, 0, 0.0, 1) }, // Small Component Pouch
            { (2, HeritageSho, 1055043, 0, 0.0, 1) }, // Component Pouch
            { (4, HeritageSho, 1055053, 0, 0.0, 1) }, // Large Component Pouch

            // Blue
            { (0, HeritageAluvian, 1055034, 0, 0.0, 1) }, // Small Component Pouch
            { (2, HeritageAluvian, 1055044, 0, 0.0, 1) }, // Component Pouch
            { (4, HeritageAluvian, 1055054, 0, 0.0, 1) }, // Large Component Pouch

            // Purple
            { (0, HeritageSho, 1055035, 0, 0.0, 1) }, // Small Component Pouch
            { (2, HeritageSho, 1055045, 0, 0.0, 1) }, // Component Pouch
            { (4, HeritageSho, 1055055, 0, 0.0, 1) }, // Large Component Pouch

            // Red
            { (0, HeritageGharundim, 1055036, 0, 0.0, 1) }, // Small Component Pouch
            { (2, HeritageGharundim, 1055046, 0, 0.0, 1) }, // Component Pouch
            { (4, HeritageGharundim, 1055056, 0, 0.0, 1) }, // Large Component Pouch

            // White
            { (0, HeritageSho, 1055037, 0, 0.0, 1) }, // Small Component Pouch
            { (2, HeritageSho, 1055047, 0, 0.0, 1) }, // Component Pouch
            { (4, HeritageSho, 1055057, 0, 0.0, 1) }, // Large Component Pouch

            // Gray
            { (0, HeritageGharundim, 1055038, 0, 0.0, 1) }, // Small Component Pouch
            { (2, HeritageGharundim, 1055048, 0, 0.0, 1) }, // Component Pouch
            { (4, HeritageGharundim, 1055058, 0, 0.0, 1) }, // Large Component Pouch

            // Black
            { (0, HeritageAluvian, 1055039, 0, 0.0, 1) }, // Small Component Pouch
            { (2, HeritageAluvian, 1055049, 0, 0.0, 1) }, // Component Pouch
            { (4, HeritageAluvian, 1055059, 0, 0.0, 1) }, // Large Component Pouch
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> ArmorerItems = new List<(int, int, uint, int, double, int)>()
        {
            
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> BarkeeperItems = new List<(int, int, uint, int, double, int)>()
        {
            
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> BlacksmithItems = new List<(int, int, uint, int, double, int)>()
        {
            
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> BowyerItems = new List<(int, int, uint, int, double, int)>()
        {
            // Brown
            { (0, HeritageAny, 1055000, 0, 0.0, 1) }, // small quiver
            { (2, HeritageAny, 1055010, 0, 0.0, 1) }, // quiver
            { (4, HeritageAny, 1055020, 0, 0.0, 1) }, // large quiver

            // Green
            { (0, HeritageAluvian, 1055002, 0, 0.0, 1) }, // small quiver
            { (2, HeritageAluvian, 1055012, 0, 0.0, 1) }, // quiver
            { (4, HeritageAluvian, 1055022, 0, 0.0, 1) }, // large quiver

            // Blue
            { (0, HeritageAluvian, 1055004, 0, 0.0, 1) }, // small quiver
            { (2, HeritageAluvian, 1055014, 0, 0.0, 1) }, // quiver
            { (4, HeritageAluvian, 1055024, 0, 0.0, 1) }, // large quiver

            // Black
            { (0, HeritageAluvian, 1055009, 0, 0.0, 1) }, // small quiver
            { (2, HeritageAluvian, 1055019, 0, 0.0, 1) }, // quiver
            { (4, HeritageAluvian, 1055029, 0, 0.0, 1) }, // large quiver

            // Yellow
            { (0, HeritageGharundim, 1055001, 0, 0.0, 1) }, // small quiver
            { (2, HeritageGharundim, 1055011, 0, 0.0, 1) }, // quiver
            { (4, HeritageGharundim, 1055021, 0, 0.0, 1) }, // large quiver

            // Red
            { (0, HeritageGharundim, 1055006, 0, 0.0, 1) }, // small quiver
            { (2, HeritageGharundim, 1055016, 0, 0.0, 1) }, // quiver
            { (4, HeritageGharundim, 1055026, 0, 0.0, 1) }, // large quiver

            // Gray
            { (0, HeritageGharundim, 1055008, 0, 0.0, 1) }, // small quiver
            { (2, HeritageGharundim, 1055018, 0, 0.0, 1) }, // quiver
            { (4, HeritageGharundim, 1055028, 0, 0.0, 1) }, // large quiver

            // Teal
            { (0, HeritageSho, 1055003, 0, 0.0, 1) }, // small quiver
            { (2, HeritageSho, 1055013, 0, 0.0, 1) }, // quiver
            { (4, HeritageSho, 1055023, 0, 0.0, 1) }, // large quiver

            // Purple
            { (0, HeritageSho, 1055005, 0, 0.0, 1) }, // small quiver
            { (2, HeritageSho, 1055015, 0, 0.0, 1) }, // quiver
            { (4, HeritageSho, 1055025, 0, 0.0, 1) }, // large quiver

            // White
            { (0, HeritageSho, 1055007, 0, 0.0, 1) }, // small quiver
            { (2, HeritageSho, 1055017, 0, 0.0, 1) }, // quiver
            { (4, HeritageSho, 1055027, 0, 0.0, 1) }, // large quiver
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> ButcherItems = new List<(int, int, uint, int, double, int)>()
        {
            
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> GrocerItems = new List<(int, int, uint, int, double, int)>()
        {
            
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> HealerItems = new List<(int, int, uint, int, double, int)>()
        {
            { (0, HeritageAny, 1053958, 0, 0.0, 100) },  // health draught
            { (2, HeritageAny, 1053962, 0, 0.0, 100) },  // health potion
            { (3, HeritageAny, 1053966, 0, 0.0, 100) },  // health tonic
            { (4, HeritageAny, 1053970, 0, 0.0, 100) },  // health tincture
            //{ (5, ??, 0, 0.0, 100) },     // health elixir?
            { (0, HeritageAny, 1053959, 0, 0.0, 100) },  // stamina draught
            { (2, HeritageAny, 1053963, 0, 0.0, 100) },  // stamina potion
            { (3, HeritageAny, 1053967, 0, 0.0, 100) },  // stamina tonic
            { (4, HeritageAny, 1053971, 0, 0.0, 100) },  // stamina tincture
            //{ (5, ??, 0, 0.0, 100) },     // stamina elixir?
            { (0, HeritageAny, 1053957, 0, 0.0, 100) },  // mana draught
            { (2, HeritageAny, 1053961, 0, 0.0, 100) },  // mana potion
            { (3, HeritageAny, 1053965, 0, 0.0, 100) },  // mana tonic
            { (4, HeritageAny, 1053969, 0, 0.0, 100) },  // mana tincture
            //{ (5, ??, 0, 0.0, 100) },     // mana elixir?
            { (0, HeritageAny, 628, 0, 0.0, -1) },       // Crude Healing Kit
            { (2, HeritageAny, 629, 0, 0.0, -1) },       // Adept Healing Kit
            { (3, HeritageAny, 630, 0, 0.0, -1) },       // Gifted Healing Kit
            { (4, HeritageAny, 631, 0, 0.0, -1) },       // Excellent Healing Kit
            { (5, HeritageAny, 632, 0, 0.0, -1) },       // Peerless Healing Kit
            { (6, HeritageAny, 9229, 0, 0.0, -1) },      // Treated Healing Kit
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> IvoryTraderItems = new List<(int, int, uint, int, double, int)>()
        {
            
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> JewelerItems = new List<(int, int, uint, int, double, int)>()
        {

        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> LeatherTraderItems = new List<(int, int, uint, int, double, int)>()
        {

        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> ProvisionerItems = new List<(int, int, uint, int, double, int)>()
        {
            { (0, HeritageAny, 1055060, 0, 0.0, 1) }, // Salvage Sacks
            { (2, HeritageAny, 1055070, 0, 0.0, 1) },
            { (4, HeritageAny, 1055080, 0, 0.0, 1) },
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> ScribeItems = new List<(int, int, uint, int, double, int)>()
        {

        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> ShopkeeperItems = new List<(int, int, uint, int, double, int)>()
        {
            // Packs
            { (0, HeritageAny, 136, 21, 0.0, 1) },          // brown - pack
            { (0, HeritageAny, 166, 21, 0.0, 1) },          // brown - sack
            { (0, HeritageAny, 138, 4, 0.0, 1) },          // brown - belt pouch
            { (0, HeritageAny, 139, 4, 0.0, 1) },          // brown - small belt pouch

            { (0, HeritageAluvian, 136, 8, 0.0, 1) },       // green - pack
            { (0, HeritageAluvian, 166, 8, 0.0, 1) },       // green - sack
            { (0, HeritageAluvian, 138, 84, 0.0, 1) },       // green - belt pouch
            { (0, HeritageAluvian, 139, 84, 0.0, 1) },       // green - small belt pouch

            { (0, HeritageAluvian, 136, 2, 0.0, 1) },       // blue - pack
            { (0, HeritageAluvian, 166, 2, 0.0, 1) },       // blue - sack
            { (0, HeritageAluvian, 138, 91, 0.0, 1) },       // blue - belt pouch
            { (0, HeritageAluvian, 139, 91, 0.0, 1) },       // blue - small belt pouch

            { (0, HeritageAluvian, 136, 39, 0.0, 1) },      // black - pack
            { (0, HeritageAluvian, 166, 39, 0.0, 1) },      // black - sack
            { (0, HeritageAluvian, 138, 93, 0.0, 1) },      // black - belt pouch
            { (0, HeritageAluvian, 139, 93, 0.0, 1) },      // black - small belt pouch

            { (0, HeritageGharundim, 136, 17, 0.0, 1) },    // yellow - pack
            { (0, HeritageGharundim, 166, 17, 0.0, 1) },    // yellow - sack
            { (0, HeritageGharundim, 138, 86, 0.0, 1) },    // yellow - belt pouch
            { (0, HeritageGharundim, 139, 86, 0.0, 1) },    // yellow - small belt pouch

            { (0, HeritageGharundim, 136, 14, 0.0, 1) },    // red - pack
            { (0, HeritageGharundim, 166, 14, 0.0, 1) },    // red - sack
            { (0, HeritageGharundim, 138, 85, 0.0, 1) },    // red - belt pouch
            { (0, HeritageGharundim, 139, 85, 0.0, 1) },    // red - small belt pouch

            { (0, HeritageGharundim, 136, 9, 0.0, 1) },     // gray - pack
            { (0, HeritageGharundim, 166, 9, 0.0, 1) },     // gray - sack
            { (0, HeritageGharundim, 138, 90, 0.0, 1) },     // gray - belt pouch
            { (0, HeritageGharundim, 139, 90, 0.0, 1) },     // gray - small belt pouch

            { (0, HeritageSho, 136, 77, 0.0, 1) },          // teal - pack
            { (0, HeritageSho, 166, 77, 0.0, 1) },          // teal - sack
            { (0, HeritageSho, 138, 88, 0.0, 1) },          // teal - belt pouch
            { (0, HeritageSho, 139, 88, 0.0, 1) },          // teal - small belt pouch

            { (0, HeritageSho, 136, 13, 0.0, 1) },          // purple - pack
            { (0, HeritageSho, 166, 13, 0.0, 1) },          // purple - sack
            { (0, HeritageSho, 138, 92, 0.0, 1) },          // purple - belt pouch
            { (0, HeritageSho, 139, 92, 0.0, 1) },          // purple - small belt pouch

            { (0, HeritageSho, 136, 61, 0.0, 1) },          // white - pack
            { (0, HeritageSho, 166, 61, 0.0, 1) },          // white - sack
            { (0, HeritageSho, 138, 90, 0.0, 1) },          // white - belt pouch
            { (0, HeritageSho, 139, 90, 0.0, 1) },          // white - small belt pouch

            // Salvage Sacks
            { (0, HeritageAny, 1055060, 0, 0.0, 1) },
            { (2, HeritageAny, 1055070, 0, 0.0, 1) },
            { (4, HeritageAny, 1055080, 0, 0.0, 1) },
        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> TailorItems = new List<(int, int, uint, int, double, int)>()
        {

        };

        // <(tier, heritage, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, int, uint, int, double, int)> WeaponsmithItems = new List<(int, int, uint, int, double, int)>()
        {

        };
    }
}
