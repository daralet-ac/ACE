using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class FellowBuff
{
    // fellowbuff [name]
    [CommandHandler(
        "fellowbuff",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Buffs your fellowship (or a player's fellowship) with all beneficial spells.",
        "[name]\n" + "This command buffs your fellowship (or the fellowship of the specified character)."
    )]
    public static void HandleFellowBuff(Session session, params string[] parameters)
    {
        var aceParams = new List<CommandParameterHelpers.ACECommandParameter>()
        {
            new CommandParameterHelpers.ACECommandParameter()
            {
                Type = CommandParameterHelpers.ACECommandParameterType.OnlinePlayerNameOrIid,
                Required = false,
                DefaultValue = session.Player
            }
        };
        if (!CommandParameterHelpers.ResolveACEParameters(session, parameters, aceParams))
        {
            return;
        }

        if (aceParams[0].AsPlayer.Fellowship == null)
        {
            session.Player.CreateSentinelBuffPlayers(
                new Player[] { aceParams[0].AsPlayer },
                aceParams[0].AsPlayer == session.Player
            );
            return;
        }

        var fellowshipMembers = aceParams[0].AsPlayer.Fellowship.GetFellowshipMembers();

        session.Player.CreateSentinelBuffPlayers(
            fellowshipMembers.Values,
            fellowshipMembers.Count == 1
            && aceParams[0].AsPlayer.Fellowship.FellowshipLeaderGuid == session.Player.Guid.Full
        );
    }
}
