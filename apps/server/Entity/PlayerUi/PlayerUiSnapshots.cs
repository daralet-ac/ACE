using System.Collections.Generic;

namespace ACE.Server.Entity.PlayerUi;

// The records @playerui sends, as camelCase JSON. The Daralet plugin's Feed/Models.cs mirrors them; tests on both sides
// pin the names.

/// <summary>
/// Who the plugin is talking for, and every request this server answers (Kinds), so a newer plugin can tell which of its
/// tabs this server doesn't have yet.
/// </summary>
public record PlayerUiHello(int Protocol, string Name, uint Guid, string[] Kinds);

/// <summary>
/// The player's fellowship. Leader is the leader's guid.
/// </summary>
public record PlayerUiFellowship(
    string Name,
    uint Leader,
    bool Open,
    bool ShareXp,
    bool EvenShare,
    bool Locked,
    List<PlayerUiFellow> Members
);

/// <summary>
/// A fellowship member with their vitals. Distance is in meters, or null when they're too far away to say (not in the
/// player's landblock or the ones around it, or in another instance). MapX is east (+) / west (-) and MapY north (+) /
/// south (-), in map units, or null in a dungeon, where the map doesn't reach. InBuilding: they're inside a building, which
/// still has a place on the map (PlayerUiData.GetMapCoords).
/// </summary>
public record PlayerUiFellow(
    uint Guid,
    string Name,
    int Level,
    uint Health,
    uint MaxHealth,
    uint Stamina,
    uint MaxStamina,
    uint Mana,
    uint MaxMana,
    float? Distance,
    string Location,
    float? MapX,
    float? MapY,
    bool InBuilding
);

/// <summary>
/// Where the player is on the map, the same way as a fellow's MapX / MapY / InBuilding. The plugin works it out itself
/// outdoors; indoors it can't tell a building from a dungeon, so it asks.
/// </summary>
public record PlayerUiPosition(float? MapX, float? MapY, bool InBuilding);

/// <summary>
/// Whether the plugin shows the player's bank (@playerui bankwindow, Player.PlayerUiBankWindow), or their corpses and chests
/// (@playerui lootwindow, Player.PlayerUiLootWindow), in its own window instead of the game's.
/// </summary>
public record PlayerUiWindowSetting(bool On);

/// <summary>
/// The player's character stats, for the plugin's Character tab: the sections the Combat Focus appraisal shows at its
/// bottom (Player.GetStatsSheetSections), each with its title and lines; a section with nothing in it has no lines.
/// </summary>
public record PlayerUiCharacter(List<ACE.Server.WorldObjects.StatsSheetSection> Sections);

/// <summary>The player's contracts, for the plugin's Quests tab (PlayerUiQuestData).</summary>
public record PlayerUiQuests(List<PlayerUiQuest> Quests);

/// <summary>
/// One contract: its name and descriptions from the ContractTable, its stage (available, started, done), how many times
/// its progress quest is solved and how many it takes, seconds until its timer is done (DoneIn) and until it can be done
/// again (RepeatsIn), its NPCs, and where they and the quest area are on the map (null in a dungeon).
/// </summary>
public record PlayerUiQuest(
    uint Id,
    string Name,
    string Description,
    string Progress,
    string Stage,
    int? Solves,
    int? MaxSolves,
    double? DoneIn,
    double? RepeatsIn,
    string StartNpc,
    string EndNpc,
    PlayerUiPlace StartAt,
    PlayerUiPlace EndAt,
    PlayerUiPlace Area,
    bool CanAbandon
);

/// <summary>A spot on the map, in the game's map coordinates (east and north positive).</summary>
public record PlayerUiPlace(float MapX, float MapY);

/// <summary>
/// The player's bank, for the plugin's bank window. Open: they have a bank open and are close enough to use it (what every
/// /bank command but balance, log and intro needs). Loaded: its items have come from the database (a moment after opening).
/// Guid is the bank chest's, so the plugin can find its items in what the client was sent. Packs are numbered as /bank packs
/// and /bank inscribe number them.
/// </summary>
public record PlayerUiBank(
    bool Open,
    bool Loaded,
    uint Guid,
    int Used,
    int Capacity,
    long Coins,
    long Notes,
    int NoteCount,
    bool AutoSort,
    List<PlayerUiBankPack> Packs
);

/// <summary>
/// A pack in the bank. Tags is what it collects, in words (BankCommand.PackTags); Inscription is the text itself.
/// </summary>
public record PlayerUiBankPack(int Number, uint Guid, string Name, string Tags, string Inscription, int Used, int Capacity);

/// <summary>
/// The player's side of the market, for the plugin's Market tab: their account's listings, recent sales and expired
/// listings waiting to be claimed, and whether a Market Broker is near enough (PlayerUiMarket.BrokerRange) for the broker
/// actions. Fees are fractions (0.05 is 5%). Times are seconds from now: ExpiresIn ahead, SoldAgo and ExpiredAgo behind.
/// </summary>
public record PlayerUiMarket(
    bool NearBroker,
    string BrokerName,
    float? BrokerDistance,
    double PayoutFee,
    double CancelFee,
    long PendingPayout,
    List<PlayerUiListing> Listings,
    List<PlayerUiSale> Sales,
    List<PlayerUiExpired> Expired
);

/// <summary>An active listing. Id is the listing's own id, which "cancel" takes.</summary>
public record PlayerUiListing(int Id, string Name, uint WeenieClassId, int? Tier, int Price, double ExpiresIn, PlayerUiIcon Icon);

/// <summary>A sold listing. Payout is what the seller gets after the fee; Claimed: it's been collected from a broker.</summary>
public record PlayerUiSale(int Id, string Name, uint WeenieClassId, int Price, int Payout, bool Claimed, double SoldAgo, PlayerUiIcon Icon);

/// <summary>A listing that ran out unsold, whose item is waiting at the broker.</summary>
public record PlayerUiExpired(int Id, string Name, uint WeenieClassId, int Price, double ExpiredAgo, PlayerUiIcon Icon);

/// <summary>
/// A listed item's icon, as market search has it (MarketListing.IconHex, IconOverlayHex, IconUnderlay): portal.dat ids in
/// hex, the underlay as the kind of item ("Weapon", "Misc"...), and the effect that colours its outline (MarketListing.IconEffect).
/// Null when the listing has no snapshot to read it from.
/// </summary>
public record PlayerUiIcon(string Icon, string Overlay, string Underlay, string Effect);

/// <summary>
/// The arena as the player sees it, for the plugin's Arena tab. State is one of off, idle, queued, watching, defeated
/// (watching the rest of their duel), accepting, starting or fighting; Status says the same in words (ArenaManager.Status).
/// QueuePlace is 0 when they aren't queued. Boards are the ones they've fought a rated duel on. Duels can be watched;
/// Recent are the latest that ended, newest first.
/// </summary>
public record PlayerUiArena(
    bool Enabled,
    int MinimumLevel,
    int StartingRating,
    string State,
    string Status,
    int QueuePlace,
    int QueueCount,
    List<PlayerUiArenaBoard> Boards,
    List<PlayerUiArenaDuel> Duels,
    List<PlayerUiArenaResult> Recent,
    List<string> Maps
);

public record PlayerUiArenaBoard(string Name, int Size, bool Scaled, int Rating, int Wins, int Losses, int Draws);

/// <summary>A duel going on that can be watched. Sides are the fighters' names, "Alice vs Bob".</summary>
public record PlayerUiArenaDuel(int Id, string Sides, int Size, bool Scaled, bool Rated, string Map, string State);

/// <summary>A duel that ended. Winners is null for a draw or one called off.</summary>
public record PlayerUiArenaResult(int Id, string Sides, int Size, bool Scaled, bool Rated, string Map, string Winners, bool CalledOff, double EndedAgo);

/// <summary>
/// A corpse's or an open chest's items, for the plugin's loot rules. Status: "ok"; "gone" (not near enough to use, or not
/// there); "denied" (a corpse someone else's to loot, or a chest this player hasn't got open); "notcorpse" (neither);
/// "closed" (sent unasked when it closes, with the plugin's loot window on, Player.PlayerUiLootWindow; also sent unasked,
/// as the rest, when it opens). Item
/// is each item described the way the Market API describes a listing (ACE.MarketApi.MarketRepository.Describe), so the
/// rules can use the market search's filters.
/// </summary>
public record PlayerUiCorpse(uint Guid, string Status, string Name, List<PlayerUiCorpseItem> Items);

/// <summary>
/// An item on a corpse. Salvageable: salvaging could turn it into salvage (Player.IsSalvageable). RollQuality: where
/// each rolled stat landed in its range, 0 to 1, by the stat's name in appraisal's Roll Quality section
/// (LootGenerationFactory.GetItemRollRanges); null when nothing on it rolled.
/// </summary>
public record PlayerUiCorpseItem(
    uint Guid,
    string Name,
    int StackSize,
    int Value,
    int Burden,
    bool Salvageable,
    string Material,
    double? Workmanship,
    ACE.MarketApi.MarketListing Item,
    Dictionary<string, double> RollQuality
);

/// <summary>
/// One of the creatures the plugin asked about. Status says how much there is:
/// <list type="bullet">
/// <item>"ok": a creature near the player, with everything filled in.</item>
/// <item>"player": another player. Only the name is sent; the plugin shows the health the client already has.</item>
/// <item>"gone": not near the player (dead, despawned, too far away, in another instance) or not a creature. Only Guid is set.</item>
/// </list>
/// Threat is null when the creature doesn't keep a threat table.
/// </summary>
public record PlayerUiTarget(
    uint Guid,
    string Status,
    string Name,
    int Level,
    uint Health,
    uint MaxHealth,
    uint Stamina,
    uint MaxStamina,
    uint Mana,
    uint MaxMana,
    float? Distance,
    uint? AttackTarget,
    string AttackTargetName,
    List<PlayerUiEnchantment> Enchantments,
    List<PlayerUiThreat> Threat
);

/// <summary>
/// A spell on a creature: the one in effect in each spell category. SecondsLeft is -1 for one that never runs out.
/// </summary>
public record PlayerUiEnchantment(
    int SpellId,
    string Name,
    uint Level,
    string School,
    bool Beneficial,
    double SecondsLeft,
    uint CasterGuid,
    string CasterName,
    bool Yours,
    uint IconId
);

/// <summary>
/// A row of a monster's threat table: who it would like to attack, and how much.
/// </summary>
public record PlayerUiThreat(uint Guid, string Name, int Threat, bool IsYou, bool IsPlayer);
