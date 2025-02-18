using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Factories.Enum;
using ACE.Server.Factories.Tables;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;

namespace ACE.Server.WorldObjects;

public class VendorItemComparer : IComparer<WorldObject>
{
    public VendorItemComparer() { }

    public int Compare(WorldObject a, WorldObject b)
    {
        var result = a.WeenieType.CompareTo(b.WeenieType);

        if (result == 0)
        {
            result = a.ItemType.CompareTo(b.ItemType);
            if (result == 0)
            {
                if (a.WeaponSkill != Skill.None)
                {
                    result = a.WeaponSkill.CompareTo(b.WeaponSkill);
                }
                else if (a.ArmorType != 0)
                {
                    var armorTypeA = a.ArmorType ?? 0;
                    var armorTypeB = b.ArmorType ?? 0;
                    result = armorTypeA.CompareTo(armorTypeB);
                }

                if (result == 0)
                {
                    result = a.WeenieClassId.CompareTo(b.WeenieClassId);
                    if (result == 0)
                    {
                        result = a.Name.CompareTo(b.Name);
                    }
                }
            }
        }

        return result;
    }
}

/// <summary>
/// ** Buy Data Flow **
///
/// Player.HandleActionBuyItem -> Vendor.BuyItems_ValidateTransaction -> Player.FinalizeBuyTransaction -> Vendor.BuyItems_FinalTransaction
///
/// </summary>
public class Vendor : Creature
{
    private static readonly VendorItemComparer VendorItemComparer = new VendorItemComparer();

    public readonly Dictionary<ObjectGuid, WorldObject> DefaultItemsForSale = new Dictionary<ObjectGuid, WorldObject>();
    private Dictionary<ObjectGuid, WorldObject> TempDefaultItemsForSale = new Dictionary<ObjectGuid, WorldObject>();

    // unique items purchased from other players
    public Dictionary<ObjectGuid, WorldObject> UniqueItemsForSale = new Dictionary<ObjectGuid, WorldObject>();

    private bool inventoryloaded { get; set; }

    /// <summary>
    ///  The last player who used this vendor
    /// </summary>
    private WorldObjectInfo lastPlayerInfo { get; set; }

    private DateTime LastRestockTime;

    public uint? AlternateCurrency
    {
        get => GetProperty(PropertyDataId.AlternateCurrency);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyDataId.AlternateCurrency);
            }
            else
            {
                SetProperty(PropertyDataId.AlternateCurrency, value.Value);
            }
        }
    }

    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public Vendor(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public Vendor(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues()
    {
        ObjectDescriptionFlags |= ObjectDescriptionFlag.Vendor;

        if (!PropertyManager.GetBool("vendor_shop_uses_generator").Item)
        {
            GeneratorProfiles.RemoveAll(p => p.Biota.WhereCreate.HasFlag(RegenLocationType.Shop));
        }

        LastRestockTime = DateTime.UnixEpoch;
        OpenForBusiness = ValidateVendorRequirements();

        SetMerchandiseItemTypes();
    }

    private bool ValidateVendorRequirements()
    {
        var success = true;

        var currencyWCID = AlternateCurrency ?? (uint)ACE.Entity.Enum.WeenieClassName.W_COINSTACK_CLASS;
        var currencyWeenie = DatabaseManager.World.GetCachedWeenie(currencyWCID);
        if (currencyWeenie == null)
        {
            var errorMsg =
                $"WCID {currencyWCID}{(AlternateCurrency.HasValue ? ", which comes from PropertyDataId.AlternateCurrency," : "")} is not found in the database, Vendor has been disabled as a result!";
            _log.Error($"[VENDOR] {Name} (0x{Guid}:{WeenieClassId}) Currency {errorMsg}");
            success = false;
        }

        if (!MerchandiseItemTypes.HasValue)
        {
            _log.Error(
                $"[VENDOR] {Name} (0x{Guid}:{WeenieClassId}) MerchandiseItemTypes is NULL, Vendor has been disabled as a result!"
            );
            success = false;
        }

        if (!MerchandiseMinValue.HasValue)
        {
            _log.Error(
                $"[VENDOR] {Name} (0x{Guid}:{WeenieClassId}) MerchandiseMinValue is NULL, Vendor has been disabled as a result!"
            );
            success = false;
        }

        if (!MerchandiseMaxValue.HasValue)
        {
            _log.Error(
                $"[VENDOR] {Name} (0x{Guid}:{WeenieClassId}) MerchandiseMaxValue is NULL, Vendor has been disabled as a result!"
            );
            success = false;
        }

        if (!BuyPrice.HasValue)
        {
            _log.Error(
                $"[VENDOR] {Name} (0x{Guid}:{WeenieClassId}) BuyPrice is NULL, Vendor has been disabled as a result!"
            );
            success = false;
        }

        if (!SellPrice.HasValue)
        {
            _log.Error(
                $"[VENDOR] {Name} (0x{Guid}:{WeenieClassId}) SellPrice is NULL, Vendor has been disabled as a result!"
            );
            success = false;
        }

        return success;
    }

    /// <summary>
    /// Populates this vendor's DefaultItemsForSale
    /// </summary>
    private void LoadInventory()
    {
        if (inventoryloaded)
        {
            return;
        }

        SetShopTier();

        var itemsForSale = new Dictionary<(uint weenieClassId, int paletteTemplate, double shade), uint>();

        var templateDefaultItems = new List<(int, bool, int, uint, int, double, int)>();

        switch (GetProperty(PropertyString.Template))
        {
            case "Archmage":
            case "Apprentice":
                templateDefaultItems = VendorBaseItems.ArchmageItems;
                break;
            case "Armorer":
                templateDefaultItems = VendorBaseItems.ArmorerItems;
                break;
            case "Barkeeper":
                templateDefaultItems = VendorBaseItems.BarkeeperItems;
                break;
            case "Blacksmith":
                templateDefaultItems = VendorBaseItems.BlacksmithItems;
                break;
            case "Bowyer":
                templateDefaultItems = VendorBaseItems.BowyerItems;
                break;
            case "Butcher":
                templateDefaultItems = VendorBaseItems.ButcherItems;
                break;
            case "Grocer":
                templateDefaultItems = VendorBaseItems.GrocerItems;
                break;
            case "Healer":
                templateDefaultItems = VendorBaseItems.HealerItems;
                break;
            case "Ivory Trader":
                templateDefaultItems = VendorBaseItems.IvoryTraderItems;
                break;
            case "Jeweler":
                templateDefaultItems = VendorBaseItems.JewelerItems;
                break;
            case "Leather Trader":
                templateDefaultItems = VendorBaseItems.LeatherTraderItems;
                break;
            case "Provisioner":
                templateDefaultItems = VendorBaseItems.ProvisionerItems;
                break;
            case "Scribe":
                templateDefaultItems = VendorBaseItems.ScribeItems;
                break;
            case "Shopkeeper":
                templateDefaultItems = VendorBaseItems.ShopkeeperItems;
                break;
            case "Tailor":
                templateDefaultItems = VendorBaseItems.TailorItems;
                break;
            case "Weaponsmith":
                templateDefaultItems = VendorBaseItems.WeaponsmithItems;
                break;
        }

        LoadDefaultItems(itemsForSale, templateDefaultItems);

        foreach (var item in Biota.PropertiesCreateList.Where(x => x.DestinationType == DestinationType.Shop))
        {
            LoadInventoryItem(itemsForSale, item.WeenieClassId, item.Palette, item.Shade, item.StackSize);
        }

        //if (Biota.PropertiesGenerator != null && !PropertyManager.GetBool("vendor_shop_uses_generator").Item)
        //{
        //    foreach (var item in Biota.PropertiesGenerator.Where(x => x.WhereCreate.HasFlag(RegenLocationType.Shop)))
        //        LoadInventoryItem(itemsForSale, item.WeenieClassId, (int?)item.PaletteId, item.Shade, item.StackSize);
        //}

        inventoryloaded = true;
    }

    private void LoadInventoryItem(
        Dictionary<(uint weenieClassId, int paletteTemplate, double shade), uint> itemsForSale,
        uint weenieClassId,
        int? palette,
        double? shade,
        int? stackSize
    )
    {
        var itemProfile = (weenieClassId, palette ?? 0, shade ?? 0);

        // let's skip dupes if there are any
        if (itemsForSale.ContainsKey(itemProfile))
        {
            return;
        }

        var wo = WorldObjectFactory.CreateNewWorldObject(weenieClassId);

        if (wo == null)
        {
            return;
        }

        if (palette > 0)
        {
            wo.PaletteTemplate = palette;
        }

        if (shade > 0)
        {
            wo.Shade = shade;
        }

        wo.ContainerId = Guid.Full;

        wo.CalculateObjDesc();

        itemsForSale.Add(itemProfile, wo.Guid.Full);

        wo.VendorShopCreateListStackSize = stackSize ?? -1;

        DefaultItemsForSale.Add(wo.Guid, wo);
    }

    public void AddDefaultItem(WorldObject item)
    {
        var existing = GetDefaultItemsByWcid(item.WeenieClassId);

        // add to existing stack?
        if (existing.Count > 0)
        {
            var stackLeft = existing.FirstOrDefault(i => (i.StackSize ?? 1) < (i.MaxStackSize ?? 1));
            if (stackLeft != null)
            {
                stackLeft.SetStackSize((stackLeft.StackSize ?? 1) + 1);
                return;
            }
        }

        // create new item
        item.ContainerId = Guid.Full;

        item.CalculateObjDesc();

        DefaultItemsForSale.Add(item.Guid, item);
    }

    /// <summary>
    /// Helper function to replace the previous 'AllItemsForSale' combiner
    /// While AllItemsForSale was a useful concept, it was only used in 2 places, and was inefficient
    /// </summary>
    public void forEachItem(Action<WorldObject> action)
    {
        foreach (var kvp in DefaultItemsForSale)
        {
            if (UseAltCurrencValue(kvp.Value.AltCurrencyValue))
            {
                kvp.Value.Value = kvp.Value.AltCurrencyValue;
            }

            action(kvp.Value);
        }

        foreach (var kvp in UniqueItemsForSale)
        {
            action(kvp.Value);
        }
    }

    public List<WorldObject> GetDefaultItemsByWcid(uint wcid)
    {
        return DefaultItemsForSale.Values.Where(i => i.WeenieClassId == wcid).ToList();
    }

    /// <summary>
    /// Searches the vendor's inventory for an item
    /// </summary>
    public bool TryGetItemForSale(ObjectGuid itemGuid, out WorldObject itemForSale)
    {
        return DefaultItemsForSale.TryGetValue(itemGuid, out itemForSale)
            || UniqueItemsForSale.TryGetValue(itemGuid, out itemForSale);
    }

    /// <summary>
    /// This is raised by Player.HandleActionUseItem.<para />
    /// The item does not exist in the players possession.<para />
    /// If the item was outside of range, the player will have been commanded to move using DoMoveTo before ActOnUse is called.<para />
    /// When this is called, it should be assumed that the player is within range.
    /// </summary>
    public override void ActOnUse(WorldObject wo)
    {
        var player = wo as Player;
        if (player == null)
        {
            return;
        }

        if (player.IsBusy)
        {
            player.SendWeenieError(WeenieError.YoureTooBusy);
            return;
        }

        if (!OpenForBusiness || !ValidateVendorRequirements())
        {
            // should there be some sort of feedback to player here?
            return;
        }

        var rotateTime = Rotate(player); // vendor rotates towards player

        // TODO: remove this when DelayManager is not forward propagating current tick time

        var actionChain = new ActionChain();
        actionChain.AddDelaySeconds(0.001f); // force to run after rotate.EnqueueBroadcastAction
        actionChain.AddAction(this, LoadInventory);
        actionChain.AddDelaySeconds(rotateTime);
        actionChain.AddAction(this, () => ApproachVendor(player, VendorType.Open));
        actionChain.EnqueueChain();

        if (lastPlayerInfo == null)
        {
            var closeChain = new ActionChain();
            closeChain.AddDelaySeconds(closeInterval);
            closeChain.AddAction(this, CheckClose);
            closeChain.EnqueueChain();
        }

        lastPlayerInfo = new WorldObjectInfo(player);
    }

    /// <summary>
    /// Sends the latest vendor inventory list to player, rotates vendor towards player, and performs the appropriate emote.
    /// </summary>
    /// <param name="action">The action performed by the player</param>
    public void ApproachVendor(Player player, VendorType action = VendorType.Undef, uint altCurrencySpent = 0)
    {
        RotUniques();

        RestockRandomItems();

        player.Session.Network.EnqueueSend(new GameEventApproachVendor(player.Session, this, altCurrencySpent));

        var rotateTime = Rotate(player); // vendor rotates to player

        if (action != VendorType.Undef)
        {
            DoVendorEmote(action, player);
        }

        player.LastOpenedContainerId = Guid;
    }

    public void DoVendorEmote(VendorType vendorType, WorldObject player)
    {
        switch (vendorType)
        {
            case VendorType.Open:
                EmoteManager.DoVendorEmote(vendorType, player);
                break;

            case VendorType.Buy: // player buys item from vendor
                EmoteManager.DoVendorEmote(vendorType, player);
                if (DefaultItemsForSale.Count == 0 && UniqueItemsForSale.Count == 0)
                {
                    EmoteManager.ExecuteEmoteSet(EmoteManager.GetEmoteSet(EmoteCategory.Vendor, null), player, true);
                }

                break;

            case VendorType.Sell: // player sells item to vendor
                EmoteManager.DoVendorEmote(vendorType, player);
                break;

            default:
                _log.Warning(
                    $"Vendor.DoVendorEmote - Encountered Unhandled VendorType {vendorType} for {Name} ({WeenieClassId})"
                );
                break;
        }
    }

    private const float closeInterval = 1.5f;

    /// <summary>
    /// After a player approaches a vendor, this is called every closeInterval seconds
    /// to see if the player is still within the UseRadius of the vendor.
    ///
    /// If the player has moved away, the vendor Close emote is called (waving goodbye, saying farewell)
    /// </summary>
    public void CheckClose()
    {
        if (lastPlayerInfo == null)
        {
            return;
        }

        var lastPlayer = lastPlayerInfo.TryGetWorldObject() as Player;

        if (lastPlayer == null)
        {
            lastPlayerInfo = null;
            return;
        }

        // handles player logging out at vendor
        if (lastPlayer.CurrentLandblock == null)
        {
            lastPlayerInfo = null;
            return;
        }

        var dist = GetCylinderDistance(lastPlayer);

        if (dist > UseRadius)
        {
            if (lastPlayer.LastOpenedContainerId == Guid)
            {
                lastPlayer.LastOpenedContainerId = ObjectGuid.Invalid;
            }

            EmoteManager.DoVendorEmote(VendorType.Close, lastPlayer);
            lastPlayerInfo = null;

            return;
        }

        var closeChain = new ActionChain();
        closeChain.AddDelaySeconds(closeInterval);
        closeChain.AddAction(this, CheckClose);
        closeChain.EnqueueChain();
    }

    /// <summary>
    /// Creates world objects for generic items
    /// </summary>
    private List<WorldObject> ItemProfileToWorldObjects(ItemProfile itemProfile)
    {
        var results = new List<WorldObject>();

        var remaining = itemProfile.Amount;

        while (remaining > 0)
        {
            var wo = WorldObjectFactory.CreateNewWorldObject(itemProfile.WeenieClassId);

            if (itemProfile.Palette != null)
            {
                wo.PaletteTemplate = itemProfile.Palette;
            }

            if (itemProfile.Shade != null)
            {
                wo.Shade = itemProfile.Shade;
            }

            if ((wo.MaxStackSize ?? 0) > 0)
            {
                // stackable
                var currentStackSize = Math.Min(remaining, wo.MaxStackSize.Value);

                wo.SetStackSize(currentStackSize);
                results.Add(wo);
                remaining -= currentStackSize;
            }
            else
            {
                // non-stackable
                wo.StackSize = null;
                results.Add(wo);
                remaining--;
            }
        }
        return results;
    }

    /// <summary>
    /// Handles validation for player buying items from vendor
    /// </summary>
    public bool BuyItems_ValidateTransaction(List<ItemProfile> itemProfiles, Player player)
    {
        // one difference between buy and sell currently
        // is that if *any* items in the buy transactions are detected as invalid,
        // we reject the entire transaction.
        // this seems to be the "safest" route, however in terms of player convenience
        // where only 1 item has an error from a large purchase set,
        // this might not be the most convenient for the player.

        var defaultItemProfiles = new List<ItemProfile>();
        var uniqueItems = new List<WorldObject>();

        // find item profiles in default and unique items
        foreach (var itemProfile in itemProfiles)
        {
            if (!itemProfile.IsValidAmount)
            {
                // reject entire transaction immediately
                player.SendTransientError($"Invalid amount");
                return false;
            }

            var itemGuid = new ObjectGuid(itemProfile.ObjectGuid);

            // check default items
            if (DefaultItemsForSale.TryGetValue(itemGuid, out var defaultItemForSale))
            {
                itemProfile.WeenieClassId = defaultItemForSale.WeenieClassId;
                itemProfile.Palette = defaultItemForSale.PaletteTemplate;
                itemProfile.Shade = defaultItemForSale.Shade;

                defaultItemProfiles.Add(itemProfile);
            }
            // check unique items
            else if (UniqueItemsForSale.TryGetValue(itemGuid, out var uniqueItemForSale))
            {
                uniqueItems.Add(uniqueItemForSale);
            }
        }

        // ensure player has enough free inventory slots / container slots / available burden to receive items
        var itemsToReceive = new ItemsToReceive(player);

        foreach (var defaultItemProfile in defaultItemProfiles)
        {
            itemsToReceive.Add(defaultItemProfile.WeenieClassId, defaultItemProfile.Amount);

            if (itemsToReceive.PlayerExceedsLimits)
            {
                break;
            }
        }

        if (!itemsToReceive.PlayerExceedsLimits)
        {
            foreach (var uniqueItem in uniqueItems)
            {
                itemsToReceive.Add(uniqueItem.WeenieClassId, uniqueItem.StackSize ?? 1);

                if (itemsToReceive.PlayerExceedsLimits)
                {
                    break;
                }
            }
        }

        if (itemsToReceive.PlayerExceedsLimits)
        {
            if (itemsToReceive.PlayerExceedsAvailableBurden)
            {
                player.Session.Network.EnqueueSend(
                    new GameEventCommunicationTransientString(player.Session, "You are too encumbered to buy that!")
                );
            }
            else if (itemsToReceive.PlayerOutOfInventorySlots)
            {
                player.Session.Network.EnqueueSend(
                    new GameEventCommunicationTransientString(
                        player.Session,
                        "You do not have enough pack space to buy that!"
                    )
                );
            }
            else if (itemsToReceive.PlayerOutOfContainerSlots)
            {
                player.Session.Network.EnqueueSend(
                    new GameEventCommunicationTransientString(
                        player.Session,
                        "You do not have enough container slots to buy that!"
                    )
                );
            }

            return false;
        }

        // ideally the creation of the wo's would be delayed even further,
        // and all validations would be performed on weenies beforehand
        // this would require:
        // - a forEach helper function to iterate through both defaultItemProfiles (ItemProfiles) and uniqueItems (WorldObjects),
        //   so that 2 foreach iterators don't have to be written each time
        // - weenie to have more functions that mimic the functionality of WorldObject

        // create world objects for default items
        var defaultItems = new List<WorldObject>();

        foreach (var defaultItemProfile in defaultItemProfiles)
        {
            defaultItems.AddRange(ItemProfileToWorldObjects(defaultItemProfile));
        }

        var purchaseItems = defaultItems.Concat(uniqueItems).ToList();

        if (IsBusy && purchaseItems.Any(i => i.GetProperty(PropertyBool.VendorService) == true))
        {
            player.SendWeenieErrorWithString(WeenieErrorWithString._IsTooBusyToAcceptGifts, Name);
            CleanupCreatedItems(defaultItems);
            return false;
        }

        // check uniques
        if (!player.CheckUniques(purchaseItems, this))
        {
            CleanupCreatedItems(defaultItems);
            return false;
        }

        // calculate price
        uint totalPrice = 0;

        foreach (var item in purchaseItems)
        {
            var cost = GetSellCost(item);

            // detect rollover?
            totalPrice += cost;
        }

        // verify player has enough currency
        if (AlternateCurrency == null)
        {
            if (player.CoinValue < totalPrice)
            {
                CleanupCreatedItems(defaultItems);
                return false;
            }
        }
        else
        {
            var playerAltCurrency = player.GetNumInventoryItemsOfWCID(AlternateCurrency.Value);

            if (playerAltCurrency < totalPrice)
            {
                CleanupCreatedItems(defaultItems);
                return false;
            }
        }

        // everything is verified at this point

        // send transaction to player for further processing
        player.FinalizeBuyTransaction(this, defaultItems, uniqueItems, totalPrice);

        return true;
    }

    public uint GetSellCost(WorldObject item) => GetSellCost(item.Value, item.ItemType, item.AltCurrencyValue);

    public uint GetSellCost(Weenie item) => GetSellCost(item.GetValue(), item.GetItemType());

    private uint GetSellCost(int? value, ItemType? itemType, int? altCurrencyValue = null)
    {
        var sellRate = SellPrice ?? 1.0;
        if (itemType == ItemType.PromissoryNote)
        {
            sellRate = 1.15;
        }

        if (UseAltCurrencValue(altCurrencyValue))
        {
            value = altCurrencyValue;
        }

        var cost = Math.Max(1, (uint)Math.Ceiling(((float)sellRate * (value ?? 0)) - 0.1));
        return cost;
    }

    private bool UseAltCurrencValue(int? altCurrencyValue)
    {
        return AlternateCurrency is not null && altCurrencyValue is not null;
    }

    public int GetBuyCost(WorldObject item) => GetBuyCost(item.Value, item.ItemType);

    public int GetBuyCost(Weenie item) => GetBuyCost(item.GetValue(), item.GetItemType());

    private int GetBuyCost(int? value, ItemType? itemType)
    {
        var buyRate = BuyPrice ?? 1;
        if (itemType == ItemType.PromissoryNote)
        {
            buyRate = 1.0;
        }

        var cost = Math.Max(1, (int)Math.Floor(((float)buyRate * (value ?? 0)) + 0.1));
        return cost;
    }

    public int CalculatePayoutCoinAmount(Dictionary<uint, WorldObject> items)
    {
        var payout = 0;

        foreach (var item in items.Values)
        {
            payout += GetBuyCost(item);
        }

        return payout;
    }

    /// <summary>
    /// This will either add the item to the vendors temporary sellables, or destroy it.<para />
    /// In both cases, the item will be removed from the database.<para />
    /// The item should already have been removed from the players inventory
    /// </summary>
    public void ProcessItemsForPurchase(Player player, Dictionary<uint, WorldObject> items)
    {
        foreach (var item in items.Values)
        {
            var resellItem = true;

            // don't resell DestroyOnSell
            if (item.GetProperty(PropertyBool.DestroyOnSell) ?? false)
            {
                resellItem = false;
            }

            // don't resell Attuned items that can be sold
            if (item.Attuned == AttunedStatus.Attuned)
            {
                resellItem = false;
            }

            // don't resell stackables?
            if (item.MaxStackSize != null || item.MaxStructure != null)
            {
                resellItem = false;
            }

            if (resellItem)
            {
                item.ContainerId = Guid.Full;

                if (!UniqueItemsForSale.TryAdd(item.Guid, item))
                {
                    var sellItems = string.Join(", ", items.Values.Select(i => $"{i.Name} ({i.Guid})"));
                    _log.Error(
                        $"[VENDOR] {Name}.ProcessItemsForPurchase({player.Name}): duplicate item found, sell list: {sellItems}"
                    );
                }

                item.SoldTimestamp = Time.GetUnixTime();

                // verify no gap: even though the guid is technically free in the database at this point,
                // is it still marked as consumed in guid manager, and not marked as freed here?
                // if player repurchases item sometime later, we must ensure the guid is still marked as consumed for re-add

                // remove object from shard db, but keep a reference to it in memory
                // for DestroyOnSell items, these will effectively be destroyed immediately
                // for other items, if a player re-purchases, it will be added to the shard db again
                item.RemoveBiotaFromDatabase();
            }
            else
            {
                item.Destroy();
            }

            NumItemsBought++;
        }

        ApproachVendor(player, VendorType.Sell);
    }

    public void ApplyService(WorldObject item, Player target)
    {
        // verify -- players purchasing multiple services in 1 transaction, and IsBusy state?
        var spell = new Spell(item.SpellDID ?? 0);

        if (spell.NotFound)
        {
            return;
        }

        IsBusy = true;

        var preCastTime = PreCastMotion(target);

        var castChain = new ActionChain();
        castChain.AddDelaySeconds(preCastTime);
        castChain.AddAction(
            this,
            () =>
            {
                TryCastSpell(spell, target, this);
                PostCastMotion();
            }
        );

        var postCastTime = GetPostCastTime(spell);

        castChain.AddDelaySeconds(postCastTime);
        castChain.AddAction(this, () => IsBusy = false);

        castChain.EnqueueChain();

        NumServicesSold++;
    }

    private int ShopTier = 0;
    private int ShopRandomItemStockAmount = 0;
    private float ShopQualityMod = 0.0f;
    private bool RandomItemGenerationInitialized = false;
    private bool IsStarterOutpostVendor = false;

    private bool sellsRandomArmor;
    private bool sellsRandomMeleeWeapons;
    private bool sellsRandomMissileWeapons;
    private bool sellsRandomCasters;
    private bool sellsRandomClothing;
    private bool sellsRandomJewelry;
    private bool sellsRandomGems;
    private bool sellsRandomScrolls;

    private TreasureHeritageGroup ShopHeritage;

    private void SetupRandomItemShop()
    {
        RandomItemGenerationInitialized = true;

        SetShopTier();

        sellsRandomArmor = ((ItemType)MerchandiseItemTypes & ItemType.Armor) == ItemType.Armor;
        sellsRandomMeleeWeapons = ((ItemType)MerchandiseItemTypes & ItemType.MeleeWeapon) == ItemType.MeleeWeapon;
        sellsRandomMissileWeapons = ((ItemType)MerchandiseItemTypes & ItemType.MissileWeapon) == ItemType.MissileWeapon;
        sellsRandomCasters = ((ItemType)MerchandiseItemTypes & ItemType.Caster) == ItemType.Caster;

        if (!IsStarterOutpostVendor)
        {
            sellsRandomClothing = ((ItemType)MerchandiseItemTypes & ItemType.Clothing) == ItemType.Clothing;
            sellsRandomJewelry = ((ItemType)MerchandiseItemTypes & ItemType.Jewelry) == ItemType.Jewelry;
            sellsRandomGems = ((ItemType)MerchandiseItemTypes & ItemType.Gem) == ItemType.Gem;
            sellsRandomScrolls =
                ((ItemType)MerchandiseItemTypes & ItemType.Writable) == ItemType.Writable && sellsRandomCasters; // Check if we also sell casters to prevent scribes from carrying scrolls
        }

        var categoriesSold = 0;
        if (sellsRandomArmor)
        {
            categoriesSold++;
        }

        if (sellsRandomMeleeWeapons)
        {
            categoriesSold++;
        }

        if (sellsRandomMissileWeapons)
        {
            categoriesSold++;
        }

        if (sellsRandomCasters)
        {
            categoriesSold++;
        }

        if (sellsRandomClothing)
        {
            categoriesSold++;
        }

        if (sellsRandomJewelry)
        {
            categoriesSold++;
        }

        if (sellsRandomGems)
        {
            categoriesSold++;
        }

        if (sellsRandomScrolls)
        {
            categoriesSold++;
        }

        if (VendorStockMaxAmount == 0)
        {
            ShopRandomItemStockAmount = categoriesSold * ThreadSafeRandom.Next(5, 10);
        }
        else
        {
            ShopRandomItemStockAmount = categoriesSold * VendorStockMaxAmount;
        }

        ShopHeritage = (TreasureHeritageGroup)(Heritage ?? 0);

        if (ShopHeritage > TreasureHeritageGroup.Sho)
        {
            ShopHeritage = TreasureHeritageGroup.Invalid;
        }

        if (ShopTier == 0) // We're not in a town and no defined shop tier! See what's around us.
        {
            ShopTier = 1; // Fallback to tier 1 if there's nothing around us.

            var creatures = CurrentLandblock
                .GetAllWorldObjectsForDiagnostics()
                .Where(x => x.ItemType == ItemType.Creature)
                .Select(x => x as Creature)
                .Where(x => x is not null);

            foreach (var creature in creatures)
            {
                var pkStatus = (PlayerKillerStatus)(creature.GetProperty(PropertyInt.PlayerKillerStatus) ?? 0);

                if (string.IsNullOrEmpty(creature.Name) || creature.Guid.IsPlayer())
                {
                    continue;
                }

                if (
                    pkStatus != PlayerKillerStatus.RubberGlue
                    && creature.DeathTreasure is not null
                    && creature.DeathTreasure.Tier > ShopTier
                )
                {
                    ShopTier = creature.DeathTreasure.Tier; // Find highest monster tier
                }

                if (creature.Tier.HasValue && creature.Tier > ShopTier)
                {
                    ShopTier = creature.Tier.Value; // Find highest NPC tier
                }
            }
        }

        // Let's overwrite the database values for MerchandiseMaxValue depending on our tier.
        switch (ShopTier)
        {
            default:
            case 1:
                if (IsStarterOutpostVendor)
                {
                    MerchandiseMaxValue = 1000;
                }
                else
                {
                    MerchandiseMaxValue = 50000;
                }

                break;
            case 2:
                MerchandiseMaxValue = 50000;
                break;
            case 3:
                MerchandiseMaxValue = 50000;
                break;
            case 4:
                MerchandiseMaxValue = 50000;
                break;
            case 5:
                MerchandiseMaxValue = 50000;
                break;
            case 6:
                MerchandiseMaxValue = 75000;
                break;
            case 7:
                MerchandiseMaxValue = 100000;
                break;
            case 8:
                MerchandiseMaxValue = 1000000;
                break;
        }
    }

    private void RestockRandomItems()
    {
        if (!RandomItemGenerationInitialized)
        {
            SetupRandomItemShop();
        }

        if (ShopTier == 0)
        {
            return;
        }

        if (
            VendorRestockInterval == 0
            && (DateTime.UtcNow - LastRestockTime).TotalSeconds
                < PropertyManager.GetDouble("vendor_unique_rot_time", 300).Item
        )
        {
            return;
        }
        else if ((DateTime.UtcNow - LastRestockTime).TotalSeconds < VendorRestockInterval)
        {
            return;
        }

        RotUniques();

        LastRestockTime = DateTime.UtcNow;

        if (UniqueItemsForSale.Count >= ShopRandomItemStockAmount)
        {
            return;
        }

        var itemsToGenerate = ShopRandomItemStockAmount - UniqueItemsForSale.Count;

        if (itemsToGenerate <= 0)
        {
            return;
        }

        var added = 0;
        while (added < itemsToGenerate)
        {
            if (sellsRandomArmor && added < itemsToGenerate)
            {
                AddRandomItem(TreasureItemType_Orig.Armor, TreasureArmorType.Undef, TreasureWeaponType.Undef);
                added++;
            }
            if (sellsRandomMeleeWeapons && added < itemsToGenerate)
            {
                AddRandomItem(TreasureItemType_Orig.Weapon, TreasureArmorType.Undef, TreasureWeaponType.MeleeWeapon);
                added++;
            }
            if (sellsRandomMissileWeapons && added < itemsToGenerate)
            {
                AddRandomItem(TreasureItemType_Orig.Weapon, TreasureArmorType.Undef, TreasureWeaponType.MissileWeapon);
                added++;
            }
            if (sellsRandomCasters && added < itemsToGenerate)
            {
                AddRandomItem(TreasureItemType_Orig.Caster, TreasureArmorType.Undef, TreasureWeaponType.Undef);
                added++;
            }
            if (sellsRandomClothing && added < itemsToGenerate)
            {
                AddRandomItem(TreasureItemType_Orig.Clothing, TreasureArmorType.Undef, TreasureWeaponType.Undef);
                added++;
                if (added < itemsToGenerate)
                {
                    AddRandomItem(TreasureItemType_Orig.Armor, TreasureArmorType.Cloth, TreasureWeaponType.Undef);
                    added++;
                }
            }
            if (sellsRandomJewelry && added < itemsToGenerate)
            {
                AddRandomItem(TreasureItemType_Orig.Jewelry, TreasureArmorType.Undef, TreasureWeaponType.Undef);
                added++;
            }
            if (sellsRandomGems && added < itemsToGenerate)
            {
                AddRandomItem(TreasureItemType_Orig.Gem, TreasureArmorType.Undef, TreasureWeaponType.Undef);
                added++;
            }
            if (sellsRandomScrolls && added < itemsToGenerate)
            {
                AddRandomItem(TreasureItemType_Orig.Scroll, TreasureArmorType.Undef, TreasureWeaponType.Undef);
                added++;
            }
        }

        UniqueItemsForSale = new Dictionary<ObjectGuid, WorldObject>(
            UniqueItemsForSale.OrderBy(key => key.Value, VendorItemComparer)
        );
    }

    private void AddRandomItem(
        TreasureItemType_Orig treasureItemType,
        TreasureArmorType armorType = TreasureArmorType.Undef,
        TreasureWeaponType weaponType = TreasureWeaponType.Undef
    )
    {
        var item = LootGenerationFactory.CreateRandomLootObjects_New(
            ShopTier,
            ShopQualityMod,
            (DealMagicalItems ?? false) ? TreasureItemCategory.MagicItem : TreasureItemCategory.Item,
            treasureItemType,
            armorType,
            weaponType,
            ShopHeritage
        );

        if (item == null)
        {
            _log.Warning(
                $"AddRandomItem({treasureItemType}, {armorType}, {weaponType}) - item is null for LootGenerationFactory.CreateRandomLootObjects_New({ShopTier}, {ShopQualityMod}, {DealMagicalItems}, {treasureItemType}, {armorType}, {weaponType}, {ShopHeritage})"
            );
        }
        else
        {
            var amount = item.StackSize ?? 1;
            if (amount > 1 && !item.Workmanship.HasValue) // Split stackable uniques.
            {
                for (var i = 0; i < amount; i++)
                {
                    var newItem = WorldObjectFactory.CreateNewWorldObject(item.WeenieClassId);
                    newItem.ContainerId = Guid.Full;

                    UniqueItemsForSale.Add(newItem.Guid, newItem);

                    newItem.SoldTimestamp = Time.GetUnixTime();
                    newItem.RemoveBiotaFromDatabase();
                }
                item.Destroy();
            }
            else
            {
                item.ContainerId = Guid.Full;

                UniqueItemsForSale.Add(item.Guid, item);

                item.SoldTimestamp = Time.GetUnixTime();

                item.RemoveBiotaFromDatabase();
            }
        }
    }

    /// <summary>
    /// Unique items in the vendor's inventory sold to the vendor by players
    /// expire after vendor_unique_rot_time seconds
    /// </summary>
    private void RotUniques()
    {
        List<WorldObject> itemsToRemove = null;

        foreach (var uniqueItem in UniqueItemsForSale.Values)
        {
            var soldTime = uniqueItem.SoldTimestamp;

            if (soldTime == null)
            {
                _log.Warning(
                    $"[VENDOR] Vendor {Name} has unique item {uniqueItem.Name} ({uniqueItem.Guid}) without a SoldTimestamp -- this shouldn't happen"
                );
                continue; // keep in list?
            }

            var rotTime = Time.GetDateTimeFromTimestamp(soldTime.Value);

            if (VendorStockTimeToRot != 0)
            {
                rotTime = rotTime.AddSeconds(VendorStockTimeToRot);
            }
            else
            {
                rotTime = rotTime.AddSeconds(PropertyManager.GetDouble("vendor_unique_rot_time", 300).Item);
            }

            if (DateTime.UtcNow >= rotTime)
            {
                if (itemsToRemove == null)
                {
                    itemsToRemove = new List<WorldObject>();
                }

                itemsToRemove.Add(uniqueItem);
            }
        }
        if (itemsToRemove != null)
        {
            foreach (var itemToRemove in itemsToRemove)
            {
                _log.Debug(
                    "[VENDOR] Vendor {Name} has discontinued sale of {ItemToRemove} and removed it from its UniqueItemsForSale list.",
                    Name,
                    itemToRemove.Name
                );
                UniqueItemsForSale.Remove(itemToRemove.Guid);

                itemToRemove.Destroy(); // even though it has already been removed from the db at this point, we want to mark as freed in guid manager now
            }
        }
    }

    private void SetShopTier()
    {
        ShopTier = Tier ?? 0;

        var townName = GetProperty(PropertyString.TownName);

        switch (townName)
        {
            case "South Holtburg Outpost":
            case "West Holtburg Outpost":
            case "Southeast Shoushi Outpost":
            case "West Shoushi Outpost":
            case "East Yaraq Outpost":
            case "North Yaraq Outpost":
                if (ShopTier == 0)
                {
                    ShopTier = 1;
                }

                ShopQualityMod = -0.5f;
                IsStarterOutpostVendor = true;
                break;
            case "Holtburg":
            case "Shoushi":
            case "Yaraq":
                if (ShopTier == 0)
                {
                    ShopTier = 1;
                }

                ShopQualityMod = 0.0f;
                break;
            case "Cragstone":
            case "Glenden Wood":
            case "Arwic":
            case "Hebian-to":
            case "Sawato":
            case "Tou-Tou":
            case "Zaikhal":
            case "Al-Arqas":
            case "Tufa":
                if (ShopTier == 0)
                {
                    ShopTier = 2;
                }

                ShopQualityMod = 0.0f;
                break;
            case "Rithwic":
            case "Eastham":
            case "Lytelthorpe":
            case "Yanshi":
            case "Lin":
            case "Nanto":
            case "Al-Jalima":
            case "Samsur":
            case "Uziz":
                if (ShopTier == 0)
                {
                    ShopTier = 3;
                }

                ShopQualityMod = 0.0f;
                break;
            case "Dryreach":
            case "Baishi":
            case "Mayoi":
            case "Xarabydun":
            case "Underground City":
            case "Khayyaban":
            case "Lost Wish Mountains":
            case "Plateau":
            case "Mountain Retreat":
            case "Danby's Outpost":
                if (ShopTier == 0)
                {
                    ShopTier = 4;
                }

                ShopQualityMod = 0.0f;
                break;
            case "Qalaba'r":
            case "Kara":
            case "Stonehold":
            case "Colier":
            case "Crater":
            case "Bandit Castle":
            case "Neydisa Castle":
            case "Beach Fort":
                if (ShopTier == 0)
                {
                    ShopTier = 5;
                }

                ShopQualityMod = 0.0f;
                break;
            case "Linvak Tukal":
            case "Bluespire":
            case "Greenspire":
            case "Ahurenga":
            case "Redspire":
                if (ShopTier == 0)
                {
                    ShopTier = 6;
                }

                ShopQualityMod = 0.0f;
                break;
            case "Fort Tethana":
            case "Wai Jhou":
            case "Ayan Baqur":
            case "Candeth Keep":
            case "Timaru":
                if (ShopTier == 0)
                {
                    ShopTier = 7;
                }

                ShopQualityMod = 0.0f;
                break;
            //case "Martine's Retreat":
            //case "Oolutanga's Refuge":
            //case "MacNiall's Freehold":
            //case "Kryst":
        }
    }

    private void LoadDefaultItems(
        Dictionary<(uint weenieClassId, int paletteTemplate, double shade), uint> itemsForSale,
        List<(int, bool, int, uint, int, double, int)> defaultItems
    )
    {
        foreach (var item in defaultItems)
        {
            var (itemTier, onlyThisTier, itemHeritage, itemWcid, itemPaletteTemplate, itemShade, itemStackSize) = item;

            if (itemHeritage != 0 && Heritage != itemHeritage)
            {
                continue;
            }

            if (onlyThisTier && itemTier != ShopTier - 1)
            {
                continue;
            }

            if (ShopTier - 1 >= itemTier)
            {
                LoadInventoryItem(itemsForSale, itemWcid, itemPaletteTemplate, (float)itemShade, itemStackSize);
            }
        }
    }

    private static void CleanupCreatedItems(List<WorldObject> createdItems)
    {
        foreach (var createdItem in createdItems)
        {
            createdItem.Destroy();
        }
    }

    private void SetMerchandiseItemTypes()
    {
        switch (GetProperty(PropertyString.Template))
        {
            case "Archmage":
            case "Apprentice":
                MerchandiseItemTypes = (int)ItemType.VendorArchmage;
                break;
            case "Armorer":
                MerchandiseItemTypes = (int)ItemType.VendorArmorer;
                break;
            case "Barkeeper":
                MerchandiseItemTypes = (int)ItemType.VendorBarkeep;
                break;
            case "Blacksmith":
                MerchandiseItemTypes = (int)ItemType.VendorBlacksmith;
                break;
            case "Bowyer":
                MerchandiseItemTypes = (int)ItemType.VendorBowyer;
                break;
            case "Grocer":
                MerchandiseItemTypes = (int)ItemType.VendorGrocer;
                break;
            case "Healer":
                MerchandiseItemTypes = (int)ItemType.VendorHealer;
                break;
            case "Jeweler":
                MerchandiseItemTypes = (int)ItemType.VendorJeweler;
                break;
            case "Scribe":
                MerchandiseItemTypes = (int)ItemType.VendorScribe;
                break;
            case "Shopkeeper":
                MerchandiseItemTypes = (int)ItemType.VendorShopKeep;
                break;
            case "Tailor":
                MerchandiseItemTypes = (int)ItemType.VendorTailor;
                break;
            case "Weaponsmith":
                MerchandiseItemTypes = (int)ItemType.VendorWeaponsmith;
                break;
        }
    }

    public bool OpenForBusiness
    {
        get => GetProperty(PropertyBool.OpenForBusiness) ?? true;
        set
        {
            if (value)
            {
                RemoveProperty(PropertyBool.OpenForBusiness);
            }
            else
            {
                SetProperty(PropertyBool.OpenForBusiness, value);
            }
        }
    }

    public int? MerchandiseMinValue
    {
        get => GetProperty(PropertyInt.MerchandiseMinValue);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyInt.MerchandiseMinValue);
            }
            else
            {
                SetProperty(PropertyInt.MerchandiseMinValue, value.Value);
            }
        }
    }

    public int? MerchandiseMaxValue
    {
        get => GetProperty(PropertyInt.MerchandiseMaxValue);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyInt.MerchandiseMaxValue);
            }
            else
            {
                SetProperty(PropertyInt.MerchandiseMaxValue, value.Value);
            }
        }
    }

    public double? BuyPrice
    {
        get => GetProperty(PropertyFloat.BuyPrice);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyFloat.BuyPrice);
            }
            else
            {
                SetProperty(PropertyFloat.BuyPrice, value.Value);
            }
        }
    }

    public double? SellPrice
    {
        get => GetProperty(PropertyFloat.SellPrice);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyFloat.SellPrice);
            }
            else
            {
                SetProperty(PropertyFloat.SellPrice, value.Value);
            }
        }
    }

    public bool? DealMagicalItems
    {
        get => GetProperty(PropertyBool.DealMagicalItems);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyBool.DealMagicalItems);
            }
            else
            {
                SetProperty(PropertyBool.DealMagicalItems, value.Value);
            }
        }
    }

    public bool? VendorService
    {
        get => GetProperty(PropertyBool.VendorService);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyBool.VendorService);
            }
            else
            {
                SetProperty(PropertyBool.VendorService, value.Value);
            }
        }
    }

    public int? VendorHappyMean
    {
        get => GetProperty(PropertyInt.VendorHappyMean);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyInt.VendorHappyMean);
            }
            else
            {
                SetProperty(PropertyInt.VendorHappyMean, value.Value);
            }
        }
    }

    public int? VendorHappyVariance
    {
        get => GetProperty(PropertyInt.VendorHappyVariance);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyInt.VendorHappyVariance);
            }
            else
            {
                SetProperty(PropertyInt.VendorHappyVariance, value.Value);
            }
        }
    }

    public int? VendorHappyMaxItems
    {
        get => GetProperty(PropertyInt.VendorHappyMaxItems);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyInt.VendorHappyMaxItems);
            }
            else
            {
                SetProperty(PropertyInt.VendorHappyMaxItems, value.Value);
            }
        }
    }

    public int NumItemsSold
    {
        get => GetProperty(PropertyInt.NumItemsSold) ?? 0;
        set
        {
            if (value == 0)
            {
                RemoveProperty(PropertyInt.NumItemsSold);
            }
            else
            {
                SetProperty(PropertyInt.NumItemsSold, value);
            }
        }
    }

    public int NumItemsBought
    {
        get => GetProperty(PropertyInt.NumItemsBought) ?? 0;
        set
        {
            if (value == 0)
            {
                RemoveProperty(PropertyInt.NumItemsBought);
            }
            else
            {
                SetProperty(PropertyInt.NumItemsBought, value);
            }
        }
    }

    public int NumServicesSold
    {
        get => GetProperty(PropertyInt.NumServicesSold) ?? 0;
        set
        {
            if (value == 0)
            {
                RemoveProperty(PropertyInt.NumServicesSold);
            }
            else
            {
                SetProperty(PropertyInt.NumServicesSold, value);
            }
        }
    }

    public int MoneyIncome
    {
        get => GetProperty(PropertyInt.MoneyIncome) ?? 0;
        set
        {
            if (value == 0)
            {
                RemoveProperty(PropertyInt.MoneyIncome);
            }
            else
            {
                SetProperty(PropertyInt.MoneyIncome, value);
            }
        }
    }

    public int MoneyOutflow
    {
        get => GetProperty(PropertyInt.MoneyOutflow) ?? 0;
        set
        {
            if (value == 0)
            {
                RemoveProperty(PropertyInt.MoneyOutflow);
            }
            else
            {
                SetProperty(PropertyInt.MoneyOutflow, value);
            }
        }
    }

    protected double VendorRestockInterval
    {
        get => GetProperty(PropertyFloat.VendorRestockInterval) ?? 0;
        set
        {
            if (value == 0)
            {
                RemoveProperty(PropertyFloat.VendorRestockInterval);
            }
            else
            {
                SetProperty(PropertyFloat.VendorRestockInterval, value);
            }
        }
    }

    protected double VendorStockTimeToRot
    {
        get => GetProperty(PropertyFloat.VendorStockTimeToRot) ?? 0;
        set
        {
            if (value == 0)
            {
                RemoveProperty(PropertyFloat.VendorStockTimeToRot);
            }
            else
            {
                SetProperty(PropertyFloat.VendorStockTimeToRot, value);
            }
        }
    }

    protected int VendorStockMaxAmount
    {
        get => GetProperty(PropertyInt.VendorStockMaxAmount) ?? 0;
        set
        {
            if (value == 0)
            {
                RemoveProperty(PropertyInt.VendorStockMaxAmount);
            }
            else
            {
                SetProperty(PropertyInt.VendorStockMaxAmount, value);
            }
        }
    }
}
