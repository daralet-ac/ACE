using System;
using System.Linq;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.Structure;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class AdminHouse
{
    // adminhouse
    [CommandHandler(
        "adminhouse",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        0,
        "House management tools for admins."
    )]
    public static void HandleAdminhouse(Session session, params string[] parameters)
    {
        // @adminhouse dump: dumps info about currently selected house or house owned by currently selected player.
        // @adminhouse dump name<name>: dumps info about house owned by the account of the named player.
        // @adminhouse dump account<account_name>: dumps info about house owned by named account.
        // @adminhouse dump hid<houseID>: dumps info about specified house.
        // @adminhouse dump_all: dumps one line about each house in the world.
        // @adminhouse dump_all summary: dumps info about total houses owned for each house type.
        // @adminhouse dump_all dangerous: dumps full info about all houses.Use with caution.
        // @adminhouse rent pay: fully pay the rent of the selected house.
        // @adminhouse rent warn: rent timestamp is pushed far enough back in time to cause the rent to be almost due for the selected house.
        // @adminhouse rent due: rent timestamp is pushed back to cause the rent to be due for the selected house.
        // @adminhouse rent overdue: sets the rent timestamp far enough back to cause the selected house's rent to be overdue.
        // @adminhouse rent payall: fully pay the rent for all houses.
        // @adminhouse payrent on / off: sets the targeted house to not require / require normal maintenance payments.
        // @adminhouse - House management tools for admins.

        if (parameters.Length >= 1 && parameters[0] == "dump")
        {
            if (parameters.Length == 1)
            {
                if (
                    session.Player.HealthQueryTarget.HasValue
                    || session.Player.ManaQueryTarget.HasValue
                    || session.Player.CurrentAppraisalTarget.HasValue
                )
                {
                    var house = GetSelectedHouse(session, out var wo);

                    if (house == null)
                    {
                        return;
                    }

                    DumpHouse(session, house, wo);
                }
                else
                {
                    session.Player.SendMessage("No object is selected.");
                    return;
                }
            }
            else if (parameters.Length > 1 && parameters[1] == "name")
            {
                var playerName = "";
                for (var i = 2; i < parameters.Length; i++)
                {
                    playerName += $"{parameters[i]} ";
                }

                playerName = playerName.Trim();

                if (playerName == "")
                {
                    session.Player.SendMessage("You must specify a player's name.");
                    return;
                }

                var player = PlayerManager.FindByName(playerName);

                if (player == null)
                {
                    session.Player.SendMessage($"Could not find {playerName} in PlayerManager!");
                    return;
                }

                //var houses = HouseManager.GetCharacterHouses(player.Guid.Full);
                var houses = HouseManager.GetAccountHouses(player.Account.AccountId);

                if (houses.Count == 0)
                {
                    session.Player.SendMessage($"Player {playerName} does not own a house.");
                    return;
                }

                foreach (var house in houses)
                {
                    DumpHouse(session, house, house);
                }
            }
            else if (parameters.Length > 1 && parameters[1] == "account")
            {
                var accountName = "";
                for (var i = 2; i < parameters.Length; i++)
                {
                    accountName += $"{parameters[i]} ";
                }

                accountName = accountName.Trim();

                if (accountName == "")
                {
                    session.Player.SendMessage("You must specify an account name.");
                    return;
                }

                var player = PlayerManager
                    .GetAllPlayers()
                    .Where(p => p.Account.AccountName.Equals(accountName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (player == null)
                {
                    session.Player.SendMessage($"Could not find {accountName} in PlayerManager!");
                    return;
                }

                var houses = HouseManager.GetAccountHouses(player.Account.AccountId);

                if (houses.Count == 0)
                {
                    session.Player.SendMessage($"Account {accountName} does not own a house.");
                    return;
                }

                foreach (var house in houses)
                {
                    DumpHouse(session, house, house);
                }
            }
            else if (parameters.Length > 1 && parameters[1] == "hid")
            {
                if (parameters.Length < 2)
                {
                    session.Player.SendMessage("You must specify a house id.");
                    return;
                }

                if (!uint.TryParse(parameters[2], out var houseId))
                {
                    session.Player.SendMessage($"{parameters[2]} is not a valid house id.");
                    return;
                }

                var houses = HouseManager.GetHouseById(houseId);

                if (houses.Count == 0)
                {
                    session.Player.SendMessage($"HouseId {houseId} is not currently owned.");
                    return;
                }

                foreach (var house in houses)
                {
                    DumpHouse(session, house, house);
                }
            }
            else
            {
                session.Player.SendMessage("You must specify either \"name\", \"account\" or \"hid\".");
            }
        }
        else if (parameters.Length >= 1 && parameters[0] == "dump_all")
        {
            if (parameters.Length == 1)
            {
                for (var i = 1u; i < 6251; i++)
                {
                    var msg = $"{i}: ";

                    var house = HouseManager.GetHouseById(i).FirstOrDefault();

                    if (house != null)
                    {
                        var houseData = house.GetHouseData(
                            PlayerManager.FindByGuid(new ObjectGuid(house.HouseOwner ?? 0))
                        );
                        msg +=
                            $"{house.HouseType} | Owner: {house.HouseOwnerName} (0x{house.HouseOwner:X8}) | BuyTime: {Time.GetDateTimeFromTimestamp(houseData.BuyTime).ToLocalTime()} ({houseData.BuyTime}) | RentTime: {Time.GetDateTimeFromTimestamp(houseData.RentTime).ToLocalTime()} ({houseData.RentTime}) | RentDue: {Time.GetDateTimeFromTimestamp(house.GetRentDue(houseData.RentTime)).ToLocalTime()} ({house.GetRentDue(houseData.RentTime)}) | Rent is {(house.SlumLord.IsRentPaid() ? "" : "NOT ")}paid{(house.HouseStatus != HouseStatus.Active ? $"  ({house.HouseStatus})" : "")}";
                    }
                    else
                    {
                        msg += "House is NOT currently owned";
                    }

                    session.Player.SendMessage(msg);
                }
            }
            else if (parameters.Length > 1 && parameters[1] == "summary")
            {
                var apartmentsTotal = 3000d;
                var cottagesTotal = 2600d;
                var villasTotal = 570d;
                var mansionsTotal = 80d;

                var cottages = 0;
                var villas = 0;
                var mansions = 0;
                var apartments = 0;

                for (var i = 1u; i < 6251; i++)
                {
                    var house = HouseManager.GetHouseById(i).FirstOrDefault();

                    if (house == null)
                    {
                        continue;
                    }

                    //var houseData = house.GetHouseData(PlayerManager.FindByGuid(new ObjectGuid(house.HouseOwner ?? 0)));
                    switch (house.HouseType)
                    {
                        case HouseType.Apartment:
                            apartments++;
                            break;
                        case HouseType.Cottage:
                            cottages++;
                            break;
                        case HouseType.Mansion:
                            mansions++;
                            break;
                        case HouseType.Villa:
                            villas++;
                            break;
                    }
                }

                var apartmentsAvail = (apartmentsTotal - apartments) / apartmentsTotal;
                var cottagesAvail = (cottagesTotal - cottages) / cottagesTotal;
                var villasAvail = (villasTotal - villas) / villasTotal;
                var mansionsAvail = (mansionsTotal - mansions) / mansionsTotal;

                var msg = "HUD Report:\n";
                msg += "=========================================================\n";

                msg += string.Format(
                    "{0, -12} {1, 4:0} / {2, 4:0} ({3, 7:P2} available for purchase)\n",
                    "Apartments:",
                    apartments,
                    apartmentsTotal,
                    apartmentsAvail
                );
                msg += string.Format(
                    "{0, -12} {1, 4:0} / {2, 4:0} ({3, 7:P2} available for purchase)\n",
                    "Cottages:",
                    cottages,
                    cottagesTotal,
                    cottagesAvail
                );
                msg += string.Format(
                    "{0, -12} {1, 4:0} / {2, 4:0} ({3, 7:P2} available for purchase)\n",
                    "Villas:",
                    villas,
                    villasTotal,
                    villasAvail
                );
                msg += string.Format(
                    "{0, -12} {1, 4:0} / {2, 4:0} ({3, 7:P2} available for purchase)\n",
                    "Mansions:",
                    mansions,
                    mansionsTotal,
                    mansionsAvail
                );

                var housesTotal = apartmentsTotal + cottagesTotal + villasTotal + mansionsTotal;
                var housesSold = apartments + cottages + villas + mansions;
                var housesAvail = (housesTotal - housesSold) / housesTotal;

                msg += string.Format(
                    "{0, -12} {1, 4:0} / {2, 4:0} ({3, 7:P2} available for purchase)\n",
                    "Total:",
                    housesSold,
                    housesTotal,
                    housesAvail
                );

                msg += "=========================================================\n";

                session.Player.SendMessage(msg);
            }
            else if (parameters.Length > 1 && parameters[1] == "dangerous")
            {
                for (var i = 1u; i < 6251; i++)
                {
                    var houses = HouseManager.GetHouseById(i);

                    if (houses.Count == 0)
                    {
                        session.Player.SendMessage($"HouseId {i} is not currently owned.");
                        continue;
                    }

                    foreach (var house in houses)
                    {
                        DumpHouse(session, house, house);
                    }
                }
            }
            else
            {
                session.Player.SendMessage("You must specify either nothing, \"summary\" or \"dangerous\".");
            }
        }
        else if (parameters.Length >= 1 && parameters[0] == "rent")
        {
            if (parameters.Length > 1 && parameters[1] == "pay")
            {
                if (
                    session.Player.HealthQueryTarget.HasValue
                    || session.Player.ManaQueryTarget.HasValue
                    || session.Player.CurrentAppraisalTarget.HasValue
                )
                {
                    var house = GetSelectedHouse(session, out var wo);

                    if (house == null)
                    {
                        return;
                    }

                    if (HouseManager.PayRent(house))
                    {
                        PlayerManager.BroadcastToAuditChannel(
                            session.Player,
                            $"{session.Player.Name} paid rent for HouseId {house.HouseId} (0x{house.Guid}:{house.WeenieClassId})"
                        );
                    }
                }
                else
                {
                    session.Player.SendMessage("No object is selected.");
                    return;
                }
            }
            else if (parameters.Length > 1 && parameters[1] == "payall")
            {
                HouseManager.PayAllRent();

                PlayerManager.BroadcastToAuditChannel(
                    session.Player,
                    $"{session.Player.Name} paid all rent for player housing."
                );
            }
            else
            {
                session.Player.SendMessage("You must specify either \"pay\" or \"payall\".");
            }
        }
        else if (parameters.Length >= 1 && parameters[0] == "payrent")
        {
            if (parameters.Length > 1 && parameters[1] == "off")
            {
                if (
                    session.Player.HealthQueryTarget.HasValue
                    || session.Player.ManaQueryTarget.HasValue
                    || session.Player.CurrentAppraisalTarget.HasValue
                )
                {
                    var house = GetSelectedHouse(session, out _);

                    if (house == null)
                    {
                        return;
                    }

                    if (house.HouseStatus != HouseStatus.InActive)
                    {
                        house.HouseStatus = HouseStatus.InActive;
                        house.SaveBiotaToDatabase();

                        session.Player.SendMessage($"{house.Name} (0x{house.Guid}) is now maintenance free.");

                        if (house.HouseOwner > 0)
                        {
                            var onlinePlayer = PlayerManager.GetOnlinePlayer(house.HouseOwner ?? 0);
                            if (onlinePlayer != null)
                            {
                                var updateHouseChain = new ActionChain();
                                updateHouseChain.AddDelaySeconds(5.0f);
                                updateHouseChain.AddAction(onlinePlayer, onlinePlayer.HandleActionQueryHouse);
                                updateHouseChain.EnqueueChain();
                            }
                        }

                        PlayerManager.BroadcastToAuditChannel(
                            session.Player,
                            $"{session.Player.Name} set HouseStatus to {house.HouseStatus} for HouseId {house.HouseId} (0x{house.Guid}:{house.WeenieClassId}) which equates to MaintenanceFree = {house.HouseStatus == HouseStatus.InActive}"
                        );
                    }
                    else
                    {
                        session.Player.SendMessage($"{house.Name} (0x{house.Guid}) is already maintenance free.");
                    }
                }
                else
                {
                    session.Player.SendMessage("No object is selected.");
                    return;
                }
            }
            else if (parameters.Length > 1 && parameters[1] == "on")
            {
                if (
                    session.Player.HealthQueryTarget.HasValue
                    || session.Player.ManaQueryTarget.HasValue
                    || session.Player.CurrentAppraisalTarget.HasValue
                )
                {
                    var house = GetSelectedHouse(session, out _);

                    if (house == null)
                    {
                        return;
                    }

                    if (house.HouseStatus != HouseStatus.Active)
                    {
                        house.HouseStatus = HouseStatus.Active;
                        house.SaveBiotaToDatabase();

                        session.Player.SendMessage($"{house.Name} (0x{house.Guid}) now requires maintenance.");

                        if (house.HouseOwner > 0)
                        {
                            var onlinePlayer = PlayerManager.GetOnlinePlayer(house.HouseOwner ?? 0);
                            if (onlinePlayer != null)
                            {
                                var updateHouseChain = new ActionChain();
                                updateHouseChain.AddDelaySeconds(5.0f);
                                updateHouseChain.AddAction(onlinePlayer, onlinePlayer.HandleActionQueryHouse);
                                updateHouseChain.EnqueueChain();
                            }
                        }

                        PlayerManager.BroadcastToAuditChannel(
                            session.Player,
                            $"{session.Player.Name} set HouseStatus to {house.HouseStatus} for HouseId {house.HouseId} (0x{house.Guid}:{house.WeenieClassId}) which equates to MaintenanceFree = {house.HouseStatus == HouseStatus.InActive}"
                        );
                    }
                    else
                    {
                        session.Player.SendMessage($"{house.Name} (0x{house.Guid}) already requires maintenance.");
                    }
                }
                else
                {
                    session.Player.SendMessage("No object is selected.");
                    return;
                }
            }
            else
            {
                session.Player.SendMessage("You must specify either \"on\" or \"off\".");
            }
        }
        else
        {
            var msg =
                "@adminhouse dump: dumps info about currently selected house or house owned by currently selected player.\n";
            msg += "@adminhouse dump name <name>: dumps info about house owned by the account of the named player.\n";
            msg += "@adminhouse dump account <account_name>: dumps info about house owned by named account.\n";
            msg += "@adminhouse dump hid <houseID>: dumps info about specified house.\n";
            msg += "@adminhouse dump_all: dumps one line about each house in the world.\n";
            msg += "@adminhouse dump_all summary: dumps info about total houses owned for each house type.\n";
            msg += "@adminhouse dump_all dangerous: dumps full info about all houses. Use with caution.\n";
            msg += "@adminhouse rent pay: fully pay the rent of the selected house.\n";
            msg += "@adminhouse rent payall: fully pay the rent for all houses.\n";
            msg +=
                "@adminhouse payrent off / on: sets the targeted house to not require / require normal maintenance payments.\n";

            session.Player.SendMessage(msg);
        }
    }

    private static void DumpHouse(Session session, House targetHouse, WorldObject wo)
    {
        HouseManager.GetHouse(
            targetHouse.Guid.Full,
            (house) =>
            {
                var msg = "";
                msg = $"House Dump for {wo.Name} (0x{wo.Guid})\n";
                msg += $"===House=======================================\n";
                msg +=
                    $"Name: {house.Name} | {house.WeenieClassName} | WCID: {house.WeenieClassId} | GUID: 0x{house.Guid}\n";
                msg += $"Location: {house.Location.ToLOCString()}\n";
                msg += $"HouseID: {house.HouseId}\n";
                msg += $"HouseType: {house.HouseType} ({(int)house.HouseType})\n";
                msg += $"HouseStatus: {house.HouseStatus} ({(int)house.HouseStatus})\n";
                msg += $"RestrictionEffect: {(PlayScript)house.GetProperty(PropertyDataId.RestrictionEffect)} ({house.GetProperty(PropertyDataId.RestrictionEffect)})\n";
                msg += $"HouseMaxHooksUsable: {house.HouseMaxHooksUsable}\n";
                msg += $"HouseCurrentHooksUsable: {house.HouseCurrentHooksUsable}\n";
                msg += $"HouseHooksVisible: {house.HouseHooksVisible ?? false}\n";
                msg += $"OpenToEveryone: {house.OpenToEveryone}\n";
                session.Player.SendMessage(msg, ChatMessageType.System);

                if (house.LinkedHouses.Count > 0)
                {
                    msg = "";
                    msg += $"===LinkedHouses================================\n";
                    foreach (var link in house.LinkedHouses)
                    {
                        msg +=
                            $"Name: {link.Name} | {link.WeenieClassName} | WCID: {link.WeenieClassId} | GUID: 0x{link.Guid}\n";
                        msg += $"Location: {link.Location.ToLOCString()}\n";
                    }
                    session.Player.SendMessage(msg, ChatMessageType.System);
                }

                msg = "";
                msg += $"===SlumLord====================================\n";
                var slumLord = house.SlumLord;
                msg +=
                    $"Name: {slumLord.Name} | {slumLord.WeenieClassName} | WCID: {slumLord.WeenieClassId} | GUID: 0x{slumLord.Guid}\n";
                msg += $"Location: {slumLord.Location.ToLOCString()}\n";
                msg += $"MinLevel: {slumLord.MinLevel}\n";
                msg += $"AllegianceMinLevel: {slumLord.AllegianceMinLevel ?? 0}\n";
                msg += $"HouseRequiresMonarch: {slumLord.HouseRequiresMonarch}\n";
                msg += $"IsRentPaid: {slumLord.IsRentPaid()}\n";
                session.Player.SendMessage(msg, ChatMessageType.System);

                msg = "";
                msg += $"===HouseProfile================================\n";
                var houseProfile = slumLord.GetHouseProfile();

                msg += $"Type: {houseProfile.Type} | Bitmask: {houseProfile.Bitmask}\n";

                msg += $"MinLevel: {houseProfile.MinLevel} | MaxLevel: {houseProfile.MaxLevel}\n";
                msg += $"MinAllegRank: {houseProfile.MinAllegRank} | MaxAllegRank: {houseProfile.MaxAllegRank}\n";

                msg +=
                    $"OwnerID: 0x{houseProfile.OwnerID} | OwnerName: {(string.IsNullOrEmpty(houseProfile.OwnerName) ? "N/A" : $"{houseProfile.OwnerName}")}\n";
                msg += $"MaintenanceFree: {houseProfile.MaintenanceFree}\n";
                msg += "--== Buy Cost==--\n";
                foreach (var cost in houseProfile.Buy)
                {
                    msg +=
                        $"{cost.Num:N0} {(cost.Num > 1 ? $"{cost.PluralName}" : $"{cost.Name}")} (WCID: {cost.WeenieID})\n";
                }

                msg += "--==Rent Cost==--\n";
                foreach (var cost in houseProfile.Rent)
                {
                    msg +=
                        $"{cost.Num:N0} {(cost.Num > 1 ? $"{cost.PluralName}" : $"{cost.Name}")} (WCID: {cost.WeenieID}) | Paid: {cost.Paid:N0}\n";
                }

                session.Player.SendMessage(msg, ChatMessageType.System);

                var houseData = house.GetHouseData(PlayerManager.FindByGuid(houseProfile.OwnerID));
                if (houseData != null)
                {
                    msg = "";
                    msg += $"===HouseData===================================\n";
                    msg += $"Location: {houseData.Position.ToLOCString()}\n";
                    msg += $"Type: {houseData.Type}\n";
                    msg +=
                        $"BuyTime: {(houseData.BuyTime > 0 ? $"{Time.GetDateTimeFromTimestamp(houseData.BuyTime).ToLocalTime()}" : "N/A")} ({houseData.BuyTime})\n";
                    msg +=
                        $"RentTime: {(houseData.RentTime > 0 ? $"{Time.GetDateTimeFromTimestamp(houseData.RentTime).ToLocalTime()}" : "N/A")} ({houseData.RentTime})\n";
                    msg +=
                        $"RentDue: {(houseData.RentTime > 0 ? $"{Time.GetDateTimeFromTimestamp(house.GetRentDue(houseData.RentTime)).ToLocalTime()} ({house.GetRentDue(houseData.RentTime)})" : " N/A (0)")}\n";
                    msg += $"MaintenanceFree: {houseData.MaintenanceFree}\n";
                    session.Player.SendMessage(msg, ChatMessageType.System);
                }

                session.Player.SendMessage(AppendHouseLinkDump(house), ChatMessageType.System);

                if (house.HouseType == HouseType.Villa || house.HouseType == HouseType.Mansion)
                {
                    var basement = house.GetDungeonHouse();
                    if (basement != null)
                    {
                        msg = "";
                        msg += $"===Basement====================================\n";
                        msg +=
                            $"Name: {basement.Name} | {basement.WeenieClassName} | WCID: {basement.WeenieClassId} | GUID: 0x{basement.Guid}\n";
                        msg += $"Location: {basement.Location.ToLOCString()}\n";
                        msg += $"HouseMaxHooksUsable: {basement.HouseMaxHooksUsable}\n";
                        msg += $"HouseCurrentHooksUsable: {basement.HouseCurrentHooksUsable}\n";
                        session.Player.SendMessage(msg, ChatMessageType.System);
                        session.Player.SendMessage(AppendHouseLinkDump(basement), ChatMessageType.System);
                    }
                }

                var guestList = house.Guests.ToList();
                if (guestList.Count > 0)
                {
                    msg = "";
                    msg += $"===GuestList===================================\n";
                    foreach (var guest in guestList)
                    {
                        var player = PlayerManager.FindByGuid(guest.Key);
                        msg +=
                            $"{(player != null ? $"{player.Name}" : "[N/A]")} (0x{guest.Key}){(guest.Value ? " *" : "")}\n";
                    }
                    msg += "* denotes granted access to the home's storage\n";
                    session.Player.SendMessage(msg, ChatMessageType.System);
                }

                var restrictionDB = new RestrictionDB(house);
                msg = "";
                msg += $"===RestrictionDB===============================\n";
                var owner = PlayerManager.FindByGuid(restrictionDB.HouseOwner);
                msg += $"HouseOwner: {(owner != null ? $"{owner.Name}" : "N/A")} (0x{restrictionDB.HouseOwner:X8})\n";
                msg += $"OpenStatus: {restrictionDB.OpenStatus}\n";
                var monarchRDB = PlayerManager.FindByGuid(restrictionDB.MonarchID);
                msg +=
                    $"MonarchID: {(monarchRDB != null ? $"{monarchRDB.Name}" : "N/A")} (0x{restrictionDB.MonarchID:X8})\n";
                if (restrictionDB.Table.Count > 0)
                {
                    msg += "--==Guests==--\n";
                    foreach (var guest in restrictionDB.Table)
                    {
                        var player = PlayerManager.FindByGuid(guest.Key);
                        msg +=
                            $"{(player != null ? $"{player.Name}" : "[N/A]")} (0x{guest.Key}){(guest.Value == 1 ? " *" : "")}\n";
                    }
                    msg += "* denotes granted access to the home's storage\n";
                }
                session.Player.SendMessage(msg, ChatMessageType.System);

                var har = new HouseAccess(house);
                msg = "";
                msg += $"===HouseAccess=================================\n";
                msg += $"Bitmask: {har.Bitmask}\n";
                var monarchHAR = PlayerManager.FindByGuid(har.MonarchID);
                msg += $"MonarchID: {(monarchHAR != null ? $"{monarchHAR.Name}" : "N/A")} (0x{har.MonarchID:X8})\n";
                if (har.GuestList.Count > 0)
                {
                    msg += "--==Guests==--\n";
                    foreach (var guest in har.GuestList)
                    {
                        msg +=
                            $"{(guest.Value.GuestName != null ? $"{guest.Value.GuestName}" : "[N/A]")} (0x{guest.Key}){(guest.Value.ItemStoragePermission ? " *" : "")}\n";
                    }
                    msg += "* denotes granted access to the home's storage\n";
                }
                if (har.Roommates.Count > 0)
                {
                    msg += "--==Roommates==--\n";
                    foreach (var guest in har.Roommates)
                    {
                        var player = PlayerManager.FindByGuid(guest);
                        msg += $"{(player != null ? $"{player.Name}" : "[N/A]")} (0x{guest})\n";
                    }
                }
                session.Player.SendMessage(msg, ChatMessageType.System);
            }
        );
    }

    private static House GetSelectedHouse(Session session, out WorldObject target)
    {
        ObjectGuid objectId;
        if (session.Player.HealthQueryTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);
        }
        else if (session.Player.ManaQueryTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.ManaQueryTarget);
        }
        else if (session.Player.CurrentAppraisalTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);
        }
        else
        {
            session.Player.SendMessage("No object is selected or unable to locate in world.");
            target = null;
            return null;
        }

        target = session.Player.CurrentLandblock?.GetObject(objectId);

        if (target == null)
        {
            session.Player.SendMessage("No object is selected or unable to locate in world.");
            return null;
        }

        House house;

        if (target is Player player)
        {
            if (player.House == null)
            {
                session.Player.SendMessage($"Player {player.Name} does not own a house.");
                return null;
            }

            house = player.House;
            //house = HouseManager.GetCharacterHouses(player.Guid.Full).FirstOrDefault();
        }
        else if (target is House house1)
        {
            house = house1.RootHouse;
        }
        else if (target is Hook hook)
        {
            house = hook.House.RootHouse;
        }
        else if (target is SlumLord slumLord1)
        {
            house = slumLord1.House.RootHouse;
        }
        else if (target is HousePortal housePortal)
        {
            house = housePortal.House.RootHouse;
        }
        else
        {
            session.Player.SendMessage("Selected object is not a player or housing object.");
            return null;
        }

        if (house == null)
        {
            session.Player.SendMessage("Selected house object is null");
            return null;
        }

        return house;
    }
    private static string AppendHouseLinkDump(House house)
    {
        var msg = "";

        if (house.Storage.Count > 0)
        {
            msg += $"===Storage for House 0x{house.Guid}================\n";
            msg += $"Storage.Count: {house.Storage.Count}\n";
            foreach (var chest in house.Storage)
            {
                msg +=
                    $"Name: {chest.Name} | {chest.WeenieClassName} | WCID: {chest.WeenieClassId} | GUID: 0x{chest.Guid}\n";
                msg += $"Location: {chest.Location.ToLOCString()}\n";
            }
        }

        if (house.Hooks.Count > 0)
        {
            msg += $"===Hooks for House 0x{house.Guid}==================\n";
            msg +=
                $"Hooks.Count: {house.Hooks.Count(h => h.HasItem)} in use / {house.HouseMaxHooksUsable} max allowed usable / {house.Hooks.Count} total\n";
            msg += "--==HooksGroups==--\n";
            foreach (var hookGroup in (HookGroupType[])Enum.GetValues(typeof(HookGroupType)))
            {
                msg +=
                    $"{hookGroup}.Count: {house.GetHookGroupCurrentCount(hookGroup)} in use / {house.GetHookGroupMaxCount(hookGroup)} max allowed per group\n";
            }
            msg += "--==Hooks==--\n";
            foreach (var hook in house.Hooks)
            {
                msg +=
                    $"Name: {hook.Name} | {hook.WeenieClassName} | WCID: {hook.WeenieClassId} | GUID: 0x{hook.Guid}\n";
                // msg += $"Location: {hook.Location.ToLOCString()}\n";
                msg +=
                    $"HookType: {(HookType)hook.HookType} ({hook.HookType}){(hook.HasItem ? $" | Item on Hook: {hook.Item.Name} (0x{hook.Item.Guid}:{hook.Item.WeenieClassId}:{hook.Item.WeenieType}) | HookGroup: {hook.Item.HookGroup ?? HookGroupType.Undef} ({(int)(hook.Item.HookGroup ?? 0)})" : "")}\n";
            }
        }

        if (house.BootSpot != null)
        {
            msg += $"===BootSpot for House 0x{house.Guid}===============\n";
            msg +=
                $"Name: {house.BootSpot.Name} | {house.BootSpot.WeenieClassName} | WCID: {house.BootSpot.WeenieClassId} | GUID: 0x{house.BootSpot.Guid}\n";
            msg += $"Location: {house.BootSpot.Location.ToLOCString()}\n";
        }

        if (house.HousePortal != null)
        {
            msg += $"===HousePortal for House 0x{house.Guid}============\n";
            msg +=
                $"Name: {house.HousePortal.Name} | {house.HousePortal.WeenieClassName} | WCID: {house.HousePortal.WeenieClassId} | GUID: 0x{house.HousePortal.Guid}\n";
            msg += $"Location: {house.HousePortal.Location.ToLOCString()}\n";
            msg += $"Destination: {house.HousePortal.Destination.ToLOCString()}\n";
        }

        return msg;
    }
}
