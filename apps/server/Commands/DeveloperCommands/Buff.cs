using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class Buff
{
    // buff [name]
    [CommandHandler(
        "buff",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Buffs you (or a player) with all beneficial spells.",
        "[name] [maxLevel]\n" + "This command buffs yourself (or the specified character)."
    )]
    public static void HandleBuff(Session session, params string[] parameters)
    {
        var aceParams = new List<CommandParameterHelpers.ACECommandParameter>()
        {
            new CommandParameterHelpers.ACECommandParameter()
            {
                Type = CommandParameterHelpers.ACECommandParameterType.OnlinePlayerNameOrIid,
                Required = false,
                DefaultValue = session.Player
            },
            new CommandParameterHelpers.ACECommandParameter()
            {
                Type = CommandParameterHelpers.ACECommandParameterType.ULong,
                Required = false,
                DefaultValue = (ulong)8
            }
        };
        if (!CommandParameterHelpers.ResolveACEParameters(session, parameters, aceParams))
        {
            return;
        }

        session.Player.CreateSentinelBuffPlayers(
            new Player[] { aceParams[0].AsPlayer },
            aceParams[0].AsPlayer == session.Player,
            aceParams[1].AsULong
        );
    }
}
