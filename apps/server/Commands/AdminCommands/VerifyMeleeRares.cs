using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Database.Models.Shard;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Factories;
using ACE.Server.Network;
using Microsoft.EntityFrameworkCore;

namespace ACE.Server.Commands.AdminCommands;

public class VerifyMeleeRares
{
    [CommandHandler(
        "verify-melee-rares",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Verifies and optionally fixes any melee rares to EoR wcids"
    )]
    public static void HandleFixMeleeRares(Session session, params string[] parameters)
    {
        var fix = parameters.Length > 0 && parameters[0].Equals("fix");
        var foundIssues = false;
        var foundRaresPre = 0;
        var foundRaresPost = 0;
        var foundRareCoins = 0;
        var deletedRareCoins = 0;
        var newRaresFromCoins = 0;
        var deletedPostRares = 0;
        var replacedPostRares = 0;
        var adjustedRaresPre = 0;
        var adjustedRaresPost = 0;
        var validPostRares = 0;
        var validPostRaresV1 = 0;

        using (var ctx = new WorldDbContext())
        {
            var minPatchVer = "v0.9.271";

            var worldDbVersion = ctx.Version.FirstOrDefault();

            if (worldDbVersion == null)
            {
                Console.WriteLine(
                    $"Unable to determine World Database version. Your World Database must be {minPatchVer} or higher to run this command."
                );
                return;
            }
            else
            {
                var currentPatchVer = worldDbVersion.PatchVersion;

                if (!currentPatchVer.StartsWith("v"))
                {
                    Console.WriteLine(
                        $"Unexpected patch version format found. Your World Database must be {minPatchVer} or higher to run this command."
                    );
                    return;
                }
                else
                {
                    var currentPatchVerSplit = currentPatchVer.TrimStart('v').Split('.');
                    var minPatchVerSplit = minPatchVer.TrimStart('v').Split('.');

                    _ = int.TryParse(minPatchVerSplit[0], out var minMajor);
                    _ = int.TryParse(minPatchVerSplit[1], out var minMinor);
                    _ = int.TryParse(minPatchVerSplit[2], out var minBuild);

                    if (!int.TryParse(currentPatchVerSplit[0], out var curMajor) || curMajor < minMajor)
                    {
                        Console.WriteLine(
                            $"World Database must be {minPatchVer} or higher to run this command. Your current World Database patch version is: {currentPatchVer}"
                        );
                        return;
                    }
                    else if (!int.TryParse(currentPatchVerSplit[1], out var curMinor) || curMinor < minMinor)
                    {
                        Console.WriteLine(
                            $"World Database must be {minPatchVer} or higher to run this command. Your current World Database patch version is: {currentPatchVer}"
                        );
                        return;
                    }
                    else if (!int.TryParse(currentPatchVerSplit[2], out var curBuild) || curBuild < minBuild)
                    {
                        if (currentPatchVerSplit[2].Contains('-'))
                        {
                            var currentBuildSplit = currentPatchVerSplit[2].Split('-');

                            if (!int.TryParse(currentBuildSplit[0], out curBuild) || curBuild < minBuild)
                            {
                                Console.WriteLine(
                                    $"World Database must be {minPatchVer} or higher to run this command. Your current World Database patch version is: {currentPatchVer}"
                                );
                                return;
                            }
                            else
                            {
                                // all good here
                            }
                        }
                        else
                        {
                            Console.WriteLine(
                                $"World Database must be {minPatchVer} or higher to run this command. Your current World Database patch version is: {currentPatchVer}"
                            );
                            return;
                        }
                    }
                    else
                    {
                        // all good here
                    }
                }
            }
        }

        using (var ctx = new ShardDbContext())
        {
            ctx.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            // get preMoA rares
            //var preRares = ctx.Biota.Where(i => i.WeenieClassId >= 30310 && i.WeenieClassId <= 30344).Select(i => i.Id).ToList();
            var queryPreRares =
                from biota in ctx.Biota
                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                where
                    name.Type == (ushort)PropertyString.Name
                    && biota.WeenieClassId >= 30310
                    && biota.WeenieClassId <= 30344
                // from version in ctx.BiotaPropertiesInt.Where(x => x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.Version).DefaultIfEmpty()
                from container in ctx
                    .BiotaPropertiesIID.Where(x =>
                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInstanceId.Container
                    )
                    .DefaultIfEmpty()
                from placement in ctx
                    .BiotaPropertiesInt.Where(x =>
                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.PlacementPosition
                    )
                    .DefaultIfEmpty()
                from wielder in ctx
                    .BiotaPropertiesIID.Where(x =>
                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInstanceId.Wielder
                    )
                    .DefaultIfEmpty()
                from wieldedlocation in ctx
                    .BiotaPropertiesInt.Where(x =>
                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.CurrentWieldedLocation
                    )
                    .DefaultIfEmpty()
                from location in ctx
                    .BiotaPropertiesPosition.Where(x =>
                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                    )
                    .DefaultIfEmpty()
                select new
                {
                    Biota = biota,
                    Name = name,
                    // Version = version ?? null,
                    Container = container ?? null,
                    PlacementPosition = placement ?? null,
                    Wielder = wielder ?? null,
                    CurrentWieldedLocation = wieldedlocation ?? null,
                    Location = location ?? null,
                };
            var preRares = queryPreRares.ToDictionary(i => i.Biota.Id, i => i);
            foundRaresPre = preRares.Count;

            // get PostMoA rares
            //var postRares = ctx.Biota.Where(i => i.WeenieClassId >= 45436 && i.WeenieClassId <= 45470).Select(i => i.Id).ToList();
            var queryPostMoA =
                from biota in ctx.Biota
                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                where
                    name.Type == (ushort)PropertyString.Name
                    && biota.WeenieClassId >= 45436
                    && biota.WeenieClassId <= 45470
                from version in ctx
                    .BiotaPropertiesInt.Where(x => x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.Version)
                    .DefaultIfEmpty()
                from container in ctx
                    .BiotaPropertiesIID.Where(x =>
                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInstanceId.Container
                    )
                    .DefaultIfEmpty()
                from placement in ctx
                    .BiotaPropertiesInt.Where(x =>
                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.PlacementPosition
                    )
                    .DefaultIfEmpty()
                from wielder in ctx
                    .BiotaPropertiesIID.Where(x =>
                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInstanceId.Wielder
                    )
                    .DefaultIfEmpty()
                from wieldedlocation in ctx
                    .BiotaPropertiesInt.Where(x =>
                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.CurrentWieldedLocation
                    )
                    .DefaultIfEmpty()
                from location in ctx
                    .BiotaPropertiesPosition.Where(x =>
                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                    )
                    .DefaultIfEmpty()
                from shortcut in ctx
                    .CharacterPropertiesShortcutBar.Where(x => x.ShortcutObjectId == biota.Id)
                    .DefaultIfEmpty()
                select new
                {
                    Biota = biota,
                    Name = name,
                    Version = version ?? null,
                    Container = container ?? null,
                    PlacementPosition = placement ?? null,
                    Wielder = wielder ?? null,
                    CurrentWieldedLocation = wieldedlocation ?? null,
                    Location = location ?? null,
                    Shortcut = shortcut ?? null,
                };
            var postRares = queryPostMoA.ToDictionary(i => i.Biota.Id, i => i);
            foundRaresPost = postRares.Count;

            // get Rare Coins
            //var queryRareCoin = ctx.Biota.Where(i => i.WeenieClassId == 45493).Select(i => i.Id).ToList();
            var queryRareCoin =
                from biota in ctx.Biota
                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                where name.Type == (ushort)PropertyString.Name && biota.WeenieClassId == 45493
                from container in ctx
                    .BiotaPropertiesIID.Where(x =>
                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInstanceId.Container
                    )
                    .DefaultIfEmpty()
                from placement in ctx
                    .BiotaPropertiesInt.Where(x =>
                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.PlacementPosition
                    )
                    .DefaultIfEmpty()
                from location in ctx
                    .BiotaPropertiesPosition.Where(x =>
                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                    )
                    .DefaultIfEmpty()
                from shortcut in ctx
                    .CharacterPropertiesShortcutBar.Where(x => x.ShortcutObjectId == biota.Id)
                    .DefaultIfEmpty()
                select new
                {
                    Biota = biota,
                    Name = name,
                    Container = container ?? null,
                    PlacementPosition = placement ?? null,
                    Location = location ?? null,
                    Shortcut = shortcut ?? null,
                };
            var rareCoins = queryRareCoin.ToDictionary(i => i.Biota.Id, i => i);
            foundRareCoins = rareCoins.Count;

            foreach (var rare in postRares)
            {
                if (rare.Value.Version is not null && rare.Value.Version.Value >= 2)
                {
                    validPostRares++;
                    continue;
                }

                if (rare.Value.Biota.WeenieClassId == 45461)
                {
                    validPostRaresV1++;
                }

                foundIssues = true;

                var logline = $"0x{rare.Key:X8} {rare.Value.Name.Value} ({rare.Value.Biota.WeenieClassId}) is";

                if (rare.Value.Container is not null)
                {
                    logline += " contained by: ";

                    var queryContainer =
                        from biota in ctx.Biota
                        join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                        where name.Type == (ushort)PropertyString.Name && biota.Id == rare.Value.Container.Value
                        from placement in ctx
                            .BiotaPropertiesInt.Where(x =>
                                x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.PlacementPosition
                            )
                            .DefaultIfEmpty()
                        from parentcontainer in ctx
                            .BiotaPropertiesIID.Where(x =>
                                x.ObjectId == biota.Id && x.Type == (ushort)PropertyInstanceId.Container
                            )
                            .DefaultIfEmpty()
                        select new
                        {
                            Biota = biota,
                            Name = name,
                            PlacementPosition = placement ?? null,
                            ParentContainer = parentcontainer ?? null,
                        };

                    var container = queryContainer.FirstOrDefault();

                    if (container is null)
                    {
                        logline += $"\n Unable to find 0x{rare.Value.Container.Value} in database. Orphaned object?";
                    }
                    else
                    {
                        if (container.Biota.WeenieType == (int)WeenieType.Container)
                        {
                            var queryParentContainer =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where
                                    name.Type == (ushort)PropertyString.Name
                                    && biota.Id == container.ParentContainer.Value
                                select new { Biota = biota, Name = name, };

                            var parentContainer = queryParentContainer.FirstOrDefault();

                            logline +=
                                $"\n 0x{parentContainer.Biota.Id:X8} {parentContainer.Name.Value}{(parentContainer.Biota.WeenieType == (int)WeenieType.Storage ? "" : "'s")} Sub Pack 0x{container.Biota.Id:X8} {container.Name.Value} (Slot {container.PlacementPosition?.Value ?? 0}) at placement position {rare.Value.PlacementPosition?.Value ?? 0}";
                        }
                        else if (container.Biota.WeenieType == (int)WeenieType.Hook)
                        {
                            var queryHook =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where name.Type == (ushort)PropertyString.Name && biota.Id == rare.Value.Container.Value
                                from hooktype in ctx
                                    .BiotaPropertiesInt.Where(x =>
                                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.HookType
                                    )
                                    .DefaultIfEmpty()
                                from location in ctx
                                    .BiotaPropertiesPosition.Where(x =>
                                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                                    )
                                    .DefaultIfEmpty()
                                select new
                                {
                                    Biota = biota,
                                    Name = name,
                                    HookType = hooktype,
                                    Location = location ?? null,
                                };

                            var hook = queryHook.FirstOrDefault();

                            logline +=
                                $"\n 0x{container.Biota.Id:X8} {(HookType)hook.HookType.Value} Hook located at:\n 0x{hook.Location.ObjCellId:X8} [{hook.Location.OriginX:F6} {hook.Location.OriginY:F6} {hook.Location.OriginZ:F6}] {hook.Location.AnglesW:F6} {hook.Location.AnglesX:F6} {hook.Location.AnglesY:F6} {hook.Location.AnglesZ:F6}";
                        }
                        else if (
                            container.Biota.WeenieType == (int)WeenieType.Storage
                            || container.Biota.WeenieType == (int)WeenieType.Chest
                            || container.Biota.WeenieType == (int)WeenieType.SlumLord
                        )
                        {
                            var queryStorage =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where name.Type == (ushort)PropertyString.Name && biota.Id == rare.Value.Container.Value
                                from location in ctx
                                    .BiotaPropertiesPosition.Where(x =>
                                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                                    )
                                    .DefaultIfEmpty()
                                select new
                                {
                                    Biota = biota,
                                    Name = name,
                                    Location = location ?? null,
                                };

                            var storage = queryStorage.FirstOrDefault();

                            logline +=
                                $"\n 0x{container.Biota.Id:X8} {container.Name.Value} Main Pack at placement position {rare.Value.PlacementPosition?.Value ?? 0} and located at:\n 0x{storage.Location.ObjCellId:X8} [{storage.Location.OriginX:F6} {storage.Location.OriginY:F6} {storage.Location.OriginZ:F6}] {storage.Location.AnglesW:F6} {storage.Location.AnglesX:F6} {storage.Location.AnglesY:F6} {storage.Location.AnglesZ:F6}";
                        }
                        else if (container.Biota.WeenieType == (int)WeenieType.Corpse)
                        {
                            var queryCorpse =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where name.Type == (ushort)PropertyString.Name && biota.Id == rare.Value.Container.Value
                                from location in ctx
                                    .BiotaPropertiesPosition.Where(x =>
                                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                                    )
                                    .DefaultIfEmpty()
                                select new
                                {
                                    Biota = biota,
                                    Name = name,
                                    Location = location ?? null
                                };

                            var corpse = queryCorpse.FirstOrDefault();

                            logline +=
                                $"\n 0x{corpse.Biota.Id:X8} {corpse.Name.Value}'s Main Pack 0x{container.Biota.Id:X8} at placement position {rare.Value.PlacementPosition?.Value ?? 0} and located at:\n 0x{corpse.Location.ObjCellId:X8} [{corpse.Location.OriginX:F6} {corpse.Location.OriginY:F6} {corpse.Location.OriginZ:F6}] {corpse.Location.AnglesW:F6} {corpse.Location.AnglesX:F6} {corpse.Location.AnglesY:F6} {corpse.Location.AnglesZ:F6}";
                        }
                        else if (
                            container.Biota.WeenieType == (int)WeenieType.Creature
                            || container.Biota.WeenieType == (int)WeenieType.Admin
                            || container.Biota.WeenieType == (int)WeenieType.Sentinel
                            || container.Biota.WeenieType == (int)WeenieType.Cow
                            || container.Biota.WeenieType == (int)WeenieType.Pet
                            || container.Biota.WeenieType == (int)WeenieType.CombatPet
                            || container.Biota.WeenieType == (int)WeenieType.Vendor
                        )
                        {
                            logline +=
                                $"\n 0x{container.Biota.Id:X8} {container.Name.Value}'s Main Pack at placement position {rare.Value.PlacementPosition?.Value ?? 0}";
                        }
                        else
                        {
                            logline +=
                                $"\n Unexpected WeenieType '{container.Biota.WeenieType}' for container 0x{container.Biota.Id:X8} {container.Name.Value}. Invalid custom content?";
                        }
                    }
                }
                else if (rare.Value.Wielder is not null)
                {
                    logline += " wielded by: ";

                    var queryWielder =
                        from biota in ctx.Biota
                        join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                        where name.Type == (ushort)PropertyString.Name && biota.Id == rare.Value.Wielder.Value
                        select new { Biota = biota, Name = name, };

                    var wielder = queryWielder.FirstOrDefault();

                    logline +=
                        $"\n 0x{wielder.Biota.Id:X8} {wielder.Name.Value} in the {(EquipMask)rare.Value.CurrentWieldedLocation.Value} slot";
                }
                else if (rare.Value.Location is not null)
                {
                    logline +=
                        $" on a landblock and located at:\n 0x{rare.Value.Location.ObjCellId:X8} [{rare.Value.Location.OriginX:F6} {rare.Value.Location.OriginY:F6} {rare.Value.Location.OriginZ:F6}] {rare.Value.Location.AnglesW:F6} {rare.Value.Location.AnglesX:F6} {rare.Value.Location.AnglesY:F6} {rare.Value.Location.AnglesZ:F6}";
                }
                else
                {
                    logline +=
                        $" found in database but does not have a Container, Wielder, or Location. Orphaned object?";
                }

                if (fix)
                {
                    if (rare.Value.Biota.WeenieClassId == 45461)
                    {
                        // this rare requires no swap, it was always valid, so we'll update to version 2 and move on.

                        var version = rare.Value.Biota.BiotaPropertiesInt.FirstOrDefault(x =>
                            x.Type == (int)PropertyInt.Version
                        );

                        if (version == null)
                        {
                            rare.Value.Biota.BiotaPropertiesInt.Add(
                                new BiotaPropertiesInt
                                {
                                    ObjectId = rare.Value.Biota.Id,
                                    Type = (int)PropertyInt.Version,
                                    Value = 2
                                }
                            );
                        }
                        else
                        {
                            version.Value = 2;
                        }

                        adjustedRaresPost++;

                        logline += "\n\\----- Updated Version, no other changes required.";
                    }
                    else
                    {
                        // this rare was purchased from the Melee Rare Vendor. Generate a new v2 rare, copy over "link" data to effectively mutate in place "illegally" swapped rare for another randomly generated V2 rare that is also not the same as the one it started out as.

                        var tierRares = PreToPostMeleeRareConversions.Values.ToList();

                        tierRares.Remove(rare.Value.Biota.WeenieClassId);

                        var rng = ThreadSafeRandom.Next(0, tierRares.Count - 1);

                        var rareWCID = tierRares[rng];

                        var wo = WorldObjectFactory.CreateNewWorldObject(rareWCID);

                        if (wo != null)
                        {
                            if (rare.Value.Container != null)
                            {
                                wo.ContainerId = rare.Value.Container.Value;
                            }

                            if (rare.Value.PlacementPosition != null)
                            {
                                wo.PlacementPosition = rare.Value.PlacementPosition.Value;
                            }

                            if (rare.Value.Wielder != null)
                            {
                                wo.WielderId = rare.Value.Wielder.Value;
                            }

                            if (rare.Value.CurrentWieldedLocation != null)
                            {
                                wo.CurrentWieldedLocation = (EquipMask)rare.Value.CurrentWieldedLocation.Value;
                            }

                            if (rare.Value.Location != null)
                            {
                                wo.Location = new ACE.Entity.Position(
                                    rare.Value.Location.ObjCellId,
                                    rare.Value.Location.OriginX,
                                    rare.Value.Location.OriginY,
                                    rare.Value.Location.OriginZ,
                                    rare.Value.Location.AnglesX,
                                    rare.Value.Location.AnglesY,
                                    rare.Value.Location.AnglesZ,
                                    rare.Value.Location.AnglesW
                                );
                            }

                            if (rare.Value.Shortcut != null)
                            {
                                var shortcut = ctx
                                    .CharacterPropertiesShortcutBar.Where(x =>
                                        x.ShortcutObjectId == rare.Value.Biota.Id
                                    )
                                    .FirstOrDefault();

                                if (shortcut != null)
                                {
                                    shortcut.ShortcutObjectId = wo.Biota.Id;
                                }
                            }

                            wo.SaveBiotaToDatabase();
                            replacedPostRares++;

                            ctx.Biota.Remove(rare.Value.Biota);
                            deletedPostRares++;

                            logline += $"\n\\----- Replaced with 0x{wo.Guid} {wo.Name} ({wo.WeenieClassId})";
                        }
                        else
                        {
                            logline += $"\n\\----- Unable to replace with ({rareWCID}). Rare not found in database.";
                            return;
                        }
                    }

                    ctx.SaveChanges();
                }
                else
                {
                    if (rare.Value.Biota.WeenieClassId == 45461)
                    {
                        logline += "\n\\----- Version needs updating, no other changes required.";
                    }
                    else
                    {
                        logline +=
                            $"\n\\----- Rare obtained from Melee Rare Vendor, needs to be deleted and replaced with a random newly generated melee rare.";
                    }
                }

                Console.WriteLine(logline);
            }

            foreach (var rare in preRares)
            {
                foundIssues = true;

                var logline = $"0x{rare.Key:X8} {rare.Value.Name.Value} ({rare.Value.Biota.WeenieClassId}) is";

                if (rare.Value.Container is not null)
                {
                    logline += " contained by: ";

                    var queryContainer =
                        from biota in ctx.Biota
                        join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                        where name.Type == (ushort)PropertyString.Name && biota.Id == rare.Value.Container.Value
                        from placement in ctx
                            .BiotaPropertiesInt.Where(x =>
                                x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.PlacementPosition
                            )
                            .DefaultIfEmpty()
                        from parentcontainer in ctx
                            .BiotaPropertiesIID.Where(x =>
                                x.ObjectId == biota.Id && x.Type == (ushort)PropertyInstanceId.Container
                            )
                            .DefaultIfEmpty()
                        select new
                        {
                            Biota = biota,
                            Name = name,
                            PlacementPosition = placement ?? null,
                            ParentContainer = parentcontainer ?? null,
                        };

                    var container = queryContainer.FirstOrDefault();

                    if (container is null)
                    {
                        logline += $"\n Unable to find 0x{rare.Value.Container.Value} in database. Orphaned object?";
                    }
                    else
                    {
                        if (container.Biota.WeenieType == (int)WeenieType.Container)
                        {
                            var queryParentContainer =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where
                                    name.Type == (ushort)PropertyString.Name
                                    && biota.Id == container.ParentContainer.Value
                                select new { Biota = biota, Name = name, };

                            var parentContainer = queryParentContainer.FirstOrDefault();

                            logline +=
                                $"\n 0x{parentContainer.Biota.Id:X8} {parentContainer.Name.Value}{(parentContainer.Biota.WeenieType == (int)WeenieType.Storage ? "" : "'s")} Sub Pack 0x{container.Biota.Id:X8} {container.Name.Value} (Slot {container.PlacementPosition?.Value ?? 0}) at placement position {rare.Value.PlacementPosition?.Value ?? 0}";
                        }
                        else if (container.Biota.WeenieType == (int)WeenieType.Hook)
                        {
                            var queryHook =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where name.Type == (ushort)PropertyString.Name && biota.Id == rare.Value.Container.Value
                                from hooktype in ctx
                                    .BiotaPropertiesInt.Where(x =>
                                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.HookType
                                    )
                                    .DefaultIfEmpty()
                                from location in ctx
                                    .BiotaPropertiesPosition.Where(x =>
                                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                                    )
                                    .DefaultIfEmpty()
                                select new
                                {
                                    Biota = biota,
                                    Name = name,
                                    HookType = hooktype,
                                    Location = location ?? null,
                                };

                            var hook = queryHook.FirstOrDefault();

                            logline +=
                                $"\n 0x{container.Biota.Id:X8} {(HookType)hook.HookType.Value} Hook located at:\n 0x{hook.Location.ObjCellId:X8} [{hook.Location.OriginX:F6} {hook.Location.OriginY:F6} {hook.Location.OriginZ:F6}] {hook.Location.AnglesW:F6} {hook.Location.AnglesX:F6} {hook.Location.AnglesY:F6} {hook.Location.AnglesZ:F6}";
                        }
                        else if (
                            container.Biota.WeenieType == (int)WeenieType.Storage
                            || container.Biota.WeenieType == (int)WeenieType.Chest
                            || container.Biota.WeenieType == (int)WeenieType.SlumLord
                        )
                        {
                            var queryStorage =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where name.Type == (ushort)PropertyString.Name && biota.Id == rare.Value.Container.Value
                                from location in ctx
                                    .BiotaPropertiesPosition.Where(x =>
                                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                                    )
                                    .DefaultIfEmpty()
                                select new
                                {
                                    Biota = biota,
                                    Name = name,
                                    Location = location ?? null,
                                };

                            var storage = queryStorage.FirstOrDefault();

                            logline +=
                                $"\n 0x{container.Biota.Id:X8} {container.Name.Value} Main Pack at placement position {rare.Value.PlacementPosition?.Value ?? 0} and located at:\n 0x{storage.Location.ObjCellId:X8} [{storage.Location.OriginX:F6} {storage.Location.OriginY:F6} {storage.Location.OriginZ:F6}] {storage.Location.AnglesW:F6} {storage.Location.AnglesX:F6} {storage.Location.AnglesY:F6} {storage.Location.AnglesZ:F6}";
                        }
                        else if (container.Biota.WeenieType == (int)WeenieType.Corpse)
                        {
                            var queryCorpse =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where name.Type == (ushort)PropertyString.Name && biota.Id == rare.Value.Container.Value
                                from location in ctx
                                    .BiotaPropertiesPosition.Where(x =>
                                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                                    )
                                    .DefaultIfEmpty()
                                select new
                                {
                                    Biota = biota,
                                    Name = name,
                                    Location = location ?? null
                                };

                            var corpse = queryCorpse.FirstOrDefault();

                            logline +=
                                $"\n 0x{corpse.Biota.Id:X8} {corpse.Name.Value}'s Main Pack 0x{container.Biota.Id:X8} at placement position {rare.Value.PlacementPosition?.Value ?? 0} and located at:\n 0x{corpse.Location.ObjCellId:X8} [{corpse.Location.OriginX:F6} {corpse.Location.OriginY:F6} {corpse.Location.OriginZ:F6}] {corpse.Location.AnglesW:F6} {corpse.Location.AnglesX:F6} {corpse.Location.AnglesY:F6} {corpse.Location.AnglesZ:F6}";
                        }
                        else if (
                            container.Biota.WeenieType == (int)WeenieType.Creature
                            || container.Biota.WeenieType == (int)WeenieType.Admin
                            || container.Biota.WeenieType == (int)WeenieType.Sentinel
                            || container.Biota.WeenieType == (int)WeenieType.Cow
                            || container.Biota.WeenieType == (int)WeenieType.Pet
                            || container.Biota.WeenieType == (int)WeenieType.CombatPet
                            || container.Biota.WeenieType == (int)WeenieType.Vendor
                        )
                        {
                            logline +=
                                $"\n 0x{container.Biota.Id:X8} {container.Name.Value}'s Main Pack at placement position {rare.Value.PlacementPosition?.Value ?? 0}";
                        }
                        else
                        {
                            logline +=
                                $"\n Unexpected WeenieType '{container.Biota.WeenieType}' for container 0x{container.Biota.Id:X8} {container.Name.Value}. Invalid custom content?";
                        }
                    }
                }
                else if (rare.Value.Wielder is not null)
                {
                    logline += " wielded by: ";

                    var queryWielder =
                        from biota in ctx.Biota
                        join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                        where name.Type == (ushort)PropertyString.Name && biota.Id == rare.Value.Wielder.Value
                        select new { Biota = biota, Name = name, };

                    var wielder = queryWielder.FirstOrDefault();

                    logline +=
                        $"\n 0x{wielder.Biota.Id:X8} {wielder.Name.Value} in the {(EquipMask)rare.Value.CurrentWieldedLocation.Value} slot";
                }
                else if (rare.Value.Location is not null)
                {
                    logline +=
                        $" on a landblock and located at:\n 0x{rare.Value.Location.ObjCellId:X8} [{rare.Value.Location.OriginX:F6} {rare.Value.Location.OriginY:F6} {rare.Value.Location.OriginZ:F6}] {rare.Value.Location.AnglesW:F6} {rare.Value.Location.AnglesX:F6} {rare.Value.Location.AnglesY:F6} {rare.Value.Location.AnglesZ:F6}";
                }
                else
                {
                    logline +=
                        $" found in database but does not have a Container, Wielder, or Location. Orphaned object?";
                }

                if (fix)
                {
                    // this is a preMoA rare and requires updating its WCID to a postMoA WCID, version 2 with no other changes.

                    var newWCIDFound = PreToPostMeleeRareConversions.TryGetValue(
                        rare.Value.Biota.WeenieClassId,
                        out var newWCID
                    );

                    if (newWCIDFound)
                    {
                        var oldWCID = rare.Value.Biota.WeenieClassId;

                        rare.Value.Biota.WeenieClassId = newWCID;

                        var version = rare.Value.Biota.BiotaPropertiesInt.FirstOrDefault(x =>
                            x.Type == (int)PropertyInt.Version
                        );

                        if (version == null)
                        {
                            rare.Value.Biota.BiotaPropertiesInt.Add(
                                new BiotaPropertiesInt
                                {
                                    ObjectId = rare.Value.Biota.Id,
                                    Type = (int)PropertyInt.Version,
                                    Value = 2
                                }
                            );
                        }
                        else
                        {
                            version.Value = 2;
                        }

                        adjustedRaresPre++;

                        logline +=
                            $"\n\\----- Updated WCID from {oldWCID} to {newWCID} and Updated Version, no other changes required.";

                        ctx.SaveChanges();
                    }
                    else
                    {
                        logline +=
                            $"\n\\----- Unable to change WCID from {rare.Value.Biota.WeenieClassId} for 0x{rare.Key:X8}. Not a melee rare?";
                    }
                }
                else
                {
                    var oldWCID = rare.Value.Biota.WeenieClassId;
                    PreToPostMeleeRareConversions.TryGetValue(rare.Value.Biota.WeenieClassId, out var newWCID);
                    logline +=
                        $"\n\\----- Rare needs to change WCID from {oldWCID} to {newWCID} and have its version updated.";
                }

                Console.WriteLine(logline);
            }

            foreach (var coin in rareCoins)
            {
                foundIssues = true;

                var logline = $"0x{coin.Key:X8} {coin.Value.Name.Value} ({coin.Value.Biota.WeenieClassId}) is";

                if (coin.Value.Container is not null)
                {
                    logline += " contained by: ";

                    var queryContainer =
                        from biota in ctx.Biota
                        join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                        where name.Type == (ushort)PropertyString.Name && biota.Id == coin.Value.Container.Value
                        from placement in ctx
                            .BiotaPropertiesInt.Where(x =>
                                x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.PlacementPosition
                            )
                            .DefaultIfEmpty()
                        from parentcontainer in ctx
                            .BiotaPropertiesIID.Where(x =>
                                x.ObjectId == biota.Id && x.Type == (ushort)PropertyInstanceId.Container
                            )
                            .DefaultIfEmpty()
                        select new
                        {
                            Biota = biota,
                            Name = name,
                            PlacementPosition = placement ?? null,
                            ParentContainer = parentcontainer ?? null,
                        };

                    var container = queryContainer.FirstOrDefault();

                    if (container is null)
                    {
                        logline += $"\n Unable to find 0x{coin.Value.Container.Value} in database. Orphaned object?";
                    }
                    else
                    {
                        if (container.Biota.WeenieType == (int)WeenieType.Container)
                        {
                            var queryParentContainer =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where
                                    name.Type == (ushort)PropertyString.Name
                                    && biota.Id == container.ParentContainer.Value
                                select new { Biota = biota, Name = name, };

                            var parentContainer = queryParentContainer.FirstOrDefault();

                            logline +=
                                $"\n 0x{parentContainer.Biota.Id:X8} {parentContainer.Name.Value}{(parentContainer.Biota.WeenieType == (int)WeenieType.Storage ? "" : "'s")} Sub Pack 0x{container.Biota.Id:X8} {container.Name.Value} (Slot {container.PlacementPosition?.Value ?? 0}) at placement position {coin.Value.PlacementPosition?.Value ?? 0}";
                        }
                        else if (container.Biota.WeenieType == (int)WeenieType.Hook)
                        {
                            var queryHook =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where name.Type == (ushort)PropertyString.Name && biota.Id == coin.Value.Container.Value
                                from hooktype in ctx
                                    .BiotaPropertiesInt.Where(x =>
                                        x.ObjectId == biota.Id && x.Type == (ushort)PropertyInt.HookType
                                    )
                                    .DefaultIfEmpty()
                                from location in ctx
                                    .BiotaPropertiesPosition.Where(x =>
                                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                                    )
                                    .DefaultIfEmpty()
                                select new
                                {
                                    Biota = biota,
                                    Name = name,
                                    HookType = hooktype,
                                    Location = location ?? null,
                                };

                            var hook = queryHook.FirstOrDefault();

                            logline +=
                                $"\n 0x{container.Biota.Id:X8} {(HookType)hook.HookType.Value} Hook located at:\n 0x{hook.Location.ObjCellId:X8} [{hook.Location.OriginX:F6} {hook.Location.OriginY:F6} {hook.Location.OriginZ:F6}] {hook.Location.AnglesW:F6} {hook.Location.AnglesX:F6} {hook.Location.AnglesY:F6} {hook.Location.AnglesZ:F6}";
                        }
                        else if (
                            container.Biota.WeenieType == (int)WeenieType.Storage
                            || container.Biota.WeenieType == (int)WeenieType.Chest
                            || container.Biota.WeenieType == (int)WeenieType.SlumLord
                        )
                        {
                            var queryStorage =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where name.Type == (ushort)PropertyString.Name && biota.Id == coin.Value.Container.Value
                                from location in ctx
                                    .BiotaPropertiesPosition.Where(x =>
                                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                                    )
                                    .DefaultIfEmpty()
                                select new
                                {
                                    Biota = biota,
                                    Name = name,
                                    Location = location ?? null,
                                };

                            var storage = queryStorage.FirstOrDefault();

                            logline +=
                                $"\n 0x{container.Biota.Id:X8} {container.Name.Value} Main Pack at placement position {coin.Value.PlacementPosition?.Value ?? 0} and located at:\n 0x{storage.Location.ObjCellId:X8} [{storage.Location.OriginX:F6} {storage.Location.OriginY:F6} {storage.Location.OriginZ:F6}] {storage.Location.AnglesW:F6} {storage.Location.AnglesX:F6} {storage.Location.AnglesY:F6} {storage.Location.AnglesZ:F6}";
                        }
                        else if (container.Biota.WeenieType == (int)WeenieType.Corpse)
                        {
                            var queryCorpse =
                                from biota in ctx.Biota
                                join name in ctx.BiotaPropertiesString on biota.Id equals name.ObjectId
                                where name.Type == (ushort)PropertyString.Name && biota.Id == coin.Value.Container.Value
                                from location in ctx
                                    .BiotaPropertiesPosition.Where(x =>
                                        x.ObjectId == biota.Id && x.PositionType == (ushort)PositionType.Location
                                    )
                                    .DefaultIfEmpty()
                                select new
                                {
                                    Biota = biota,
                                    Name = name,
                                    Location = location ?? null
                                };

                            var corpse = queryCorpse.FirstOrDefault();

                            logline +=
                                $"\n 0x{corpse.Biota.Id:X8} {corpse.Name.Value}'s Main Pack 0x{container.Biota.Id:X8} at placement position {coin.Value.PlacementPosition?.Value ?? 0} and located at:\n 0x{corpse.Location.ObjCellId:X8} [{corpse.Location.OriginX:F6} {corpse.Location.OriginY:F6} {corpse.Location.OriginZ:F6}] {corpse.Location.AnglesW:F6} {corpse.Location.AnglesX:F6} {corpse.Location.AnglesY:F6} {corpse.Location.AnglesZ:F6}";
                        }
                        else if (
                            container.Biota.WeenieType == (int)WeenieType.Creature
                            || container.Biota.WeenieType == (int)WeenieType.Admin
                            || container.Biota.WeenieType == (int)WeenieType.Sentinel
                            || container.Biota.WeenieType == (int)WeenieType.Cow
                            || container.Biota.WeenieType == (int)WeenieType.Pet
                            || container.Biota.WeenieType == (int)WeenieType.CombatPet
                            || container.Biota.WeenieType == (int)WeenieType.Vendor
                        )
                        {
                            logline +=
                                $"\n 0x{container.Biota.Id:X8} {container.Name.Value}'s Main Pack at placement position {coin.Value.PlacementPosition?.Value ?? 0}";
                        }
                        else
                        {
                            logline +=
                                $"\n Unexpected WeenieType '{container.Biota.WeenieType}' for container 0x{container.Biota.Id:X8} {container.Name.Value}. Invalid custom content?";
                        }
                    }
                }
                else if (coin.Value.Location is not null)
                {
                    logline +=
                        $" on a landblock and located at:\n 0x{coin.Value.Location.ObjCellId:X8} [{coin.Value.Location.OriginX:F6} {coin.Value.Location.OriginY:F6} {coin.Value.Location.OriginZ:F6}] {coin.Value.Location.AnglesW:F6} {coin.Value.Location.AnglesX:F6} {coin.Value.Location.AnglesY:F6} {coin.Value.Location.AnglesZ:F6}";
                }
                else
                {
                    logline +=
                        $" found in database but does not have a Container, Wielder, or Location. Orphaned object?";
                }

                if (fix)
                {
                    // this coin was distributed by Emissary of Asheron (45492) incorrectly to be used at the Melee Rare Vendor. Generate a new v2 rare, copy over "link" data to effectively mutate in place "illegally" swapped coin for another randomly generated V2 rare.

                    var tierRares = PreToPostMeleeRareConversions.Values.ToList();

                    //tierRares.Remove(coin.Value.Biota.WeenieClassId);

                    var rng = ThreadSafeRandom.Next(0, tierRares.Count - 1);

                    var rareWCID = tierRares[rng];

                    var wo = WorldObjectFactory.CreateNewWorldObject(rareWCID);

                    if (wo != null)
                    {
                        if (coin.Value.Container != null)
                        {
                            wo.ContainerId = coin.Value.Container.Value;
                        }

                        if (coin.Value.PlacementPosition != null)
                        {
                            wo.PlacementPosition = coin.Value.PlacementPosition.Value;
                        }

                        if (coin.Value.Location != null)
                        {
                            wo.Location = new ACE.Entity.Position(
                                coin.Value.Location.ObjCellId,
                                coin.Value.Location.OriginX,
                                coin.Value.Location.OriginY,
                                coin.Value.Location.OriginZ,
                                coin.Value.Location.AnglesX,
                                coin.Value.Location.AnglesY,
                                coin.Value.Location.AnglesZ,
                                coin.Value.Location.AnglesW
                            );
                        }

                        if (coin.Value.Shortcut != null)
                        {
                            var shortcut = ctx
                                .CharacterPropertiesShortcutBar.Where(x => x.ShortcutObjectId == coin.Value.Biota.Id)
                                .FirstOrDefault();

                            if (shortcut != null)
                            {
                                shortcut.ShortcutObjectId = wo.Biota.Id;
                            }
                        }

                        wo.SaveBiotaToDatabase();
                        newRaresFromCoins++;

                        ctx.Biota.Remove(coin.Value.Biota);
                        deletedRareCoins++;

                        logline += $"\n\\----- Replaced with 0x{wo.Guid} {wo.Name} ({wo.WeenieClassId})";
                    }
                    else
                    {
                        logline += $"\n\\----- Unable to replace with ({rareWCID}). Rare not found in database.";
                        return;
                    }

                    ctx.SaveChanges();
                }
                else
                {
                    logline +=
                        $"\n\\----- Rare Coin obtained from Emissary of Asheron (45492), needs to be deleted and replaced with a random newly generated melee rare.";
                }

                Console.WriteLine(logline);
            }

            if (!fix && foundIssues)
            {
                Console.WriteLine(
                    $"Found {foundRaresPre:N0} Pre-MoA Rares. These need to be converted to Post-MoA WCIDs and their version updated to V2."
                );
                Console.WriteLine(
                    $"Found {foundRaresPost:N0} Post-MoA Rares. {foundRaresPost - validPostRares - validPostRaresV1:N0} are invalid and need to be regenerated due to incorrectly being distributed by Melee Rare Vendor. {validPostRaresV1:N0} need to have their version updated to V2."
                );
                Console.WriteLine(
                    $"Found {foundRareCoins:N0} Rare Coins. These need to be deleted and replaced with newly randomly generated rare."
                );
                Console.WriteLine($"Dry run completed. Type 'verify-melee-rares fix' to fix any issues.");
            }

            if (!foundIssues)
            {
                Console.WriteLine($"Verified {foundRaresPre + foundRaresPost:N0} melee rares. No changes required.");
            }

            if (foundIssues && fix)
            {
                Console.WriteLine(
                    $"Found {foundRaresPre:N0} Pre-MoA Rares. {adjustedRaresPre:N0} were converted to Post-MoA WCIDs and their version updated to V2."
                );
                Console.WriteLine(
                    $"Found {foundRaresPost:N0} Post-MoA Rares. {validPostRares:N0} valid, {deletedPostRares:N0} deleted, {replacedPostRares:N0} replaced, {adjustedRaresPost:N0} updated to V2."
                );
                Console.WriteLine(
                    $"Found {foundRareCoins:N0} Rare Coins. {deletedRareCoins:N0} deleted, {newRaresFromCoins:N0} replaced."
                );
            }
        }
    }

    public static Dictionary<uint, uint> PreToPostMeleeRareConversions =
        new()
        {
            /* Ridgeback Dagger */{ 30310, 45444 },
            /* Zharalim Crookblade */{ 30311, 45445 },
            /* Baton of Tirethas */{ 30312, 45446 },
            /* Dripping Death */{ 30313, 45447 },
            /* Star of Tukal */{ 30314, 45448 },
            /* Subjugator */{ 30315, 45449 },
            /* Black Thistle */{ 30316, 45441 },
            /* Moriharu's Kitchen Knife */{ 30317, 45442 },
            /* Pitfighter's Edge */{ 30318, 45443 },
            /* Champion's Demise */{ 30319, 45451 },
            /* Pillar of Fearlessness */{ 30320, 45452 },
            /* Squire's Glaive */{ 30321, 45453 },
            /* Star of Gharu'n */{ 30322, 45454 },
            /* Tri-Blade Spear */{ 30323, 45455 },
            /* Staff of All Aspects */{ 30324, 45456 },
            /* Death's Grip Staff */{ 30325, 45457 },
            /* Staff of Fettered Souls */{ 30326, 45458 },
            /* Spirit Shifting Staff */{ 30327, 45459 },
            /* Staff of Tendrils */{ 30328, 45460 },
            /* Brador's Frozen Eye */{ 30329, 45461 },
            /* Defiler of Milantos */{ 30330, 45462 },
            /* Desert Wyrm */{ 30331, 45463 },
            /* Guardian of Pwyll */{ 30332, 45464 },
            /* Morrigan's Vanity */{ 30333, 45465 },
            /* Fist of Three Principles */{ 30334, 45466 },
            /* Hevelio's Half-Moon */{ 30335, 45467 },
            /* Malachite Slasher */{ 30336, 45468 },
            /* Skullpuncher */{ 30337, 45469 },
            /* Steel Butterfly */{ 30338, 45470 },
            /* Thunderhead */{ 30339, 45450 },
            /* Bearded Axe of Souia-Vey */{ 30340, 45436 },
            /* Canfield Cleaver */{ 30341, 45437 },
            /* Count Renari's Equalizer */{ 30342, 45438 },
            /* Smite */{ 30343, 45439 },
            /* Tusked Axe of Ayan Baqur */{ 30344, 45440 },
        };
}
