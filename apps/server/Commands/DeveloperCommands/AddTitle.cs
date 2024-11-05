using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class AddTitle
{
    /// <summary>
    /// Add a specific title to yourself
    /// </summary>
    [CommandHandler(
        "addtitle",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Add title to yourself",
        "[titleid]"
    )]
    public static void HandleAddTitle(Session session, params string[] parameters)
    {
        if (uint.TryParse(parameters[0], out var titleId))
        {
            session.Player.AddTitle(titleId);
        }
    }
}
