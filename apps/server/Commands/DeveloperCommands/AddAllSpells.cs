using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class AddAllSpells
{
    // addallspells
    [CommandHandler(
        "addallspells",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Adds all known spells to your own spellbook."
    )]
    public static void HandleAddAllSpells(Session session, params string[] parameters)
    {
        for (uint spellLevel = 1; spellLevel <= 7; spellLevel++)
        {
            session.Player.LearnSpellsInBulk(MagicSchool.CreatureEnchantment, spellLevel);
            session.Player.LearnSpellsInBulk(MagicSchool.PortalMagic, spellLevel);
            session.Player.LearnSpellsInBulk(MagicSchool.LifeMagic, spellLevel);
            //session.Player.LearnSpellsInBulk(MagicSchool.VoidMagic, spellLevel);
            session.Player.LearnSpellsInBulk(MagicSchool.WarMagic, spellLevel);
        }
    }
}
