using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum.Properties;
using ACE.Server.Managers;
using ACE.Server.Mods;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Entity;

public class AllegianceNode
{
    private static readonly ILogger _log = Log.ForContext(typeof(ModManager));

    public readonly ObjectGuid PlayerGuid;
    public IPlayer Player => PlayerManager.FindByGuid(PlayerGuid);

    public readonly Allegiance Allegiance;

    public readonly AllegianceNode Monarch;
    public readonly AllegianceNode Patron;
    public Dictionary<uint, AllegianceNode> Vassals;

    public uint Rank;

    public bool IsMonarch => Patron == null;

    public bool HasVassals => TotalVassals > 0;

    public int TotalVassals => Vassals != null ? Vassals.Count : 0;

    public int TotalFollowers
    {
        get
        {
            var totalFollowers = 0;

            foreach (var vassal in Vassals.Values)
            {
                totalFollowers += vassal.TotalFollowers + 1;
            }

            return totalFollowers;
        }
    }

    public AllegianceNode(
        ObjectGuid playerGuid,
        Allegiance allegiance,
        AllegianceNode monarch = null,
        AllegianceNode patron = null
    )
    {
        PlayerGuid = playerGuid;
        Allegiance = allegiance;
        Monarch = monarch ?? this;
        Patron = patron;
    }

    public void BuildChain(Allegiance allegiance, List<IPlayer> players, Dictionary<uint, List<IPlayer>> patronVassals)
    {
        patronVassals.TryGetValue(PlayerGuid.Full, out var vassals);

        Vassals = new Dictionary<uint, AllegianceNode>();

        if (vassals != null)
        {
            foreach (var vassal in vassals)
            {
                var node = new AllegianceNode(vassal.Guid, allegiance, Monarch, this);
                node.BuildChain(allegiance, players, patronVassals);

                Vassals.Add(vassal.Guid.Full, node);
            }
        }
        CalculateRank();
    }

    private void CalculateRank()
    {
        // NEW RANK FORMULA
        // A player's allegiance rank depends on the number of unique accounts are under them in
        // their allegiance tree. Accounts who are also above them in the chain do not count towards
        // their rank. Additionally, up to 4 ranks may be obtained from the Leadership skill.
        //
        // Final Rank = FollowerRank + Leadership bonus.
        //
        // Leadership bonus = Leadership / 100
        // Follower Rank:
        // - 1 unique follower = 2
        // - 5 unique followers = 3
        // - 10 unique followers = 4
        // - 25 unique followers = 5
        // - 50 unique followers = 6
        //
        // Leadership bonus = 1 per 50, up to 4

        if (Player == null)
        {
            Rank = 1;
            return;
        }

        var uniqueFollowers = GetUniqueFollowers(this);
        uint baseRank = uniqueFollowers switch
        {
            >= 50 => 6,
            >= 20 => 5,
            >= 10 => 4,
            >= 5 => 3,
            >= 1 => 2,
            _ => 1
        };

        var currentLeadership = Player.GetCurrentLeadership();
        uint leadershipBonus = currentLeadership switch
        {
            >= 200 => 4,
            >= 150 => 3,
            >= 100 => 2,
            >= 50 => 1,
            _ => 0
        };

        Rank = Math.Min(baseRank + leadershipBonus, 10u);

        _log.Information("Rank calculation for {PlayerName}: {UniqueFollowers} followers (rank {BaseRank}) + {CurrentLeadership} leadership (rank {LeadershipBonus}) = final rank {Rank}",
            Player.Name, uniqueFollowers, baseRank, currentLeadership, leadershipBonus, Rank);
    }

    private double GetUniqueFollowers(AllegianceNode playerNode)
    {
        double uniqueFollowers = 0;

        var vassals = playerNode.Vassals.Values.ToList();

        if (Vassals.Count == 0)
        {
            return 0;
        }

        foreach (var vassal in vassals)
        {
            // check to see if player has logged in within the past 2 weeks
            if (vassal.Player.GetProperty(PropertyFloat.LoginTimestamp) + 1209600 < Time.GetUnixTime())
            {
                continue;
            }

            // check to see if this character is an alt on the same account
            if (vassal.Player.Account.AccountId == Player.Account.AccountId)
            {
                continue;
            }

            var rankContrib = vassal.Player.GetProperty(PropertyFloat.RankContribution);

            if (rankContrib != null)
            {
                uniqueFollowers += (double)rankContrib;
            }

            if (vassal.Vassals.Count > 0)
            {
                uniqueFollowers += GetUniqueFollowers(vassal);
            }
        }

        return uniqueFollowers;
    }

    public void Walk(Action<AllegianceNode> action, bool self = true)
    {
        if (self)
        {
            action(this);
        }

        foreach (var vassal in Vassals.Values)
        {
            vassal.Walk(action, true);
        }
    }

    public void ShowInfo(int depth = 0)
    {
        var prefix = "".PadLeft(depth * 2, ' ');
        Console.WriteLine($"{prefix}- {Player.Name}");
        foreach (var vassal in Vassals.Values)
        {
            vassal.ShowInfo(depth + 1);
        }
    }

    public void OnLevelUp()
    {
        // patron = self node
        var patronLevel = Player.Level ?? 1;

        // find vassals who are not passing xp
        foreach (var vassal in Vassals.Values.Where(i => !i.Player.ExistedBeforeAllegianceXpChanges))
        {
            var vassalLevel = vassal.Player.Level ?? 1;

            // check if vassal now meets criteria for passing xp
            if (patronLevel >= vassalLevel)
            {
                vassal.Player.ExistedBeforeAllegianceXpChanges = true;
            }
        }
    }
}
