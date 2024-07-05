using System.Collections.Generic;
using ACE.Entity.Enum;

namespace ACE.Server.Factories.Tables;

public static class MaterialTable
{
    private static readonly Dictionary<MaterialType, float> ValueMod = new Dictionary<MaterialType, float>()
    {
        { MaterialType.Ceramic, 1.0f }, // common
        { MaterialType.Porcelain, 1.1f }, // uncommon
        { MaterialType.Linen, 1.0f }, // common
        { MaterialType.Satin, 1.1f }, // uncommon
        { MaterialType.Silk, 1.1f }, // uncommon
        { MaterialType.Velvet, 1.1f }, // uncommon
        { MaterialType.Wool, 1.0f }, // common
        { MaterialType.Agate, 1.1f }, // uncommon
        { MaterialType.Amber, 1.1f }, // uncommon
        { MaterialType.Amethyst, 1.25f }, // scarce
        { MaterialType.Aquamarine, 1.5f }, // rare
        { MaterialType.Azurite, 1.25f }, // scarce
        { MaterialType.BlackGarnet, 1.25f }, // scarce
        { MaterialType.BlackOpal, 1.25f }, // scarce
        { MaterialType.Bloodstone, 1.1f }, // uncommon
        { MaterialType.Carnelian, 1.25f }, // scarce
        { MaterialType.Citrine, 1.25f }, // scarce
        { MaterialType.Diamond, 2.0f }, // extraordinary
        { MaterialType.Emerald, 2.0f }, // extraordinary
        { MaterialType.FireOpal, 1.25f }, // scarce
        { MaterialType.GreenGarnet, 2.0f }, // extraordinary
        { MaterialType.GreenJade, 1.25f }, // scarce
        { MaterialType.Hematite, 1.25f }, // scarce
        { MaterialType.ImperialTopaz, 2.0f }, // extraordinary
        { MaterialType.Jet, 1.5f }, // rare
        { MaterialType.LapisLazuli, 1.25f }, // scarce
        { MaterialType.LavenderJade, 1.25f }, // scarce
        { MaterialType.Malachite, 1.1f }, // uncommon
        { MaterialType.Moonstone, 1.25f }, // scarce
        { MaterialType.Onyx, 1.1f }, // uncommon
        { MaterialType.Opal, 1.25f }, // scarce
        { MaterialType.Peridot, 1.5f }, // rare
        { MaterialType.RedGarnet, 1.5f }, // rare
        { MaterialType.RedJade, 1.25f }, // scarce
        { MaterialType.RoseQuartz, 1.25f }, // scarce
        { MaterialType.Ruby, 2.0f }, // extraordinary
        { MaterialType.Sapphire, 2.0f }, // extraordinary
        { MaterialType.SmokeyQuartz, 1.25f }, // scarce
        { MaterialType.Sunstone, 1.5f }, // rare
        { MaterialType.TigerEye, 1.25f }, // scarce
        { MaterialType.Tourmaline, 1.5f }, // rare
        { MaterialType.Turquoise, 1.25f }, // scarce
        { MaterialType.WhiteJade, 1.25f }, // scarce
        { MaterialType.WhiteQuartz, 1.25f }, // scarce
        { MaterialType.WhiteSapphire, 2.0f }, // extraordinary
        { MaterialType.YellowGarnet, 1.5f }, // rare
        { MaterialType.YellowTopaz, 1.5f }, // rare
        { MaterialType.Zircon, 1.5f }, // rare
        { MaterialType.Ivory, 1.25f }, // scarce
        { MaterialType.Leather, 1.0f }, // common
        { MaterialType.ArmoredilloHide, 1.25f }, // scarce
        { MaterialType.GromnieHide, 1.1f }, // uncommon
        { MaterialType.ReedSharkHide, 1.25f }, // scarce
        { MaterialType.Brass, 1.0f }, // low
        { MaterialType.Bronze, 1.0f }, // common
        { MaterialType.Copper, 1.1f }, // uncommon
        { MaterialType.Gold, 1.5f }, // rare
        { MaterialType.Iron, 1.0f }, // common
        { MaterialType.Pyreal, 2.0f }, // extraordinary
        { MaterialType.Silver, 1.25f }, // scarce
        { MaterialType.Steel, 1.1f }, // uncommon
        { MaterialType.Alabaster, 1.1f }, // uncommon
        { MaterialType.Granite, 1.0f }, // common
        { MaterialType.Marble, 1.25f }, // scarce
        { MaterialType.Obsidian, 1.1f }, // uncommon
        { MaterialType.Sandstone, 1.0f }, // common
        { MaterialType.Serpentine, 1.25f }, // scarce
        { MaterialType.Ebony, 1.25f }, // scarce
        { MaterialType.Mahogany, 1.1f }, // uncommon
        { MaterialType.Oak, 1.0f }, // low
        { MaterialType.Pine, 1.0f }, // common
        { MaterialType.Teak, 1.1f }, // uncommon
    };

    public static float GetValueMod(MaterialType? materialType)
    {
        if (materialType != null && ValueMod.TryGetValue(materialType.Value, out var valueMod))
        {
            return valueMod;
        }
        else
        {
            return 1.0f; // default?
        }
    }
}
