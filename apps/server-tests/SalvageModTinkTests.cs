using System;
using System.Linq;
using ACE.Server.Factories;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class SalvageModTinkTests
{
    [TestMethod]
    public void ModTink_AddsAShareOfTheUntinkeredMod()
    {
        // a T7 bow and a T7 war caster at their tier medians
        Assert.AreEqual(0.350625, Salvage.GetModTinkBonus(4.675, Salvage.MahoganyTinkPercent), 1e-9);
        Assert.AreEqual(0.309375, Salvage.GetModTinkBonus(4.125, Salvage.GreenGarnetTinkPercent), 1e-9);

        // Opal and Rose Quartz take 5%
        Assert.AreEqual(0.20625, Salvage.GetModTinkBonus(4.125, 0.05), 1e-9);
    }

    [TestMethod]
    public void ModTink_NeverAddsLessThanTheOldFlatAmount()
    {
        // T1 bows roll as low as 0.83, and T1 elemental casters keep their authored 1.0
        Assert.AreEqual(0.075, Salvage.GetModTinkBonus(0.83, Salvage.MahoganyTinkPercent), 1e-9);
        Assert.AreEqual(0.075, Salvage.GetModTinkBonus(1.0, Salvage.GreenGarnetTinkPercent), 1e-9);
        Assert.AreEqual(0.05, Salvage.GetModTinkBonus(0.83, 0.05), 1e-9);

        // loot snapshots a missing mod as 0, and an item may carry no snapshot at all
        Assert.AreEqual(0.075, Salvage.GetModTinkBonus(0, Salvage.LavenderJadeTinkPercent), 1e-9);
        Assert.AreEqual(0.075, Salvage.GetModTinkBonus(null, Salvage.LavenderJadeTinkPercent), 1e-9);
    }

    [TestMethod]
    public void ModTinks_KeepIronsShareOfDamageAtEveryTier()
    {
        // Iron adds 7.5% of a melee weapon's damage at any tier; the old flat +0.075 fell to about 1.5% of a T7 launcher's
        var subtypes = Enum.GetValues<LootTables.WeaponSubtype>()
            .Where(subtype => LootTables.IsMissileLauncherSubtype(subtype) || subtype == LootTables.WeaponSubtype.Caster);

        foreach (var subtype in subtypes)
        {
            var percent = subtype == LootTables.WeaponSubtype.Caster ? Salvage.GreenGarnetTinkPercent : Salvage.MahoganyTinkPercent;

            for (var tier = 0; tier < 8; tier++)
            {
                double minimum = LootTables.GetMissileCasterSubtypeMinimumDamage(subtype, tier);
                var maximum = minimum + LootTables.GetMissileCasterSubtypeDamageRange(subtype, tier);

                foreach (var baseMod in new[] { minimum, (minimum + maximum) / 2, maximum })
                {
                    var bonus = Salvage.GetModTinkBonus(baseMod, percent);

                    Assert.IsTrue(bonus / baseMod >= Salvage.IronTinkPercent - 1e-9, $"{subtype} T{tier + 1} at {baseMod}: {bonus / baseMod:P2}");
                    Assert.IsTrue(bonus >= percent - 1e-9, $"{subtype} T{tier + 1} at {baseMod}: {bonus} is under the old flat amount");
                }
            }
        }
    }

    [TestMethod]
    public void ModTinkBonus_IsShownInAppraisalPercentagePoints()
    {
        // the appraisal panel shows a mod as (mod - 1) x 100%, so a T7 bow going 4.675 -> 5.026 reads +367.5% -> +402.6%
        Assert.AreEqual("35.1%", Salvage.FormatModTinkBonus(0.350625));
        Assert.AreEqual("7.5%", Salvage.FormatModTinkBonus(0.075));
        Assert.AreEqual("5%", Salvage.FormatModTinkBonus(0.05));
        Assert.AreEqual("20.6%", Salvage.FormatModTinkBonus(Salvage.GetModTinkBonus(4.125, 0.05)));
    }
}
