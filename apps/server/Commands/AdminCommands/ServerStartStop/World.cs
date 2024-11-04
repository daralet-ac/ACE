using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ServerStartStop;

public class World
{
    [CommandHandler(
        "world",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        0,
        "Open or Close world to player access.",
        "[open | close] <boot>\nIf closing world, using @world close boot will force players to logoff immediately"
    )]
    public static void HandleHelp(Session session, params string[] parameters)
    {
        var open = false;
        var close = false;
        var bootPlayers = false;

        var message =
            $"World is currently {WorldManager.WorldStatus.ToString()}\nPlease specify state to change\n@world [open | close] <boot>\nIf closing world, using @world close boot will force players to logoff immediately";
        if (parameters.Length >= 1)
        {
            switch (parameters[0].ToLower())
            {
                case "open":
                    if (WorldManager.WorldStatus != WorldManager.WorldStatusState.Open)
                    {
                        message = "Opening world to players...";
                        open = true;
                    }
                    else
                    {
                        message = "World is already open.";
                    }
                    break;
                case "close":
                    if (WorldManager.WorldStatus != WorldManager.WorldStatusState.Closed)
                    {
                        if (parameters.Length > 1)
                        {
                            if (parameters[1].ToLower() == "boot")
                            {
                                bootPlayers = true;
                            }
                        }
                        message = "Closing world";
                        if (bootPlayers)
                        {
                            message += ", and booting all online players.";
                        }
                        else
                        {
                            message += "...";
                        }

                        close = true;
                    }
                    else
                    {
                        message = "World is already closed.";
                    }
                    break;
            }
        }

        CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.WorldBroadcast);

        if (open)
        {
            WorldManager.Open(session == null ? null : session.Player);
        }
        else if (close)
        {
            WorldManager.Close(session == null ? null : session.Player, bootPlayers);
        }
    }
}
