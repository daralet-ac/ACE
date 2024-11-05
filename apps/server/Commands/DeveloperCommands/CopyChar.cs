using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace ACE.Server.Commands.DeveloperCommands;

public class CopyChar
{
    // copychar < character name >, < copy name >
    [CommandHandler(
        "copychar",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        2,
        "Copies an existing character into your character list.",
        "< Existing Character Name >, < New Character Name >\n"
            + "Given the name of an existing character \"character name\", this command makes a copy of that character with the name \"copy name\" and places it into your character list."
    )]
    public static void HandleCopychar(Session session, params string[] parameters)
    {
        // usage: @copychar < character name >, < copy name >
        // Given the name of an existing character "character name", this command makes a copy of that character with the name "copy name" and places it into your character list.
        // @copychar - Copies an existing character into your character list.

        if (!string.Join(" ", parameters).Contains(','))
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Error, cannot copy. You must include the existing character name followed by a comma and then the new name.\n Example: @copychar Old Name, New Name",
                ChatMessageType.Broadcast
            );
            return;
        }

        var names = string.Join(" ", parameters).Split(",");

        var existingCharName = names[0].TrimStart(' ').TrimEnd(' ');
        var newCharName = names[1].TrimStart(' ').TrimEnd(' ');

        if (existingCharName.StartsWith("+"))
        {
            existingCharName = existingCharName.Substring(1);
        }

        if (newCharName.StartsWith("+"))
        {
            newCharName = newCharName.Substring(1);
        }

        newCharName = newCharName.First().ToString().ToUpper() + newCharName.Substring(1);

        var existingPlayer = PlayerManager.FindByName(existingCharName);

        if (existingPlayer == null || session.Characters.Count >= PropertyManager.GetLong("max_chars_per_account").Item)
        {
            //CommandHandlerHelper.WriteOutputInfo(session, $"Failed to copy the character \"{existingCharName}\" to a new character \"{newCharName}\" for the account \"{session.Account}\"! Does the character exist _AND_ is not currently logged in? Is the new character name already taken, or is the account out of free character slots?", ChatMessageType.Broadcast);
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Failed to copy the character \"{existingCharName}\" to a new character \"{newCharName}\" for the account \"{session.Account}\"! Does the character exist? Is the new character name already taken, or is the account out of free character slots?",
                ChatMessageType.Broadcast
            );
            return;
        }

        DoCopyChar(session, existingCharName, existingPlayer.Guid.Full, false, newCharName);
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
