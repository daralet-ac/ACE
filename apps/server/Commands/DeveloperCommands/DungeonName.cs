using System.Linq;
using ACE.Common.Extensions;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class DungeonName
{
    /// <summary>
    /// Shows the dungeon name for the current landblock
    /// </summary>
    [CommandHandler(
        "dungeonname",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows the dungeon name for the current landblock"
    )]
    public static void HandleDungeonName(Session session, params string[] parameters)
    {
        var landblock = session.Player.Location.Landblock;

        var blockStart = landblock << 16;
        var blockEnd = blockStart | 0xFFFF;

        using (var ctx = new WorldDbContext())
        {
            var query =
                from weenie in ctx.Weenie
                join wstr in ctx.WeeniePropertiesString on weenie.ClassId equals wstr.ObjectId
                join wpos in ctx.WeeniePropertiesPosition on weenie.ClassId equals wpos.ObjectId
                where
                    weenie.Type == (int)WeenieType.Portal
                    && wpos.PositionType == (int)PositionType.Destination
                    && wpos.ObjCellId >= blockStart
                    && wpos.ObjCellId <= blockEnd
                select wstr;

            var results = query.ToList();

            if (results.Count() == 0)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"Couldn't find dungeon {landblock:X4}", ChatMessageType.Broadcast)
                );
                return;
            }

            foreach (var result in results)
            {
                var name = result.Value.TrimStart("Portal to ").TrimEnd(" Portal");

                session.Network.EnqueueSend(new GameMessageSystemChat(name, ChatMessageType.Broadcast));
            }
        }
    }
}
