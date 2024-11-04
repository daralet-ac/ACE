using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class ShowAllegiances
{
    [CommandHandler(
        "show-allegiances",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        "Shows all of the allegiance chains on the server."
    )]
    public static void HandleShowAllegiances(Session session, params string[] parameters)
    {
        var players = PlayerManager.GetAllPlayers();

        // build allegiances
        foreach (var player in players)
        {
            AllegianceManager.GetAllegiance(player);
        }

        foreach (var allegiance in AllegianceManager.Allegiances.Values)
        {
            allegiance.ShowInfo();
            Console.WriteLine("---------------");
        }
    }
}
