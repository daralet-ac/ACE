using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;

namespace ACE.Server.Commands.SentinelCommands;

public class Run
{
    // run < on | off | toggle | check >
    [CommandHandler(
        "run",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Temporarily boosts your run skill.",
        "( on | off | toggle | check )\n"
            + "Boosts the run skill of the PSR so they can pursue the \"bad folks\". The enchantment will wear off after a while. This command defaults to toggle."
    )]
    public static void HandleRun(Session session, params string[] parameters)
    {
        // usage: @run on| off | toggle | check
        // Boosts the run skill of the PSR so they can pursue the "bad folks".The enchantment will wear off after a while.This command defaults to toggle.
        // @run - Temporarily boosts your run skill.

        string param;

        if (parameters.Length > 0)
        {
            param = parameters[0];
        }
        else
        {
            param = "toggle";
        }

        var spellID = (uint)SpellId.SentinelRun;

        switch (param)
        {
            case "toggle":
                if (session.Player.EnchantmentManager.HasSpell(spellID))
                {
                    goto case "off";
                }
                else
                {
                    goto case "on";
                }

            case "check":
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Run speed boost is currently {(session.Player.EnchantmentManager.HasSpell(spellID) ? "ACTIVE" : "INACTIVE")}",
                        ChatMessageType.Broadcast
                    )
                );
                break;
            case "off":
                var runBoost = session.Player.EnchantmentManager.GetEnchantment(spellID);
                if (runBoost != null)
                {
                    session.Player.EnchantmentManager.Remove(runBoost);
                }
                else
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat("Run speed boost is currently INACTIVE", ChatMessageType.Broadcast)
                    );
                }

                break;
            case "on":
                var spell = new Spell(spellID);
                var addResult = session.Player.EnchantmentManager.Add(spell, session.Player, null);
                session.Network.EnqueueSend(
                    new GameEventMagicUpdateEnchantment(session, new Enchantment(session.Player, addResult.Enchantment))
                );
                session.Network.EnqueueSend(new GameMessageSystemChat("Run forrest, run!", ChatMessageType.Broadcast));
                session.Player.HandleSpellHooks(spell);
                break;
        }
    }
}
