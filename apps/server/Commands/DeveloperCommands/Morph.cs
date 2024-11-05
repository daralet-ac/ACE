using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class Morph
{
    // morph
    [CommandHandler(
        "morph",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Morphs your bodily form into that of the specified creature. Be careful with this one!",
        "<wcid or weenie class name> [character name]"
    )]
    public static void HandleMorph(Session session, params string[] parameters)
    {
        // @morph - Morphs your bodily form into that of the specified creature. Be careful with this one!

        var weenieDesc = parameters[0];

        Weenie weenie = null;

        if (uint.TryParse(weenieDesc, out var wcid))
        {
            weenie = DatabaseManager.World.GetCachedWeenie(wcid);
        }
        else
        {
            weenie = DatabaseManager.World.GetCachedWeenie(weenieDesc);
        }

        if (weenie == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Weenie {weenieDesc} not found in database, unable to morph.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        if (
            weenie.WeenieType != WeenieType.Creature
            && weenie.WeenieType != WeenieType.Cow
            && weenie.WeenieType != WeenieType.Admin
            && weenie.WeenieType != WeenieType.Sentinel
            && weenie.WeenieType != WeenieType.Vendor
            && weenie.WeenieType != WeenieType.Pet
            && weenie.WeenieType != WeenieType.CombatPet
        )
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Weenie {weenie.GetProperty(PropertyString.Name)} ({weenieDesc}) is of WeenieType.{Enum.GetName(typeof(WeenieType), weenie.WeenieType)} ({weenie.WeenieType}), unable to morph because that is not allowed.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Morphing you into {weenie.GetProperty(PropertyString.Name)} ({weenieDesc})... You will be logged out.",
                ChatMessageType.Broadcast
            )
        );

        var guid = GuidManager.NewPlayerGuid();

        var player = new Player(weenie, guid, session.AccountId);

        player.Biota.WeenieType = session.Player.WeenieType;

        var name = string.Join(' ', parameters.Skip(1));
        if (parameters.Length > 1)
        {
            name = name.TrimStart('+').TrimStart(' ').TrimEnd(' ');
            player.Name = name;
            player.Character.Name = name;
        }
        else
        {
            name = weenie.GetProperty(PropertyString.Name);
        }

        DatabaseManager.Shard.IsCharacterNameAvailable(
            name,
            isAvailable =>
            {
                if (!isAvailable)
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"{name} is not available to use for the morphed character, try another name.",
                        ChatMessageType.Broadcast
                    );
                    return;
                }

                player.Location = session.Player.Location;

                player.Character.CharacterOptions1 = session.Player.Character.CharacterOptions1;
                player.Character.CharacterOptions2 = session.Player.Character.CharacterOptions2;

                if (weenie.PropertiesCreateList != null)
                {
                    var wearables = weenie
                        .PropertiesCreateList.Where(x =>
                            x.DestinationType == DestinationType.Wield
                            || x.DestinationType == DestinationType.WieldTreasure
                        )
                        .ToList();
                    foreach (var wearable in wearables)
                    {
                        var weenieOfWearable = DatabaseManager.World.GetCachedWeenie(wearable.WeenieClassId);

                        if (weenieOfWearable == null)
                        {
                            continue;
                        }

                        var worldObject = WorldObjectFactory.CreateNewWorldObject(weenieOfWearable);

                        if (worldObject == null)
                        {
                            continue;
                        }

                        if (wearable.Palette > 0)
                        {
                            worldObject.PaletteTemplate = wearable.Palette;
                        }

                        if (wearable.Shade > 0)
                        {
                            worldObject.Shade = wearable.Shade;
                        }

                        worldObject.CalculateObjDesc();

                        player.TryEquipObject(worldObject, worldObject.ValidLocations ?? 0);
                    }

                    var containables = weenie
                        .PropertiesCreateList.Where(x =>
                            x.DestinationType == DestinationType.Contain
                            || x.DestinationType == DestinationType.Shop
                            || x.DestinationType == DestinationType.Treasure
                            || x.DestinationType == DestinationType.ContainTreasure
                            || x.DestinationType == DestinationType.ShopTreasure
                        )
                        .ToList();
                    foreach (var containable in containables)
                    {
                        var weenieOfWearable = DatabaseManager.World.GetCachedWeenie(containable.WeenieClassId);

                        if (weenieOfWearable == null)
                        {
                            continue;
                        }

                        var worldObject = WorldObjectFactory.CreateNewWorldObject(weenieOfWearable);

                        if (worldObject == null)
                        {
                            continue;
                        }

                        if (containable.Palette > 0)
                        {
                            worldObject.PaletteTemplate = containable.Palette;
                        }

                        if (containable.Shade > 0)
                        {
                            worldObject.Shade = containable.Shade;
                        }

                        worldObject.CalculateObjDesc();

                        player.TryAddToInventory(worldObject);
                    }
                }

                player.GenerateNewFace();

                var possessions = player.GetAllPossessions();
                var possessedBiotas = new Collection<(Biota biota, ReaderWriterLockSlim rwLock)>();
                foreach (var possession in possessions)
                {
                    possessedBiotas.Add((possession.Biota, possession.BiotaDatabaseLock));
                }

                DatabaseManager.Shard.AddCharacterInParallel(
                    player.Biota,
                    player.BiotaDatabaseLock,
                    possessedBiotas,
                    player.Character,
                    player.CharacterDatabaseLock,
                    null
                );

                PlayerManager.AddOfflinePlayer(player);
                session.Characters.Add(player.Character);

                session.LogOffPlayer();
            }
        );
    }
}
