using System;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.SentinelCommands;

public class God
{
    // god
    [CommandHandler(
        "god",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Turns current character into a god!",
        "Sets attributes and skills to higher than max levels.\n"
            + "To return to a mortal state, use the /ungod command.\n"
            + "Use the /god command with caution. While unlikely, there is a possibility that the character that runs the command will not be able to return to normal or will end up in a state that is unrecoverable."
    )]
    public static void HandleGod(Session session, params string[] parameters)
    {
        // @god - Sets your own stats to a godly level.
        // need to save stats so that we can return with /ungod
        DatabaseManager.Shard.SaveBiota(
            session.Player.Biota,
            session.Player.BiotaDatabaseLock,
            result => DoGodMode(result, session)
        );
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

        currentPlayer.PlayParticleEffect(PlayScript.LevelUp, currentPlayer.Guid);
        currentPlayer.PlayParticleEffect(PlayScript.BaelZharonSmite, currentPlayer.Guid);

        currentPlayer.SetMaxVitals();

        ChatPacket.SendServerMessage(session, "You are now a god!!!", ChatMessageType.Broadcast);
    }
}
