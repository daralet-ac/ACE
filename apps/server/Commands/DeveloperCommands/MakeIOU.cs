using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class MakeIOU
{
    [CommandHandler(
        "makeiou",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Make an IOU and put it in your inventory",
        "<wcid>"
    )]
    public static void HandleMakeIOU(Session session, params string[] parameters)
    {
        var weenieClassDescription = parameters[0];
        var wcid = uint.TryParse(weenieClassDescription, out var weenieClassId);

        if (!wcid)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"WCID must be a valid weenie id", ChatMessageType.Broadcast)
            );
            return;
        }

        var iou = PlayerFactory.CreateIOU(weenieClassId);

        if (iou != null)
        {
            session.Player.TryCreateInInventoryWithNetworking(iou);
        }
    }
}
