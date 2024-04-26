using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects
{
    partial class Creature
    {
        public uint CalculateManaUsage(Creature caster, Spell spell, WorldObject target = null)
        {
            uint baseCost = spell.BaseMana;

            var manaResourcePen = 1.0f;
            if (spell.School != MagicSchool.PortalMagic)
                manaResourcePen = (float)(1 + GetArmorResourcePenalty());

            baseCost = (uint)(baseCost * manaResourcePen);

            // for casting spells built into a casting implement, use the ItemManaCost
            var castItem = caster.GetEquippedWand();
            if (castItem != null && (castItem.SpellDID ?? 0) == spell.Id)
                baseCost = (uint)(castItem.ItemManaCost ?? 0);

            if ((spell.School == MagicSchool.PortalMagic) && (spell.MetaSpellType == SpellType.Enchantment) &&
                (spell.Category >= SpellCategory.ArmorValueRaising) && (spell.Category <= SpellCategory.AcidicResistanceLowering) && target is Player targetPlayer)
            {
                var numTargetItems = 1;
                if (targetPlayer != null)
                    numTargetItems = targetPlayer.EquippedObjects.Values.Count(i => (i is Clothing || i.IsShield) && i.IsEnchantable);

                baseCost += spell.ManaMod * (uint)numTargetItems;
            }
            else if ((spell.Flags & SpellFlags.FellowshipSpell) != 0)
            {
                var numFellows = 1;
                if (this is Player player && player.Fellowship != null)
                    numFellows = player.Fellowship.FellowshipMembers.Count;

                baseCost += spell.ManaMod * (uint)numFellows;
            }

            // Check Overload and Battery Focuses
            var combatAbility = CombatAbility.None;
            var combatFocus = GetEquippedCombatFocus();
            if (combatFocus != null)
                combatAbility = combatFocus.GetCombatAbility();

            // Overload - Increased cost up to 50%+ with Overload stacks
            if (combatAbility == CombatAbility.Overload && this.QuestManager.HasQuest($"{this.Name},Overload"))
            {
                if (spell.Flags.HasFlag(SpellFlags.FastCast))
                    baseCost = 5 * spell.Level;

                var overloadStacks = this.QuestManager.GetCurrentSolves($"{this.Name},Overload");
                float overloadMod = 1 + (overloadStacks / 1000);
                baseCost = (uint)(baseCost * overloadMod);
            }
            // Battery - 20% mana cost reduction minimum, increasing with lower mana or 0 cost during Battery Activated
            else if (combatAbility == CombatAbility.Battery)
            {
                if (this is Player player && player.LastBatteryActivated > Time.GetUnixTime() - player.BatteryActivatedDuration)
                {
                    baseCost = 0;
                }
                else
                {
                    var maxMana = (float)Mana.MaxValue;
                    var currentMana = (float)Mana.Current == 0 ? 1 : (float)Mana.Current;

                    float manaMod = 1 - (maxMana - currentMana) / maxMana;
                    var batteryMod = manaMod > 0.8f ? 0.8f : manaMod;
                    baseCost = (uint)(baseCost * batteryMod);
                }
            }

            // Mana Conversion
            var manaConversion = caster.GetCreatureSkill(Skill.ManaConversion);

            if (manaConversion.AdvancementClass < SkillAdvancementClass.Trained || spell.Flags.HasFlag(SpellFlags.IgnoresManaConversion))
                return baseCost;

            uint difficulty = spell.Level * 50;

            var robeManaConversionMod = 0.0;
            var robe = EquippedObjects.Values.FirstOrDefault(e => e.CurrentWieldedLocation == EquipMask.Armor);
            if (robe != null)
                robeManaConversionMod = robe.ManaConversionMod ?? 0;

            var mana_conversion_skill = (uint)Math.Round(manaConversion.Current * (GetWeaponManaConversionModifier(caster) + robeManaConversionMod));

            // Final Calculation
            var manaCost = GetManaCost(caster, difficulty, baseCost, mana_conversion_skill);

            return manaCost;
        }

        public static uint GetManaCost(Creature caster, uint difficulty, uint manaCost, uint manaConv)
        {
            if (manaConv == 0 || manaCost <= 1)
                return manaCost;

            var successChance = SkillCheck.GetSkillChance(manaConv, difficulty);
            var roll = ThreadSafeRandom.Next(0.0f, 1.0f);

            if (roll < successChance)
            {
                var maxManaReduction = 0.5f;
                var reductionRoll = maxManaReduction * ThreadSafeRandom.Next(0.0f, (float)successChance);
                var savedMana = (uint)Math.Round(manaCost * reductionRoll);

                manaCost = manaCost - savedMana;

                if (caster.GetCreatureSkill(Skill.ManaConversion).AdvancementClass == SkillAdvancementClass.Specialized)
                {
                    var conversionAmount = (int)Math.Round(savedMana * 0.5f);
                    caster.UpdateVitalDelta(caster.Health, conversionAmount);
                    caster.UpdateVitalDelta(caster.Stamina, conversionAmount);
                }
            }

            return Math.Max(manaCost, 1);
        }

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
                        player.Session.Network.EnqueueSend(new GameEventCommunicationTransientString(player.Session, $"SpellID {spellID} Invalid."));
                    else
                        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"{spell.Name} spell not implemented, yet!", ChatMessageType.System));
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
                        HandleCastSpell(spell, this, item, item, equip: true);
                    else
                        HandleCastSpell(spell, item, item, item, equip: true);

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
            if (item == null) return;

            var spell = new Spell(spellId);

            if (spell._spellBase == null)
            {
                if (this is Player player)
                    player.Session.Network.EnqueueSend(new GameEventCommunicationTransientString(player.Session, $"SpellId {spellId} Invalid."));

                return;
            }

            var target = spell.School == MagicSchool.PortalMagic && !spell.HasItemCategory ? item : this;

            // Retrieve enchantment on target and remove it, if present
            var propertiesEnchantmentRegistry = target.EnchantmentManager.GetEnchantment(spellId, item.Guid.Full);

            if (propertiesEnchantmentRegistry != null)
            {
                if (!silent)
                    target.EnchantmentManager.Remove(propertiesEnchantmentRegistry);
                else
                    target.EnchantmentManager.Dispel(propertiesEnchantmentRegistry);
            }
        }

        /// <summary>
        /// Returns the creature's effective magic defense skill
        /// with item.WeaponMagicDefense and imbues factored in
        /// </summary>
        public uint GetEffectiveMagicDefense()
        {
            var current = GetCreatureSkill(Skill.MagicDefense).Current;
            var weaponDefenseMod = GetWeaponMagicDefenseModifier(this);
            var defenseImbues = (uint)GetDefenseImbues(ImbuedEffectType.MagicDefense);

            var effectiveMagicDefense = (uint)Math.Round((current * weaponDefenseMod) + defenseImbues);

            //Console.WriteLine($"EffectiveMagicDefense: {effectiveMagicDefense}");

            return effectiveMagicDefense;
        }
    }
}
