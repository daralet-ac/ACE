using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;

namespace ACE.Server.Managers;

/// <summary>
/// The capstone blacklist (@capstone blacklist): characters whose capstone runs stay off the website's capstone leaderboard,
/// on top of the staff accounts it already leaves out. It is a flag on the character (CapstoneTrackingExcluded), so it
/// works for a character who is offline and is kept with them. The runs themselves are still recorded (CapstoneRunTracker),
/// and the leaderboard API leaves out any run with a blacklisted character in it, so taking someone off the blacklist
/// brings their runs back.
/// </summary>
public static class CapstoneBlacklist
{
    public static bool IsBlacklisted(IPlayer player) => player.GetProperty(PropertyBool.CapstoneTrackingExcluded) ?? false;

    public static void Set(IPlayer player, bool blacklisted)
    {
        if (blacklisted)
        {
            player.SetProperty(PropertyBool.CapstoneTrackingExcluded, true);
        }
        else
        {
            player.RemoveProperty(PropertyBool.CapstoneTrackingExcluded);
        }
    }
}
