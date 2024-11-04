using System;
using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class CreateInventoryAetheria
{
    [CommandHandler(
        "ciaetheria",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        4,
        "Spawns an Aetheria in the player's inventory",
        "[color] [set] [surge] [level]"
            + "\nColor: Blue, Yellow, Red"
            + "\nSet: Defense, Destruction, Fury, Growth, Vigor"
            + "\nSurge: Destruction, Protection, Regeneration, Affliction, Festering"
            + "\nLevel: 1 - 5"
    )]
    public static void HandleCIAetheria(Session session, params string[] parameters)
    {
        if (!Enum.TryParse(parameters[0], true, out AetheriaColor color))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Invalid color: {parameters[0]}", ChatMessageType.Broadcast);
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Available colors: Blue, Yellow, Red",
                ChatMessageType.Broadcast
            );
            return;
        }

        if (!Enum.TryParse(parameters[1], true, out Sigil set))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Invalid set: {parameters[1]}", ChatMessageType.Broadcast);
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Available sets: Defense, Destruction, Fury, Growth, Vigor",
                ChatMessageType.Broadcast
            );
            return;
        }

        if (!Enum.TryParse(parameters[2], true, out Surge surgeSpell))
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Invalid surge spell: {parameters[2]}",
                ChatMessageType.Broadcast
            );
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Available surge spells: Destruction, Protection, Regeneration, Affliction, Festering",
                ChatMessageType.Broadcast
            );
            return;
        }

        if (!int.TryParse(parameters[3], out var maxLevel) || maxLevel < 1 || maxLevel > 5)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Invalid level: {parameters[3]}", ChatMessageType.Broadcast);
            CommandHandlerHelper.WriteOutputInfo(session, $"Available levels: 1 - 5", ChatMessageType.Broadcast);
            return;
        }

        var wcid = AetheriaWcids[color];

        var wo = WorldObjectFactory.CreateNewWorldObject(wcid) as Gem;

        if (wo == null)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Failed to create Aetheria wcid", ChatMessageType.Broadcast);
            return;
        }

        wo.ItemMaxLevel = maxLevel;
        wo.IconOverlayId = LootGenerationFactory.IconOverlay_ItemMaxLevel[wo.ItemMaxLevel.Value - 1];

        wo.EquipmentSetId = Aetheria.SigilToEquipmentSet[set];

        wo.IconId = Aetheria.Icons[color][set];

        var procSpell = SurgeSpells[surgeSpell];

        wo.ProcSpell = (uint)procSpell;

        if (Aetheria.SurgeTargetSelf[procSpell])
        {
            wo.ProcSpellSelfTargeted = true;
        }

        wo.ValidLocations = Aetheria.ColorToMask[color];

        wo.ItemTotalXp = (long)
            ExperienceSystem.ItemLevelToTotalXP(
                wo.ItemMaxLevel.Value,
                (ulong)wo.ItemBaseXp,
                wo.ItemMaxLevel.Value,
                wo.ItemXpStyle.Value
            );

        if (!session.Player.TryCreateInInventoryWithNetworking(wo))
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Failed to add Aetheria item to player inventory",
                ChatMessageType.Broadcast
            );
        }
    }

    private static readonly Dictionary<AetheriaColor, uint> AetheriaWcids = new Dictionary<AetheriaColor, uint>()
    {
        { AetheriaColor.Blue, Aetheria.AetheriaBlue },
        { AetheriaColor.Yellow, Aetheria.AetheriaYellow },
        { AetheriaColor.Red, Aetheria.AetheriaRed }
    };

    private static readonly Dictionary<Surge, SpellId> SurgeSpells = new Dictionary<Surge, SpellId>()
    {
        { Surge.Destruction, SpellId.AetheriaProcDamageBoost },
        { Surge.Protection, SpellId.AetheriaProcDamageReduction },
        { Surge.Regeneration, SpellId.AetheriaProcHealthOverTime },
        { Surge.Affliction, SpellId.AetheriaProcDamageOverTime },
        { Surge.Festering, SpellId.AetheriaProcHealDebuff },
    };
}
