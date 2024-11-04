using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class FetchDouble
{
    [CommandHandler(
        "fetchdouble",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        1,
        "Fetches a server property that is a double",
        "fetchdouble (string)"
    )]
    public static void HandleFetchServerFloatProperty(Session session, params string[] parameters)
    {
        var floatVal = PropertyManager.GetDouble(parameters[0], cacheFallback: false);
        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"{parameters[0]} - {floatVal.Description ?? "No Description"}: {floatVal.Item}"
        );
    }
}
