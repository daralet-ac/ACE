using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ACE.Database;
using ACE.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Network;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Managers;

/// <summary>
/// Fake players for testing (@fakeplayer): real Player objects that stand where an admin put them, with a session that has
/// no client behind it. They count as online players (wave sizes, fellowships, the Town Defense) but are never saved:
/// not their character, their biota or anything they carry, and they leave no offline record. Logging one out, booting it
/// or shutting the server down just takes it out of the world.
/// </summary>
public static class FakePlayerManager
{
    private static readonly ILogger _log = Log.ForContext(typeof(FakePlayerManager));

    public const int MaxPerSpawn = 50;

    private static readonly object fakesLock = new();

    /// <summary>
    /// The fake players in the world, by guid
    /// </summary>
    private static readonly Dictionary<uint, Player> fakes = new();

    /// <summary>
    /// Every fake player made since the server started, so a record kept after one is gone can still leave it out
    /// </summary>
    private static readonly HashSet<uint> everMade = new();

    private static int nextNumber = 1;

    public static List<Player> GetAll()
    {
        lock (fakesLock)
        {
            return fakes.Values.ToList();
        }
    }

    public static bool WasFake(uint guid)
    {
        lock (fakesLock)
        {
            return everMade.Contains(guid);
        }
    }

    /// <summary>
    /// Makes count fake players of the level in front of the admin, in rows of five facing them. Their heritage and look
    /// are random; they can't be attacked or hurt, so they never leave a corpse. Call on the world thread.
    /// </summary>
    public static List<Player> Spawn(Player admin, int count, int level)
    {
        var made = new List<Player>();

        var weenie = DatabaseManager.World.GetCachedWeenie("human");
        if (weenie == null || admin.Location == null)
        {
            return made;
        }

        for (var i = 0; i < count; i++)
        {
            int number;
            lock (fakesLock)
            {
                number = nextNumber++;
            }

            var player = PlayerFactoryEx.CreateFake(weenie, GuidManager.NewPlayerGuid(), admin.Account.AccountId, $"Fake Player {number}");
            if (player == null)
            {
                _log.Warning("[FAKE PLAYER] couldn't make Fake Player {Number}", number);
                continue;
            }

            player.MakeFake(new Session(admin.Account.AccountId, admin.Account.AccountName));

            player.Level = level;
            player.Attackable = false;
            player.Invincible = true;
            player.Location = SpotFor(admin.Location, i);
            player.InstanceId = admin.InstanceId;

            lock (fakesLock)
            {
                fakes[player.Guid.Full] = player;
                everMade.Add(player.Guid.Full);
            }

            if (!PlayerManager.AddFakePlayer(player) || !LandblockManager.AddObject(player, true))
            {
                _log.Warning("[FAKE PLAYER] {Name} couldn't be placed at {Location}", player.Name, player.Location);
                Remove(player);
                continue;
            }

            // what a client's login complete would do: out of the pink bubble
            var materialize = new ActionChain();
            materialize.AddDelaySeconds(1);
            materialize.AddAction(player, player.OnTeleportComplete);
            materialize.EnqueueChain();

            made.Add(player);
        }

        _log.Information("[FAKE PLAYER] {Admin} made {Count} level {Level} fake players", admin.Name, made.Count, level);

        return made;
    }

    /// <summary>
    /// The i-th fake's spot: rows of five in front of the admin, fanned out, each turned to face them
    /// </summary>
    private static Position SpotFor(Position adminPos, int i)
    {
        var row = i / 5;
        var column = i % 5 - 2;

        var turned = new Position(adminPos);
        turned.Rotation = adminPos.Rotation * Quaternion.CreateFromYawPitchRoll(0, 0, column * 0.25f);

        var spot = turned.InFrontOf(3 + row * 1.5, true);
        spot.SetLandblock();
        return spot;
    }

    /// <summary>
    /// Takes a fake player out of the world: out of its fellowship, off its landblock, and off the online list
    /// </summary>
    public static void Remove(Player player)
    {
        if (!player.IsFakePlayer)
        {
            return;
        }

        lock (fakesLock)
        {
            if (!fakes.Remove(player.Guid.Full))
            {
                return;
            }
        }

        if (player.Fellowship != null)
        {
            player.FellowshipQuit(false);
        }

        player.CurrentLandblock?.RemoveWorldObject(player.Guid, false);
        PlayerManager.RemoveFakePlayer(player);
    }

    public static int RemoveAll()
    {
        var all = GetAll();

        foreach (var player in all)
        {
            Remove(player);
        }

        return all.Count;
    }
}
