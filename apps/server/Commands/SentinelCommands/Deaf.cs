using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.SentinelCommands;

public class Deaf
{
    // deaf < on / off >
    [CommandHandler("deaf", AccessLevel.Sentinel, CommandHandlerFlag.RequiresWorld, 1)]
    public static void HandleDeaf(Session session, params string[] parameters)
    {
        // @deaf - Block @tells except for the player you are currently helping.
        // @deaf on -Make yourself deaf to players.
        // @deaf off -You can hear players again.

        // TODO: output
    }

    // deaf < hear | mute > < player >
    [CommandHandler("deaf", AccessLevel.Sentinel, CommandHandlerFlag.RequiresWorld, 2)]
    public static void HandleDeafHearOrMute(Session session, params string[] parameters)
    {
        // @deaf hear[name] -add a player to the list of players that you can hear.
        // @deaf mute[name] -remove a player from the list of players you can hear.

        // TODO: output
    }
}
