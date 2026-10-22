using ACE.Entity.Enum;
using ACE.Server.Arena;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.AdminUi;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.SentinelCommands;

public class AdminUi
{
    private static readonly ILogger _log = Log.ForContext(typeof(AdminUi));

    // adminui
    [CommandHandler(
        "adminui",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Data feed for the Daralet Admin Decal plugin.",
        "hello | players | fellowships | pois | instances | capstones | arenas | olthoinorth | tidemage | esper\n"
            + "The plugin sends these itself and hides the replies from chat. See AdminUiProtocol for the reply format."
    )]
    public static void HandleAdminui(Session session, params string[] parameters)
    {
        var kind = parameters[0].ToLowerInvariant();

        // worded differently from the "You are not authorized to use @adminui" the plugin takes to mean no access at all
        if (AdminUiAccess.Requests.TryGetValue(kind, out var required) && session.AccessLevel < required)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"@adminui {kind} needs {required} access.",
                ChatMessageType.Broadcast
            );
            return;
        }

        object payload;

        switch (kind)
        {
            case "hello":
                payload = new AdminUiHello(
                    2,
                    session.Player.Name,
                    session.AccessLevel.ToString(),
                    (int)session.AccessLevel,
                    AdminUiAccess.GetTabLevels(),
                    AdminUiAccess.GetCommandLevels()
                );
                break;
            case "players":
                payload = AdminUiSnapshots.GetPlayers();
                break;
            case "fellowships":
                payload = AdminUiSnapshots.GetFellowships();
                break;
            case "pois":
                payload = AdminUiSnapshots.GetPois();
                break;
            case "instances":
                payload = AdminUiInstances.GetInstances();
                break;
            case "capstones":
                payload = AdminUiCapstones.Get();
                break;
            case "arenas":
                payload = ArenaManager.GetAdminUiReport();
                break;
            case "olthoinorth":
                payload = AdminUiRegions.GetOlthoiNorth();
                break;
            case "tidemage":
                payload = AdminUiRegions.GetTideMage();
                break;
            case "esper":
                payload = AdminUiRegions.GetEsper();
                break;
            case "settings":
                payload = AdminUiSettings.GetValues();
                break;
            case "settingdocs":
                payload = AdminUiSettings.GetDocs();
                break;
            default:
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"Unknown @adminui request: {parameters[0]}. Use hello, players, fellowships, pois, instances, capstones, arenas, olthoinorth, tidemage, esper, settings or settingdocs.",
                    ChatMessageType.Broadcast
                );
                return;
        }

        foreach (var message in AdminUiProtocol.BuildMessages(kind, payload))
        {
            CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.Broadcast);
        }
    }
}
