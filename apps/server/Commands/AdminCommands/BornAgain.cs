using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class BornAgain
{
    // bornagain deletedCharID[, newCharName[, accountName]]
    [CommandHandler(
        "bornagain",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Restores a deleted character to an account.",
        "deletedCharID(, newCharName)(, accountName)\n"
            + "Given the ID of a deleted character, this command restores that character to its owner.  (You can find the ID of a deleted character using the @finger command.)\n"
            + "If the deleted character's name has since been taken by a new character, you can specify a new name for the restored character as the second parameter.  (You can find if the name has been taken by also using the @finger command.)  Use a comma to separate the arguments.\n"
            + "If needed, you can specify an account name as a third parameter if the character should be restored to an account other than its original owner.  Again, use a comma between the arguments."
    )]
    public static void HandleBornAgain(Session session, params string[] parameters)
    {
        // usage: @bornagain deletedCharID[, newCharName[, accountName]]
        // Given the ID of a deleted character, this command restores that character to its owner.  (You can find the ID of a deleted character using the @finger command.)
        // If the deleted character's name has since been taken by a new character, you can specify a new name for the restored character as the second parameter.  (You can find if the name has been taken by also using the @finger command.)  Use a comma to separate the arguments.
        // If needed, you can specify an account name as a third parameter if the character should be restored to an account other than its original owner.  Again, use a comma between the arguments.
        // @bornagain - Restores a deleted character to an account.

        var hexNumber = parameters[0];

        if (hexNumber.StartsWith("0x"))
        {
            hexNumber = hexNumber.Substring(2);
        }

        if (hexNumber.EndsWith(","))
        {
            hexNumber = hexNumber[..^1];
        }

        if (uint.TryParse(hexNumber, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var existingCharIID))
        {
            var args = string.Join(" ", parameters);

            if (args.Contains(','))
            {
                var twoCommas = args.Count(c => c == ',') == 2;

                var names = string.Join(" ", parameters).Split(",");

                var newCharName = names[1].TrimStart(' ').TrimEnd(' ');

                if (newCharName.StartsWith("+"))
                {
                    newCharName = newCharName.Substring(1);
                }

                newCharName = newCharName.First().ToString().ToUpper() + newCharName.Substring(1);

                string newAccountName;
                if (twoCommas)
                {
                    newAccountName = names[2].TrimStart(' ').TrimEnd(' ').ToLower();

                    var account = DatabaseManager.Authentication.GetAccountByName(newAccountName);

                    if (account == null)
                    {
                        CommandHandlerHelper.WriteOutputInfo(
                            session,
                            $"Error, cannot restore. Account \"{newAccountName}\" is not in database.",
                            ChatMessageType.Broadcast
                        );
                        return;
                    }

                    if (PlayerManager.IsAccountAtMaxCharacterSlots(account.AccountName))
                    {
                        CommandHandlerHelper.WriteOutputInfo(
                            session,
                            $"Error, cannot restore. Account \"{newAccountName}\" has no free character slots.",
                            ChatMessageType.Broadcast
                        );
                        return;
                    }

                    DoCopyChar(
                        session,
                        $"0x{existingCharIID:X8}",
                        existingCharIID,
                        true,
                        newCharName,
                        account.AccountId
                    );
                }
                else
                {
                    if (PlayerManager.IsAccountAtMaxCharacterSlots(session.Player.Account.AccountName))
                    {
                        CommandHandlerHelper.WriteOutputInfo(
                            session,
                            $"Error, cannot restore. Account \"{session.Player.Account.AccountName}\" has no free character slots.",
                            ChatMessageType.Broadcast
                        );
                        return;
                    }

                    DoCopyChar(session, $"0x{existingCharIID:X8}", existingCharIID, true, newCharName);
                }
            }
            else
            {
                if (PlayerManager.IsAccountAtMaxCharacterSlots(session.Player.Account.AccountName))
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Error, cannot restore. Account \"{session.Player.Account.AccountName}\" has no free character slots.",
                        ChatMessageType.Broadcast
                    );
                    return;
                }

                DoCopyChar(session, $"0x{existingCharIID:X8}", existingCharIID, true);
            }
        }
        else
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Error, cannot restore. You must include an existing character id in hex form.\nExample: @copychar 0x500000AC\n         @copychar 0x500000AC, Newly Restored\n         @copychar 0x500000AC, Newly Restored, differentaccount\n",
                ChatMessageType.Broadcast
            );
        }
    }

    private static void DoCopyChar(
        Session session,
        string existingCharName,
        uint existingCharId,
        bool isDeletedChar,
        string newCharacterName = null,
        uint newAccountId = 0
    )
    {
        DatabaseManager.Shard.GetCharacter(
            existingCharId,
            existingCharacter =>
            {
                if (existingCharacter != null)
                {
                    var newCharName = newCharacterName ?? existingCharacter.Name;

                    var existingPlayerBiota = DatabaseManager.Shard.BaseDatabase.GetBiota(existingCharId);

                    DatabaseManager.Shard.GetPossessedBiotasInParallel(
                        existingCharId,
                        existingPossessions =>
                        {
                            DatabaseManager.Shard.IsCharacterNameAvailable(
                                newCharName,
                                isAvailable =>
                                {
                                    if (!isAvailable)
                                    {
                                        CommandHandlerHelper.WriteOutputInfo(
                                            session,
                                            $"{newCharName} is not available to use for the {(isDeletedChar ? "restored" : "copied")} character name, try another name.",
                                            ChatMessageType.Broadcast
                                        );
                                        return;
                                    }

                                    var newPlayerGuid = GuidManager.NewPlayerGuid();

                                    var newCharacter = new Database.Models.Shard.Character
                                    {
                                        Id = newPlayerGuid.Full,
                                        AccountId = newAccountId > 0 ? newAccountId : session.Player.Account.AccountId,
                                        Name = newCharName,
                                        CharacterOptions1 = existingCharacter.CharacterOptions1,
                                        CharacterOptions2 = existingCharacter.CharacterOptions2,
                                        DefaultHairTexture = existingCharacter.DefaultHairTexture,
                                        GameplayOptions = existingCharacter.GameplayOptions,
                                        HairTexture = existingCharacter.HairTexture,
                                        IsPlussed = existingCharacter.IsPlussed,
                                        SpellbookFilters = existingCharacter.SpellbookFilters,
                                        TotalLogins = 1 // existingCharacter.TotalLogins
                                    };

                                    foreach (var entry in existingCharacter.CharacterPropertiesContractRegistry)
                                    {
                                        newCharacter.CharacterPropertiesContractRegistry.Add(
                                            new Database.Models.Shard.CharacterPropertiesContractRegistry
                                            {
                                                CharacterId = newPlayerGuid.Full,
                                                ContractId = entry.ContractId,
                                                DeleteContract = entry.DeleteContract,
                                                SetAsDisplayContract = entry.SetAsDisplayContract
                                            }
                                        );
                                    }

                                    foreach (var entry in existingCharacter.CharacterPropertiesFillCompBook)
                                    {
                                        newCharacter.CharacterPropertiesFillCompBook.Add(
                                            new Database.Models.Shard.CharacterPropertiesFillCompBook
                                            {
                                                CharacterId = newPlayerGuid.Full,
                                                QuantityToRebuy = entry.QuantityToRebuy,
                                                SpellComponentId = entry.SpellComponentId
                                            }
                                        );
                                    }

                                    foreach (var entry in existingCharacter.CharacterPropertiesFriendList)
                                    {
                                        newCharacter.CharacterPropertiesFriendList.Add(
                                            new Database.Models.Shard.CharacterPropertiesFriendList
                                            {
                                                CharacterId = newPlayerGuid.Full,
                                                FriendId = entry.FriendId
                                            }
                                        );
                                    }

                                    foreach (var entry in existingCharacter.CharacterPropertiesQuestRegistry)
                                    {
                                        newCharacter.CharacterPropertiesQuestRegistry.Add(
                                            new Database.Models.Shard.CharacterPropertiesQuestRegistry
                                            {
                                                CharacterId = newPlayerGuid.Full,
                                                LastTimeCompleted = entry.LastTimeCompleted,
                                                NumTimesCompleted = entry.NumTimesCompleted,
                                                QuestName = entry.QuestName
                                            }
                                        );
                                    }

                                    foreach (var entry in existingCharacter.CharacterPropertiesShortcutBar)
                                    {
                                        newCharacter.CharacterPropertiesShortcutBar.Add(
                                            new Database.Models.Shard.CharacterPropertiesShortcutBar
                                            {
                                                CharacterId = newPlayerGuid.Full,
                                                ShortcutBarIndex = entry.ShortcutBarIndex,
                                                ShortcutObjectId = entry.ShortcutObjectId
                                            }
                                        );
                                    }

                                    foreach (var entry in existingCharacter.CharacterPropertiesSpellBar)
                                    {
                                        newCharacter.CharacterPropertiesSpellBar.Add(
                                            new Database.Models.Shard.CharacterPropertiesSpellBar
                                            {
                                                CharacterId = newPlayerGuid.Full,
                                                SpellBarIndex = entry.SpellBarIndex,
                                                SpellBarNumber = entry.SpellBarNumber,
                                                SpellId = entry.SpellId
                                            }
                                        );
                                    }

                                    foreach (var entry in existingCharacter.CharacterPropertiesSquelch)
                                    {
                                        newCharacter.CharacterPropertiesSquelch.Add(
                                            new Database.Models.Shard.CharacterPropertiesSquelch
                                            {
                                                CharacterId = newPlayerGuid.Full,
                                                SquelchAccountId = entry.SquelchAccountId,
                                                SquelchCharacterId = entry.SquelchCharacterId,
                                                Type = entry.Type
                                            }
                                        );
                                    }

                                    foreach (var entry in existingCharacter.CharacterPropertiesTitleBook)
                                    {
                                        newCharacter.CharacterPropertiesTitleBook.Add(
                                            new Database.Models.Shard.CharacterPropertiesTitleBook
                                            {
                                                CharacterId = newPlayerGuid.Full,
                                                TitleId = entry.TitleId
                                            }
                                        );
                                    }

                                    var idSwaps = new ConcurrentDictionary<uint, uint>();

                                    var newPlayerBiota = Database.Adapter.BiotaConverter.ConvertToEntityBiota(
                                        existingPlayerBiota
                                    );

                                    idSwaps[newPlayerBiota.Id] = newPlayerGuid.Full;

                                    newPlayerBiota.Id = newPlayerGuid.Full;
                                    if (newPlayerBiota.PropertiesAllegiance != null)
                                    {
                                        newPlayerBiota.PropertiesAllegiance.Clear();
                                    }

                                    if (newPlayerBiota.HousePermissions != null)
                                    {
                                        newPlayerBiota.HousePermissions.Clear();
                                    }

                                    var newTempWieldedItems = new List<Biota>();
                                    foreach (var item in existingPossessions.WieldedItems)
                                    {
                                        var newItemBiota = Database.Adapter.BiotaConverter.ConvertToEntityBiota(item);
                                        var newGuid = GuidManager.NewDynamicGuid();
                                        idSwaps[newItemBiota.Id] = newGuid.Full;
                                        newItemBiota.Id = newGuid.Full;
                                        newTempWieldedItems.Add(newItemBiota);
                                    }

                                    var newTempInventoryItems = new List<Biota>();
                                    foreach (var item in existingPossessions.Inventory)
                                    {
                                        if (item.WeenieClassId == (uint)WeenieClassName.W_DEED_CLASS)
                                        {
                                            continue;
                                        }

                                        var newItemBiota = Database.Adapter.BiotaConverter.ConvertToEntityBiota(item);
                                        var newGuid = GuidManager.NewDynamicGuid();
                                        idSwaps[newItemBiota.Id] = newGuid.Full;
                                        newItemBiota.Id = newGuid.Full;
                                        newTempInventoryItems.Add(newItemBiota);
                                    }

                                    var newWieldedItems = new List<Database.Models.Shard.Biota>();
                                    foreach (var item in newTempWieldedItems)
                                    {
                                        if (item.PropertiesEnchantmentRegistry != null)
                                        {
                                            foreach (var entry in item.PropertiesEnchantmentRegistry)
                                            {
                                                if (idSwaps.ContainsKey(entry.CasterObjectId))
                                                {
                                                    entry.CasterObjectId = idSwaps[entry.CasterObjectId];
                                                }
                                            }
                                        }

                                        if (item.PropertiesIID != null)
                                        {
                                            if (
                                                item.PropertiesIID.TryGetValue(
                                                    PropertyInstanceId.Owner,
                                                    out var ownerId
                                                )
                                            )
                                            {
                                                if (idSwaps.ContainsKey(ownerId))
                                                {
                                                    item.PropertiesIID.Remove(PropertyInstanceId.Owner);
                                                    item.PropertiesIID.Add(PropertyInstanceId.Owner, idSwaps[ownerId]);
                                                }
                                            }

                                            if (
                                                item.PropertiesIID.TryGetValue(
                                                    PropertyInstanceId.Wielder,
                                                    out var wielderId
                                                )
                                            )
                                            {
                                                if (idSwaps.ContainsKey(wielderId))
                                                {
                                                    item.PropertiesIID.Remove(PropertyInstanceId.Wielder);
                                                    item.PropertiesIID.Add(
                                                        PropertyInstanceId.Wielder,
                                                        idSwaps[wielderId]
                                                    );
                                                }
                                            }

                                            if (
                                                item.PropertiesIID.TryGetValue(
                                                    PropertyInstanceId.AllowedActivator,
                                                    out var allowedActivatorId
                                                )
                                            )
                                            {
                                                if (idSwaps.ContainsKey(allowedActivatorId))
                                                {
                                                    item.PropertiesIID.Remove(PropertyInstanceId.AllowedActivator);
                                                    item.PropertiesIID.Add(
                                                        PropertyInstanceId.AllowedActivator,
                                                        idSwaps[allowedActivatorId]
                                                    );

                                                    item.PropertiesString.Remove(PropertyString.CraftsmanName);
                                                    item.PropertiesString.Add(
                                                        PropertyString.CraftsmanName,
                                                        newCharName
                                                    );
                                                }
                                            }

                                            if (
                                                item.PropertiesIID.TryGetValue(
                                                    PropertyInstanceId.AllowedWielder,
                                                    out var allowedWielderId
                                                )
                                            )
                                            {
                                                if (idSwaps.ContainsKey(allowedWielderId))
                                                {
                                                    item.PropertiesIID.Remove(PropertyInstanceId.AllowedWielder);
                                                    item.PropertiesIID.Add(
                                                        PropertyInstanceId.AllowedWielder,
                                                        idSwaps[allowedWielderId]
                                                    );

                                                    item.PropertiesString.Remove(PropertyString.CraftsmanName);
                                                    item.PropertiesString.Add(
                                                        PropertyString.CraftsmanName,
                                                        newCharName
                                                    );
                                                }
                                            }
                                        }

                                        newWieldedItems.Add(
                                            Database.Adapter.BiotaConverter.ConvertFromEntityBiota(item)
                                        );
                                    }

                                    var newInventoryItems = new List<Database.Models.Shard.Biota>();
                                    foreach (var item in newTempInventoryItems)
                                    {
                                        if (item.PropertiesEnchantmentRegistry != null)
                                        {
                                            foreach (var entry in item.PropertiesEnchantmentRegistry)
                                            {
                                                if (idSwaps.ContainsKey(entry.CasterObjectId))
                                                {
                                                    entry.CasterObjectId = idSwaps[entry.CasterObjectId];
                                                }
                                            }
                                        }

                                        if (item.PropertiesIID != null)
                                        {
                                            if (
                                                item.PropertiesIID.TryGetValue(
                                                    PropertyInstanceId.Owner,
                                                    out var ownerId
                                                )
                                            )
                                            {
                                                if (idSwaps.ContainsKey(ownerId))
                                                {
                                                    item.PropertiesIID.Remove(PropertyInstanceId.Owner);
                                                    item.PropertiesIID.Add(PropertyInstanceId.Owner, idSwaps[ownerId]);
                                                }
                                            }

                                            if (
                                                item.PropertiesIID.TryGetValue(
                                                    PropertyInstanceId.Container,
                                                    out var containerId
                                                )
                                            )
                                            {
                                                if (idSwaps.ContainsKey(containerId))
                                                {
                                                    item.PropertiesIID.Remove(PropertyInstanceId.Container);
                                                    item.PropertiesIID.Add(
                                                        PropertyInstanceId.Container,
                                                        idSwaps[containerId]
                                                    );
                                                }
                                            }

                                            if (
                                                item.PropertiesIID.TryGetValue(
                                                    PropertyInstanceId.AllowedActivator,
                                                    out var allowedActivatorId
                                                )
                                            )
                                            {
                                                if (idSwaps.ContainsKey(allowedActivatorId))
                                                {
                                                    item.PropertiesIID.Remove(PropertyInstanceId.AllowedActivator);
                                                    item.PropertiesIID.Add(
                                                        PropertyInstanceId.AllowedActivator,
                                                        idSwaps[allowedActivatorId]
                                                    );

                                                    item.PropertiesString.Remove(PropertyString.CraftsmanName);
                                                    item.PropertiesString.Add(
                                                        PropertyString.CraftsmanName,
                                                        $"{(existingCharacter.IsPlussed ? "+" : "")}{newCharName}"
                                                    );
                                                }
                                            }

                                            if (
                                                item.PropertiesIID.TryGetValue(
                                                    PropertyInstanceId.AllowedWielder,
                                                    out var allowedWielderId
                                                )
                                            )
                                            {
                                                if (idSwaps.ContainsKey(allowedWielderId))
                                                {
                                                    item.PropertiesIID.Remove(PropertyInstanceId.AllowedWielder);
                                                    item.PropertiesIID.Add(
                                                        PropertyInstanceId.AllowedWielder,
                                                        idSwaps[allowedWielderId]
                                                    );

                                                    item.PropertiesString.Remove(PropertyString.CraftsmanName);
                                                    item.PropertiesString.Add(
                                                        PropertyString.CraftsmanName,
                                                        $"{(existingCharacter.IsPlussed ? "+" : "")}{newCharName}"
                                                    );
                                                }
                                            }
                                        }

                                        newInventoryItems.Add(
                                            Database.Adapter.BiotaConverter.ConvertFromEntityBiota(item)
                                        );
                                    }

                                    Player newPlayer;
                                    if (newPlayerBiota.WeenieType == WeenieType.Admin)
                                    {
                                        newPlayer = new Admin(
                                            newPlayerBiota,
                                            newInventoryItems,
                                            newWieldedItems,
                                            newCharacter,
                                            session
                                        );
                                    }
                                    else if (newPlayerBiota.WeenieType == WeenieType.Sentinel)
                                    {
                                        newPlayer = new Sentinel(
                                            newPlayerBiota,
                                            newInventoryItems,
                                            newWieldedItems,
                                            newCharacter,
                                            session
                                        );
                                    }
                                    else
                                    {
                                        newPlayer = new Player(
                                            newPlayerBiota,
                                            newInventoryItems,
                                            newWieldedItems,
                                            newCharacter,
                                            session
                                        );
                                    }

                                    newPlayer.Name = newCharName;
                                    newPlayer.ChangesDetected = true;
                                    newPlayer.CharacterChangesDetected = true;

                                    newPlayer.Allegiance = null;
                                    newPlayer.AllegianceOfficerRank = null;
                                    newPlayer.MonarchId = null;
                                    newPlayer.PatronId = null;
                                    newPlayer.HouseId = null;
                                    newPlayer.HouseInstance = null;

                                    if (newPlayer.Character.CharacterPropertiesShortcutBar != null)
                                    {
                                        foreach (var entry in newPlayer.Character.CharacterPropertiesShortcutBar)
                                        {
                                            if (idSwaps.ContainsKey(entry.ShortcutObjectId))
                                            {
                                                entry.ShortcutObjectId = idSwaps[entry.ShortcutObjectId];
                                            }
                                        }
                                    }

                                    if (newPlayer.Biota.PropertiesEnchantmentRegistry != null)
                                    {
                                        foreach (var entry in newPlayer.Biota.PropertiesEnchantmentRegistry)
                                        {
                                            if (idSwaps.ContainsKey(entry.CasterObjectId))
                                            {
                                                entry.CasterObjectId = idSwaps[entry.CasterObjectId];
                                            }
                                        }
                                    }

                                    var possessions = newPlayer.GetAllPossessions();
                                    var possessedBiotas = new Collection<(Biota biota, ReaderWriterLockSlim rwLock)>();
                                    foreach (var possession in possessions)
                                    {
                                        possessedBiotas.Add((possession.Biota, possession.BiotaDatabaseLock));
                                    }

                                    // We must await here --
                                    DatabaseManager.Shard.AddCharacterInParallel(
                                        newPlayer.Biota,
                                        newPlayer.BiotaDatabaseLock,
                                        possessedBiotas,
                                        newPlayer.Character,
                                        newPlayer.CharacterDatabaseLock,
                                        saveSuccess =>
                                        {
                                            if (!saveSuccess)
                                            {
                                                //CommandHandlerHelper.WriteOutputInfo(session, $"Failed to copy the character \"{(existingCharacter.IsPlussed ? "+" : "")}{existingCharacter.Name}\" to a new character \"{newPlayer.Name}\" for the account \"{newPlayer.Account.AccountName}\"! Does the character exist _AND_ is not currently logged in? Is the new character name already taken, or is the account out of free character slots?", ChatMessageType.Broadcast);
                                                CommandHandlerHelper.WriteOutputInfo(
                                                    session,
                                                    $"Failed to {(isDeletedChar ? "restore" : "copy")} the character \"{(existingCharacter.IsPlussed ? "+" : "")}{existingCharacter.Name}\" to a new character \"{newPlayer.Name}\" for the account \"{newPlayer.Account.AccountName}\"! Does the character exist? Is the new character name already taken, or is the account out of free character slots?",
                                                    ChatMessageType.Broadcast
                                                );
                                                return;
                                            }

                                            PlayerManager.AddOfflinePlayer(newPlayer);

                                            if (newAccountId == 0)
                                            {
                                                session.Characters.Add(newPlayer.Character);
                                            }
                                            else
                                            {
                                                var foundActiveSession = Network.Managers.NetworkManager.Find(
                                                    newAccountId
                                                );

                                                if (foundActiveSession != null)
                                                {
                                                    foundActiveSession.Characters.Add(newPlayer.Character);
                                                }
                                            }

                                            var msg =
                                                $"Successfully {(isDeletedChar ? "restored" : "copied")} the character \"{(existingCharacter.IsPlussed ? "+" : "")}{existingCharacter.Name}\" to a new character \"{newPlayer.Name}\" for the account \"{newPlayer.Account.AccountName}\".";
                                            CommandHandlerHelper.WriteOutputInfo(
                                                session,
                                                msg,
                                                ChatMessageType.Broadcast
                                            );
                                            PlayerManager.BroadcastToAuditChannel(session.Player, msg);
                                        }
                                    );
                                }
                            );
                        }
                    );
                }
                else
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Failed to {(isDeletedChar ? "restore" : "copy")} the character \"{existingCharName}\" to a new character \"{newCharacterName}\" for the account \"{session.Account}\"! Does the character exist? Is the new character name already taken, or is the account out of free character slots?",
                        ChatMessageType.Broadcast
                    );
                }
            }
        );
    }
}
