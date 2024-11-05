using System;
using System.Linq;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class TelePoi
{
    // telepoi location
    [CommandHandler(
        "telepoi",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Teleport yourself to a named Point of Interest",
        "[POI|list]\n" + "@telepoi Arwic\n" + "Get the list of POIs\n" + "@telepoi list"
    )]
    public static void HandleTeleportPoi(Session session, params string[] parameters)
    {
        var poi = String.Join(" ", parameters);

        if (poi.ToLower() == "list")
        {
            DatabaseManager.World.CacheAllPointsOfInterest();
            var pois = DatabaseManager.World.GetPointsOfInterestCache();
            var list = pois.Select(k => k.Key).OrderBy(k => k).DefaultIfEmpty().Aggregate((a, b) => a + ", " + b);
            session.Network.EnqueueSend(new GameMessageSystemChat($"All POIs: {list}", ChatMessageType.Broadcast));
        }
        else
        {
            var teleportPOI = DatabaseManager.World.GetCachedPointOfInterest(poi);
            if (teleportPOI == null)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Location: \"{poi}\" not found. Use \"list\" to display all valid locations.",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }
            var weenie = DatabaseManager.World.GetCachedWeenie(teleportPOI.WeenieClassId);
            var portalDest = new Position(weenie.GetPosition(PositionType.Destination));
            WorldObject.AdjustDungeon(portalDest);
            session.Player.Teleport(portalDest);
        }
    }
}
