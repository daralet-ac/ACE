using System;
using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Commands.PlayerCommands;

public class HouseSelect
{
    private static readonly ILogger _log = Log.ForContext(typeof(HouseSelect));

    /// <summary>
    /// For characters/accounts who currently own multiple houses, used to select which house they want to keep
    /// </summary>
    [CommandHandler(
        "house-select",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        1,
        "For characters/accounts who currently own multiple houses, used to select which house they want to keep"
    )]
    public static void HandleHouseSelect(Session session, params string[] parameters)
    {
        HandleHouseSelect(session, false, parameters);
    }

    public static void HandleHouseSelect(Session session, bool confirmed, params string[] parameters)
    {
        if (!int.TryParse(parameters[0], out var houseIdx))
        {
            return;
        }

        // ensure current multihouse owner
        if (!session.Player.IsMultiHouseOwner(false))
        {
            _log.Warning(
                "{Player} tried to /house-select {HouseIndex}, but they are not currently a multi-house owner!",
                session.Player.Name,
                houseIdx
            );
            return;
        }

        // get house info for this index
        var multihouses = session.Player.GetMultiHouses();

        if (houseIdx < 1 || houseIdx > multihouses.Count)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Please enter a number between 1 and {multihouses.Count}.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        var keepHouse = multihouses[houseIdx - 1];

        // show confirmation popup
        if (!confirmed)
        {
            var houseType = $"{keepHouse.HouseType}".ToLower();
            var loc = HouseManager.GetCoords(keepHouse.SlumLord.Location);

            var msg = $"Are you sure you want to keep the {houseType} at\n{loc}?";
            if (
                !session.Player.ConfirmationManager.EnqueueSend(
                    new Confirmation_Custom(session.Player.Guid, () => HandleHouseSelect(session, true, parameters)),
                    msg
                )
            )
            {
                session.Player.SendWeenieError(WeenieError.ConfirmationInProgress);
            }

            return;
        }

        // house to keep confirmed, abandon the other houses
        var abandonHouses = new List<House>(multihouses);
        abandonHouses.RemoveAt(houseIdx - 1);

        foreach (var abandonHouse in abandonHouses)
        {
            var house = session.Player.GetHouse(abandonHouse.Guid.Full);

            HouseManager.HandleEviction(house, house.HouseOwner ?? 0, true);
        }

        // set player properties for house to keep
        var player = PlayerManager.FindByGuid(keepHouse.HouseOwner ?? 0, out var isOnline);
        if (player == null)
        {
            _log.Error(
                "{Player}.HandleHouseSelect({HouseIndex}) - couldn't find HouseOwner {HouseOwner} for {House} ({HouseGuid})",
                session.Player.Name,
                houseIdx,
                keepHouse.HouseOwner,
                keepHouse.Name,
                keepHouse.Guid
            );
            return;
        }

        player.HouseId = keepHouse.HouseId;
        player.HouseInstance = keepHouse.Guid.Full;

        player.SaveBiotaToDatabase();

        // update house panel for current player
        var actionChain = new ActionChain();
        actionChain.AddDelaySeconds(3.0f); // wait for slumlord inventory biotas above to save
        actionChain.AddAction(session.Player, session.Player.HandleActionQueryHouse);
        actionChain.EnqueueChain();

        Console.WriteLine("OK");
    }
}
