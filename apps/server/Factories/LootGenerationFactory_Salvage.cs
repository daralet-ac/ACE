using System;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Factories.Tables;
using ACE.Server.Factories.Tables.Wcids;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories;

public static partial class LootGenerationFactory
{
    private static void MutateSalvage(WorldObject salvage, int tier)
    {
        if (salvage.ItemType != ACE.Entity.Enum.ItemType.TinkeringMaterial)
        {
            return;
        }

        ushort structure = 100;
        var workmanship = SalvageWorkmanshipChances.Roll(tier);
        var numItemsInMaterial = 20;
        var itemWorkmanship = (int)Math.Round(workmanship * numItemsInMaterial);
        var value = (int)(workmanship * 5000) * SalvageWcids.GetValueMod(salvage.WeenieClassId);
        value *= (float)ThreadSafeRandom.Next(0.8f, 1.2f);

        var workmanshipInt = (int)Math.Round(workmanship);
        salvage.Name = $"Salvage Wk{workmanshipInt} ({structure})";
        salvage.Workmanship = workmanshipInt;
        salvage.IconId = Salvage.GetSalvageBagIcon((MaterialType)(salvage.MaterialType ?? 0), workmanshipInt);
        salvage.IgnoreCloIcons = true;
        salvage.UiEffects = UiEffects.Magical;
        salvage.Structure = structure;
        salvage.ItemWorkmanship = itemWorkmanship;
        salvage.NumItemsInMaterial = numItemsInMaterial;
        salvage.Value = (int)value;
    }
}
