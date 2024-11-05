using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class FetchLong
{
    [CommandHandler(
        "fetchlong",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        1,
        "Fetches a server property that is a long",
        "fetchlong (string)"
    )]
    public static void HandleFetchServerLongProperty(Session session, params string[] parameters)
    {
        var intVal = PropertyManager.GetLong(parameters[0], cacheFallback: false);
        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"{parameters[0]} - {intVal.Description ?? "No Description"}: {intVal.Item}"
        );
    }
}
