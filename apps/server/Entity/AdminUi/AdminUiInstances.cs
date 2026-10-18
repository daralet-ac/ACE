using System.Collections.Generic;
using System.Linq;
using ACE.Server.Arena;
using ACE.Server.Managers;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// An open instance. Owner says what it was opened for (a fellowship's capstone run, an arena duel), if anything.
/// </summary>
public record AdminUiInstance(
    uint Id,
    string Template,
    string Owner,
    double AgeSeconds,
    bool Closing,
    double? EmptySeconds,
    List<AdminUiFellow> Players
);

public static class AdminUiInstances
{
    public static List<AdminUiInstance> GetInstances()
    {
        var now = InstanceManager.UtcNow();
        var playersByInstance = PlayerManager
            .GetAllOnline()
            .Where(p => p.InstanceId != Landblock.PersistentInstance)
            .ToLookup(p => p.InstanceId);

        return InstanceManager
            .GetInstances()
            .OrderBy(i => i.Id)
            .Select(instance => new AdminUiInstance(
                instance.Id,
                instance.Template.Name,
                DescribeOwner(instance.Owner),
                (now - instance.CreatedAt).TotalSeconds,
                instance.IsClosing,
                instance.EmptySince.HasValue ? (now - instance.EmptySince.Value).TotalSeconds : null,
                playersByInstance[instance.Id].OrderBy(p => p.Name).Select(AdminUiSnapshots.GetFellow).ToList()
            ))
            .ToList();
    }

    private static string DescribeOwner(object owner)
    {
        return owner switch
        {
            null => null,
            Fellowship fellowship => $"Fellowship {fellowship.FellowshipName}",
            ArenaMatch match => $"Arena duel #{match.Id}",
            _ => owner.ToString(),
        };
    }
}
