using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    private bool CalculateManaUsage(
        CastingPreCheckStatus castingPreCheckStatus,
        Spell spell,
        WorldObject target,
        WorldObject casterItem,
        out uint manaUsed,
        out ManaCastRefund manaRefund
    )
    {
        manaUsed = 0;
        manaRefund = ManaCastRefund.None;
        if (castingPreCheckStatus == CastingPreCheckStatus.Success)
        {
            manaUsed = CalculateManaUsage(spell, target, out manaRefund);
        }
        else if (castingPreCheckStatus == CastingPreCheckStatus.CastFailed)
        {
            manaUsed = 5; // todo: verify with retail
        }

        var currentMana = Mana.Current;
        if (casterItem != null)
        {
            currentMana = (uint)(casterItem.ItemCurMana ?? 0);
        }

        if (manaUsed > currentMana)
        {
            SendUseDoneEvent(WeenieError.YouDontHaveEnoughManaToCast);
            return false;
        }

        Proficiency.OnSuccessUse(this, GetCreatureSkill(Skill.ManaConversion), spell.PowerMod);

        return true;
    }

    private void TryBurnComponents(Spell spell)
    {
        if (SafeSpellComponents || PropertyManager.GetBool("safe_spell_comps").Item)
        {
            return;
        }

        var burned = spell.TryBurnComponents(this);
        if (burned.Count == 0)
        {
            return;
        }

        // decrement components
        for (var i = burned.Count - 1; i >= 0; i--)
        {
            var component = burned[i];

            if (!SpellFormula.SpellComponentsTable.SpellComponents.TryGetValue(component, out var spellComponent))
            {
                _log.Error($"{Name}.TryBurnComponents(): Couldn't find SpellComponent {component}");
                continue;
            }

            var wcid = Spell.GetComponentWCID(component);
            if (wcid == 0)
            {
                continue;
            }

            var item = GetInventoryItemsOfWCID(wcid).FirstOrDefault();
            if (item == null)
            {
                if (SpellComponentsRequired && PropertyManager.GetBool("require_spell_comps").Item)
                {
                    _log.Warning($"{Name}.TryBurnComponents({spellComponent.Name}): not found in inventory");
                }
                else
                {
                    burned.RemoveAt(i);
                }

                continue;
            }
            TryConsumeFromInventoryWithNetworking(item, 1);
        }

        if (burned.Count == 0)
        {
            return;
        }

        // send message to player
        var msg = Spell.GetConsumeString(burned);
        Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.Magic));
    }

    /// <summary>
    /// Returns TRUE if the player has the required number of components to cast spell
    /// </summary>
    private bool HasComponentsForSpell(Spell spell)
    {
        spell.Formula.GetPlayerFormula(this);

        if (!SpellComponentsRequired || !PropertyManager.GetBool("require_spell_comps").Item)
        {
            return true;
        }

        var requiredComps = spell.Formula.GetRequiredComps();

        foreach (var kvp in requiredComps)
        {
            var wcid = kvp.Key;
            var required = kvp.Value;

            var available = GetNumInventoryItemsOfWCID(wcid);

            if (required > available)
            {
                return false;
            }
        }
        return true;
    }
}
