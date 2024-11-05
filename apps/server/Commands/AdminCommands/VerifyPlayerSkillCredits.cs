using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Database.Models.Shard;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyPlayerSkillCredits
{
    [CommandHandler(
        "verify-skill-credits",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any bugs with player skill credits"
    )]
    public static void HandleVerifySkillCredits(Session session, params string[] parameters)
    {
        var players = PlayerManager.GetAllOffline();

        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;

        HashSet<uint> oswaldSkillCredit = null;
        HashSet<uint> ralireaSkillCredit = null;
        Dictionary<uint, int> lumAugSkillCredits = null;

        using (var ctx = new ShardDbContext())
        {
            // 4 possible skill credits from quests
            // - OswaldManualCompleted
            // - ArantahKill1 (no 'turned in' stamp, only if given figurine?)
            // - LumAugSkillQuest (stamped either 1 or 2 times)

            oswaldSkillCredit = ctx
                .CharacterPropertiesQuestRegistry.Where(i => i.QuestName.Equals("OswaldManualCompleted"))
                .Select(i => i.CharacterId)
                .ToHashSet();
            ralireaSkillCredit = ctx
                .CharacterPropertiesQuestRegistry.Where(i => i.QuestName.Equals("ArantahKill1"))
                .Select(i => i.CharacterId)
                .ToHashSet();
            lumAugSkillCredits = ctx
                .CharacterPropertiesQuestRegistry.Where(i => i.QuestName.Equals("LumAugSkillQuest"))
                .ToDictionary(i => i.CharacterId, i => i.NumTimesCompleted);
        }

        foreach (var player in players)
        {
            // skip admins
            if (player.Account == null || player.Account.AccessLevel == (uint)AccessLevel.Admin)
            {
                continue;
            }

            if (!player.Heritage.HasValue)
            {
                Console.WriteLine($"{player.Name} (0x{player.Guid}) does not have a Heritage, skipping!");
                continue;
            }

            var heritage = (uint)player.Heritage.Value;
            var heritageGroup = DatManager.PortalDat.CharGen.HeritageGroups[heritage];
            var adjustedSkillCosts = heritageGroup.Skills.ToDictionary(s => (Skill)s.SkillNum, s => s);

            var startCredits = (int)heritageGroup.SkillCredits;

            var levelCredits = GetAdditionalCredits(player.Level ?? 1);

            var questCredits = 0;

            // 4 possible skill credits from quests

            // - OswaldManualCompleted
            if (oswaldSkillCredit.Contains(player.Guid.Full))
            {
                questCredits++;
            }

            // - ArantahKill1 (no 'turned in' stamp, only if given figurine?)
            if (ralireaSkillCredit.Contains(player.Guid.Full))
            {
                questCredits++;
            }

            // - LumAugSkillQuest (stamped either 1 or 2 times)
            if (lumAugSkillCredits.TryGetValue(player.Guid.Full, out var lumSkillCredits))
            {
                questCredits += lumSkillCredits;
            }

            var totalCredits = startCredits + levelCredits + questCredits;

            //Console.WriteLine($"{player.Name} (0x{player.Guid}) Heritage: {heritage}, Level: {player.Level}, Base Credits: {startCredits}, Additional Level Credits: {levelCredits}, Quest Credits: {questCredits}, Total Skill Credits: {totalCredits}");

            var used = 0;

            var specCreditsSpent = 0;

            foreach (var skill in new Dictionary<Skill, PropertiesSkill>(player.Biota.PropertiesSkill))
            {
                var sac = skill.Value.SAC;
                if (sac < SkillAdvancementClass.Trained)
                {
                    continue;
                }

                if (!DatManager.PortalDat.SkillTable.SkillBaseHash.TryGetValue((uint)skill.Key, out var skillInfo))
                {
                    Console.WriteLine(
                        $"{player.Name}:0x{player.Guid}.HandleVerifySkillCredits({skill.Key}): unknown skill"
                    );
                    continue;
                }

                adjustedSkillCosts.TryGetValue(skill.Key, out var adjustedCost);

                var trainedCost = adjustedCost?.NormalCost ?? skillInfo.TrainedCost;
                var specializedCost = adjustedCost?.PrimaryCost ?? skillInfo.SpecializedCost;

                //Console.WriteLine($"{(Skill)skill.Type} trained cost: {skillInfo.TrainedCost}, spec cost: {skillInfo.SpecializedCost}, adjusted trained cost: {trainedCost}, adjusted spec cost: {specializedCost}");

                used += trainedCost;

                if (sac == SkillAdvancementClass.Specialized)
                {
                    switch (skill.Key)
                    {
                        // these can only be speced through augs, they have >= 999 in the spec data
                        case Skill.ArmorTinkering:
                        case Skill.ItemTinkering:
                        case Skill.MagicItemTinkering:
                        case Skill.WeaponTinkering:
                        case Skill.Salvaging:
                            continue;
                    }

                    used += specializedCost - trainedCost;

                    specCreditsSpent += specializedCost;
                }
            }

            var targetCredits = totalCredits - used;
            var targetMsg = $"{player.Name} (0x{player.Guid}) should have {targetCredits} available skill credits";

            if (targetCredits < 0)
            {
                // if the player has already spent more skill credits than they should have,
                // unfortunately this situation requires a partial reset..

                Console.WriteLine(
                    $"{targetMsg}. To fix this situation, trained skill reset will need to be applied{fixStr}"
                );
                foundIssues = true;

                if (fix)
                {
                    UntrainSkills(player, targetCredits);
                }

                continue;
            }

            if (specCreditsSpent > 70)
            {
                // if the player has already spent more skill credits than they should have,
                // unfortunately this situation requires a partial reset..

                Console.WriteLine(
                    $"{player.Name} (0x{player.Guid}) has spent {specCreditsSpent} skill credits on specialization, {specCreditsSpent - 70} over the limit of 70. To fix this situation, specialized skill reset will need to be applied{fixStr}"
                );
                foundIssues = true;

                if (fix)
                {
                    UnspecializeSkills(player);
                }

                continue;
            }

            var availableCredits = player.GetProperty(PropertyInt.AvailableSkillCredits) ?? 0;

            if (availableCredits != targetCredits)
            {
                Console.WriteLine($"{targetMsg}, but they have {availableCredits}{fixStr}");
                foundIssues = true;

                if (fix)
                {
                    player.SetProperty(PropertyInt.AvailableSkillCredits, targetCredits);
                    player.SaveBiotaToDatabase();
                }
            }

            var totalSkillCredits = player.GetProperty(PropertyInt.TotalSkillCredits) ?? 0;

            if (totalSkillCredits != totalCredits)
            {
                Console.WriteLine(
                    $"{player.Name} (0x{player.Guid}) should have {totalCredits} total skill credits, but they have {totalSkillCredits}{fixStr}"
                );
                foundIssues = true;

                if (fix)
                {
                    player.SetProperty(PropertyInt.TotalSkillCredits, totalCredits);
                    player.SaveBiotaToDatabase();
                }
            }
        }

        if (!fix && foundIssues)
        {
            Console.WriteLine($"Dry run completed. Type 'verify-skill-credits fix' to fix any issues.");
        }

        if (!foundIssues)
        {
            Console.WriteLine($"Verified skill credits for {players.Count:N0} players");
        }
    }

    public static int GetAdditionalCredits(int level)
    {
        foreach (var kvp in AdditionalCredits.Reverse())
        {
            if (level >= kvp.Key)
            {
                return kvp.Value;
            }
        }

        return 0;
    }

    /// <summary>
    /// level => total additional credits
    /// </summary>
    public static SortedDictionary<int, int> AdditionalCredits = new SortedDictionary<int, int>()
    {
        { 2, 1 },
        { 3, 2 },
        { 4, 3 },
        { 5, 4 },
        { 6, 5 },
        { 7, 6 },
        { 8, 7 },
        { 9, 8 },
        { 10, 9 },
        { 12, 10 },
        { 14, 11 },
        { 16, 12 },
        { 18, 13 },
        { 20, 14 },
        { 23, 15 },
        { 26, 16 },
        { 29, 17 },
        { 32, 18 },
        { 35, 19 },
        { 40, 20 },
        { 45, 21 },
        { 50, 22 },
        { 55, 23 },
        { 60, 24 },
        { 65, 25 },
        { 70, 26 },
        { 75, 27 },
        { 80, 28 },
        { 85, 29 },
        { 90, 30 },
        { 95, 31 },
        { 100, 32 },
        { 105, 33 },
        { 110, 34 },
        { 115, 35 },
        { 120, 36 },
        { 125, 37 },
        { 130, 38 },
        { 140, 39 },
        { 150, 40 },
        { 160, 41 },
        { 180, 42 },
        { 200, 43 },
        { 225, 44 },
        { 250, 45 },
        { 275, 46 }
    };

    /// <summary>
    /// This method is only required in the rare situation when the amount of available skill credits
    /// a player should have is negative.
    /// </summary>
    private static void UntrainSkills(OfflinePlayer player, int targetCredits)
    {
        long refundXP = 0;

        foreach (var skill in new Dictionary<Skill, PropertiesSkill>(player.Biota.PropertiesSkill))
        {
            if (!DatManager.PortalDat.SkillTable.SkillBaseHash.TryGetValue((uint)skill.Key, out var skillBase))
            {
                Console.WriteLine($"{player.Name}.UntrainSkills({skill.Key}) - unknown skill");
                continue;
            }

            var sac = skill.Value.SAC;

            if (
                sac != SkillAdvancementClass.Trained
                || !Player.IsSkillUntrainable(skill.Key, (HeritageGroup)player.Heritage)
            )
            {
                continue;
            }

            refundXP += skill.Value.PP;

            skill.Value.SAC = SkillAdvancementClass.Untrained;
            skill.Value.InitLevel = 0;
            skill.Value.PP = 0;
            skill.Value.LevelFromPP = 0;

            targetCredits += skillBase.TrainedCost;
        }

        var availableExperience = player.GetProperty(PropertyInt64.AvailableExperience) ?? 0;

        player.SetProperty(PropertyInt64.AvailableExperience, availableExperience + refundXP);

        player.SetProperty(PropertyInt.AvailableSkillCredits, targetCredits);

        player.SetProperty(PropertyBool.UntrainedSkills, true);

        player.SetProperty(PropertyBool.FreeSkillResetRenewed, true);
        player.SetProperty(PropertyBool.SkillTemplesTimerReset, true);

        player.SaveBiotaToDatabase();
    }

    /// <summary>
    /// This method is only required if the player is found to be over the spec skill limit of 70 credits
    /// </summary>
    private static void UnspecializeSkills(OfflinePlayer player)
    {
        long refundXP = 0;

        var refundedCredits = 0;

        foreach (var skill in new Dictionary<Skill, PropertiesSkill>(player.Biota.PropertiesSkill))
        {
            if (!DatManager.PortalDat.SkillTable.SkillBaseHash.TryGetValue((uint)skill.Key, out var skillBase))
            {
                Console.WriteLine($"{player.Name}.UntrainSkills({skill.Key}) - unknown skill");
                continue;
            }

            var sac = skill.Value.SAC;

            if (sac != SkillAdvancementClass.Specialized || Player.AugSpecSkills.Contains(skill.Key))
            {
                continue;
            }

            refundXP += skill.Value.PP;

            skill.Value.SAC = SkillAdvancementClass.Trained;
            skill.Value.InitLevel = 0;
            skill.Value.PP = 0;
            skill.Value.LevelFromPP = 0;

            refundedCredits += skillBase.UpgradeCostFromTrainedToSpecialized;
        }

        var availableExperience = player.GetProperty(PropertyInt64.AvailableExperience) ?? 0;

        player.SetProperty(PropertyInt64.AvailableExperience, availableExperience + refundXP);

        var availableSkillCredits = player.GetProperty(PropertyInt.AvailableSkillCredits) ?? 0;

        player.SetProperty(PropertyInt.AvailableSkillCredits, availableSkillCredits + refundedCredits);

        player.SetProperty(PropertyBool.UnspecializedSkills, true);

        player.SetProperty(PropertyBool.FreeSkillResetRenewed, true);
        player.SetProperty(PropertyBool.SkillTemplesTimerReset, true);

        player.SaveBiotaToDatabase();
    }
}
