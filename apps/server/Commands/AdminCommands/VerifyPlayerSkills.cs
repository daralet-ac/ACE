using System;
using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyPlayerSkills
{
    [CommandHandler(
        "verify-skills",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any bugs with player skill data"
    )]
    public static void HandleVerifySkills(Session session, params string[] parameters)
    {
        var players = PlayerManager.GetAllOffline();

        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;

        foreach (var player in players)
        {
            var updated = false;

            foreach (var skill in new Dictionary<Skill, PropertiesSkill>(player.Biota.PropertiesSkill))
            {
                // ensure this is a valid player skill
                if (!Player.PlayerSkills.Contains(skill.Key))
                {
                    Console.WriteLine($"{player.Name} has unknown skill {skill.Key}{fixStr}");
                    foundIssues = true;
                    if (fix)
                    {
                        // i have found no instances of these skills ever having xp put into them,
                        // but if there were, verify-xp will fix that
                        player.Biota.PropertiesSkill.Remove((Skill)skill.Key);
                        updated = true;
                    }
                    continue;
                }

                var rank = skill.Value.LevelFromPP;

                var sac = skill.Value.SAC;
                if (sac < SkillAdvancementClass.Trained)
                {
                    if (skill.Value.PP > 0 || skill.Value.LevelFromPP > 0)
                    {
                        Console.WriteLine(
                            $"{player.Name} has {sac} skill {skill.Key} with {skill.Value.PP:N0} xp (rank {skill.Value.LevelFromPP}){fixStr}"
                        );
                        foundIssues = true;

                        if (fix)
                        {
                            // i have found no instances of this situation being run into,
                            // but if it does happen, verify-xp will refund the player xp properly
                            skill.Value.PP = 0;
                            skill.Value.LevelFromPP = 0;

                            updated = true;
                        }
                    }
                    continue;
                }

                if (sac != SkillAdvancementClass.Specialized)
                {
                    if (skill.Value.InitLevel > 0)
                    {
                        Console.WriteLine(
                            $"{player.Name} has {sac} skill {skill.Key} with {skill.Value.InitLevel:N0} InitLevel{fixStr}"
                        );
                        foundIssues = true;

                        if (fix)
                        {
                            skill.Value.InitLevel = 0;

                            updated = true;
                        }
                    }
                }
                else
                {
                    if (skill.Value.InitLevel != 10)
                    {
                        Console.WriteLine(
                            $"{player.Name} has {sac} skill {skill.Key} with {skill.Value.InitLevel:N0} InitLevel{fixStr}"
                        );
                        foundIssues = true;

                        if (fix)
                        {
                            skill.Value.InitLevel = 10;

                            updated = true;
                        }
                    }
                }

                // verify skill rank
                var correctRank = Player.CalcSkillRank(sac, skill.Value.PP);
                if (rank != correctRank)
                {
                    Console.WriteLine($"{player.Name}'s {skill.Key} rank is {rank}, should be {correctRank}{fixStr}");
                    foundIssues = true;

                    if (fix)
                    {
                        skill.Value.LevelFromPP = (ushort)correctRank;
                        updated = true;
                    }
                }

                // verify skill xp is within bounds

                // in retail, if a player had a trained skill maxed out, and then they speced it in spec temple,
                // they would sort of temporarily 'lose' that ~103m xp, unless they reset the trained skill, and then speced it

                // so the data can be in a legit situation here where a character has a skill speced,
                // but their xp is beyond the spec xp cap (4,100,490,438) and <= the trained xp cap (4,203,819,496)

                //var skillXPTable = Player.GetSkillXPTable(sac);
                var skillXPTable = Player.GetSkillXPTable(SkillAdvancementClass.Trained);
                var maxSkillXp = skillXPTable[skillXPTable.Count - 1];

                if (skill.Value.PP > maxSkillXp)
                {
                    Console.WriteLine(
                        $"{player.Name}'s {sac} {skill.Key} skill total xp is {skill.Value.PP:N0}, should be capped at {maxSkillXp:N0}{fixStr}"
                    );
                    foundIssues = true;
                    if (fix)
                    {
                        // again i have found no instances of this situation being run into,
                        // but if it does happen, verify-xp will refund the player xp properly
                        skill.Value.PP = maxSkillXp;
                        updated = true;
                    }
                }
            }

            if (fix && updated)
            {
                player.SaveBiotaToDatabase();
            }
        }

        if (!fix && foundIssues)
        {
            Console.WriteLine($"Dry run completed. Type 'verify-skills fix' to fix any issues.");
        }

        if (!foundIssues)
        {
            Console.WriteLine($"Verified skills for {players.Count:N0} players");
        }
    }
}
