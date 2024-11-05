using System;
using System.Linq;
using System.Text;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyPlayerAttributes
{
    [CommandHandler(
        "verify-attributes",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any bugs with player attribute data"
    )]
    public static void HandleVerifyAttributes(Session session, params string[] parameters)
    {
        var players = PlayerManager.GetAllOffline();

        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;
        var resetFreeAttributeRedistributionTimer = false;

        foreach (var player in players)
        {
            var updated = false;

            foreach (
                var attr in new System.Collections.Generic.Dictionary<PropertyAttribute, PropertiesAttribute>(player.Biota.PropertiesAttribute)
            )
            {
                // ensure this is a valid attribute
                if (attr.Key < PropertyAttribute.Strength || attr.Key > PropertyAttribute.Self)
                {
                    Console.WriteLine($"{player.Name} has unknown attribute {attr.Key}{fixStr}");
                    foundIssues = true;

                    if (fix)
                    {
                        // i have found no instances of this situation being run into,
                        // but if it does happen, verify-xp will refund the player xp properly

                        player.Biota.PropertiesAttribute.Remove(attr);
                        updated = true;
                    }
                    continue;
                }

                var rank = attr.Value.LevelFromCP;

                // verify attribute rank
                var correctRank = Player.CalcAttributeRank(attr.Value.CPSpent);
                if (rank != correctRank)
                {
                    Console.WriteLine($"{player.Name}'s {attr.Key} rank is {rank}, should be {correctRank}{fixStr}");
                    foundIssues = true;

                    if (fix)
                    {
                        attr.Value.LevelFromCP = (ushort)correctRank;
                        updated = true;
                    }
                }

                // verify attribute xp is within bounds
                var attributeXPTable = DatManager.PortalDat.XpTable.AttributeXpList;
                var maxAttributeXp = attributeXPTable[attributeXPTable.Count - 1];

                if (attr.Value.CPSpent > maxAttributeXp)
                {
                    Console.WriteLine(
                        $"{player.Name}'s {attr.Key} attribute total xp is {attr.Value.CPSpent:N0}, should be capped at {maxAttributeXp:N0}{fixStr}"
                    );
                    foundIssues = true;

                    if (fix)
                    {
                        // again i have found no instances of this situation being run into,
                        // but if it does happen, verify-xp will refund the player xp properly

                        attr.Value.CPSpent = maxAttributeXp;
                        updated = true;
                    }
                }

                // Verify that an attribute has not been augmented above 100
                // only do this if server operators have opted into this functionality
                if (
                    attr.Value.InitLevel > 100
                    && attr.Value.InitLevel <= 104
                    && player.Account.AccessLevel == (uint)AccessLevel.Player
                    && PropertyManager.GetBool("attribute_augmentation_safety_cap").Item
                )
                {
                    var augmentationExploitMessageBuilder = new StringBuilder();
                    foundIssues = true;
                    augmentationExploitMessageBuilder.AppendFormat(
                        "{0}'s {1} is currently {2}, augmented above 100.{3}",
                        player.Name,
                        attr.Key,
                        attr.Value.InitLevel,
                        Environment.NewLine
                    );

                    // only search strength, endurance, coordination, quicknesss, focus, and self
                    var validAttributes = player.Biota.PropertiesAttribute.Where(attr =>
                        attr.Key >= PropertyAttribute.Strength && attr.Key <= PropertyAttribute.Self
                    );
                    // find the lowest value of an attribute to distribute points to
                    var lowestInitAttributeLevel = validAttributes.Min(x => x.Value.InitLevel);

                    // find the lowest attribute to distribute the extra points to so they're not lost
                    var targetAttribute = validAttributes.FirstOrDefault(x =>
                        x.Value.InitLevel == lowestInitAttributeLevel
                    );
                    augmentationExploitMessageBuilder.AppendLine(
                        "5 points will be redistributed to lowest eligible innate attribute to fix this issue."
                    );

                    Console.WriteLine(augmentationExploitMessageBuilder.ToString());
                    if (lowestInitAttributeLevel < 96 && fix)
                    {
                        attr.Value.InitLevel -= 5;
                        targetAttribute.Value.InitLevel += 5;
                        updated = true;
                        resetFreeAttributeRedistributionTimer = true;
                    }
                }
            }
            if (fix && updated)
            {
                // if we've redistributed augmented attribute points, give people the opportunity
                // to redistribute them legitimately as they please
                if (resetFreeAttributeRedistributionTimer)
                {
                    player.SetProperty(PropertyBool.FreeAttributeResetRenewed, true);
                }
                player.SaveBiotaToDatabase();
            }
        }

        if (!fix && foundIssues)
        {
            Console.WriteLine("Dry run completed. Type 'verify-attributes fix' to fix any issues.");
        }

        if (!foundIssues)
        {
            Console.WriteLine($"Verified attributes for {players.Count:N0} players");
        }
    }
}
