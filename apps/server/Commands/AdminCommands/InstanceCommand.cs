using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class InstanceCommand
{
    private const string Usage =
        "instance [list] | open <template> [new] | here [radius] | enter <id> | leave | close <id>\n"
        + "  list: the templates that are set up and the instances that are open\n"
        + "  open <template> [new]: goes into the instance of an island (or any other template that is set up), and makes it if there is none. new makes another one\n"
        + "  here [radius]: opens a private copy of the landblock you are in (and the ones within radius, up to 3) and takes you into it. Nothing in it is saved.\n"
        + "  enter <id>: goes into an open instance\n"
        + "  leave: goes back to where the instance sends players\n"
        + "  close <id>: shuts an instance down, sending everyone in it out";

    [CommandHandler(
        "instance",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Opens, enters, leaves and closes instances: private copies of landblocks that nothing in is saved.",
        Usage
    )]
    public static void HandleInstance(Session session, params string[] parameters)
    {
        var player = session.Player;
        var subcommand = parameters.Length > 0 ? parameters[0].ToLowerInvariant() : "list";

        switch (subcommand)
        {
            case "list":
                List(player);
                break;

            case "open":
                Open(player, parameters);
                break;

            case "here":
                Here(player, parameters);
                break;

            case "enter":
                Enter(player, parameters);
                break;

            case "leave":
                Leave(player);
                break;

            case "close":
                Close(player, parameters);
                break;

            default:
                player.SendMessage($"Usage: {Usage}", ChatMessageType.System);
                break;
        }
    }

    private static void List(WorldObjects.Player player)
    {
        var templates = InstanceManager.GetTemplates();
        var instances = InstanceManager.GetInstances();

        player.SendMessage($"-----------Instance templates ({templates.Count})-----------", ChatMessageType.System);
        foreach (var template in templates)
        {
            player.SendMessage(
                $"{template.Name}: {template.Footprint.Count} landblock(s){(template.HasBoundary ? $", {template.Boundary.Count} of them ring" : "")}{(template.InstanceOnly ? ", instance only" : "")}",
                ChatMessageType.System
            );
        }

        var timeout = InstanceManager.EmptyTimeout();

        player.SendMessage($"-----------Open instances ({instances.Count})-----------", ChatMessageType.System);
        foreach (var instance in instances)
        {
            var state = instance.IsClosing
                ? "shutting down"
                : instance.MemberCount > 0
                    ? $"{instance.MemberCount} player(s) in it"
                    : $"empty for {(InstanceManager.UtcNow() - instance.EmptySince)?.TotalMinutes:N1} of {timeout.TotalMinutes:N0} min";

            player.SendMessage($"{instance.Id}: {instance.Template.Name} - {state}", ChatMessageType.System);
        }
    }

    private static void Open(WorldObjects.Player player, string[] parameters)
    {
        var template = parameters.Length > 1 ? InstanceManager.GetTemplate(parameters[1]) : null;

        if (template == null)
        {
            player.SendMessage(
                parameters.Length > 1
                    ? $"There is no template {parameters[1]}. /instance list shows the ones there are."
                    : $"Usage: {Usage}",
                ChatMessageType.System
            );
            return;
        }

        var makeAnother = parameters.Length > 2 && parameters[2].Equals("new", StringComparison.OrdinalIgnoreCase);

        var instance = makeAnother
            ? InstanceManager.Create(template)
            : InstanceManager.FindOrCreate(template, null, out _);

        InstanceManager.Enter(player, instance);
    }

    private static void Here(WorldObjects.Player player, string[] parameters)
    {
        if (player.InstanceId != Landblock.PersistentInstance)
        {
            player.SendMessage("You are already in an instance. Leave it first.", ChatMessageType.System);
            return;
        }

        var radius = 0;
        if (parameters.Length > 1 && !int.TryParse(parameters[1], out radius))
        {
            player.SendMessage($"Usage: {Usage}", ChatMessageType.System);
            return;
        }

        radius = Math.Clamp(radius, 0, 3);

        var location = player.Location;
        var footprint = new List<LandblockId>();

        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                var x = location.LandblockX + dx;
                var y = location.LandblockY + dy;

                if (x >= 0 && x <= 254 && y >= 0 && y <= 254)
                {
                    footprint.Add(new LandblockId((byte)x, (byte)y));
                }
            }
        }

        // The place they were standing is where they come back to. It is not a registered template, so it doesn't affect anyone logging in.
        var template = new InstanceTemplate(
            $"here-{location.LandblockId.Landblock:X4}",
            footprint,
            new Position(location),
            new Position(location)
        );

        var instance = InstanceManager.Create(template);

        player.SendMessage(
            $"Opened instance {instance.Id} ({footprint.Count} landblock(s)). It is deleted {InstanceManager.EmptyTimeout().TotalMinutes:N0} minutes after the last player leaves it.",
            ChatMessageType.System
        );

        InstanceManager.Enter(player, instance);
    }

    private static void Enter(WorldObjects.Player player, string[] parameters)
    {
        var instance = GetInstance(player, parameters);
        if (instance == null)
        {
            return;
        }

        if (instance.IsClosing)
        {
            player.SendMessage($"Instance {instance.Id} is shutting down.", ChatMessageType.System);
            return;
        }

        InstanceManager.Enter(player, instance);
    }

    private static void Leave(WorldObjects.Player player)
    {
        if (!InstanceManager.Leave(player))
        {
            player.SendMessage("You are not in an instance.", ChatMessageType.System);
        }
    }

    private static void Close(WorldObjects.Player player, string[] parameters)
    {
        var instance = GetInstance(player, parameters);
        if (instance == null)
        {
            return;
        }

        InstanceManager.Close(instance.Id);

        player.SendMessage(
            $"Instance {instance.Id} is shutting down. It is deleted as soon as everyone in it has left.",
            ChatMessageType.System
        );
    }

    private static WorldInstance GetInstance(WorldObjects.Player player, string[] parameters)
    {
        if (parameters.Length < 2 || !uint.TryParse(parameters[1], out var id))
        {
            player.SendMessage($"Usage: {Usage}", ChatMessageType.System);
            return null;
        }

        var instance = InstanceManager.Get(id);
        if (instance == null)
        {
            player.SendMessage($"There is no instance {id}.", ChatMessageType.System);
        }

        return instance;
    }
}
