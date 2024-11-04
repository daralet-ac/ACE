using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class HousePurchase
{
    [CommandHandler(
        "purchase-house",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Instantly purchase the house for the last appraised covenant crystal."
    )]
    public static void HandlePurchaseHouse(Session session, params string[] parameters)
    {
        var slumlord = CommandHandlerHelper.GetLastAppraisedObject(session) as SlumLord;

        if (slumlord == null)
        {
            session.Network.EnqueueSend(new GameMessageSystemChat("Couldn't find slumlord", ChatMessageType.Broadcast));
            return;
        }
        session.Player.SetHouseOwner(slumlord);
        session.Player.GiveDeed(slumlord);
    }
}
