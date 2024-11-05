using System;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class SetPurchaseTime
{
    /// <summary>
    /// Sets the house purchase time for this player
    /// </summary>
    [CommandHandler(
        "setpurchasetime",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Sets the house purchase time for this player"
    )]
    public static void HandleSetPurchaseTime(Session session, params string[] parameters)
    {
        var currentTime = DateTime.UtcNow;
        Console.WriteLine($"Current time: {currentTime}");
        // subtract 30 days
        var purchaseTime = currentTime - TimeSpan.FromDays(30);
        // add buffer
        purchaseTime += TimeSpan.FromSeconds(1);
        //purchaseTime += TimeSpan.FromMinutes(2);
        var rentDue = DateTimeOffset
            .FromUnixTimeSeconds(session.Player.House.GetRentDue((uint)Time.GetUnixTime(purchaseTime)))
            .UtcDateTime;

        var prevPurchaseTime = DateTimeOffset
            .FromUnixTimeSeconds(session.Player.HousePurchaseTimestamp ?? 0)
            .UtcDateTime;
        var prevRentDue = DateTimeOffset
            .FromUnixTimeSeconds(session.Player.House.GetRentDue((uint)(session.Player.HousePurchaseTimestamp ?? 0)))
            .UtcDateTime;

        Console.WriteLine($"Previous purchase time: {prevPurchaseTime}");
        Console.WriteLine($"New purchase time: {purchaseTime}");

        Console.WriteLine($"Previous rent time: {prevRentDue}");
        Console.WriteLine($"New rent time: {rentDue}");

        session.Player.HousePurchaseTimestamp = (int)Time.GetUnixTime(purchaseTime);
        session.Player.HouseRentTimestamp = (int)session.Player.House.GetRentDue((uint)Time.GetUnixTime(purchaseTime));

        HouseManager.BuildRentQueue();
    }
}
