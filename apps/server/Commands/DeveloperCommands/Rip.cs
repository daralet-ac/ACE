using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class Rip
{
    [CommandHandler("rip", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld)]
    public static void HandleRip(Session session, params string[] parameters)
    {
        // insta-death, without the confirmation dialog from /die
        // useful during developer testing
        session.Player.TakeDamage(session.Player, DamageType.Bludgeon, session.Player.Health.Current);
    }
}
