using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class Sticky
{
    /// <summary>
    /// Sets whether you lose items should you die.
    /// </summary>
    [CommandHandler(
        "sticky",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Sets whether you lose items should you die.",
        "<off/on>"
    )]
    public static void HandleSticky(Session session, params string[] parameters)
    {
        var sticky = !(parameters.Length > 0 && parameters[0] == "off");

        if (sticky)
        {
            CommandHandlerHelper.WriteOutputInfo(session, "You will no longer drop any items on death.");
        }
        else
        {
            CommandHandlerHelper.WriteOutputInfo(session, "You will now drop items on death normally.");
        }

        session.Player.NoCorpse = sticky;
    }
}
