using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class EnableAetheria
{
    /// <summary>
    /// Enables the aetheria slots for the player
    /// </summary>
    [CommandHandler(
        "enable-aetheria",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Enables the aetheria slots for the player"
    )]
    public static void HandleEnableAetheria(Session session, params string[] parameters)
    {
        var flags = (int)AetheriaBitfield.All;

        if (parameters.Length > 0)
        {
            int.TryParse(parameters[0], out flags);
        }

        session.Player.UpdateProperty(session.Player, PropertyInt.AetheriaBitfield, flags);
    }
}
