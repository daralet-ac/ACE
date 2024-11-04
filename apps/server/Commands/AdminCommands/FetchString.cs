using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class FetchString
{
    [CommandHandler(
        "fetchstring",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        1,
        "Fetches a server property that is a string",
        "fetchstring (string)"
    )]
    public static void HandleFetchServerStringProperty(Session session, params string[] parameters)
    {
        var stringVal = PropertyManager.GetString(parameters[0], cacheFallback: false);
        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"{parameters[0]} - {stringVal.Description ?? "No Description"}: {stringVal.Item}"
        );
    }
}
