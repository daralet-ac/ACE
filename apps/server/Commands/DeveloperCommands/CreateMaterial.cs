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
        "Create salvage in your inventory",
        "<material_type>, optional: <units> <workmanship>"
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
        "Create salvage in your inventory",
        "<material_type>, optional: <units> <workmanship>"
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

        if (!SalvageStacks.HasSalvage(materialType))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"{materialType} has no salvage", ChatMessageType.Broadcast)
            );
            return;
        }

        var units = 100;
        if (parameters.Length > 1)
        {
            int.TryParse(parameters[1], out units);
        }

        var workmanship = 10;
        if (parameters.Length > 2)
        {
            int.TryParse(parameters[2], out workmanship);
        }

        workmanship = Math.Clamp(workmanship, SalvageStacks.MinTier, SalvageStacks.MaxTier);

        // salvage is stackable now: whole-number workmanship, and the units go into stacks of up to 1,000
        session.Player.AddSalvageUnits(materialType, workmanship, Math.Max(units, 1));
    }
}
