using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;

namespace ACE.Server.WorldObjects;

partial class Player
{
    /// <summary>
    /// Tailoring (skill 29) was split into Leatherworking (still 29) and Weaving (26). A character that had Tailoring keeps
    /// it as Leatherworking, and on their first login after the split is also given Weaving at the same advancement class
    /// and experience. Runs once per character (TailoringSplitMigrated).
    /// </summary>
    public void MigrateTailoringSplit()
    {
        if (GetProperty(PropertyBool.TailoringSplitMigrated) == true)
        {
            return;
        }

        SetProperty(PropertyBool.TailoringSplitMigrated, true);

        var leatherworking = GetCreatureSkill(Skill.Leatherworking);
        var weaving = GetCreatureSkill(Skill.Weaving);

        if (!TailoringSplit.ShouldGrantWeaving(leatherworking.AdvancementClass, weaving.AdvancementClass))
        {
            return;
        }

        weaving.AdvancementClass = leatherworking.AdvancementClass;
        weaving.InitLevel = leatherworking.InitLevel;
        weaving.ExperienceSpent = leatherworking.ExperienceSpent;
        weaving.Ranks = leatherworking.Ranks;

        // Weaving is free for a former Tailor, but it still counts toward the trade skill limit: someone with Tailoring
        // and nothing else now has both of their two trade skills.
        var tradeSkills = QuestManager.GetCurrentSolves(TailoringSplit.TradeSkillQuest);
        var newTradeSkills = TailoringSplit.TradeSkillsAfterGrant(tradeSkills);
        if (newTradeSkills > tradeSkills)
        {
            QuestManager.SetQuestCompletions(TailoringSplit.TradeSkillQuest, newTradeSkills);
        }

        ChangesDetected = true;

        _log.Information(
            "[TAILORING SPLIT] {Player} (0x{Guid}) had Tailoring ({Class}), given Weaving at the same level",
            Name,
            Guid,
            leatherworking.AdvancementClass
        );
    }
}

/// <summary>
/// The rules of the Tailoring split migration, kept apart from Player so they can be tested without a database.
/// </summary>
public static class TailoringSplit
{
    /// <summary>
    /// The trade skill quest the trainers count: a character may hold at most MaxTradeSkills trade skills.
    /// </summary>
    public const string TradeSkillQuest = "TradeSkill";

    public const int MaxTradeSkills = 2;

    public static bool ShouldGrantWeaving(SkillAdvancementClass tailoring, SkillAdvancementClass weaving)
    {
        return tailoring >= SkillAdvancementClass.Trained && weaving < SkillAdvancementClass.Trained;
    }

    public static int TradeSkillsAfterGrant(int tradeSkills)
    {
        return Math.Min(tradeSkills + 1, MaxTradeSkills);
    }
}
