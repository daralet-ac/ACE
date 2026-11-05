using System;
using System.Linq;
using System.Threading.Tasks;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.Actions;
using ACE.Server.Entity.AdminUi;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Commands.AdminCommands;

/// <summary>
/// @itemcopy and @itemrestore, the Daralet Admin plugin's Items tab's Copy and Restore: a new item, just like one that
/// exists (or existed), in your packs or an online player's. Every one is logged in the item history and the audit channel,
/// with who made it and from what.
/// </summary>
public class ItemCopy
{
    private static readonly ILogger _log = Log.ForContext(typeof(ItemCopy));

    // itemcopy
    [CommandHandler(
        "itemcopy",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Puts a copy of an item in your packs, or an online player's.",
        "<item guid> [character name]\nThe item can be anyone's, online or not, in a bank or on the market. Packs can't be copied. The copy is a new item with a guid of its own, and is logged in the item history."
    )]
    public static void HandleItemCopy(Session session, params string[] parameters)
    {
        if (!AdminUiItems.TryParseGuid(parameters[0], out var guid))
        {
            Reply(session, $"{parameters[0]} isn't a guid (0x8... as the game shows it).");
            return;
        }

        if (!TryFindRecipient(session, parameters, out var recipient))
        {
            return;
        }

        // an online player's is read from the game: the database may be minutes behind it
        foreach (var player in PlayerManager.GetAllOnline())
        {
            var item = player.GetAllPossessions().FirstOrDefault(i => i.Guid.Full == guid);

            if (item != null)
            {
                if (!ItemSnapshots.CanCopy(item.WeenieType, out var reason))
                {
                    Reply(session, reason);
                    return;
                }

                Give(session, recipient, ItemSnapshots.FromLive(item), ItemTransferLog.Copied,
                    player.Guid.Full, player.Name, player.Account?.AccountId, $"copy of 0x{guid:X8}");
                return;
            }
        }

        _ = Task.Run(() =>
        {
            string snapshot = null;
            uint? ownerId = null;
            WeenieType type = WeenieType.Undef;

            try
            {
                var biota = DatabaseManager.Shard.BaseDatabase.GetBiota(guid, true);

                if (biota != null)
                {
                    type = (WeenieType)biota.WeenieType;
                    snapshot = ItemSnapshots.FromDatabase(guid);
                    ownerId = biota.BiotaPropertiesIID
                        .Where(p => p.Type == (ushort)PropertyInstanceId.Wielder || p.Type == (ushort)PropertyInstanceId.Container)
                        .Select(p => (uint?)p.Value)
                        .FirstOrDefault(id => id != null && new ObjectGuid(id.Value).IsPlayer());
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "[ITEMS] @itemcopy couldn't read 0x{Guid:X8}.", guid);
            }

            WorldManager.EnqueueAction(new ActionEventDelegate(() =>
            {
                if (snapshot == null)
                {
                    Reply(session, $"There's no item 0x{guid:X8}.");
                    return;
                }

                if (!ItemSnapshots.CanCopy(type, out var reason))
                {
                    Reply(session, reason);
                    return;
                }

                var owner = ownerId != null ? PlayerManager.FindByGuid(ownerId.Value) : null;

                Give(session, recipient, snapshot, ItemTransferLog.Copied,
                    owner?.Guid.Full, owner?.Name, owner?.Account?.AccountId, $"copy of 0x{guid:X8}");
            }));
        });
    }

    // itemrestore
    [CommandHandler(
        "itemrestore",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Makes an item that was sold, salvaged or dropped again, from the item history, in your packs or an online player's.",
        "<history entry> [character name]\nThe entry's number is in the Daralet Admin plugin's Items tab, which has a Restore button for it. Only entries with a copy of the item can be restored: quest gear, quest items, rares, and loot of good workmanship for its tier (item_history_min_workmanship), sold, salvaged or dropped in the last item_history_destroyed_days. The restored item is a new one with a guid of its own, and is logged in the item history."
    )]
    public static void HandleItemRestore(Session session, params string[] parameters)
    {
        if (!ulong.TryParse(parameters[0].TrimStart('#'), out var entryId))
        {
            Reply(session, $"{parameters[0]} isn't a history entry number.");
            return;
        }

        if (!TryFindRecipient(session, parameters, out var recipient))
        {
            return;
        }

        _ = Task.Run(() =>
        {
            Database.Models.Shard.ItemTransfer entry = null;
            var failed = false;

            try
            {
                entry = DatabaseManager.Shard.BaseDatabase.GetItemTransfer(entryId);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "[ITEMS] @itemrestore couldn't read entry {Entry}.", entryId);
                failed = true;
            }

            WorldManager.EnqueueAction(new ActionEventDelegate(() =>
            {
                if (failed)
                {
                    Reply(session, "The item history couldn't be read. The server log has the details.");
                    return;
                }

                if (entry == null)
                {
                    Reply(session, $"There's no history entry #{entryId}.");
                    return;
                }

                if (string.IsNullOrEmpty(entry.Snapshot))
                {
                    Reply(session, $"Entry #{entryId} has no copy of {entry.ItemName} to restore it from: only quest gear, quest items, rares and loot of good workmanship for its tier (item_history_min_workmanship), sold, salvaged or dropped, have one, for item_history_destroyed_days.");
                    return;
                }

                // whoever had it: the seller, the salvager, the one who dropped it
                var owner = entry.FromCharacterId ?? entry.ToCharacterId;
                var ownerName = entry.FromCharacterId != null ? entry.FromName : entry.ToName;
                var ownerAccount = entry.FromCharacterId != null ? entry.FromAccountId : entry.ToAccountId;

                Give(session, recipient, entry.Snapshot, ItemTransferLog.Restored,
                    owner, ownerName, ownerAccount, $"from entry #{entryId} (0x{entry.ItemId:X8}, {entry.Kind})");
            }));
        });
    }

    /// <summary>
    /// Who gets the item: the character named after the guid or entry, who must be online, or you.
    /// </summary>
    private static bool TryFindRecipient(Session session, string[] parameters, out Player recipient)
    {
        recipient = session.Player;

        if (parameters.Length < 2)
        {
            return true;
        }

        var name = string.Join(" ", parameters.Skip(1));
        recipient = PlayerManager.GetOnlinePlayer(name);

        if (recipient == null)
        {
            Reply(session, $"{name} isn't online. The item goes into an online player's packs.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Makes the item from its snapshot and puts it in recipient's packs, then logs it. On the world thread.
    /// </summary>
    private static void Give(Session session, Player recipient, string snapshot, string kind, uint? fromId, string fromName,
        uint? fromAccount, string original)
    {
        var admin = session.Player;

        if (admin == null)
        {
            return;
        }

        if (recipient?.Session?.Player != recipient || recipient.IsLoggingOut)
        {
            Reply(session, "They've logged off.");
            return;
        }

        var item = snapshot == null ? null : ItemSnapshots.Recreate(snapshot);

        if (item == null)
        {
            Reply(session, "The item couldn't be made from its copy.");
            return;
        }

        if (!recipient.TryCreateInInventoryWithNetworking(item))
        {
            item.Destroy();
            Reply(session, $"{item.Name} doesn't fit in {(recipient == admin ? "your" : recipient.Name + "'s")} packs.");
            return;
        }

        ItemTransferLog.RecordCopy(kind, admin, item, recipient, fromId, fromName, fromAccount, original);

        var what = kind == ItemTransferLog.Restored ? "restored" : "copied";
        var stack = item.StackSize > 1 ? $"{item.StackSize:N0} " : "";

        PlayerManager.BroadcastToAuditChannel(admin, $"{admin.Name} {what} {stack}{item.Name} (0x{item.Guid.Full:X8}, {original}) into {recipient.Name}'s packs.");
        _log.Information("[ITEMS] {Admin} {What} {Item} (0x{Guid:X8}, {Original}) into {Recipient}'s packs.", admin.Name, what, item.Name, item.Guid.Full, original, recipient.Name);

        Reply(session, $"{(kind == ItemTransferLog.Restored ? "Restored" : "Copied")} {stack}{item.Name} into {(recipient == admin ? "your" : recipient.Name + "'s")} packs (0x{item.Guid.Full:X8}).");

        if (recipient != admin)
        {
            recipient.Session.Network.EnqueueSend(new Network.GameMessages.Messages.GameMessageSystemChat(
                $"{stack}{item.Name} has been put in your packs.", ChatMessageType.Broadcast));
        }
    }

    private static void Reply(Session session, string text)
    {
        if (session?.Player != null)
        {
            CommandHandlerHelper.WriteOutputInfo(session, text, ChatMessageType.Broadcast);
        }
    }
}
