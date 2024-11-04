using System;
using System.Linq;
using ACE.Common;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class VendorDump
{
    [CommandHandler(
        "vendordump",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Lists all properties for the last vendor you examined.",
        ""
    )]
    public static void HandleVendorDump(Session session, params string[] parameters)
    {
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

            if (wo == null)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"Unable to find 0x{objectId:X8}", ChatMessageType.System)
                );
                return;
            }

            if (objectId.IsPlayer())
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"{wo.Name} (0x{wo.Guid}) is not a vendor.", ChatMessageType.System)
                );
                return;
            }

            var all = false;
            var summary = false;
            var createList = false;
            var uniques = false;

            if (parameters.Length > 0)
            {
                var args = string.Join(" ", parameters);

                if (args.Contains("all", StringComparison.OrdinalIgnoreCase))
                {
                    all = true;
                }

                if (args.Contains("summary", StringComparison.OrdinalIgnoreCase))
                {
                    summary = true;
                }

                if (args.Contains("createList", StringComparison.OrdinalIgnoreCase))
                {
                    createList = true;
                }

                if (args.Contains("uniques", StringComparison.OrdinalIgnoreCase))
                {
                    uniques = true;
                }

                if (!all && !summary && !createList && !uniques)
                {
                    all = true;
                }
            }
            else
            {
                all = true;
            }

            var msg = "";
            if (wo is Vendor vendor)
            {
                var currencyWCID = vendor.AlternateCurrency ?? (uint)ACE.Entity.Enum.WeenieClassName.W_COINSTACK_CLASS;
                var currencyWeenie = DatabaseManager.World.GetCachedWeenie(currencyWCID);

                msg = $"Vendor Dump for {wo.Name} (0x{wo.Guid})\n";

                if (all || summary)
                {
                    msg += $"Vendor WCID: {wo.WeenieClassId}\n";
                    msg += $"Vendor WeenieClassName: {wo.WeenieClassName}\n";
                    msg += $"Vendor WeenieType: {wo.WeenieType}\n";
                    msg += $"OpenForBusiness: {vendor.OpenForBusiness}\n";

                    msg += $"Currency: ";
                    if (currencyWeenie != null)
                    {
                        msg +=
                            $"{currencyWeenie.GetPluralName()} (WCID: {currencyWCID}){(vendor.AlternateCurrency.HasValue ? " | AlternateCurrency" : "")}\n";
                    }
                    else
                    {
                        var errorMsg =
                            $"WCID {currencyWCID}{(vendor.AlternateCurrency.HasValue ? ", which comes from PropertyDataId.AlternateCurrency," : "")} is not found in the database, Vendor has been disabled as a result!\n";
                        msg += errorMsg;
                    }
                    msg += $"BuyPrice: {(vendor.BuyPrice.HasValue ? $"{vendor.BuyPrice:F}" : "NULL")}\n";
                    msg += $"SellPrice: {(vendor.SellPrice.HasValue ? $"{vendor.SellPrice:F}" : "NULL")}\n";

                    msg +=
                        $"DealMagicalItems: {(vendor.DealMagicalItems.HasValue ? $"{vendor.DealMagicalItems}" : "NULL")}\n";
                    msg += $"VendorService: {(vendor.VendorService.HasValue ? $"{vendor.VendorService}" : "NULL")}\n";

                    msg +=
                        $"MerchandiseItemTypes: {(vendor.MerchandiseItemTypes.HasValue ? $"{(ItemType)vendor.MerchandiseItemTypes} ({vendor.MerchandiseItemTypes})" : "NULL")}\n";
                    msg +=
                        $"MerchandiseMinValue: {(vendor.MerchandiseMinValue.HasValue ? $"{vendor.MerchandiseMinValue}" : "NULL")}\n";
                    msg +=
                        $"MerchandiseMaxValue: {(vendor.MerchandiseMaxValue.HasValue ? $"{vendor.MerchandiseMaxValue}" : "NULL")}\n";

                    msg +=
                        $"VendorHappyMean: {(vendor.VendorHappyMean.HasValue ? $"{vendor.VendorHappyMean}" : "NULL")}\n";
                    msg +=
                        $"VendorHappyVariance: {(vendor.VendorHappyVariance.HasValue ? $"{vendor.VendorHappyVariance}" : "NULL")}\n";
                    msg +=
                        $"VendorHappyMaxItems: {(vendor.VendorHappyMaxItems.HasValue ? $"{vendor.VendorHappyMaxItems}" : "NULL")}\n";

                    msg +=
                        $"MoneyOutflow: {vendor.MoneyOutflow:N0} {(vendor.MoneyOutflow == 1 ? currencyWeenie.GetName() : currencyWeenie.GetPluralName())}\n";
                    msg += $"NumItemsBought: {vendor.NumItemsBought:N0}\n";
                    msg += $"NumItemsSold: {vendor.NumItemsSold:N0}\n";
                    msg += $"NumServicesSold: {vendor.NumServicesSold:N0}\n";
                    msg +=
                        $"MoneyIncome: {vendor.MoneyIncome:N0} {(vendor.MoneyIncome == 1 ? currencyWeenie.GetName() : currencyWeenie.GetPluralName())}\n";
                }

                if (all || createList)
                {
                    var createListShop = vendor.Biota.PropertiesCreateList.Where(x =>
                        x.DestinationType == DestinationType.Shop
                    );

                    msg += $"createListShop.Count: {createListShop.Count()}\n";
                    msg += $"===============================================\n";
                    foreach (var shopItem in createListShop)
                    {
                        var itemWeenie = DatabaseManager.World.GetCachedWeenie(shopItem.WeenieClassId);
                        if (itemWeenie == null)
                        {
                            msg +=
                                $"{shopItem.WeenieClassId} is not in the database, which will be skipped on load, and will not be sold by this vendor.\n";
                        }
                        else
                        {
                            msg +=
                                $"{itemWeenie.GetName()} ({itemWeenie.WeenieClassId} | {itemWeenie.ClassName} | {itemWeenie.WeenieType})\n";
                            if (itemWeenie.IsVendorService())
                            {
                                var serviceSpell = itemWeenie.GetProperty(PropertyDataId.Spell);
                                var spell = new Spell(serviceSpell ?? 0);
                                msg +=
                                    $"This is a vendor service which casts the following spell on purchaser: {(serviceSpell.HasValue ? $"{spell.Name} ({spell.Id}): {spell.Description}" : "NULL SPELL")}\n";
                            }
                            else
                            {
                                msg +=
                                    $"StackSize: {shopItem.StackSize}{(shopItem.StackSize == -1 ? " (Unlimited)" : " (per single transction)")} | PaletteTemplate: {(PaletteTemplate)shopItem.Palette} ({shopItem.Palette}) | Shade: {shopItem.Shade}\n";
                            }

                            var cost = vendor.GetSellCost(itemWeenie);
                            msg +=
                                $"Cost: {cost:N0} {(cost == 1 ? currencyWeenie.GetName() : currencyWeenie.GetPluralName())}\n";
                        }

                        msg += $"===============================================\n";
                    }
                }

                if (all || uniques)
                {
                    msg += $"UniqueItemsForSale.Count: {vendor.UniqueItemsForSale.Count}\n";
                    msg += $"===============================================\n";
                    foreach (var shopItem in vendor.UniqueItemsForSale.Values)
                    {
                        msg +=
                            $"{shopItem.Name} (0x{shopItem.Guid} | {shopItem.WeenieClassId} | {shopItem.WeenieClassName} | {shopItem.WeenieType})\n";
                        msg +=
                            $"StackSize: {shopItem.StackSize ?? 1} | PaletteTemplate: {(PaletteTemplate)shopItem.PaletteTemplate} ({shopItem.PaletteTemplate}) | Shade: {shopItem.Shade:F3}\n";
                        var soldTimestamp = Time.GetDateTimeFromTimestamp(shopItem.SoldTimestamp ?? 0);
                        msg +=
                            $"SoldTimestamp: {soldTimestamp.ToLocalTime()} ({(shopItem.SoldTimestamp.HasValue ? $"{shopItem.SoldTimestamp}" : "NULL")})\n";
                        var rotTime = soldTimestamp.AddSeconds(
                            PropertyManager.GetDouble("vendor_unique_rot_time").Item
                        );
                        msg += $"RotTimestamp: {rotTime.ToLocalTime()}\n";
                        var payout = vendor.GetBuyCost(shopItem);
                        msg +=
                            $"Paid: {payout:N0} {(payout == 1 ? currencyWeenie.GetName() : currencyWeenie.GetPluralName())}\n";
                        var cost = vendor.GetSellCost(shopItem);
                        msg +=
                            $"Cost: {cost:N0} {(cost == 1 ? currencyWeenie.GetName() : currencyWeenie.GetPluralName())}\n";

                        msg += $"===============================================\n";
                    }
                }
            }
            else
            {
                msg = $"{wo.Name} (0x{wo.Guid}) is not a vendor.";
            }

            session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.System));
        }
    }
}
