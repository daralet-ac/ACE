using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class HouseKeep
{
    // housekeep
    [CommandHandler("housekeep", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleHousekeep(Session session, params string[] parameters)
    {
        // @housekeep[never { off | on}] -With no parameters, this command displays the housekeeping info for the selected item.With the 'never' flag, it sets the item to never housekeep, or turns that state off.
        // @housekeep - Queries or sets the housekeeping status for the selected item.

        // TODO: output
    }
}
