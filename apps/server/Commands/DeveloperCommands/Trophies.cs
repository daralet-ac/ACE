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

public class Trophies
{
    // trophies
    [CommandHandler(
        "trophies",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Shows a list of the trophies dropped by the target creature, and the percentage chance of dropping.",
        ""
    )]
    public static void HandleTrophies(Session session, params string[] parameters)
    {
        // @trophies - Shows a list of the trophies dropped by the target creature, and the percentage chance of dropping.

        var objectId = new ObjectGuid();

        if (
            session.Player.HealthQueryTarget.HasValue
            || session.Player.ManaQueryTarget.HasValue
            || session.Player.CurrentAppraisalTarget.HasValue
        )
        {
            if (session.Player.HealthQueryTarget.HasValue)
            {
                objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);
            }
            else if (session.Player.ManaQueryTarget.HasValue)
            {
                objectId = new ObjectGuid((uint)session.Player.ManaQueryTarget);
            }
            else
            {
                objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);
            }

            var wo = session.Player.CurrentLandblock?.GetObject(objectId);

            if (objectId.IsPlayer())
            {
                return;
            }

            var msg = "";
            if (
                wo is Creature creature
                && wo.Biota.PropertiesCreateList != null
                && wo.Biota.PropertiesCreateList.Count > 0
            )
            {
                var createList = creature
                    .Biota.PropertiesCreateList.Where(i =>
                        (i.DestinationType & DestinationType.Contain) != 0
                        || (i.DestinationType & DestinationType.Treasure) != 0
                            && (i.DestinationType & DestinationType.Wield) == 0
                    )
                    .ToList();

                var wieldedTreasure = creature
                    .Inventory.Values.Concat(creature.EquippedObjects.Values)
                    .Where(i => i.DestinationType.HasFlag(DestinationType.Treasure))
                    .ToList();

                msg = $"Trophy Dump for {creature.Name} (0x{creature.Guid})\n";
                msg += $"WCID: {creature.WeenieClassId}\n";
                msg += $"WeenieClassName: {creature.WeenieClassName}\n";

                if (createList.Count > 0)
                {
                    foreach (var item in createList)
                    {
                        if (item.WeenieClassId == 0)
                        {
                            msg +=
                                $"{((DestinationType)item.DestinationType).ToString()}: {item.Shade, 7:P2} - {item.WeenieClassId, 5} - Nothing\n";
                            continue;
                        }

                        var weenie = DatabaseManager.World.GetCachedWeenie(item.WeenieClassId);
                        msg +=
                            $"{((DestinationType)item.DestinationType).ToString()}: {item.Shade, 7:P2} - {item.WeenieClassId, 5} - {(weenie != null ? weenie.ClassName : "Item not found in DB")} - {(weenie != null ? weenie.GetProperty(PropertyString.Name) : "Item not found in DB")}\n";
                    }
                }
                else
                {
                    msg += "Creature has no trophies to drop.\n";
                }

                if (wieldedTreasure.Count > 0)
                {
                    foreach (var item in wieldedTreasure)
                    {
                        msg +=
                            $"{item.DestinationType.ToString()}: 100.00% - {item.WeenieClassId, 5} - {item.WeenieClassName} - {item.Name}\n";
                    }
                }
                else
                {
                    msg += "Creature has no wielded items to drop.\n";
                }
            }
            else
            {
                msg = $"{wo?.Name} (0x{wo?.Guid}) has no trophies.";
            }

            session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.System));
        }
    }
}
