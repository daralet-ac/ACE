using System;
using ACE.Entity.Enum;
using ACE.Server.Factories.Tables;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories;

public static partial class LootGenerationFactory
{
    /// <summary>
    /// A stack of 100 units of loot salvage of the material, at a workmanship rolled for the tier and rounded to a whole
    /// number (salvage stacks by whole-number workmanship). Null if the material has no salvage.
    ///
    /// Not called at the moment: salvage isn't in the loot tables (SalvageWcids.Roll has no caller either).
    /// </summary>
    private static WorldObject CreateSalvage(MaterialType material, int tier)
    {
        var workmanship = (int)Math.Round(SalvageWorkmanshipChances.Roll(tier));

        return SalvageStacks.Create(material, workmanship, 100);
    }
}
