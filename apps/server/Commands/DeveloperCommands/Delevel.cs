using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class Delevel
{
    [CommandHandler(
        "delevel",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Attempts to delevel the current player. Requires enough unassigned xp and unspent skill credits.",
        "new level"
    )]
    public static void HandleDelevel(Session session, params string[] parameters)
    {
        HandleDelevel(session, false, parameters);
    }

    public static void HandleDelevel(Session session, bool confirmed, params string[] parameters)
    {
        if (!int.TryParse(parameters[0], out var delevel))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Invalid level {parameters[0]}", ChatMessageType.Broadcast)
            );
            return;
        }
        if (delevel < 1 || delevel > Player.GetMaxLevel())
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Invalid level {delevel}", ChatMessageType.Broadcast)
            );
            return;
        }
        if (delevel > session.Player.Level)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Delevel # must be less than current level {session.Player.Level}",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        // get amount of unassigned xp required
        var currentLevel = session.Player.Level.Value;
        var xpBetweenLevels = (long)session.Player.GetXPBetweenLevels(delevel, currentLevel);
        var xpIntoCurrentLevel =
            session.Player.TotalExperience - (long)DatManager.PortalDat.XpTable.CharacterLevelXPList[currentLevel];
        var unassignedXPRequired = xpBetweenLevels + xpIntoCurrentLevel;

        session.Network.EnqueueSend(
            new GameMessageSystemChat($"Unassigned XP required: {unassignedXPRequired:N0}", ChatMessageType.Broadcast)
        );

        if (session.Player.AvailableExperience < unassignedXPRequired)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You only have {session.Player.AvailableExperience:N0} unassigned XP -- delevel failed",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        // get # of available skill credits required
        var skillCreditsRequired = 0;
        for (var i = delevel + 1; i <= currentLevel; i++)
        {
            skillCreditsRequired += (int)DatManager.PortalDat.XpTable.CharacterLevelSkillCreditList[i];
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat($"Skill credits required: {skillCreditsRequired:N0}", ChatMessageType.Broadcast)
        );

        if (session.Player.AvailableSkillCredits < skillCreditsRequired)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You only have {session.Player.AvailableSkillCredits:N0} available skill credits -- delevel failed",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        if (!confirmed)
        {
            var msg = $"Are you sure you want to delevel {session.Player.Name} to level {delevel}?";
            if (
                !session.Player.ConfirmationManager.EnqueueSend(
                    new Confirmation_Custom(session.Player.Guid, () => HandleDelevel(session, true, parameters)),
                    msg
                )
            )
            {
                session.Player.SendWeenieError(WeenieError.ConfirmationInProgress);
            }

            return;
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat($"Deleveling {session.Player.Name} to level {delevel}", ChatMessageType.Broadcast)
        );

        var newAvailableExperience = session.Player.AvailableExperience - unassignedXPRequired;
        var newTotalExperience = session.Player.TotalExperience - unassignedXPRequired;

        var newAvailableSkillCredits = session.Player.AvailableSkillCredits - skillCreditsRequired;
        var newTotalSkillCredits = session.Player.TotalSkillCredits - skillCreditsRequired;

        session.Player.UpdateProperty(session.Player, PropertyInt64.AvailableExperience, newAvailableExperience);
        session.Player.UpdateProperty(session.Player, PropertyInt64.TotalExperience, newTotalExperience);

        session.Player.UpdateProperty(session.Player, PropertyInt.AvailableSkillCredits, newAvailableSkillCredits);
        session.Player.UpdateProperty(session.Player, PropertyInt.TotalSkillCredits, newTotalSkillCredits);

        session.Player.UpdateProperty(session.Player, PropertyInt.Level, delevel);

        PlayerManager.BroadcastToAuditChannel(
            session.Player,
            $"{session.Player.Name} has deleveled themselves from {currentLevel} to {session.Player.Level} - unassignedXPRequired: {unassignedXPRequired:N0} | skillCreditsRequired: {skillCreditsRequired:N0}"
        );
    }
}
