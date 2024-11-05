using System;
using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyPlayerMaxAugs
{
    [CommandHandler(
        "verify-max-augs",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any bugs with the # of augs each player has"
    )]
    public static void HandleVerifyMaxAugs(Session session, params string[] parameters)
    {
        var players = PlayerManager.GetAllOffline();

        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var fixStr = fix ? " -- fixed" : "";
        var foundIssues = false;

        foreach (var player in players)
        {
            foreach (var kvp in AugmentationDevice.AugProps)
            {
                var type = kvp.Key;
                var prop = kvp.Value;

                var max = AugmentationDevice.MaxAugs[type];

                var augProp = player.GetProperty(prop) ?? 0;

                if (augProp >= 0 && augProp <= max)
                {
                    continue;
                }

                var msg = $"{player.Name} has {augProp} {prop}";

                if (augProp < 0)
                {
                    msg += $", min should be 0{fixStr}";
                }
                else
                {
                    msg += $", max should be {max}{fixStr}";
                }

                Console.WriteLine(msg);

                foundIssues = true;

                if (fix)
                {
                    if (augProp < 0)
                    {
                        player.SetProperty(prop, 0);
                    }
                    else
                    {
                        player.SetProperty(prop, max);
                    }

                    player.SaveBiotaToDatabase();
                }
            }
        }
        if (!fix && foundIssues)
        {
            Console.WriteLine($"Dry run completed. Type 'verify-max-augs fix' to fix any issues.");
        }

        if (!foundIssues)
        {
            Console.WriteLine($"Verified max augs for {players.Count:N0} players");
        }
    }

    public static Dictionary<PropertyInt, string> AugmentationDevices = new Dictionary<PropertyInt, string>()
    {
        { PropertyInt.AugmentationBonusImbueChance, "gemaugmentationluckonimbues" },
        { PropertyInt.AugmentationBonusSalvage, "gemaugmentationbonussalvage" },
        { PropertyInt.AugmentationBonusXp, "gemaugmentationbonusxp" },
        { PropertyInt.AugmentationCriticalDefense, "gemaugmentationcriticaldefense" },
        { PropertyInt.AugmentationCriticalExpertise, "ace41482-eyeoftheremorseless" },
        { PropertyInt.AugmentationCriticalPower, "ace41481-handoftheremorseless" },
        { PropertyInt.AugmentationDamageBonus, "ace41478-frenzyoftheslayer" },
        { PropertyInt.AugmentationDamageReduction, "ace41480-ironskinoftheinvincible" },
        { PropertyInt.AugmentationExtraPackSlot, "gemaugmentationpackslot" },
        { PropertyInt.AugmentationFasterRegen, "gemaugmentationfastregen" },
        { PropertyInt.AugmentationIncreasedCarryingCapacity, "gemaugmentationcarryingcapacityi" },
        { PropertyInt.AugmentationIncreasedSpellDuration, "gemaugmentationspellduration" },
        { PropertyInt.AugmentationInfusedCreatureMagic, "ace41472-infusedcreaturemagic" },
        { PropertyInt.AugmentationInfusedItemMagic, "ace41473-infuseditemmagic" },
        { PropertyInt.AugmentationInfusedLifeMagic, "ace41474-infusedlifemagic" },
        { PropertyInt.AugmentationInfusedVoidMagic, "ace41479-infusedvoidmagic" },
        { PropertyInt.AugmentationInfusedWarMagic, "ace41475-infusedwarmagic" },
        { PropertyInt.AugmentationInnateCoordination, "gemaugmentationattcoordination" },
        { PropertyInt.AugmentationInnateEndurance, "gemaugmentationattendurance" },
        { PropertyInt.AugmentationInnateFocus, "gemaugmentationattfocus" },
        { PropertyInt.AugmentationInnateQuickness, "gemaugmentationattquickness" },
        { PropertyInt.AugmentationInnateSelf, "gemaugmentationattself" },
        { PropertyInt.AugmentationInnateStrength, "gemaugmentationattstrength" },
        { PropertyInt.AugmentationJackOfAllTrades, "ace43167-jackofalltrades" },
        { PropertyInt.AugmentationLessDeathItemLoss, "gemaugmentationdeathreduceditems" },
        { PropertyInt.AugmentationResistanceAcid, "gemaugmentationnaturalresistanceacid" },
        { PropertyInt.AugmentationResistanceBlunt, "gemaugmentationnaturalresistancebludg" },
        { PropertyInt.AugmentationResistanceFire, "gemaugmentationnaturalresistancefire" },
        { PropertyInt.AugmentationResistanceFrost, "gemaugmentationnaturalresistancefrost" },
        { PropertyInt.AugmentationResistanceLightning, "gemaugmentationnaturalresistanceelectric" },
        { PropertyInt.AugmentationResistancePierce, "gemaugmentationnaturalresistancepierc" },
        { PropertyInt.AugmentationResistanceSlash, "gemaugmentationnaturalresistanceslash" },
        { PropertyInt.AugmentationSkilledMagic, "ace41476-masterofthefivefoldpath" },
        { PropertyInt.AugmentationSkilledMelee, "ace41477-masterofthesteelcircle" },
        { PropertyInt.AugmentationSkilledMissile, "ace41490-masterofthefocusedeye" },
        { PropertyInt.AugmentationSpecializeArmorTinkering, "gemaugmentationtinkeringspecarmor" },
        { PropertyInt.AugmentationSpecializeItemTinkering, "gemaugmentationtinkeringspecitem" },
        { PropertyInt.AugmentationSpecializeMagicItemTinkering, "gemaugmentationtinkeringspecmagic" },
        { PropertyInt.AugmentationSpecializeSalvaging, "gemaugmentationtinkeringspecsalv" },
        { PropertyInt.AugmentationSpecializeWeaponTinkering, "gemaugmentationtinkeringspecweap" },
        { PropertyInt.AugmentationSpellsRemainPastDeath, "gemaugmentationdeathspellsremain" },
    };
}
