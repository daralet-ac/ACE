using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class CreateMaterial
{
    // cm <material type> <quantity> <ave. workmanship>
    [CommandHandler(
        "cm",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Create a salvage bag in your inventory",
        "<material_type>, optional: <structure> <workmanship> <num_items>"
    )]
    public static void HandleCM(Session session, params string[] parameters)
    {
        // Format is: @cm <material type> <quantity> <ave. workmanship>
        HandleCISalvage(session, parameters);
    }

    [CommandHandler(
        "cisalvage",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Create a salvage bag in your inventory",
        "<material_type>, optional: <structure> <workmanship> <num_items>"
    )]
    public static void HandleCISalvage(Session session, params string[] parameters)
    {
        if (!Enum.TryParse(parameters[0], true, out MaterialType materialType))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Couldn't find material type {parameters[0]}", ChatMessageType.Broadcast)
            );
            return;
        }

        var wcid = (uint)Player.MaterialSalvage[(int)materialType];
        var salvageBag = WorldObjectFactory.CreateNewWorldObject(wcid);

        ushort structure = 100;
        if (parameters.Length > 1)
        {
            ushort.TryParse(parameters[1], out structure);
        }

        var workmanship = 10f;
        if (parameters.Length > 2)
        {
            float.TryParse(parameters[2], out workmanship);
        }

        var numItemsInMaterial = (int)Math.Round(workmanship);
        if (parameters.Length > 3)
        {
            int.TryParse(parameters[3], out numItemsInMaterial);
        }

        var itemWorkmanship = (int)Math.Round(workmanship * numItemsInMaterial);

        salvageBag.Name = $"Salvage ({structure})";
        salvageBag.Structure = structure;
        salvageBag.ItemWorkmanship = itemWorkmanship;
        salvageBag.NumItemsInMaterial = numItemsInMaterial;

        session.Player.TryCreateInInventoryWithNetworking(salvageBag);
    }
}
