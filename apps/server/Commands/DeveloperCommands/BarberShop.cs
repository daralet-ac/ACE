using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameEvent.Events;

namespace ACE.Server.Commands.DeveloperCommands;

public class BarberShop
{
    /// <summary>
    /// Debug command to spawn the Barber UI
    /// </summary>
    [CommandHandler("barbershop", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, "Displays the barber ui")]
    public static void HandleBarberShop(Session session, params string[] parameters)
    {
        session.Player.BarberActive = true;
        session.Network.EnqueueSend(new GameEventStartBarber(session));
    }
}
