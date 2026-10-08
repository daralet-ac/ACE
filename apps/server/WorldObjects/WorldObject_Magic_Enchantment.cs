using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;
using ACE.Server.WorldObjects.Managers;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    /// <summary>
    /// Handles casting SpellType.Enchantment / FellowEnchantment spells
    /// this is also called if SpellType.EnchantmentProjectile successfully hits
    /// </summary>
    public void CreateEnchantment(
        WorldObject target,
        WorldObject caster,
        WorldObject weapon,
        Spell spell,
        bool equip = false,
        bool fromProc = false,
        bool showMsg = true
    )
    {
        // weird itemCaster -> caster collapsing going on here -- fixme
        
        var player = this as Player;

        var aetheriaProc = false;
        var cloakProc = false;

        // technically unsafe, should be using fromProc
        if (caster.ProcSpell == spell.Id)
        {
            if (caster is Gem && Aetheria.IsAetheria(caster.WeenieClassId))
            {
                caster = this;
                aetheriaProc = true;
            }
            else if (Cloak.IsCloak(caster))
            {
                caster = this;
                cloakProc = true;
            }
        }
        else if (fromProc)
        {
            // fromProc is assumed to be cloakProc currently
            // todo: change fromProc from bool to WorldObject
            // do we need separate concepts for itemCaster and fromProc objects?
            caster = this;
            cloakProc = true;
        }

        // create enchantment
        var addResult = target.EnchantmentManager.Add(spell, caster, weapon, equip);

        // Ward reduction of Creature and Life debuffs
        if (target is Player && !IsWardExcludedSpell(spell))
        {
            var targetPlayer = target as Player;

            var wardBuffDebuffMod = targetPlayer.EnchantmentManager.GetWardMultiplicativeMod();

            var targetPlayerWard = targetPlayer.GetWardLevel() * wardBuffDebuffMod;

            if (addResult.Enchantment.StatModValue < 0 && targetPlayerWard > 0)
            {
                var ignoreWardMod = 1.0f;

                if (player != null)
                {
                    ignoreWardMod = player.GetIgnoreWardMod(weapon);

                    if (weapon != null && weapon.HasImbuedEffect(ImbuedEffectType.WardRending))
                    {
                        ignoreWardMod -= GetWardRendingMod(player.GetCreatureSkill(Skill.LifeMagic));
                    }

                    ignoreWardMod *= 1.0f - Jewel.GetJewelEffectMod(player, PropertyInt.GearWardPen, "WardPen");
                }

                var wardMod = GetWardMod(caster as Creature, targetPlayer, ignoreWardMod);

                wardMod += (1 - wardMod) * 0.5f;

                // ward shortens the debuff, it doesn't weaken it
                addResult.Enchantment.Duration *= wardMod;
            }
        }

        // build message
        var suffix = "";
        switch (addResult.StackType)
        {
            case StackType.Surpass:
                suffix = $", surpassing {addResult.SurpassSpell.Name}";
                break;
            case StackType.Refresh:
                suffix = $", refreshing {addResult.RefreshSpell.Name}";
                break;
            case StackType.Surpassed:
                suffix = $", but it is surpassed by {addResult.SurpassedSpell.Name}";
                break;
        }

        if (aetheriaProc)
        {
            var message = new GameMessageSystemChat(
                $"Aetheria surges on {target.Name} with the power of {spell.Name}!",
                ChatMessageType.Spellcasting
            );

            EnqueueBroadcast(message, LocalBroadcastRange, ChatMessageType.Spellcasting);
        }
        else if (player != null && !cloakProc)
        {
            // TODO: replace with some kind of 'rootOwner unless equip' concept?
            // for item casters where the message should be 'You cast', we still need pass the caster as item
            // down this far, to prevent using player's AugmentationIncreasedSpellDuration
            var casterCheck = caster == this || caster is Gem || caster is Food;

            if (casterCheck || target == this || caster != target)
            {
                var chargedMsg = player.GetChargedMessage();

                var casterName = casterCheck ? "You" : caster.Name;
                var targetName = target.Name;
                if (target == this)
                {
                    targetName = casterCheck ? "yourself" : "you";
                    chargedMsg = "";
                }

                if (showMsg)
                {
                    player.SendChatMessage(
                        player,
                        $"{chargedMsg}{casterName} cast {spell.Name} on {targetName}{suffix}",
                        ChatMessageType.Magic
                    );
                }
            }
        }

        var playerTarget = target as Player;

        if (playerTarget != null)
        {
            playerTarget.Session.Network.EnqueueSend(
                new GameEventMagicUpdateEnchantment(
                    playerTarget.Session,
                    new Enchantment(playerTarget, addResult.Enchantment)
                )
            );

            playerTarget.HandleSpellHooks(spell);

            if (!spell.IsBeneficial && this is Creature creatureCaster)
            {
                playerTarget.SetCurrentAttacker(creatureCaster);
            }
        }

        if (playerTarget == null && target.Wielder is Player wielder)
        {
            playerTarget = wielder;
        }

        if (playerTarget == null || playerTarget == this || cloakProc)
        {
            return;
        }

        {
            var targetName = target == playerTarget ? "you" : $"your {target.Name}";

            if (showMsg)
            {
                playerTarget.SendChatMessage(
                    this,
                    $"{caster.Name} cast {spell.Name} on {targetName}{suffix}",
                    ChatMessageType.Magic
                );
            }
        }
    }

    /// <summary>
    /// Calculates the StatModVal x buffs to enter into the enchantment registry
    /// </summary>
    /// <param name="spell">A spell with a DotDuration</param>
    public float CalculateDotEnchantment_StatModValue(
    Spell spell,
    WorldObject target,
    WorldObject weapon,
    float statModVal
)
    {
        if (spell.DotDuration == 0)
        {
            return statModVal;
        }

        var enchantment_statModVal = statModVal;

        var creatureTarget = target as Creature;

        if (spell.Category == SpellCategory.AetheriaProcHealthOverTimeRaising)
        {
            return enchantment_statModVal;
        }

        if (spell.Category == SpellCategory.AetheriaProcDamageOverTimeRaising)
        {
            return enchantment_statModVal;
        }

        var player = this as Player;
        var creatureSource = this as Creature;

        var equippedWeapon = player?.GetEquippedWeapon() ?? player?.GetEquippedWand();

        var damageRatingMod = 1.0f;

        if (creatureSource != null)
        {
            var damageRating = creatureSource.GetDamageRating();

            if (player != null)
            {
                if (player.GetHeritageBonus(equippedWeapon))
                {
                    damageRating += 5;
                }

                if (target is Player)
                {
                    damageRating += player.GetPKDamageRating();
                }
            }
            damageRatingMod = Creature.GetPositiveRatingMod(damageRating);
        }

        if (spell.Category is SpellCategory.DFBleedDamage)
        {
            return enchantment_statModVal * damageRatingMod;
        }

        if (
            spell.Category != SpellCategory.NetherDamageOverTimeRaising
            && spell.Category != SpellCategory.NetherDamageOverTimeRaising2
            && spell.Category != SpellCategory.NetherDamageOverTimeRaising3
            && spell.Category != SpellCategory.BleedDamage
            && spell.Category != SpellCategory.HealKitRegen
            && spell.Category != SpellCategory.StaminaKitRegen
            && spell.Category != SpellCategory.ManaKitRegen
            && spell.Category != SpellCategory.VitalityMend
            && spell.Category != SpellCategory.VigorMend
            && spell.Category != SpellCategory.ClarityMend
        )
        {
            _log.Error(
                $"{Name}.CalculateDamageOverTimeBase({spell.Id} - {spell.Name}, {target?.Name}) - unknown dot spell category {spell.Category}"
            );
            return enchantment_statModVal;
        }

        if (spell.Category is SpellCategory.HealKitRegen or SpellCategory.StaminaKitRegen or SpellCategory.ManaKitRegen)
        {
            return enchantment_statModVal;
        }

        var elementalDamageMod = 1.0f;
        var attributeDamageMod = 1.0f;

        if (creatureSource != null)
        {
            elementalDamageMod = GetCasterElementalDamageModifier(
                equippedWeapon,
                creatureSource,
                creatureTarget,
                spell.DamageType
            );

            attributeDamageMod = creatureSource.GetAttributeMod(creatureSource.GetEquippedWeapon(), true);
        }

        enchantment_statModVal *= elementalDamageMod * attributeDamageMod * damageRatingMod;

        return enchantment_statModVal;
    }

    public void TryCastItemEnchantment_WithRedirects(Spell spell, WorldObject target, WorldObject itemCaster = null)
    {
        var caster = itemCaster ?? this;

        var creature = this as Creature;
        var player = this as Player;

        var targetCreature = target as Creature;
        var targetPlayer = target as Player;

        // if negative item spell, can be resisted by the wielder
        if (spell.IsHarmful)
        {
            var targetResist = targetCreature;

            if (targetResist == null && target?.WielderId != null)
            {
                targetResist = CurrentLandblock?.GetObject(target.WielderId.Value) as Creature;
            }

            // skip TryResistSpell() for non-player casters, they already performed it previously
            if (player != null && targetResist != null)
            {
                if (TryResistSpell(targetResist, spell, out _, caster))
                {
                    return;
                }
            }
            // should this be set if the spell is invalid / 'fails to affect' below?
            if (creature != null && targetResist is Player playerTargetResist)
            {
                playerTargetResist.SetCurrentAttacker(creature);
            }
        }

        if (spell.IsImpenBaneType)
        {
            // impen / bane / brittlemail / lure

            // a lot of these will already be filtered out by IsInvalidTarget()
            if (targetCreature == null)
            {
                // targeting an individual item / wo
                HandleCastSpell(spell, target);
            }
            else
            {
                // targeting a creature
                if (targetPlayer == this)
                {
                    // targeting self
                    if (creature != null)
                    {
                        var items = creature
                            .EquippedObjects.Values.Where(i =>
                                (i.WeenieType == WeenieType.Clothing || i.IsShield) && i.IsEnchantable
                            )
                            .ToList();

                        foreach (var item in items)
                        {
                            HandleCastSpell(spell, item);
                        }

                        if (items.Count > 0)
                        {
                            DoSpellEffects(spell, this, creature);
                        }
                    }
                }
                else
                {
                    // targeting another player or monster
                    var item = targetCreature.EquippedObjects.Values.FirstOrDefault(i => i.IsShield && i.IsEnchantable);

                    if (item != null)
                    {
                        HandleCastSpell(spell, item);
                    }
                    else
                    {
                        SendFailsToAffectMessages(spell, targetCreature, player, targetPlayer);
                    }
                }
            }
        }
        else if (spell.IsItemRedirectableType)
        {
            // blood loather, spirit loather, lure blade, turn blade, leaden weapon, hermetic void
            if (targetCreature == null)
            {
                // targeting an individual item / wo
                HandleCastSpell(spell, target);
            }
            else
            {
                // targeting a creature, try to redirect to primary weapon
                var weapon = spell.NonComponentTargetType switch
                {
                    ItemType.Weapon => targetCreature.GetEquippedWeapon(),
                    ItemType.Caster => targetCreature.GetEquippedWand(),
                    ItemType.WeaponOrCaster => targetCreature.GetEquippedWeapon() ?? targetCreature.GetEquippedWand(),
                    ItemType.MeleeWeapon => targetCreature.GetEquippedMeleeWeapon(),
                    ItemType.MissileWeapon => targetCreature.GetEquippedMissileWeapon(),
                    _ => null
                };

                if (weapon != null && weapon.IsEnchantable)
                {
                    HandleCastSpell(spell, weapon);
                }
                else
                {
                    SendFailsToAffectMessages(spell, targetCreature, player, targetPlayer);
                }
            }
        }
        else
        {
            // all other item spells, cast directly on target
            HandleCastSpell(spell, target);
        }
    }

    /// <summary>
    /// Tells the caster and the target that an item spell had no item on the target to affect
    /// </summary>
    private void SendFailsToAffectMessages(Spell spell, Creature targetCreature, Player player, Player targetPlayer)
    {
        player?.Session.Network.EnqueueSend(
            new GameMessageSystemChat($"You fail to affect {targetCreature.Name} with {spell.Name}", ChatMessageType.Magic)
        );

        if (targetPlayer != null && !targetPlayer.SquelchManager.Squelches.Contains(this, ChatMessageType.Magic))
        {
            targetPlayer.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"{Name} fails to affect you with {spell.Name}", ChatMessageType.Magic)
            );
        }
    }

    /// <summary>
    /// Spells that are excluded from ward level debuff duration reduction
    /// </summary>
    private static readonly HashSet<SpellId> WardExcludedSpells = new HashSet<SpellId>()
{
    SpellId.Vitae,
    SpellId.RestorationResonance,
    SpellId.VoidRestorationPenalty
};

    /// <summary>
    /// Checks if a spell should be excluded from ward level debuff duration reduction
    /// </summary>
    private static bool IsWardExcludedSpell(Spell spell)
    {
        return WardExcludedSpells.Contains((SpellId)spell.Id);
    }
}
