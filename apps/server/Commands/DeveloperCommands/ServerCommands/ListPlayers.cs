using System;
using System.Linq;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.ServerCommands;

public class ListPlayers
{
    /// <summary>
    /// Debug command to print out all of the active players connected too the server.
    /// </summary>
    [CommandHandler(
        "listplayers",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Displays all of the active players connected too the server."
    )]
    public static void HandleListPlayers(Session session, params string[] parameters)
    {
        var message = " \n";

        AccessLevel? targetAccessLevel = null;
        if (parameters?.Length > 0)
        {
            if (Enum.TryParse(parameters[0], true, out AccessLevel parsedAccessLevel))
            {
                targetAccessLevel = parsedAccessLevel;
            }
            else
            {
                try
                {
                    uint accessLevel = Convert.ToUInt16(parameters[0]);
                    targetAccessLevel = (AccessLevel)accessLevel;
                }
                catch (Exception)
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        "Invalid AccessLevel value",
                        ChatMessageType.Broadcast
                    );
                    return;
                }
            }
        }

        if (targetAccessLevel.HasValue)
        {
            message += $"Listing only {targetAccessLevel.Value.ToString()}s:\n";
        }

        message += $"Total connected Players: {PlayerManager.GetAllOnline().Count}\n";

        foreach (var player in PlayerManager.GetAllOnline())
        {
            if (targetAccessLevel.HasValue && player.Account.AccessLevel != ((uint)targetAccessLevel.Value))
            {
                continue;
            }

            var locationName = GetLocationName(player.Location.LandblockId.Raw) ?? player.Location.LandblockId.ToString();
            var coordinates = player.Location.GetMapCoordStr();
            var paranthesis = coordinates != null ? $" ({coordinates})" : " (Indoors)";

            var location = $"{locationName}{paranthesis}";


            message += $"{player.Name} ({player.Account.AccountName}/{player.Session.AccountId})  -  Lv: {player.Level}  -  Loc: {location}\n";
        }


        CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.System);
    }

    private static string GetLocationName(uint cellId)
    {
        using var ctx = new WorldDbContext();

        var query =
            from landblockName in ctx.LandblockName
            where
                landblockName.ObjCellId == cellId
            select new
            {
                Name = landblockName.Name
            };

        var name = query.ToList().FirstOrDefault()?.Name;

        if (query.FirstOrDefault() is null)
        {
            var landblockId = (cellId | 0xFFFF) - 0xFFFF;

            query =
                from landblockName in ctx.LandblockName
                where
                    landblockName.ObjCellId == landblockId
                select new
                {
                    landblockName.Name
                };

            name = query.ToList().FirstOrDefault()?.Name;
        }

        return name;
    }

    // public static string GetDungeonName(uint landblock)
    // {
    //     var searchLandblock = landblock.ToString();
    //
    //     using (var ctx = new WorldDbContext())
    //     {
    //         var query =
    //             from weenie in ctx.Weenie
    //             join wstr in ctx.WeeniePropertiesString on weenie.ClassId equals wstr.ObjectId
    //             join wpos in ctx.WeeniePropertiesPosition on weenie.ClassId equals wpos.ObjectId
    //             where
    //                 weenie.Type == (int)WeenieType.Portal
    //                 && wstr.Type == (int)PropertyString.Name
    //                 && wpos.PositionType == (int)PositionType.Destination
    //             select new
    //             {
    //                 Weenie = weenie,
    //                 Name = wstr,
    //                 Dest = wpos
    //             };
    //
    //         var results = query.ToList();
    //
    //         foreach (var result in results)
    //         {
    //             Console.WriteLine($"Weenie: {result.Weenie.ClassName}, Name: {result.Name.Value}, DestId: {result.Dest.ObjCellId}");
    //         }
    //
    //         Console.WriteLine(searchLandblock);
    //
    //         var dest = results
    //             .Where(i => i.Dest.ObjCellId.ToString().Equals(searchLandblock, StringComparison.OrdinalIgnoreCase))
    //             .Select(i => i.Name)
    //             .FirstOrDefault();
    //
    //         if (dest == null)
    //         {
    //             return null;
    //         }
    //
    //         return dest.Value;
    //     }
    // }
}
