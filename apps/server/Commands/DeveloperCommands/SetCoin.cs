using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class SetCoin
{
    [CommandHandler(
        "setcoin",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Set Coin display debug only usage"
    )]
    public static void HandleSetCoin(Session session, params string[] parameters)
    {
        int coins;

        try
        {
            coins = Convert.ToInt32(parameters[0]);
        }
        catch (Exception)
        {
            ChatPacket.SendServerMessage(
                session,
                "Not a valid number - must be a number between 0 - 2,147,483,647",
                ChatMessageType.Broadcast
            );
            return;
        }

        session.Player.CoinValue = coins;
        session.Network.EnqueueSend(
            new GameMessagePrivateUpdatePropertyInt(session.Player, PropertyInt.CoinValue, coins)
        );
    }
}
