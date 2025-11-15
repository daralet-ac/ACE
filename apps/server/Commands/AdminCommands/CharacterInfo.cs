using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects.Entity;

namespace ACE.Server.Commands.AdminCommands;

public class CharacterInfo
{
    /// <summary>
    /// Debug command to print out all info about a character
    /// </summary>
    [CommandHandler(
        "characterinfo",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Displays information about a character."
    )]
    public static void HandleCharacterInfo(Session session, params string[] parameters)
    {
        var player = PlayerManager.GetOnlinePlayer(parameters[0]);

        if (player == null)
        {
            CommandHandlerHelper.WriteOutputInfo(session, "No player found with that name.", ChatMessageType.System);
            return;
        }

        var message = "";

        message += $"Character information for: {player.Name}";

        message += "\n\nÄttributes:";
        message += $"\n -Strength = {player.Strength.Base} ({player.Strength.StartingValue})";
        message += $"\n -Endurance = {player.Endurance.Base} ({player.Endurance.StartingValue})";
        message += $"\n -Coordination = {player.Coordination.Base} ({player.Coordination.StartingValue})";
        message += $"\n -Quickness = {player.Quickness.Base} ({player.Quickness.StartingValue})";
        message += $"\n -Focus = {player.Focus.Base} ({player.Focus.StartingValue})";
        message += $"\n -Self = {player.Self.Base} ({player.Self.StartingValue})";
        message += $"\n -Health = {player.Health.Base} ({player.Health.StartingValue})";
        message += $"\n -Stamina = {player.Stamina.Base} ({player.Stamina.StartingValue})";
        message += $"\n -Mana = {player.Mana.Base} ({player.Mana.StartingValue})";

        var specializedSkills = new Dictionary<Skill, CreatureSkill>();
        var trainedSkills = new Dictionary<Skill, CreatureSkill>();

        foreach (var skill in player.Skills)
        {
            if (skill.Value.AdvancementClass is SkillAdvancementClass.Specialized)
            {
                specializedSkills.Add(skill.Key, skill.Value);
            }

            if (skill.Value.AdvancementClass is SkillAdvancementClass.Trained)
            {
                trainedSkills.Add(skill.Key, skill.Value);
            }
        }

        if (specializedSkills.Count > 0)
        {
            message += "\n\nSpecialized:";

            foreach (var skill in specializedSkills)
            {
                message += $"\n -{skill.Key} = {skill.Value.Base}";
            }
        }

        if (trainedSkills.Count > 0)
        {
            message += "\n\nTrained:";

            foreach (var skill in trainedSkills)
            {
                message += $"\n -{skill.Key} = {skill.Value.Base}";
            }
        }

        CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.System);
    }
}
