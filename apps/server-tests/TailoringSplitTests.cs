using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class TailoringSplitTests
{
    [TestMethod]
    public void SkillIds_MatchTheSkillTable()
    {
        // the dat SkillTable entries are keyed by these ids, and characters store skills by id
        Assert.AreEqual(29, (int)Skill.Leatherworking);
        Assert.AreEqual(26, (int)Skill.Weaving);
        Assert.AreEqual("Leatherworking", Skill.Leatherworking.ToSentence());
        Assert.AreEqual("Weaving", Skill.Weaving.ToSentence());
    }

    [TestMethod]
    public void BothSkills_AreValidSkills()
    {
        // (Player's own skill lists can't be read here: its static constructor needs the world database)
        Assert.IsTrue(SkillHelper.ValidSkills.Contains(Skill.Leatherworking));
        Assert.IsTrue(SkillHelper.ValidSkills.Contains(Skill.Weaving));
    }

    [TestMethod]
    public void Hides_AreLeatherworking_Fabrics_AreWeaving()
    {
        Assert.AreEqual(Skill.Leatherworking, Salvage.TinkeringTarget[MaterialType.Leather]);
        Assert.AreEqual(Skill.Leatherworking, Salvage.TinkeringTarget[MaterialType.ArmoredilloHide]);
        Assert.AreEqual(Skill.Leatherworking, Salvage.TinkeringTarget[MaterialType.GromnieHide]);
        Assert.AreEqual(Skill.Leatherworking, Salvage.TinkeringTarget[MaterialType.ReedSharkHide]);

        Assert.AreEqual(Skill.Weaving, Salvage.TinkeringTarget[MaterialType.Linen]);
        Assert.AreEqual(Skill.Weaving, Salvage.TinkeringTarget[MaterialType.Satin]);
        Assert.AreEqual(Skill.Weaving, Salvage.TinkeringTarget[MaterialType.Silk]);
        Assert.AreEqual(Skill.Weaving, Salvage.TinkeringTarget[MaterialType.Velvet]);
        Assert.AreEqual(Skill.Weaving, Salvage.TinkeringTarget[MaterialType.Wool]);
    }

    [TestMethod]
    public void ArmorSalvage_StillBanksAsOneCategory()
    {
        Assert.AreEqual(BankCategory.Tailoring, BankCategories.SalvageKindOf(MaterialType.Leather));
        Assert.AreEqual(BankCategory.Tailoring, BankCategories.SalvageKindOf(MaterialType.Silk));
    }

    [TestMethod]
    public void FormerTailors_GetWeaving_OthersDoNot()
    {
        Assert.IsTrue(TailoringSplit.ShouldGrantWeaving(SkillAdvancementClass.Trained, SkillAdvancementClass.Untrained));
        Assert.IsTrue(TailoringSplit.ShouldGrantWeaving(SkillAdvancementClass.Specialized, SkillAdvancementClass.Untrained));
        Assert.IsFalse(TailoringSplit.ShouldGrantWeaving(SkillAdvancementClass.Untrained, SkillAdvancementClass.Untrained));
        Assert.IsFalse(TailoringSplit.ShouldGrantWeaving(SkillAdvancementClass.Trained, SkillAdvancementClass.Trained));
    }

    [TestMethod]
    public void ArmorPatch_AddsTenPercentOfBaseArmor()
    {
        Assert.AreEqual(50, ArmorPatch.GetArmorBonus(500));
        Assert.AreEqual(23, ArmorPatch.GetArmorBonus(225)); // 22.5 rounds up
        Assert.AreEqual(1, ArmorPatch.GetArmorBonus(4)); // never less than 1
    }

    [TestMethod]
    public void ArmorPatch_OnlyFitsArmorUpToItsWieldReq()
    {
        Assert.IsTrue(ArmorPatch.CanPatch(125, 125));
        Assert.IsTrue(ArmorPatch.CanPatch(50, 270));
        Assert.IsFalse(ArmorPatch.CanPatch(175, 125));
        Assert.IsFalse(ArmorPatch.CanPatch(270, 230));
    }

    [TestMethod]
    public void GrantedWeaving_CountsTowardTheTradeSkillCap()
    {
        Assert.AreEqual(1, TailoringSplit.TradeSkillsAfterGrant(0));
        Assert.AreEqual(2, TailoringSplit.TradeSkillsAfterGrant(1));
        Assert.AreEqual(2, TailoringSplit.TradeSkillsAfterGrant(2));
    }
}
