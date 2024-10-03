using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum.Properties;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

public class AllegianceNode
{
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
        // their rank. Additionally, up to 3 bonus rank may be obtained from the Leadership skill.
        //
        // Final Rank = FollowerRank + Leadership bonus.
        //
        // Leadership bonus = Leadership / 100
        // Follower Rank:
        // - 1 unique follower = 1
        // - 3 = 2
        // - 6 = 3
        // - 10 = 4
        // - 15 = 5
        // - 25 - 6
        // - 50 = 7

        var uniqueFollowers = GetUniqueFollowers(this);

        var leadershipBonus = Player.GetCurrentLeadership() / 100;

        switch (uniqueFollowers)
        {
            case >= 50:
                Rank = 7 + leadershipBonus;
                break;
            case >= 25:
                Rank = 6 + leadershipBonus;
                break;
            case >= 15:
                Rank = 5 + leadershipBonus;
                break;
            case >= 10:
                Rank = 4 + leadershipBonus;
                break;
            case >= 6:
                Rank = 3 + leadershipBonus;
                break;
            case >= 3:
                Rank = 2 + leadershipBonus;
                break;
            case >= 1:
                Rank = 1 + leadershipBonus;
                break;
            default:
                Rank = 0 + leadershipBonus;
                break;
        }
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
