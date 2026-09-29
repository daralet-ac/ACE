using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class ItemSortOrderTests
{
    [TestMethod]
    public void WeightClassRank_GoesClothLightHeavyThenNone()
    {
        var order = new ArmorWeightClass?[] { null, ArmorWeightClass.Heavy, ArmorWeightClass.None, ArmorWeightClass.Cloth, ArmorWeightClass.Light }
            .OrderBy(ItemSortOrder.WeightClassRank)
            .ToArray();

        Assert.AreEqual(ArmorWeightClass.Cloth, order[0]);
        Assert.AreEqual(ArmorWeightClass.Light, order[1]);
        Assert.AreEqual(ArmorWeightClass.Heavy, order[2]);
        Assert.AreEqual(ItemSortOrder.WeightClassRank(null), ItemSortOrder.WeightClassRank(ArmorWeightClass.None));
    }

    [TestMethod]
    public void SlotRank_GoesHeadToToeThenClothesShieldsAndCloaks()
    {
        int Rank(EquipMask mask) => ItemSortOrder.SlotRank(mask);

        Assert.IsTrue(Rank(EquipMask.HeadWear) < Rank(EquipMask.ChestArmor));
        Assert.IsTrue(Rank(EquipMask.ChestArmor) < Rank(EquipMask.HandWear));
        Assert.IsTrue(Rank(EquipMask.HandWear) < Rank(EquipMask.UpperLegArmor));
        Assert.IsTrue(Rank(EquipMask.LowerLegArmor) < Rank(EquipMask.FootWear));

        // shirts, then pants, after all armor
        var shirt = EquipMask.ChestWear | EquipMask.UpperArmWear;
        var pants = EquipMask.AbdomenWear | EquipMask.UpperLegWear | EquipMask.LowerLegWear;
        Assert.IsTrue(Rank(EquipMask.FootWear) < Rank(shirt));
        Assert.IsTrue(Rank(shirt) < Rank(pants));
        Assert.IsTrue(Rank(pants) < Rank(EquipMask.Shield));
        Assert.IsTrue(Rank(EquipMask.Shield) < Rank(EquipMask.Cloak));

        // A piece covering several slots sorts by the highest one it covers: a coat is a chest piece.
        var coat = EquipMask.ChestArmor | EquipMask.AbdomenArmor | EquipMask.UpperArmArmor | EquipMask.LowerArmArmor;
        Assert.AreEqual(Rank(EquipMask.ChestArmor), Rank(coat));

        Assert.IsTrue(Rank(EquipMask.Cloak) < ItemSortOrder.SlotRank(null));
        Assert.IsTrue(Rank(EquipMask.Cloak) < Rank(EquipMask.None));
    }

    [TestMethod]
    public void StyleRank_FollowsTheArmorStyleListWithNoneLast()
    {
        Assert.IsTrue(ItemSortOrder.StyleRank(ArmorStyle.Leather) < ItemSortOrder.StyleRank(ArmorStyle.Chainmail));
        Assert.IsTrue(ItemSortOrder.StyleRank(ArmorStyle.Chainmail) < ItemSortOrder.StyleRank(ArmorStyle.Platemail));
        Assert.IsTrue(ItemSortOrder.StyleRank(ArmorStyle.Buckler) < ItemSortOrder.StyleRank(ArmorStyle.TowerShield));
        Assert.IsTrue(ItemSortOrder.StyleRank(ArmorStyle.CovenantShield) < ItemSortOrder.StyleRank(ArmorStyle.None));
        Assert.AreEqual(ItemSortOrder.StyleRank(null), ItemSortOrder.StyleRank(ArmorStyle.None));
    }

    [TestMethod]
    public void SkillRank_GoesMeleeThenMissileThenMagicWithNoSkillLast()
    {
        var order = new[]
        {
            Skill.None, Skill.LifeMagic, Skill.Bow, Skill.TwoHandedCombat, Skill.Staff, Skill.MartialWeapons,
            Skill.ThrownWeapon, Skill.WarMagic, Skill.Dagger, Skill.UnarmedCombat, Skill.Healing,
        }
            .OrderBy(ItemSortOrder.SkillRank)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                Skill.MartialWeapons, Skill.Dagger, Skill.Staff, Skill.UnarmedCombat, Skill.TwoHandedCombat,
                Skill.Bow, Skill.ThrownWeapon, Skill.WarMagic, Skill.LifeMagic, Skill.Healing, Skill.None,
            },
            order
        );
    }

    [TestMethod]
    public void SubtypeRank_FollowsTheWeaponSubtypeListWithNoneLast()
    {
        Assert.IsTrue(ItemSortOrder.SubtypeRank(LootTables.WeaponSubtype.AxeLarge) < ItemSortOrder.SubtypeRank(LootTables.WeaponSubtype.AxeSmall));
        Assert.IsTrue(ItemSortOrder.SubtypeRank(LootTables.WeaponSubtype.MaceSmall) < ItemSortOrder.SubtypeRank(LootTables.WeaponSubtype.SwordLarge));
        Assert.IsTrue(ItemSortOrder.SubtypeRank(LootTables.WeaponSubtype.ThrownShuriken) < ItemSortOrder.SubtypeRank(null));
        Assert.AreEqual(ItemSortOrder.SubtypeRank(null), ItemSortOrder.SubtypeRank(LootTables.WeaponSubtype.Undef));
    }

    [TestMethod]
    public void WeaponSkillOf_UsesTheWeaponsSkillAndFallsBackToItsSubtype()
    {
        Assert.AreEqual(Skill.Dagger, ItemSortOrder.WeaponSkillOf(Skill.Dagger, LootTables.WeaponSubtype.SwordLarge));

        Assert.AreEqual(Skill.MartialWeapons, ItemSortOrder.WeaponSkillOf(Skill.None, LootTables.WeaponSubtype.MaceMedium));
        Assert.AreEqual(Skill.MartialWeapons, ItemSortOrder.WeaponSkillOf(Skill.None, LootTables.WeaponSubtype.SwordSmall));
        Assert.AreEqual(Skill.Dagger, ItemSortOrder.WeaponSkillOf(Skill.None, LootTables.WeaponSubtype.DaggerLarge));
        Assert.AreEqual(Skill.TwoHandedCombat, ItemSortOrder.WeaponSkillOf(Skill.None, LootTables.WeaponSubtype.TwohandSword));
        Assert.AreEqual(Skill.Bow, ItemSortOrder.WeaponSkillOf(Skill.None, LootTables.WeaponSubtype.CrossbowSmall));
        Assert.AreEqual(Skill.ThrownWeapon, ItemSortOrder.WeaponSkillOf(Skill.None, LootTables.WeaponSubtype.AtlatlLarge));
        Assert.AreEqual(Skill.None, ItemSortOrder.WeaponSkillOf(Skill.None, LootTables.WeaponSubtype.Caster));
        Assert.AreEqual(Skill.None, ItemSortOrder.WeaponSkillOf(Skill.None, null));
    }
}
