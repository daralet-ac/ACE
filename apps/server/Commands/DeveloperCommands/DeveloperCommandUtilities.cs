using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ACE.Database.Models.World;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Physics.Managers;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Commands.DeveloperCommands;

public static class DeveloperCommandUtilities
{
    private static readonly ILogger _log = Log.ForContext(typeof(DeveloperCommandUtilities));

    /// <summary>
    /// Attempts to remove the hourglass / fix the busy state for the player
    /// </summary>

    public static string PostionAsLandblocksGoogleSpreadsheetFormat(Position pos)
    {
        return $"0x{pos.Cell.ToString("X")} {pos.Pos.X} {pos.Pos.Y} {pos.Pos.Z} {pos.Rotation.W} {pos.Rotation.X} {pos.Rotation.Y} {pos.Rotation.Z}";
    }

    public static void AddWeeniesToInventory(Session session, HashSet<uint> weenieIds, ushort? stackSize = null)
    {
        foreach (var weenieId in weenieIds)
        {
            var loot = WorldObjectFactory.CreateNewWorldObject(weenieId);

            if (loot == null) // weenie doesn't exist
            {
                continue;
            }

            var stackSizeForThisWeenieId = stackSize ?? loot.MaxStackSize;

            if (stackSizeForThisWeenieId > 1)
            {
                loot.SetStackSize(stackSizeForThisWeenieId);
            }


            if (loot.TrophyQuality != null)
            {
                LootGenerationFactory.MutateTrophy(loot);
            }

            session.Player.TryCreateInInventoryWithNetworking(loot);
        }
    }

    public static WorldObject GetObjectMaintTarget(Session session, params string[] parameters)
    {
        WorldObject target = session.Player;

        if (parameters.Length > 0)
        {
            var targetType = parameters[0];

            if (targetType.Equals("target", StringComparison.OrdinalIgnoreCase))
            {
                target = CommandHandlerHelper.GetLastAppraisedObject(session);
            }
            else if (
                uint.TryParse(targetType, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var targetGuid)
            )
            {
                if (ServerObjectManager.ServerObjects.TryGetValue(targetGuid, out var physicsObj))
                {
                    target = physicsObj.WeenieObj.WorldObject;
                }
            }
        }
        if (target == null)
        {
            var param = parameters.Length > 0 ? $" {parameters[0]}" : "";
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Couldn't find target{param}", ChatMessageType.Broadcast)
            );
        }
        return target;
    }

    public static void HandleTeleDungeonBlock(Session session, uint landblock)
    {
        using (var ctx = new WorldDbContext())
        {
            var query =
                from weenie in ctx.Weenie
                join wpos in ctx.WeeniePropertiesPosition on weenie.ClassId equals wpos.ObjectId
                where weenie.Type == (int)WeenieType.Portal && wpos.PositionType == (int)PositionType.Destination
                select new { Weenie = weenie, Dest = wpos };

            var results = query.ToList();

            var dest = results.Where(i => i.Dest.ObjCellId >> 16 == landblock).Select(i => i.Dest).FirstOrDefault();

            if (dest == null)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"Couldn't find dungeon {landblock:X4}", ChatMessageType.Broadcast)
                );
                return;
            }

            var pos = new Position(
                dest.ObjCellId,
                dest.OriginX,
                dest.OriginY,
                dest.OriginZ,
                dest.AnglesX,
                dest.AnglesY,
                dest.AnglesZ,
                dest.AnglesW
            );
            WorldObject.AdjustDungeon(pos);

            session.Player.Teleport(pos);
        }
    }

    public static void HandleTeleDungeonName(Session session, params string[] parameters)
    {
        var searchName = string.Join(" ", parameters);

        using (var ctx = new WorldDbContext())
        {
            var query =
                from weenie in ctx.Weenie
                join wstr in ctx.WeeniePropertiesString on weenie.ClassId equals wstr.ObjectId
                join wpos in ctx.WeeniePropertiesPosition on weenie.ClassId equals wpos.ObjectId
                where
                    weenie.Type == (int)WeenieType.Portal
                    && wstr.Type == (int)PropertyString.Name
                    && wpos.PositionType == (int)PositionType.Destination
                select new
                {
                    Weenie = weenie,
                    Name = wstr,
                    Dest = wpos
                };

            var results = query.ToList();

            var dest = results
                .Where(i => i.Name.Value.Contains(searchName, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.Dest)
                .FirstOrDefault();

            if (dest == null)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"Couldn't find dungeon name {searchName}", ChatMessageType.Broadcast)
                );
                return;
            }

            var pos = new Position(
                dest.ObjCellId,
                dest.OriginX,
                dest.OriginY,
                dest.OriginZ,
                dest.AnglesX,
                dest.AnglesY,
                dest.AnglesZ,
                dest.AnglesW
            );
            WorldObject.AdjustDungeon(pos);

            session.Player.Teleport(pos);
        }
    }

    public static List<PropertyFloat> ResistProperties = new List<PropertyFloat>()
    {
        PropertyFloat.ResistSlash,
        PropertyFloat.ResistPierce,
        PropertyFloat.ResistBludgeon,
        PropertyFloat.ResistFire,
        PropertyFloat.ResistCold,
        PropertyFloat.ResistAcid,
        PropertyFloat.ResistElectric,
        PropertyFloat.ResistNether
    };

    public static WorldObject LastTestAim;
}
