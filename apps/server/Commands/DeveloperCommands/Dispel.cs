using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class Dispel
{
    // dispel
    [CommandHandler(
        "dispel",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Removes all enchantments from the player",
        "/dispel"
    )]
    public static void HandleDispel(Session session, params string[] parameters)
    {
        session.Player.EnchantmentManager.DispelAllEnchantments();

        // remove all enchantments from equipped items for now
        foreach (var item in session.Player.EquippedObjects.Values)
        {
            item.EnchantmentManager.DispelAllEnchantments();
        }
    }
}
