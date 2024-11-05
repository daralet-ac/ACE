using System;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class FixAllegiances
{
    [CommandHandler(
        "fix-allegiances",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        "Fixes the monarch data for allegiances"
    )]
    public static void HandleFixAllegiances(Session session, params string[] parameters)
    {
        var players = PlayerManager.GetAllPlayers();

        // build allegiances
        foreach (var player in players)
        {
            AllegianceManager.GetAllegiance(player);
        }

        foreach (var player in players.Where(i => i.MonarchId != null))
        {
            var monarchID = player.MonarchId.Value;

            // find multi allegiances
            foreach (var allegiance in AllegianceManager.Allegiances.Values)
            {
                if (allegiance.MonarchId == monarchID)
                {
                    continue;
                }

                if (allegiance.Members.TryGetValue(new ObjectGuid(monarchID), out var member))
                {
                    var desynced = PlayerManager.FindByGuid(monarchID);
                    Console.WriteLine(
                        $"{player.Name} has references to {desynced.Name} as monarch, but should be {allegiance.Monarch.Player.Name} -- fixing"
                    );

                    player.MonarchId = allegiance.MonarchId;
                    player.SaveBiotaToDatabase();
                }
            }

            // find missing players
            var monarch = PlayerManager.FindByGuid(player.MonarchId.Value);
            var _allegiance = AllegianceManager.GetAllegiance(monarch);

            if (_allegiance != null && !_allegiance.Members.ContainsKey(player.Guid))
            {
                // walk patrons to get the updated monarch
                var patron = PlayerManager.FindByGuid(player.PatronId.Value);
                if (patron == null)
                {
                    Console.WriteLine(
                        $"{player.Name} has references to deleted patron {player.PatronId.Value:X8}, checking for vassals"
                    );
                    player.PatronId = null;

                    var vassals = players.Where(i => i.PatronId != null && i.PatronId == player.Guid.Full).ToList();
                    if (vassals.Count > 0)
                    {
                        Console.WriteLine($"Vassals found, {player.Name} is the monarch");
                        player.MonarchId = player.Guid.Full;
                    }
                    else
                    {
                        Console.WriteLine($"No vassals found, removing patron reference to deleted character");
                        player.MonarchId = null;
                    }
                    player.SaveBiotaToDatabase();
                    continue;
                }

                while (patron.PatronId != null)
                {
                    patron = PlayerManager.FindByGuid(patron.PatronId.Value);
                }

                if (player.MonarchId != patron.Guid.Full)
                {
                    Console.WriteLine(
                        $"{player.Name} has references to {monarch.Name} as monarch, but should be {patron.Name} -- fixing missing player"
                    );

                    player.MonarchId = patron.Guid.Full;
                    player.SaveBiotaToDatabase();
                }
            }
        }

        foreach (var allegiance in AllegianceManager.Allegiances.Values.ToList())
        {
            AllegianceManager.Rebuild(allegiance);
        }
    }
}
