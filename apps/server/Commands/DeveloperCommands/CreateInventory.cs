using System;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class CreateInventory
{
    /// <summary>
    /// Creates an object in your inventory
    /// </summary>
    [CommandHandler(
        "ci",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Creates an object in your inventory.",
        "wclassid (string or number), Amount to Spawn (optional [default:1]), Palette (optional), Shade (optional)\n"
    )]
    public static void HandleCI(Session session, params string[] parameters)
    {
        var weenie = GetWeenieForCreate(session, parameters[0], true);

        if (weenie == null)
        {
            return;
        }

        ushort stackSize = 0;
        int? palette = null;
        float? shade = null;

        if (parameters.Length > 1)
        {
            if (!ushort.TryParse(parameters[1], out stackSize) || stackSize == 0)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"stacksize must be number between 1 - {ushort.MaxValue}",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }
        }

        if (parameters.Length > 2)
        {
            if (!int.TryParse(parameters[2], out var _palette))
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"palette must be number between {int.MinValue} - {int.MaxValue}",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }
            else
            {
                palette = _palette;
            }
        }

        if (parameters.Length > 3)
        {
            if (!float.TryParse(parameters[3], out var _shade))
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"shade must be number between {float.MinValue} - {float.MaxValue}",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }
            else
            {
                shade = _shade;
            }
        }

        var obj = CreateObjectForCommand(session, weenie);

        if (obj == null)
        {
            // already sent an error message
            return;
        }

        if (stackSize != 0 && obj.MaxStackSize != null)
        {
            stackSize = Math.Min(stackSize, (ushort)obj.MaxStackSize);

            obj.SetStackSize(stackSize);
        }

        if (palette != null)
        {
            obj.PaletteTemplate = palette;
        }

        if (shade != null)
        {
            obj.Shade = shade;
        }

        session.Player.TryCreateInInventoryWithNetworking(obj);

        PlayerManager.BroadcastToAuditChannel(
            session.Player,
            $"{session.Player.Name} has created {obj.Name} (0x{obj.Guid:X8}) in their inventory."
        );
    }

    /// <summary>
    /// Returns a weenie for a wcid or classname for /create, /createliveops, and /ci
    /// Performs some basic verifications for weenie types that are safe to spawn with these commands
    /// </summary>
    public static Weenie GetWeenieForCreate(Session session, string weenieDesc, bool forInventory = false)
    {
        Weenie weenie = null;

        if (uint.TryParse(weenieDesc, out var wcid))
        {
            weenie = DatabaseManager.World.GetCachedWeenie(wcid);
        }
        else
        {
            weenie = DatabaseManager.World.GetCachedWeenie(weenieDesc);
        }

        if (weenie == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"{weenieDesc} is not a valid weenie.", ChatMessageType.Broadcast)
            );
            return null;
        }

        if (!VerifyCreateWeenieType(weenie.WeenieType))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You cannot spawn {weenie.ClassName} because it is a {weenie.WeenieType}",
                    ChatMessageType.Broadcast
                )
            );
            return null;
        }

        if (forInventory && weenie.IsStuck())
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You cannot spawn {weenie.ClassName} in your inventory because it cannot be picked up",
                    ChatMessageType.Broadcast
                )
            );
            return null;
        }

        return weenie;
    }

    public static bool VerifyCreateWeenieType(WeenieType weenieType)
    {
        switch (weenieType)
        {
            case WeenieType.Admin:
            case WeenieType.AI:
            case WeenieType.Allegiance:
            case WeenieType.BootSpot:
            case WeenieType.Channel:
            case WeenieType.CombatPet:
            case WeenieType.Deed:
            case WeenieType.Entity:
            case WeenieType.EventCoordinator:
            case WeenieType.Game:
            case WeenieType.GamePiece:
            case WeenieType.GScoreGatherer:
            case WeenieType.GScoreKeeper:
            case WeenieType.GSpellEconomy:
            case WeenieType.Hook:
            case WeenieType.House:
            case WeenieType.HousePortal:
            case WeenieType.HUD:
            case WeenieType.InGameStatKeeper:
            case WeenieType.LScoreKeeper:
            case WeenieType.LSpellEconomy:
            case WeenieType.Machine:
            case WeenieType.Pet:
            case WeenieType.ProjectileSpell:
            case WeenieType.Sentinel:
            case WeenieType.SlumLord:
            case WeenieType.SocialManager:
            case WeenieType.Storage:
            case WeenieType.Undef:
            case WeenieType.UNKNOWN__GUESSEDNAME32:

                return false;
        }
        return true;
    }

    public static Position LastSpawnPos;

    /// <summary>
    /// Creates WorldObjects from Weenies for /create, /createliveops, and /ci
    /// </summary>
    private static WorldObject CreateObjectForCommand(Session session, Weenie weenie)
    {
        var obj = WorldObjectFactory.CreateNewWorldObject(weenie);

        //if (obj.TimeToRot == null)
        //obj.TimeToRot = double.MaxValue;

        if (obj.WeenieType == WeenieType.Creature)
        {
            obj.Location = session.Player.Location.InFrontOf(5f, true);
        }
        else
        {
            var dist = Math.Max(2, obj.UseRadius ?? 2);

            obj.Location = session.Player.Location.InFrontOf(dist);
        }

        obj.Location.LandblockId = new LandblockId(obj.Location.GetCell());

        LastSpawnPos = obj.Location;

        return obj;
    }
}
