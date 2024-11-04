using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class Slow
{
    [CommandHandler("slow", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld)]
    public static void HandleSlow(Session session, params string[] parameters)
    {
        var spell = new Spell(SpellId.SlownessSelf7);
        session.Player.CreateEnchantment(session.Player, session.Player, null, spell);

        spell = new Spell(SpellId.LeadenFeetSelf7);
        session.Player.CreateEnchantment(session.Player, session.Player, null, spell);

        spell = new Spell(SpellId.WeaknessSelf7);
        session.Player.CreateEnchantment(session.Player, session.Player, null, spell);
    }
}
