using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;

namespace ACE.Server.Managers;

/// <summary>
/// Characters an admin has taken off the website's general leaderboards (@leaderboard exclude): level, deaths, allegiance
/// rank, town attunements, most dangerous creatures and trade skills, on top of the staff accounts they already leave out.
/// It is a flag on the character (LeaderboardExcluded), so it works for a character who is offline and is kept with them.
/// The arena (/arena unrank) and capstone (@capstone blacklist) boards have exclusions of their own.
/// </summary>
public static class LeaderboardExclusion
{
    public static bool IsExcluded(IPlayer player) => player.GetProperty(PropertyBool.LeaderboardExcluded) ?? false;

    public static void Set(IPlayer player, bool excluded)
    {
        if (excluded)
        {
            player.SetProperty(PropertyBool.LeaderboardExcluded, true);
        }
        else
        {
            player.RemoveProperty(PropertyBool.LeaderboardExcluded);
        }

        // The leaderboard API reads the database, and a changed property is otherwise only written at the next automatic
        // save, so save it now and the leaderboards follow straight away (see CapstoneBlacklist).
        player.SaveBiotaToDatabase();
    }
}
