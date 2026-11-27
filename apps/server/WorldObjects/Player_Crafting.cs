using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects.Entity;

namespace ACE.Server.WorldObjects;

partial class Player
{
    public double CampfireTimer = 0;

    /// <summary>
    /// A lookup table for MaterialType => Salvage Bag WCIDs
    /// </summary>
    public static Dictionary<int, int> MaterialSalvage = new Dictionary<int, int>()
    {
        { 1, 20983 }, // Ceramic
        { 2, 21067 }, // Porcelain
        { 3, 0 }, // ======= Cloth =======
        { 4, 20987 }, // Linen
        { 5, 20992 }, // Satin
        { 6, 21076 }, // Silk
        { 7, 20994 }, // Velvet
        { 8, 20995 }, // Wool
        { 9, 0 }, // ======= Gems =======
        { 10, 21034 }, // Agate
        { 11, 21035 }, // Amber
        { 12, 21036 }, // Amethyst
        { 13, 21037 }, // Aquamarine
        { 14, 21038 }, // Azurite
        { 15, 21039 }, // Black Garnet
        { 16, 21040 }, // Black Opal
        { 17, 21041 }, // Bloodstone
        { 18, 21043 }, // Carnelian
        { 19, 21044 }, // Citrine
        { 20, 21046 }, // Diamond
        { 21, 21048 }, // Emerald
        { 22, 21049 }, // Fire Opal
        { 23, 21050 }, // Green Garnet
        { 24, 21051 }, // Green Jade
        { 25, 21053 }, // Hematite
        { 26, 21054 }, // Imperial Topaz
        { 27, 21056 }, // Jet
        { 28, 21057 }, // Lapis Lazuli
        { 29, 21058 }, // Lavender Jade
        { 30, 21060 }, // Malachite
        { 31, 21062 }, // Moonstone
        { 32, 21064 }, // Onyx
        { 33, 21065 }, // Opal
        { 34, 21066 }, // Peridot
        { 35, 21069 }, // Red Garnet
        { 36, 21070 }, // Red Jade
        { 37, 21071 }, // Rose Quartz
        { 38, 21072 }, // Ruby
        { 39, 21074 }, // Sapphire
        { 40, 21078 }, // Smokey Quartz
        { 41, 21079 }, // Sunstone
        { 42, 21081 }, // Tiger Eye
        { 43, 21082 }, // Tourmaline
        { 44, 21083 }, // Turquoise
        { 45, 21084 }, // White Jade
        { 46, 21085 }, // White Quartz
        { 47, 21086 }, // White Sapphire
        { 48, 21087 }, // Yellow Garnet
        { 49, 21088 }, // Yellow Topaz
        { 50, 21089 }, // Zircon
        { 51, 21055 }, // Ivory
        { 52, 21059 }, // Leather
        { 53, 20981 }, // Armoredillo Hide
        { 54, 21052 }, // Gromnie Hide
        { 55, 20991 }, // Reedshark Hide
        { 56, 0 }, // ======= Metal =======
        { 57, 21042 }, // Brass
        { 58, 20982 }, // Bronze
        { 59, 21045 }, // Copper
        { 60, 20984 }, // Gold
        { 61, 20986 }, // Iron
        { 62, 21068 }, // Pyreal
        { 63, 21077 }, // Silver
        { 64, 20993 }, // Steel
        { 65, 0 }, // ======= Stone =======
        { 66, 20980 }, // Alabaster
        { 67, 20985 }, // Granite
        { 68, 21061 }, // Marble
        { 69, 21063 }, // Obsidian
        { 70, 21073 }, // Sandstone
        { 71, 21075 }, // Serpentine
        { 72, 0 }, // ======= Wood =======
        { 73, 21047 }, // Ebony
        { 74, 20988 }, // Mahogany
        { 75, 20989 }, // Oak
        { 76, 20990 }, // Pine
        { 77, 21080 }, // Teak
    };

    /// <summary>
    /// Returns the skill with the largest current value (buffed)
    /// </summary>
    public CreatureSkill GetMaxSkill(List<Skill> skills)
    {
        CreatureSkill maxSkill = null;
        foreach (var skill in skills)
        {
            var creatureSkill = GetCreatureSkill(skill);
            if (maxSkill == null || creatureSkill.Current > maxSkill.Current)
            {
                maxSkill = creatureSkill;
            }
        }
        return maxSkill;
    }

    public static List<Skill> TinkeringSkills = new List<Skill>()
    {
        Skill.Leatherworking,
        Skill.Weaving,
        Skill.Blacksmithing,
        Skill.Jewelcrafting,
        Skill.Spellcrafting,
        Skill.Woodworking
    };

    private bool ToolIsValidUst(uint tool)
    {
        var toolObject = FindObject(tool, SearchLocations.Everywhere, out _, out var rootOwner, out _);

        if (
            toolObject == null
            || toolObject.WeenieClassId != (uint)ACE.Entity.Enum.WeenieClassName.W_TINKERINGTOOL_CLASS
        )
        {
            SendWeenieError(WeenieError.NotASalvageTool);
            return false;
        }
        else if (rootOwner != this)
        {
            SendWeenieError(WeenieError.YouDoNotOwnThatSalvageTool);
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Whether salvaging could turn an item into salvage: it has a material and a workmanship, isn't salvage already, and
    /// isn't retained or unstable. Also what the Daralet plugin's loot rules are told (PlayerUiCorpseData).
    /// </summary>
    public static bool IsSalvageable(WorldObject item)
    {
        return item.WeenieType != WeenieType.Salvage
            && item.MaterialType != null
            && item.Workmanship != null
            && !item.Retained
            && item.GetProperty(PropertyBool.IsUnstable) != true;
    }

    /// <summary>
    /// The Ust in the player's packs, or null: what /salvage-style requests salvage with.
    /// </summary>
    public WorldObject FindUst()
    {
        return GetAllPossessions().FirstOrDefault(o => o.WeenieClassId == (uint)ACE.Entity.Enum.WeenieClassName.W_TINKERINGTOOL_CLASS);
    }

    public void HandleSalvaging(uint tool, List<uint> salvageItems)
    {
        if (!ToolIsValidUst(tool))
        {
            return;
        }

        var salvageCrateWcids = new HashSet<uint> { 1055060, 1055070, 1055080 };
        var salvageCrate = Inventory.Values
            .OfType<Container>()
            .FirstOrDefault(c => salvageCrateWcids.Contains(c.WeenieClassId));

        // collect valid items first so we can check pack space before consuming anything
        var validItems = new List<WorldObject>();
        foreach (var itemGuid in salvageItems)
        {
            var item = GetInventoryItem(itemGuid);
            if (item == null || !IsSalvageable(item))
            {
                continue;
            }
            if (IsTrading && item.IsBeingTradedOrContainsItemBeingTraded(ItemsInTradeWindow))
            {
                continue;
            }
            validItems.Add(item);
        }

        // roll the units each item gives up front, so the space check counts what will actually be added
        var salvageResults = new SalvageResults();
        var produced = validItems
            .Select(item => (
                Item: item,
                Material: (MaterialType)item.MaterialType,
                Tier: SalvageStacks.GetTier(item),
                Units: GetStructure(item, salvageResults)
            ))
            .Where(p => SalvageStacks.HasSalvage(p.Material))
            .ToList();

        // the new stacks needed once the stacks already carried are topped up
        var newStacksNeeded = produced
            .GroupBy(p => (p.Material, p.Tier))
            .Sum(g => CountNewSalvageStacks(g.Key.Material, g.Key.Tier, g.Sum(p => p.Units)));

        var crateFreeSlots = salvageCrate?.GetFreeInventorySlots(false) ?? 0;
        var totalFreeSlots = GetFreeInventorySlots() + crateFreeSlots;
        if (newStacksNeeded > totalFreeSlots)
        {
            Session.Network.EnqueueSend(new GameMessageSystemChat(
                $"You do not have enough pack space for the salvage. You need {newStacksNeeded} free slot{(newStacksNeeded != 1 ? "s" : "")}.",
                ChatMessageType.Broadcast));
            return;
        }

        var salvageEvents = new List<(string itemName, MaterialType material, int tier, int addedUnits, int priorUnits, bool inCrate)>();
        var crateTouched = false;

        foreach (var (item, material, tier, units) in produced)
        {
            // random chance of receiving a jewelcrafting gem in salvage, higher based on gem count and tier
            if (item.GemType != null)
            {
                var random = new Random();
                var roll = random.NextDouble();
                var basechance = 0.001 * (double)item.GemCount;
                var tierModifier = 1.0 / Math.Pow(2, (double)item.Tier);

                var gemBonusWithTier = (basechance * tierModifier);

                if (gemBonusWithTier > roll)
                {
                    if (SalvagedGems.ContainsKey((int)item.GemType))
                    {
                        var wcid = SalvagedGems[(int)item.GemType];

                        var wo = WorldObjectFactory.CreateNewWorldObject((uint)wcid);
                        wo.Tier = item.Tier;
                        wo.Workmanship = item.Workmanship;

                        TryCreateInInventoryWithNetworking(wo);
                    }
                }

                // generate Bezel Fragments if the salvaged item has sockets
                var jewelSockets = item.JewelSockets ?? 0;

                if (jewelSockets > 0)
                {
                    var wo = WorldObjectFactory.CreateNewWorldObject(1053975);
                    wo.StackSize = jewelSockets;

                    TryCreateInInventoryWithNetworking(wo);

                    var bezelFragmentsString = jewelSockets > 1 ? "Bezel Fragments" : "Bezel Fragment";
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"You obtain {jewelSockets} {bezelFragmentsString}.",
                            ChatMessageType.Broadcast
                        )
                    );
                }
            }

            var itemName = item.Name ?? "Unknown Item";
            var priorUnits = GetInventoryItemsOfWCID(SalvageStacks.GetWcid(material, tier)).Sum(s => s.StackSize ?? 1);

            // the item goes first, freeing its slot for the salvage
            TryConsumeFromInventoryWithNetworking(item);

            var touched = new List<WorldObject>();
            var left = AddSalvageUnits(material, tier, units, salvageCrate, touched);

            if (left > 0)
            {
                _log.Warning("[SALVAGE] {Player} had no room for {Units} units of {Material} salvaging {Item}", Name, left, material, itemName);
            }

            var inCrate = salvageCrate != null && touched.Any(s => s.ContainerId == salvageCrate.Guid.Full);
            crateTouched |= inCrate;

            salvageEvents.Add((itemName, material, tier, units - left, priorUnits, inCrate));
        }

        // the crate's stacks are kept sorted; ContainId waits a tick, so it reaches the client after the CreateObject
        // of any stack made this tick
        if (salvageCrate != null && crateTouched)
        {
            SortSalvageCratePositions(salvageCrate);

            var crateRef = salvageCrate;
            var sortChain = new ActionChain();
            sortChain.AddDelaySeconds(0.1);
            sortChain.AddAction(this, () => SendSortedCrateContainIds(crateRef));
            sortChain.EnqueueChain();
        }

        if (!SquelchManager.Squelches.Contains(this, ChatMessageType.Salvaging))
        {
            foreach (var (itemName, material, tier, addedUnits, priorUnits, inCrate) in salvageEvents)
            {
                var materialName = Regex.Replace(material.ToString(), "(?<!^)([A-Z])", " $1");

                string suffix;
                if (priorUnits > 0)
                {
                    suffix = $" -> combined with {materialName} Salvage (W{tier} | {priorUnits} units)";
                }
                else if (inCrate)
                {
                    suffix = " -> moved to Salvage Crate";
                }
                else
                {
                    suffix = "";
                }

                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"{materialName} {itemName} (W{tier} | {addedUnits} units){suffix}",
                        ChatMessageType.Broadcast
                    )
                );
            }
        }
    }

    private void SortSalvageCratePositions(Container salvageCrate)
    {
        var sorted = salvageCrate.Inventory.Values
            .OrderBy(b => Salvage.GetSalvageBagSortKey(b))
            .ToList();

        for (var i = 0; i < sorted.Count; i++)
        {
            sorted[i].PlacementPosition = i;
        }
    }

    private void SendSortedCrateContainIds(Container salvageCrate)
    {
        var bags = salvageCrate.Inventory.Values.OrderBy(b => b.PlacementPosition).ToList();
        foreach (var bag in bags)
        {
            Session.Network.EnqueueSend(new GameEventItemServerSaysContainId(Session, bag, salvageCrate));
        }
    }

    public static Dictionary<int, int> SalvagedGems = new Dictionary<int, int>()
    {
        { 10, 2413 }, // Agate
        { 11, 2426 }, // Amber
        { 12, 2393 }, // Amethyst
        { 13, 2421 }, // Aquamarine
        { 14, 2414 }, // Azurite
        { 15, 2394 }, // Black Garnet
        { 16, 2402 }, // Black Opal
        { 17, 2427 }, // Bloodstone
        { 18, 2428 }, // Carnelian
        { 19, 2429 }, // Citrine
        { 20, 2409 }, // Diamond
        { 21, 2410 }, // Emerald
        { 22, 2403 }, // Fire Opal
        { 23, 2422 }, // Green Garnet
        { 24, 2395 }, // Green Jade
        { 25, 2430 }, // Hematite
        { 26, 2404 }, // Imperial Topaz
        { 27, 2396 }, // Jet
        { 28, 2415 }, // Lapis Lazuli
        { 29, 2405 }, // Lavender Jade
        { 30, 2416 }, // Malachite
        { 31, 2431 }, // Moonstone
        { 32, 2432 }, // Onyx
        { 33, 2423 }, // Opal
        { 34, 2424 }, // Peridot
        { 35, 2397 }, // Red Garnet
        { 36, 2406 }, // Red Jade
        { 37, 2433 }, // Rose Quartz
        { 38, 2411 }, // Ruby
        { 39, 2412 }, // Sapphire
        { 40, 2417 }, // Smokey Quartz
        { 41, 2407 }, // Sunstone
        { 42, 2418 }, // Tiger Eye
        { 43, 2398 }, // Tourmaline
        { 44, 2419 }, // Turquoise
        { 45, 2399 }, // White Jade
        { 46, 2420 }, // White Quartz
        { 47, 2408 }, // White Sapphire
        { 48, 2400 }, // Yellow Garnet
        { 49, 2425 }, // Yellow Topaz
        { 50, 2401 }, // Zircon
    };

    /// <summary>
    /// The units of salvage an item gives: 1 + workmanship / 2, an odd workmanship having an even chance to round up
    /// </summary>
    public int GetStructure(WorldObject salvageItem, SalvageResults salvageResults)
    {
        var addStructure = 1;

        var bonus = (int)Math.Floor((salvageItem.Workmanship ?? 1) * 0.5);
        if ((int)(salvageItem.Workmanship ?? 1) % 2 == 1 && ThreadSafeRandom.Next(0.0f, 1.0f) > 0.5f)
        {
            bonus += 1;
        }

        addStructure += bonus;

        var message = salvageResults.GetMessage(
            salvageItem.MaterialType ?? ACE.Entity.Enum.MaterialType.Unknown,
            Skill.Salvaging
        );
        message.Amount += (uint)addStructure;
        message.Workmanship += salvageItem.ItemWorkmanship ?? 0.0f;
        message.NumItemsInMaterial++;

        return addStructure;
    }

    /// <summary>
    /// Calculates the number of units returned from a salvaging operation
    /// </summary>
    /// <param name="skill">The current salvaging or highest trained tinkering skill for the player</param>
    /// <param name="workmanship">The workmanship of the item being salvaged</param>
    /// <param name="numAugs">The AugmentationBonusSalvage for the player</param>
    /// <returns></returns>
    public static int CalcNumUnits(int skill, float workmanship, int numAugs)
    {
        // https://web.archive.org/web/20170130213649/http://www.thejackcat.com/AC/Shopping/Crafts/Salvage_old.htm
        // https://web.archive.org/web/20170130194012/http://www.thejackcat.com/AC/Shopping/Crafts/Salvage.htm

        return 1 + (int)Math.Floor(skill / 194.0f * workmanship * (1.0f + 0.25f * numAugs));
    }

    public static void TryAwardCraftingXp(
        Player player,
        CreatureSkill creatureSkill,
        Skill skill,
        int difficulty,
        int armorSlots = 1,
        float bonusMod = 1.0f,
        bool fail = false
    )
    {
        // check to ensure appropriately difficult craft before granting (is player skill no more than 50 points above relative difficulty)
        if (creatureSkill.Current - difficulty >= 50)
        {
            return;
        }

        // Awarded xp scales based on level of current skill progress (from 50% of current rank awarded per craft, down to 1% at 200 skill).
        var progressPercentage = Math.Min((float)creatureSkill.Current / 200, 1.0f);
        var progressMod = 0.5f * (Math.Max(1.0f - progressPercentage, 0.01f));

        // Awarded xp received a bonus or penalty for relative difficulty of the craft (-100% to +100%).
        var relativeDifficulty = difficulty - creatureSkill.Current;
        var difficultyMod = 1 + Math.Clamp(relativeDifficulty, -50.0f, 50.0f) / 50.0f;

        var xP = (player.GetXPBetweenSkillLevels(creatureSkill.AdvancementClass, creatureSkill.Ranks, creatureSkill.Ranks + 1) ?? 0);
        var totalXp = (uint)(xP * progressMod * difficultyMod * armorSlots * bonusMod * (fail ? 0.5f : 1.0f));

        player.NoContribSkillXp(player, skill, totalXp, false);

        if (PropertyManager.GetBool("debug_crafting_system").Item)
        {
            Console.WriteLine(
                $"\nAttempt {creatureSkill.Skill} craft:\n" +
                $" Skill: {creatureSkill.Current}, RecipeDiff: {difficulty}\n" +
                $" ProgressPercent: {progressPercentage}, ProgressMod: {progressMod}\n" +
                $" CraftDiff: {relativeDifficulty}, DiffMod: {difficultyMod}\n" +
                $" BonusMod: {bonusMod}, Fail: {fail}\n" +
                $" ToLevelXp: {xP}, TotalXpAward: {totalXp}"
            );
        }
    }
}
