using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Database;
using ACE.Database.Models.Shard;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyPlayerXp
{
    [CommandHandler(
        "verify-xp",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any bugs with player xp"
    )]
    public static void HandleVerifyExperience(Session session, params string[] parameters)
    {
        var players = PlayerManager.GetAllOffline();

        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;

        var results = new List<VerifyXpResult>();

        HashSet<uint> lesserBenediction = null;

        using (var ctx = new ShardDbContext())
        {
            // Asheron's Lesser Benediction augmentation operates differently than all other augs
            lesserBenediction = ctx
                .CharacterPropertiesQuestRegistry.Where(i => i.QuestName.Equals("LesserBenedictionAug"))
                .Select(i => i.CharacterId)
                .ToHashSet();
        }

        foreach (var player in players)
        {
            var totalXP = player.GetProperty(PropertyInt64.TotalExperience) ?? 0;
            var unassignedXP = player.GetProperty(PropertyInt64.AvailableExperience) ?? 0;

            // loop through all attributes/vitals/skills, add up assigned xp
            long attributeXP = 0;
            long vitalXP = 0;
            long skillXP = 0;
            long augXP = 0;

            var diffXP = Math.Min(0, player.GetProperty(PropertyInt64.VerifyXp) ?? 0);

            foreach (var attribute in player.Biota.PropertiesAttribute)
            {
                attributeXP += attribute.Value.CPSpent;
            }

            foreach (var vital in player.Biota.PropertiesAttribute2nd)
            {
                vitalXP += vital.Value.CPSpent;
            }

            foreach (var skill in player.Biota.PropertiesSkill)
            {
                skillXP += skill.Value.PP;
            }

            // find any xp spent on augs
            var heritage = (HeritageGroup?)player.GetProperty(PropertyInt.HeritageGroup);
            if (heritage == null)
            {
                continue; // ignore admins who have morphed into asheron / bael'zharon
            }

            var heritageAug = VerifyPlayerHeritageAugs.GetHeritageAug(heritage.Value);

            foreach (var kvp in VerifyPlayerMaxAugs.AugmentationDevices)
            {
                var augProperty = kvp.Key;

                var numAugs = player.GetProperty(augProperty) ?? 0;
                if (augProperty == heritageAug)
                {
                    numAugs--;
                }

                if (numAugs <= 0)
                {
                    continue;
                }

                var aug = DatabaseManager.World.GetCachedWeenie(kvp.Value);
                aug.PropertiesInt64.TryGetValue(PropertyInt64.AugmentationCost, out var costPer);

                augXP += costPer * numAugs;
            }

            if (lesserBenediction.Contains(player.Guid.Full))
            {
                augXP += 2000000000;
            }

            var calculatedSpent = attributeXP + vitalXP + skillXP + augXP + diffXP;

            var currentSpent = totalXP - unassignedXP;

            var bonusXp = (currentSpent - calculatedSpent) % 526;

            if (calculatedSpent != currentSpent && bonusXp != 0)
            {
                // the results for this data set can be large,
                // especially due to an earlier ace bug where it wasn't calculating the Proficiency Points correctly

                // instead of displaying the results in random order,
                // we going to sort them all by diff

                foundIssues = true;
                results.Add(new VerifyXpResult(player, calculatedSpent, currentSpent));
            }
        }

        var xpList = DatManager.PortalDat.XpTable.CharacterLevelXPList;
        var maxTotalXp = (long)xpList[xpList.Count - 1];

        foreach (var result in results.OrderBy(i => i.Player.Name).OrderBy(i => i.Diff))
        {
            var player = result.Player;
            var diff = result.Diff;

            Console.WriteLine(
                $"{player.Name} is calculated to have spent {result.Calculated:N0} experience, which currently differs by {diff:N0}{fixStr}"
            );

            if (!fix)
            {
                continue;
            }

            if (diff > 0)
            {
                // add to unassigned xp
                var unassignedXP = player.GetProperty(PropertyInt64.AvailableExperience) ?? 0;
                player.SetProperty(PropertyInt64.AvailableExperience, unassignedXP + diff);
                player.SetProperty(PropertyInt64.VerifyXp, diff);
            }
            else
            {
                var totalXP = player.GetProperty(PropertyInt64.TotalExperience) ?? 0;
                if (totalXP - diff > maxTotalXp)
                {
                    var unassignedXP = player.GetProperty(PropertyInt64.AvailableExperience) ?? 0;
                    if (unassignedXP + diff >= 0)
                    {
                        // this is the only (rare) case where subtracting from AvailableExperience is required
                        player.SetProperty(PropertyInt64.AvailableExperience, unassignedXP + diff);
                    }
                    else
                    {
                        Console.WriteLine($"ERROR: couldn't fix, xp exceeds all bounds");
                    }
                }
                else
                {
                    // setting the diff property below, which will be handled on player login
                    // to properly handle possibly leveling up / skill credits
                    player.SetProperty(PropertyInt64.VerifyXp, diff);
                }
            }
            player.SaveBiotaToDatabase();
        }

        if (foundIssues)
        {
            Console.WriteLine($"{(fix ? "Fixed" : "Found")} issues for {results.Count:N0} players");

            if (!fix)
            {
                Console.WriteLine($"Dry run completed. Type 'verify-xp fix' to fix any issues.");
            }
        }
        else
        {
            Console.WriteLine($"Verified XP for {players.Count:N0} players");
        }
    }
}
