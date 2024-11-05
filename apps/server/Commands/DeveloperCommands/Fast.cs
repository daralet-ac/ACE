using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class Fast
{
    [CommandHandler("fast", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld)]
    public static void HandleFast(Session session, params string[] parameters)
    {
        var spell = new Spell(SpellId.QuicknessSelf7);
        session.Player.CreateEnchantment(session.Player, session.Player, null, spell);

        spell = new Spell(SpellId.SprintSelf7);
        session.Player.CreateEnchantment(session.Player, session.Player, null, spell);

        spell = new Spell(SpellId.StrengthSelf7);
        session.Player.CreateEnchantment(session.Player, session.Player, null, spell);
    }
}
