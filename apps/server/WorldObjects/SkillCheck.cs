using System;

namespace ACE.Server.WorldObjects;

public static class SkillCheck
{
    public static double GetSkillChance(int skill, int difficulty, float factor = 0.03f)
    {
        var chance = 1.0 - (1.0 / (1.0 + Math.Exp(factor * (skill - difficulty))));

        return Math.Min(1.0, Math.Max(0.0, chance));
    }

    public static double GetSkillChance(uint skill, uint difficulty, float factor = 0.03f)
    {
        return GetSkillChance((int)skill, (int)difficulty, factor);
    }

    public static double GetMagicSkillChance(int skill, int difficulty)
    {
        return GetSkillChance(skill, difficulty, 0.07f);
    }

    /// <summary>
    /// Returns a chance that grows with skill / comparedSkill, up to maxChance once skill reaches comparedSkill.
    /// A compared skill of 0 gives maxChance.
    /// </summary>
    public static float GetSkillRatioChance(uint skill, uint comparedSkill, float maxChance = 0.5f)
    {
        var ratio = comparedSkill > 0 ? skill / (float)comparedSkill : 1.0f;

        return ratio > 1f ? maxChance : ratio * maxChance;
    }
}
