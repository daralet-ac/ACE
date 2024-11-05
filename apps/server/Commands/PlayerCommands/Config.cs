using System;
using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.PlayerCommands;

public class Config
{
    private static List<string> configList = new List<string>()
    {
        "Common settings:\nConfirmVolatileRareUse, MainPackPreferred, SalvageMultiple, SideBySideVitals, UseCraftSuccessDialog",
        "Interaction settings:\nAcceptLootPermits, AllowGive, AppearOffline, AutoAcceptFellowRequest, DragItemOnPlayerOpensSecureTrade, FellowshipShareLoot, FellowshipShareXP, IgnoreAllegianceRequests, IgnoreFellowshipRequests, IgnoreTradeRequests, UseDeception",
        "UI settings:\nCoordinatesOnRadar, DisableDistanceFog, DisableHouseRestrictionEffects, DisableMostWeatherEffects, FilterLanguage, LockUI, PersistentAtDay, ShowCloak, ShowHelm, ShowTooltips, SpellDuration, TimeStamp, ToggleRun, UseMouseTurning",
        "Chat settings:\nHearAllegianceChat, HearGeneralChat, HearLFGChat, HearRoleplayChat, HearSocietyChat, HearTradeChat, HearPKDeaths, StayInChatMode",
        "Combat settings:\nAdvancedCombatUI, AutoRepeatAttack, AutoTarget, LeadMissileTargets, UseChargeAttack, UseFastMissiles, ViewCombatTarget, VividTargetingIndicator",
        "Character display settings:\nDisplayAge, DisplayAllegianceLogonNotifications, DisplayChessRank, DisplayDateOfBirth, DisplayFishingSkill, DisplayNumberCharacterTitles, DisplayNumberDeaths"
    };

    /// <summary>
    /// Mapping of GDLE -> ACE CharacterOptions
    /// </summary>
    private static Dictionary<string, string> translateOptions = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        // Common
        { "ConfirmVolatileRareUse", "ConfirmUseOfRareGems" },
        { "MainPackPreferred", "UseMainPackAsDefaultForPickingUpItems" },
        { "SalvageMultiple", "SalvageMultipleMaterialsAtOnce" },
        { "SideBySideVitals", "SideBySideVitals" },
        { "UseCraftSuccessDialog", "UseCraftingChanceOfSuccessDialog" },
        // Interaction
        { "AcceptLootPermits", "AcceptCorpseLootingPermissions" },
        { "AllowGive", "LetOtherPlayersGiveYouItems" },
        { "AppearOffline", "AppearOffline" },
        { "AutoAcceptFellowRequest", "AutomaticallyAcceptFellowshipRequests" },
        { "DragItemOnPlayerOpensSecureTrade", "DragItemToPlayerOpensTrade" },
        { "FellowshipShareLoot", "ShareFellowshipLoot" },
        { "FellowshipShareXP", "ShareFellowshipExpAndLuminance" },
        { "IgnoreAllegianceRequests", "IgnoreAllegianceRequests" },
        { "IgnoreFellowshipRequests", "IgnoreFellowshipRequests" },
        { "IgnoreTradeRequests", "IgnoreAllTradeRequests" },
        { "UseDeception", "AttemptToDeceiveOtherPlayers" },
        // UI
        { "CoordinatesOnRadar", "ShowCoordinatesByTheRadar" },
        { "DisableDistanceFog", "DisableDistanceFog" },
        { "DisableHouseRestrictionEffects", "DisableHouseRestrictionEffects" },
        { "DisableMostWeatherEffects", "DisableMostWeatherEffects" },
        { "FilterLanguage", "FilterLanguage" },
        { "LockUI", "LockUI" },
        { "PersistentAtDay", "AlwaysDaylightOutdoors" },
        { "ShowCloak", "ShowYourCloak" },
        { "ShowHelm", "ShowYourHelmOrHeadGear" },
        { "ShowTooltips", "Display3dTooltips" },
        { "SpellDuration", "DisplaySpellDurations" },
        { "TimeStamp", "DisplayTimestamps" },
        { "ToggleRun", "RunAsDefaultMovement" },
        { "UseMouseTurning", "UseMouseTurning" },
        // Chat
        { "HearAllegianceChat", "ListenToAllegianceChat" },
        { "HearGeneralChat", "ListenToGeneralChat" },
        { "HearLFGChat", "ListenToLFGChat" },
        { "HearRoleplayChat", "ListentoRoleplayChat" },
        { "HearSocietyChat", "ListenToSocietyChat" },
        { "HearTradeChat", "ListenToTradeChat" },
        { "HearPKDeaths", "ListenToPKDeathMessages" },
        { "StayInChatMode", "StayInChatModeAfterSendingMessage" },
        // Combat
        { "AdvancedCombatUI", "AdvancedCombatInterface" },
        { "AutoRepeatAttack", "AutoRepeatAttacks" },
        { "AutoTarget", "AutoTarget" },
        { "LeadMissileTargets", "LeadMissileTargets" },
        { "UseChargeAttack", "UseChargeAttack" },
        { "UseFastMissiles", "UseFastMissiles" },
        { "ViewCombatTarget", "KeepCombatTargetsInView" },
        { "VividTargetingIndicator", "VividTargetingIndicator" },
        // Character Display
        { "DisplayAge", "AllowOthersToSeeYourAge" },
        { "DisplayAllegianceLogonNotifications", "ShowAllegianceLogons" },
        { "DisplayChessRank", "AllowOthersToSeeYourChessRank" },
        { "DisplayDateOfBirth", "AllowOthersToSeeYourDateOfBirth" },
        { "DisplayFishingSkill", "AllowOthersToSeeYourFishingSkill" },
        { "DisplayNumberCharacterTitles", "AllowOthersToSeeYourNumberOfTitles" },
        { "DisplayNumberDeaths", "AllowOthersToSeeYourNumberOfDeaths" },
    };

    /// <summary>
    /// Manually sets a character option on the server. Use /config list to see a list of settings.
    /// </summary>
    [CommandHandler(
        "config",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Manually sets a character option on the server.\nUse /config list to see a list of settings.",
        "<setting> <on/off>"
    )]
    public static void HandleConfig(Session session, params string[] parameters)
    {
        if (!PropertyManager.GetBool("player_config_command").Item)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "The command \"config\" is not currently enabled on this server.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        // /config list - show character options
        if (parameters[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var line in configList)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat(line, ChatMessageType.Broadcast));
            }

            return;
        }

        // translate GDLE CharacterOptions for existing plugins
        if (
            !translateOptions.TryGetValue(parameters[0], out var param)
            || !Enum.TryParse(param, out CharacterOption characterOption)
        )
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Unknown character option: {parameters[0]}", ChatMessageType.Broadcast)
            );
            return;
        }

        var option = session.Player.GetCharacterOption(characterOption);

        // modes of operation:
        // on / off / toggle

        // - if none specified, default to toggle
        var mode = "toggle";

        if (parameters.Length > 1)
        {
            if (parameters[1].Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                mode = "on";
            }
            else if (parameters[1].Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                mode = "off";
            }
        }

        // set character option
        if (mode.Equals("on"))
        {
            option = true;
        }
        else if (mode.Equals("off"))
        {
            option = false;
        }
        else
        {
            option = !option;
        }

        session.Player.SetCharacterOption(characterOption, option);

        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Character option {parameters[0]} is now {(option ? "on" : "off")}.",
                ChatMessageType.Broadcast
            )
        );

        // update client
        session.Network.EnqueueSend(new GameEventPlayerDescription(session));
    }
}
