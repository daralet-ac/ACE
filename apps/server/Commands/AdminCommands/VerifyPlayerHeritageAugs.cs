using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyPlayerHeritageAugs
{
    [CommandHandler(
        "verify-heritage-augs",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies all players have their heritage augs."
    )]
    public static void HandleVerifyHeritageAugs(Session session, params string[] parameters)
    {
        var players = PlayerManager.GetAllOffline();

        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;

        foreach (var player in players)
        {
            var heritage = (HeritageGroup?)player.GetProperty(PropertyInt.HeritageGroup);
            if (heritage == null)
            {
                Console.WriteLine($"Couldn't find heritage for {player.Name}");
                continue;
            }

            var heritageAug = GetHeritageAug(heritage.Value);
            if (heritageAug == null)
            {
                Console.WriteLine($"Couldn't find heritage aug for {heritage} player {player.Name}");
                continue;
            }

            var numAugs = player.GetProperty(heritageAug.Value) ?? 0;
            if (numAugs < 1)
            {
                Console.WriteLine($"{heritageAug}={numAugs} for {heritage} player {player.Name}{fixStr}");
                foundIssues = true;

                if (fix)
                {
                    player.SetProperty(heritageAug.Value, 1);
                    player.SaveBiotaToDatabase();
                }
            }
        }
        if (!fix && foundIssues)
        {
            Console.WriteLine($"Dry run completed. Type 'verify-heritage-augs fix' to fix any issues.");
        }

        if (!foundIssues)
        {
            Console.WriteLine($"Verified heritage augs for {players.Count:N0} players");
        }
    }

    public static PropertyInt? GetHeritageAug(HeritageGroup heritage)
    {
        switch (heritage)
        {
            case HeritageGroup.Aluvian:
            case HeritageGroup.Gharundim:
            case HeritageGroup.Sho:
            case HeritageGroup.Viamontian:
                return PropertyInt.AugmentationJackOfAllTrades;

            case HeritageGroup.Shadowbound:
            case HeritageGroup.Penumbraen:
                return PropertyInt.AugmentationCriticalExpertise;

            case HeritageGroup.Gearknight:
                return PropertyInt.AugmentationDamageReduction;

            case HeritageGroup.Undead:
                return PropertyInt.AugmentationCriticalDefense;

            case HeritageGroup.Empyrean:
                return PropertyInt.AugmentationInfusedLifeMagic;

            case HeritageGroup.Tumerok:
                return PropertyInt.AugmentationCriticalPower;

            case HeritageGroup.Lugian:
                return PropertyInt.AugmentationIncreasedCarryingCapacity;
        }
        return null;
    }
}
