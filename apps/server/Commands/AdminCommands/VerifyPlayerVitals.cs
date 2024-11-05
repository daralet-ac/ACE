using System;
using System.Collections.Generic;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyPlayerVitals
{
    [CommandHandler(
        "verify-vitals",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any bugs with player vitals data"
    )]
    public static void HandleVerifyVitals(Session session, params string[] parameters)
    {
        var players = PlayerManager.GetAllOffline();

        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;

        foreach (var player in players)
        {
            var updated = false;

            foreach (
                var vital in new Dictionary<PropertyAttribute2nd, PropertiesAttribute2nd>(
                    player.Biota.PropertiesAttribute2nd
                )
            )
            {
                // ensure this is a valid MaxVital
                if (
                    vital.Key != PropertyAttribute2nd.MaxHealth
                    && vital.Key != PropertyAttribute2nd.MaxStamina
                    && vital.Key != PropertyAttribute2nd.MaxMana
                )
                {
                    Console.WriteLine($"{player.Name} has unknown vital {vital.Key}{fixStr}");
                    foundIssues = true;

                    if (fix)
                    {
                        // i have found no instances of this situation being run into,
                        // but if it does happen, verify-xp will refund the player xp properly

                        player.Biota.PropertiesAttribute2nd.Remove(vital.Key);
                        updated = true;
                    }
                    continue;
                }

                var rank = vital.Value.LevelFromCP;

                // verify vital rank
                var correctRank = Player.CalcVitalRank(vital.Value.CPSpent);
                if (rank != correctRank)
                {
                    Console.WriteLine($"{player.Name}'s {vital.Key} rank is {rank}, should be {correctRank}{fixStr}");
                    foundIssues = true;

                    if (fix)
                    {
                        vital.Value.LevelFromCP = (ushort)correctRank;
                        updated = true;
                    }
                }

                // verify vital xp is within bounds
                var vitalXPTable = DatManager.PortalDat.XpTable.VitalXpList;
                var maxVitalXp = vitalXPTable[vitalXPTable.Count - 1];

                if (vital.Value.CPSpent > maxVitalXp)
                {
                    Console.WriteLine(
                        $"{player.Name}'s {vital.Key} vital total xp is {vital.Value.CPSpent:N0}, should be capped at {maxVitalXp:N0}{fixStr}"
                    );
                    foundIssues = true;

                    if (fix)
                    {
                        // again i have found no instances of this situation being run into,
                        // but if it does happen, verify-xp will refund the player xp properly

                        vital.Value.CPSpent = maxVitalXp;
                        updated = true;
                    }
                }
            }

            if (updated)
            {
                player.SaveBiotaToDatabase();
            }
        }

        if (!fix && foundIssues)
        {
            Console.WriteLine($"Dry run completed. Type 'verify-vitals fix' to fix any issues.");
        }

        if (!foundIssues)
        {
            Console.WriteLine($"Verified vitals for {players.Count:N0} players");
        }
    }
}
