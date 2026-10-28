using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.TownEvents;

namespace ACE.Server.Commands.PlayerCommands;

/// <summary>
/// /towndefense join: joins the Town Defense of the town under attack that the player is in, for one who said no to the
/// prompt or was removed from it
/// </summary>
public class TownDefenseCommand
{
    private const string Usage = "/towndefense join: join the defense of the town you are in while it is under attack.";

    [CommandHandler("towndefense", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0, "Join the defense of a town under attack.", Usage)]
    public static void HandleTownDefense(Session session, params string[] parameters)
    {
        if (parameters.Length == 0 || parameters[0].ToLowerInvariant() != "join")
        {
            session.Player.SendMessage(Usage, ChatMessageType.Broadcast);
            return;
        }

        TownEventManager.HandleJoin(session.Player);
    }
}
