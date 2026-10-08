using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Creature
{
    /// <summary>
    /// Handles equipping an item casting a spell on player or creature
    /// </summary>
    public bool CreateItemSpell(WorldObject item, uint spellID)
    {
        var spell = new Spell(spellID);

        if (spell.NotFound)
        {
            if (this is Player player)
            {
                if (spell._spellBase == null)
                {
                    player.Session.Network.EnqueueSend(
                        new GameEventCommunicationTransientString(player.Session, $"SpellID {spellID} Invalid.")
                    );
                }
                else
                {
                    player.Session.Network.EnqueueSend(
                        new GameMessageSystemChat($"{spell.Name} spell not implemented, yet!", ChatMessageType.System)
                    );
                }
            }
            return false;
        }

        // TODO: look into condensing this
        switch (spell.School)
        {
            case MagicSchool.CreatureEnchantment:
            case MagicSchool.LifeMagic:

                HandleCastSpell(spell, this, item, equip: true);
                break;

            case MagicSchool.PortalMagic:

                if (spell.HasItemCategory || spell.IsPortalSpell)
                {
                    HandleCastSpell(spell, this, item, item, equip: true);
                }
                else
                {
                    HandleCastSpell(spell, item, item, item, equip: true);
                }

                break;
        }

        return true;
    }

    /// <summary>
    /// Removes an item's spell from the appropriate enchantment registry (either the wielder, or the item)
    /// </summary>
    /// <param name="silent">if TRUE, silently removes the spell, without sending a message to the target player</param>
    public void RemoveItemSpell(WorldObject item, uint spellId, bool silent = false)
    {
        if (item == null)
        {
            return;
        }

        var spell = new Spell(spellId);

        if (spell._spellBase == null)
        {
            if (this is Player player)
            {
                player.Session.Network.EnqueueSend(
                    new GameEventCommunicationTransientString(player.Session, $"SpellId {spellId} Invalid.")
                );
            }

            return;
        }

        var target = spell.School == MagicSchool.PortalMagic && !spell.HasItemCategory ? item : this;

        // Retrieve enchantment on target and remove it, if present
        var propertiesEnchantmentRegistry = target.EnchantmentManager.GetEnchantment(spellId, item.Guid.Full);

        if (propertiesEnchantmentRegistry != null)
        {
            if (!silent)
            {
                target.EnchantmentManager.Remove(propertiesEnchantmentRegistry);
            }
            else
            {
                target.EnchantmentManager.Dispel(propertiesEnchantmentRegistry);
            }
        }
    }
}
