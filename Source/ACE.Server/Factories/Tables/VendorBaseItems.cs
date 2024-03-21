using System.Collections.Generic;

namespace ACE.Server.Factories.Tables
{
    public static class VendorBaseItems
    {
        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> ArchmageItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> ArmorerItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> BarkeeperItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> BlacksmithItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> BowyerItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> ButcherItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> GrocerItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> HealerItems = new List<(int, uint, int, double, int)>()
        {
            { (0, 1053958, 0, 0.0, 100) },  // health draught
            { (2, 1053962, 0, 0.0, 100) },  // health potion
            { (3, 1053966, 0, 0.0, 100) },  // health tonic
            { (4, 1053970, 0, 0.0, 100) },  // health tincture
            //{ (5, ??, 0, 0.0, 100) },     // health elixir?
            { (0, 1053959, 0, 0.0, 100) },  // stamina draught
            { (2, 1053963, 0, 0.0, 100) },  // stamina potion
            { (3, 1053967, 0, 0.0, 100) },  // stamina tonic
            { (4, 1053971, 0, 0.0, 100) },  // stamina tincture
            //{ (5, ??, 0, 0.0, 100) },     // stamina elixir?
            { (0, 1053957, 0, 0.0, 100) },  // mana draught
            { (2, 1053961, 0, 0.0, 100) },  // mana potion
            { (3, 1053965, 0, 0.0, 100) },  // mana tonic
            { (4, 1053969, 0, 0.0, 100) },  // mana tincture
            //{ (5, ??, 0, 0.0, 100) },     // mana elixir?
            { (0, 628, 0, 0.0, -1) },       // Crude Healing Kit
            { (2, 629, 0, 0.0, -1) },       // Adept Healing Kit
            { (3, 630, 0, 0.0, -1) },       // Gifted Healing Kit
            { (4, 631, 0, 0.0, -1) },       // Excellent Healing Kit
            { (5, 632, 0, 0.0, -1) },       // Peerless Healing Kit
            { (6, 9229, 0, 0.0, -1) },      // Treated Healing Kit
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> IvoryTraderItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> JewelerItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> LeatherTraderItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> ProvisionerItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> ScribeItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> ShopkeeperItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> TailorItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };

        // <(tier, wcid, paletteTemplate, shade, stackSize)>
        public static readonly List<(int, uint, int, double, int)> WeaponsmithItems = new List<(int, uint, int, double, int)>()
        {
            //{ (0, 1, 0, 0.0, -1) },
            //{ (1, 1, 0, 0.0, -1) },
            //{ (2, 1, 0, 0.0, -1) },
            //{ (3, 1, 0, 0.0, -1) },
            //{ (4, 1, 0, 0.0, -1) },
            //{ (5, 1, 0, 0.0, -1) },
            //{ (6, 1, 0, 0.0, -1) },
            //{ (7, 1, 0, 0.0, -1) },
        };
    }
}
