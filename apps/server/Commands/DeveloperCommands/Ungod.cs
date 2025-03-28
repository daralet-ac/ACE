﻿using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class Ungod
{
    // ungod
    [CommandHandler(
        "ungod",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Returns character to a mortal state.",
        "Sets attributes and skills back to the values they were when you became a god.\n"
            + "If the command fails to revert your state it will default to godmode.\n"
            + "In the event of failure, contact a server administrator to sort it out."
    )]
    public static void HandleUngod(Session session, params string[] parameters)
    {
        // @ungod - Returns skills and attributues to pre-god levels.
        var currentPlayer = session.Player;
        var returnString = session.Player.GodState;

        if (returnString == null)
        {
            ChatPacket.SendServerMessage(
                session,
                "Can't get any more ungodly than you already are...",
                ChatMessageType.Broadcast
            );
            return;
        }
        else
        {
            try
            {
                var returnStringArr = returnString.Split("=");

                var correctLength = 205;

                // correctly formatted return string should have 240 entries
                // if the construction of the string changes - this will need to be updated to match
                if (returnStringArr.Length != correctLength)
                {
                    Console.WriteLine(
                        $"The returnString ({returnStringArr.Length} vs {correctLength}) was not set to the correct length while {currentPlayer.Name} was attempting to return to normal from godmode."
                    );
                    ChatPacket.SendServerMessage(
                        session,
                        "Error returning to mortal state, defaulting to godmode.",
                        ChatMessageType.Broadcast
                    );
                    return;
                }

                for (var i = 2; i < returnStringArr.Length; )
                {
                    switch (i)
                    {
                        case int n when (n <= 5):
                            currentPlayer.SetProperty(
                                (PropertyInt)uint.Parse(returnStringArr[i]),
                                int.Parse(returnStringArr[i + 1])
                            );
                            i += 2;
                            break;
                        case int n when (n <= 9):
                            currentPlayer.SetProperty(
                                (PropertyInt64)uint.Parse(returnStringArr[i]),
                                long.Parse(returnStringArr[i + 1])
                            );
                            i += 2;
                            break;
                        case int n when (n <= 33):
                            var playerAttr = currentPlayer.Attributes[
                                (PropertyAttribute)uint.Parse(returnStringArr[i])
                            ];
                            playerAttr.StartingValue = uint.Parse(returnStringArr[i + 1]);
                            playerAttr.Ranks = uint.Parse(returnStringArr[i + 2]);
                            playerAttr.ExperienceSpent = uint.Parse(returnStringArr[i + 3]);
                            currentPlayer.Session.Network.EnqueueSend(
                                new GameMessagePrivateUpdateAttribute(currentPlayer, playerAttr)
                            );
                            i += 4;
                            break;
                        case int n when (n <= 48):
                            var playerVital = currentPlayer.Vitals[(PropertyAttribute2nd)int.Parse(returnStringArr[i])];
                            playerVital.StartingValue = uint.Parse(returnStringArr[i + 1]);
                            playerVital.Ranks = uint.Parse(returnStringArr[i + 2]);
                            playerVital.ExperienceSpent = uint.Parse(returnStringArr[i + 3]);
                            playerVital.Current = uint.Parse(returnStringArr[i + 4]);
                            currentPlayer.Session.Network.EnqueueSend(
                                new GameMessagePrivateUpdateVital(currentPlayer, playerVital)
                            );
                            i += 5;
                            break;
                        case int n when (n <= correctLength - 2):
                            var playerSkill = currentPlayer.Skills[(Skill)int.Parse(returnStringArr[i])];
                            playerSkill.Ranks = ushort.Parse(returnStringArr[i + 1]);

                            // Handle god users stuck in god mode due to bad godstate with Enum string
                            SkillAdvancementClass advancement;
                            if (Enum.TryParse(returnStringArr[i + 2], out advancement))
                            {
                                playerSkill.AdvancementClass = advancement;
                            }
                            else
                            {
                                playerSkill.AdvancementClass = (SkillAdvancementClass)
                                    uint.Parse(returnStringArr[i + 2]);
                            }

                            playerSkill.ExperienceSpent = uint.Parse(returnStringArr[i + 3]);
                            playerSkill.InitLevel = uint.Parse(returnStringArr[i + 4]);
                            currentPlayer.Session.Network.EnqueueSend(
                                new GameMessagePrivateUpdateSkill(currentPlayer, playerSkill)
                            );
                            i += 5;
                            break;
                        case int n when (n == correctLength - 1): //end of returnString, this will need to be updated if the length of the string changes
                            var levelMsg = new GameMessagePrivateUpdatePropertyInt(
                                currentPlayer,
                                PropertyInt.Level,
                                (int)currentPlayer.Level
                            );
                            var skMsg = new GameMessagePrivateUpdatePropertyInt(
                                currentPlayer,
                                PropertyInt.AvailableSkillCredits,
                                (int)currentPlayer.AvailableSkillCredits
                            );
                            var totalExpMsg = new GameMessagePrivateUpdatePropertyInt64(
                                currentPlayer,
                                PropertyInt64.TotalExperience,
                                (long)currentPlayer.TotalExperience
                            );
                            var unassignedExpMsg = new GameMessagePrivateUpdatePropertyInt64(
                                currentPlayer,
                                PropertyInt64.AvailableExperience,
                                (long)currentPlayer.AvailableExperience
                            );
                            currentPlayer.Session.Network.EnqueueSend(levelMsg, skMsg, totalExpMsg, unassignedExpMsg);
                            i++;
                            break;
                        default:
                            // A warning that will alert on the console if the returnString length changes. This should suffice until a smoother way can be found.
                            Console.WriteLine(
                                $"Hit default case in /ungod command with i = {i}, did you change the length of the PropertyString.GodState array?"
                            );
                            i++;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Exception ( {e.Source} - {e.Message} ) caught while {currentPlayer.Name} was attempting to return to normal from godmode."
                );
                ChatPacket.SendServerMessage(
                    session,
                    "Error returning to mortal state, defaulting to godmode.",
                    ChatMessageType.Broadcast
                );
                DoGodMode(true, session, true);
                return;
            }

            currentPlayer.SetMaxVitals();

            currentPlayer.RemoveProperty(PropertyString.GodState);

            currentPlayer.SaveBiotaToDatabase();

            currentPlayer.PlayParticleEffect(ACE.Entity.Enum.PlayScript.DispelAll, currentPlayer.Guid);

            ChatPacket.SendServerMessage(
                session,
                "You have returned from your godly state.",
                ChatMessageType.Broadcast
            );
        }
    }

    private static void DoGodMode(bool playerSaved, Session session, bool exceptionReturn = false)
    {
        if (!playerSaved)
        {
            ChatPacket.SendServerMessage(
                session,
                "Error saving player. Godmode not available.",
                ChatMessageType.Broadcast
            );
            Console.WriteLine(
                $"Player {session.Player.Name} tried to enter god mode but there was an error saving player. Godmode not available."
            );
            return;
        }

        var biota = session.Player.Biota;

        var godString = session.Player.GodState;

        if (!exceptionReturn)
        {
            // if godstate starts with 1, you are in godmode

            if (godString != null)
            {
                if (godString.StartsWith("1"))
                {
                    ChatPacket.SendServerMessage(session, "You are already a god.", ChatMessageType.Broadcast);
                    return;
                }
            }

            var returnState = "1=";
            returnState += $"{DateTime.UtcNow}=";

            // need level 25, available skill credits 24
            returnState += $"24={session.Player.AvailableSkillCredits}=25={session.Player.Level}=";

            // need total xp 1, unassigned xp 2
            returnState += $"1={session.Player.TotalExperience}=2={session.Player.AvailableExperience}=";

            // need all attributes
            // 1 through 6 str, end, coord, quick, focus, self
            foreach (var kvp in biota.PropertiesAttribute)
            {
                var att = kvp.Value;

                if (kvp.Key > 0 && (int)kvp.Key <= 6)
                {
                    returnState += $"{(int)kvp.Key}=";
                    returnState += $"{att.InitLevel}=";
                    returnState += $"{att.LevelFromCP}=";
                    returnState += $"{att.CPSpent}=";
                }
            }

            // need all vitals
            // 1, 3, 5 H,S,M (2,4,6 are current values and are not stored since they will be maxed entering/exiting godmode)
            foreach (var kvp in biota.PropertiesAttribute2nd)
            {
                var attSec = kvp.Value;

                if ((int)kvp.Key == 1 || (int)kvp.Key == 3 || (int)kvp.Key == 5)
                {
                    returnState += $"{(int)kvp.Key}=";
                    returnState += $"{attSec.InitLevel}=";
                    returnState += $"{attSec.LevelFromCP}=";
                    returnState += $"{attSec.CPSpent}=";
                    returnState += $"{attSec.CurrentLevel}=";
                }
            }

            // need all skills
            foreach (var kvp in biota.PropertiesSkill)
            {
                var sk = kvp.Value;

                if (SkillHelper.ValidSkills.Contains(kvp.Key))
                {
                    returnState += $"{(int)kvp.Key}=";
                    returnState += $"{sk.LevelFromPP}=";
                    returnState += $"{(uint)sk.SAC}=";
                    returnState += $"{sk.PP}=";
                    returnState += $"{sk.InitLevel}=";
                }
            }

            var correctLength = 205;

            // Check string is correctly formatted before altering stats
            // correctly formatted return string should have 240 entries
            // if the construction of the string changes - this will need to be updated to match
            if (returnState.Split("=").Length != correctLength)
            {
                ChatPacket.SendServerMessage(
                    session,
                    "Godmode is not available at this time.",
                    ChatMessageType.Broadcast
                );
                Console.WriteLine(
                    $"Player {session.Player.Name} tried to enter god mode but there was an error with the godString length. (length = {returnState.Split("=").Length}) Godmode not available."
                );
                return;
            }

            // save return state to db in property string
            session.Player.SetProperty(PropertyString.GodState, returnState);
            session.Player.SaveBiotaToDatabase();
        }

        // Begin Godly Stats Increase

        var currentPlayer = session.Player;
        currentPlayer.Level = 999;
        currentPlayer.AvailableExperience = 0;
        currentPlayer.AvailableSkillCredits = 0;
        currentPlayer.TotalExperience = 191226310247;

        var levelMsg = new GameMessagePrivateUpdatePropertyInt(
            currentPlayer,
            PropertyInt.Level,
            (int)currentPlayer.Level
        );
        var expMsg = new GameMessagePrivateUpdatePropertyInt64(
            currentPlayer,
            PropertyInt64.AvailableExperience,
            (long)currentPlayer.AvailableExperience
        );
        var skMsg = new GameMessagePrivateUpdatePropertyInt(
            currentPlayer,
            PropertyInt.AvailableSkillCredits,
            (int)currentPlayer.AvailableSkillCredits
        );
        var totalExpMsg = new GameMessagePrivateUpdatePropertyInt64(
            currentPlayer,
            PropertyInt64.TotalExperience,
            (long)currentPlayer.TotalExperience
        );

        currentPlayer.Session.Network.EnqueueSend(levelMsg, expMsg, skMsg, totalExpMsg);

        foreach (var s in currentPlayer.Skills)
        {
            currentPlayer.TrainSkill(s.Key, 0);
            currentPlayer.SpecializeSkill(s.Key, 0);
            var playerSkill = currentPlayer.Skills[s.Key];
            playerSkill.Ranks = 226;
            playerSkill.ExperienceSpent = 4100490438u;
            playerSkill.InitLevel = 5000;
            currentPlayer.Session.Network.EnqueueSend(new GameMessagePrivateUpdateSkill(currentPlayer, playerSkill));
        }

        foreach (var a in currentPlayer.Attributes)
        {
            var playerAttr = currentPlayer.Attributes[a.Key];
            playerAttr.StartingValue = 9809u;
            playerAttr.Ranks = 190u;
            playerAttr.ExperienceSpent = 4019438644u;
            currentPlayer.Session.Network.EnqueueSend(new GameMessagePrivateUpdateAttribute(currentPlayer, playerAttr));
        }

        currentPlayer.SetMaxVitals();

        foreach (var v in currentPlayer.Vitals)
        {
            var playerVital = currentPlayer.Vitals[v.Key];
            playerVital.Ranks = 196u;
            playerVital.ExperienceSpent = 4285430197u;
            // my OCD will not let health/stam not be equal due to the endurance calc
            playerVital.StartingValue = (v.Key == PropertyAttribute2nd.MaxHealth) ? 94803u : 89804u;
            currentPlayer.Session.Network.EnqueueSend(new GameMessagePrivateUpdateVital(currentPlayer, playerVital));
        }

        currentPlayer.PlayParticleEffect(ACE.Entity.Enum.PlayScript.LevelUp, currentPlayer.Guid);
        currentPlayer.PlayParticleEffect(ACE.Entity.Enum.PlayScript.BaelZharonSmite, currentPlayer.Guid);

        currentPlayer.SetMaxVitals();

        ChatPacket.SendServerMessage(session, "You are now a god!!!", ChatMessageType.Broadcast);
    }
}
