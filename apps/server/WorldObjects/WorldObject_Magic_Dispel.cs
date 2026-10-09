using System.Collections.Generic;
using System.Linq;
using System.Text;
using ACE.Entity.Enum;
using ACE.Server.Entity;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    /// <summary>
    /// Handles casting SpellType.Dispel / FellowDispel spells
    /// </summary>
    private void HandleCastSpell_Dispel(Spell spell, WorldObject target, bool showMsg = true)
    {
        var player = this as Player;
        var creature = this as Creature;

        var removeSpells = target.EnchantmentManager.SelectDispel(spell);

        // dispel on server and client
        target.EnchantmentManager.Dispel(removeSpells.Select(s => s.Enchantment).ToList());

        var spellList = BuildSpellList(removeSpells);
        string suffix;
        if (removeSpells.Count > 0)
        {
            suffix = $" and dispel: {spellList}.";
        }
        else
        {
            suffix = ", but the dispel fails.";
        }

        if (player != null)
        {
            string casterMsg;

            if (player == target)
            {
                casterMsg = $"You cast {spell.Name} on yourself{suffix}";
            }
            else
            {
                casterMsg = $"You cast {spell.Name} on {target.Name}{suffix}";
            }

            if (showMsg)
            {
                player.SendChatMessage(player, casterMsg, ChatMessageType.Magic);
            }
        }

        if (target is Player targetPlayer && targetPlayer != player)
        {
            var targetMsg = $"{Name} casts {spell.Name} on you{suffix.Replace("and dispel", "and dispels")}";

            if (showMsg)
            {
                targetPlayer.SendChatMessage(this, targetMsg, ChatMessageType.Magic);
            }

            // all dispels appear to be listed as non-beneficial, even the ones that only dispel negative spells
            // we filter here to positive or all
            if (creature != null && spell.Align != DispelType.Negative)
            {
                targetPlayer.SetCurrentAttacker(creature);
            }
        }
    }

    protected static bool VerifyDispelPkStatus(WorldObject caster, WorldObject target)
    {
        // https://asheron.fandom.com/wiki/Announcements_-_2004/04_-_A_New_Threat
        // https://asheron.fandom.com/wiki/Dispel_Spells

        // Dispel spells and potions have been revised. All dispels are also now tied to the PK/L timer.

        // The feedback on the suggested dispel timer for PK/L was very mixed. There was no clear majority either for or against.
        // With that in mind, we've gone ahead with the changes that we feel best improve majority of PK/L combat:
        // we've decided to implement the PK/L timer on dispels.

        // If you have been in a PK/L action within the last 20 seconds, you will not be able to:

        // - Use a dispel gem.
        // - Use a dispel potion.
        // - Use the Awakener or Attenuated Awakener on someone else.
        // - Cast any dispel spell on yourself.
        // - Cast any dispel spell on someone else.

        var casterPlayer = caster as Player;

        if (casterPlayer != null && casterPlayer.PKTimerActive)
        {
            casterPlayer.SendWeenieError(WeenieError.YouHaveBeenInPKBattleTooRecently);
            return false;
        }

        if ((target.Wielder ?? target) is Player targetPlayer && targetPlayer.PKTimerActive)
        {
            if ( /* casterPlayer != null || */
                caster is Gem
                || caster is Food
            )
            {
                targetPlayer.SendWeenieError(WeenieError.YouHaveBeenInPKBattleTooRecently);

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a string with the spell list format as:
    /// Spell Name 1, Spell Name 2, and Spell Name 3
    /// </summary>
    private static string BuildSpellList(List<SpellEnchantment> spells)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < spells.Count; i++)
        {
            var spell = spells[i];

            if (i > 0)
            {
                sb.Append(", ");
                if (i == spells.Count - 1)
                {
                    sb.Append("and ");
                }
            }

            sb.Append(spell.Spell.Name);
        }
        return sb.ToString();
    }
}
