using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class HarmSelf
{
    /// <summary>
    /// Debug command to set player vitals to 1
    /// </summary>
    [CommandHandler("harmself", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, "Sets all player vitals to 1")]
    public static void HandleHarmSelf(Session session, params string[] parameters)
    {
        session.Player.UpdateVital(session.Player.Health, 1);
        session.Player.UpdateVital(session.Player.Stamina, 1);
        session.Player.UpdateVital(session.Player.Mana, 1);
    }
}
