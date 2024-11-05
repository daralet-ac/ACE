using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class FetchBool
{
    [CommandHandler(
        "fetchbool",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        1,
        "Fetches a server property that is a bool",
        "fetchbool (string)"
    )]
    public static void HandleFetchServerBoolProperty(Session session, params string[] parameters)
    {
        var boolVal = PropertyManager.GetBool(parameters[0], cacheFallback: false);
        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"{parameters[0]} - {boolVal.Description ?? "No Description"}: {boolVal.Item}"
        );
    }
}
