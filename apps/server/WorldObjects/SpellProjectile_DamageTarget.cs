using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class SpellProjectile
{
    /// <summary>
    /// Called for a spell projectile to damage its target
    /// </summary>
    private void DamageTarget(Creature target, float damage, bool critical, bool critDefended, bool overpower)
    {
        var targetPlayer = target as Player;

        if (target.Invincible || target.IsDead)
        {
            return;
        }

        if (target.Invulnerable)
        {
            damage = 0.0f;
        }

        var sourceCreature = ProjectileSource as Creature;
        var sourcePlayer = ProjectileSource as Player;

        // COMBAT ABILITY - Reflect: damage ratings for a reflected spell come from the original caster
        var damageSource = ReflectedCaster ?? sourceCreature;

        var pkBattle = sourcePlayer != null && targetPlayer != null;

        var amount = 0u;
        var percent = 0.0f;

        var damageRatingMod = 1.0f;
        var heritageMod = 1.0f;
        var sneakAttackMod = 1.0f;
        var critDamageRatingMod = 1.0f;
        var pkDamageRatingMod = 1.0f;

        var damageResistRatingMod = 1.0f;
        var critDamageResistRatingMod = 1.0f;
        var pkDamageResistRatingMod = 1.0f;

        WorldObject equippedCloak = null;

        // handle life projectiles for stamina / mana
        if (Spell.Category == SpellCategory.StaminaLowering)
        {
            percent = damage / target.Stamina.MaxValue;
            amount = (uint)-target.UpdateVitalDelta(target.Stamina, (int)-Math.Round(damage));
        }
        else if (Spell.Category == SpellCategory.ManaLowering)
        {
            percent = damage / target.Mana.MaxValue;
            amount = (uint)-target.UpdateVitalDelta(target.Mana, (int)-Math.Round(damage));
        }
        else
        {
            // for possibly applying sneak attack to magic projectiles,
            // only do this for health-damaging projectiles?
            if (sourcePlayer != null && ReflectedCaster == null)
            {
                // TODO: use target direction vs. projectile position, instead of player position
                // could sneak attack be applied to void DoTs?
                sneakAttackMod = sourcePlayer.GetSneakAttackMod(target);
                heritageMod = sourcePlayer.GetHeritageBonus(sourcePlayer.GetEquippedWand()) ? 1.05f : 1.0f;
            }
            // Calc sneak bonus for monsters
            else if (targetPlayer != null && sourceCreature != null && ReflectedCaster == null)
            {
                sneakAttackMod = sourceCreature.GetSneakAttackMod(targetPlayer);
            }

            var damageRating = damageSource?.GetDamageRating() ?? 0;
            damageRatingMod = Creature.AdditiveCombine(
                Creature.GetPositiveRatingMod(damageRating),
                heritageMod,
                sneakAttackMod
            );

            damageResistRatingMod = target.GetDamageResistRatingMod(CombatType.Magic);

            if (critical)
            {
                critDamageRatingMod = Creature.GetPositiveRatingMod(damageSource?.GetCritDamageRating() ?? 0);
                critDamageResistRatingMod = Creature.GetNegativeRatingMod(target.GetCritDamageResistRating());

                damageRatingMod = Creature.AdditiveCombine(damageRatingMod, critDamageRatingMod);
                damageResistRatingMod = Creature.AdditiveCombine(damageResistRatingMod, critDamageResistRatingMod);
            }
            if (pkBattle)
            {
                pkDamageRatingMod = Creature.GetPositiveRatingMod(damageSource.GetPKDamageRating());
                pkDamageResistRatingMod = Creature.GetNegativeRatingMod(target.GetPKDamageResistRating());

                damageRatingMod = Creature.AdditiveCombine(damageRatingMod, pkDamageRatingMod);
                damageResistRatingMod = Creature.AdditiveCombine(damageResistRatingMod, pkDamageResistRatingMod);
            }

            damage *= damageRatingMod * damageResistRatingMod;

            percent = damage / target.Health.MaxValue;

            equippedCloak = target.EquippedCloak;

            if (equippedCloak != null && Cloak.HasDamageProc(equippedCloak) && Cloak.RollProc(equippedCloak, percent))
            {
                var reducedDamage = Cloak.GetReducedAmount(ProjectileSource, damage);

                Cloak.ShowMessage(target, ProjectileSource, damage, reducedDamage);

                damage = reducedDamage;
                percent = damage / target.Health.MaxValue;
            }

            amount = Convert.ToUInt32(damage);

            if (targetPlayer is {ManaBarrierIsActive: true})
            {
                amount = Player.CombatAbilityManaBarrier(targetPlayer, amount, ProjectileSource, Spell.DamageType);
            }
            else
            {
                target.UpdateVitalDelta(target.Health, (int)-Math.Round(damage));
                target.DamageHistory.Add(ProjectileSource, Spell.DamageType, amount);
            }
        }

        // add threat to damaged targets
        if (target.IsMonster && sourcePlayer != null)
        {
            var percentOfTargetMaxHealth = (float)amount / target.Health.MaxValue;
            target.IncreaseTargetThreatLevel(sourcePlayer, (int)(percentOfTargetMaxHealth * 1000));
        }

        // show debug info
        if (sourceCreature != null && sourceCreature.DebugDamage.HasFlag(Creature.DebugDamageType.Attacker))
        {
            ShowInfo(
                sourceCreature,
                heritageMod,
                sneakAttackMod,
                damageRatingMod,
                damageResistRatingMod,
                critDamageRatingMod,
                critDamageResistRatingMod,
                pkDamageRatingMod,
                pkDamageResistRatingMod,
                damage
            );
        }
        if (target.DebugDamage.HasFlag(Creature.DebugDamageType.Defender))
        {
            ShowInfo(
                target,
                heritageMod,
                sneakAttackMod,
                damageRatingMod,
                damageResistRatingMod,
                critDamageRatingMod,
                critDamageResistRatingMod,
                pkDamageRatingMod,
                pkDamageResistRatingMod,
                damage
            );
        }

        if (target.IsAlive)
        {
            string verb = null,
                plural = null;
            Strings.GetAttackVerb(Spell.DamageType, percent, ref verb, ref plural);

            var elementalistRating = Math.Round(Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearElementalist, "Elementalist") * 100);
            var elementalistMsg = elementalistRating > 0.0f ? $"Elementalist {elementalistRating}%! " : "";

            var critMsg = critical ? "Critical hit! " : "";
            var sneakMsg = sneakAttackMod > 1.0f ? "Sneak Attack! " : "";
            var overpowerMsg = overpower ? "Overpower! " : "";

            var chargedMsg = "";

            var resistSome = _partialEvasion == PartialEvasion.Some ? "Partial resist! " : "";
            var strikeThrough = Strikethrough > 0 ? "Strikethrough! " : "";

            var nonHealth = Spell.Category is SpellCategory.StaminaLowering or SpellCategory.ManaLowering;

            if (sourcePlayer != null)
            {
                var critProt = critDefended ? " Your critical hit was avoided with their augmentation!" : "";

                if (sourcePlayer is {OverloadStanceIsActive: true} or {BatteryStanceIsActive: true})
                {
                    var chargedPercent = Math.Round(sourcePlayer.ManaChargeMeter * 100);
                    chargedMsg = $"{chargedPercent}% Charged! ";
                }

                chargedMsg = sourcePlayer switch
                {
                    { OverloadDischargeIsActive: true } => "Overload Discharge! ",
                    { BatteryDischargeIsActive: true } => "Battery Discharge! ",
                    _ => chargedMsg
                };

                var attackerMsg = $"{resistSome}{strikeThrough}{critMsg}{overpowerMsg}{chargedMsg}{sneakMsg}{elementalistMsg}You {verb} {target.Name} for {amount} points with {Spell.Name}.{critProt}";

                // could these crit / sneak attack?
                if (nonHealth)
                {
                    var vital = Spell.Category == SpellCategory.StaminaLowering ? "stamina" : "mana";
                    attackerMsg = $"With {Spell.Name} you drain {amount} points of {vital} from {target.Name}.";
                }

                if (!sourcePlayer.SquelchManager.Squelches.Contains(target, ChatMessageType.Magic))
                {
                    sourcePlayer.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(attackerMsg, ChatMessageType.Magic)
                    );
                }
            }

            if (targetPlayer != null)
            {
                var critProt = critDefended ? " Your augmentation allows you to avoid a critical hit!" : "";

                var defenderMsg =
                    $"{resistSome}{critMsg}{overpowerMsg}{sneakMsg}{ProjectileSource.Name} {plural} you for {amount} points with {Spell.Name}.{critProt}";

                if (nonHealth)
                {
                    var vital = Spell.Category == SpellCategory.StaminaLowering ? "stamina" : "mana";
                    defenderMsg =
                        $"{ProjectileSource.Name} casts {Spell.Name} and drains {amount} points of your {vital}.";
                }

                if (!targetPlayer.SquelchManager.Squelches.Contains(ProjectileSource, ChatMessageType.Magic))
                {
                    targetPlayer.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(defenderMsg, ChatMessageType.Magic)
                    );
                }

                if (sourceCreature != null)
                {
                    targetPlayer.SetCurrentAttacker(sourceCreature);
                }
            }

            if (!nonHealth)
            {
                if (equippedCloak != null && Cloak.HasProcSpell(equippedCloak))
                {
                    Cloak.TryProcSpell(target, ProjectileSource, equippedCloak, percent);
                }

                target.EmoteManager.OnDamage(sourcePlayer);

                if (critical)
                {
                    target.EmoteManager.OnReceiveCritical(sourcePlayer);
                }
            }
        }
        else if (targetPlayer is { IsInDeathProcess: false })
        {
            targetPlayer.IsInDeathProcess = true;
            var lastDamager = ProjectileSource != null ? new DamageHistoryInfo(ProjectileSource) : null;
            targetPlayer.OnDeath(lastDamager, Spell.DamageType, critical);
            targetPlayer.Die();
        }
        else
        {
            var lastDamager = ProjectileSource != null ? new DamageHistoryInfo(ProjectileSource) : null;
            target.OnDeath(lastDamager, Spell.DamageType, critical);
            target.Die();
        }

        HandlePostDamageRatingEffects(target, damage, sourcePlayer, targetPlayer, sourceCreature, Spell, SpellType); // (jewel effects)
    }

    private static void HandlePostDamageRatingEffects(Creature target, float damage, Player sourcePlayer, Player targetPlayer, Creature sourceCreature, Spell spell, ProjectileSpellType projectileSpellType)
    {
        if (sourcePlayer != null)
        {
            Jewel.HandleCasterAttackerRampingQuestStamps(sourcePlayer, target, spell, projectileSpellType);
            Jewel.HandlePlayerAttackerBonuses(sourcePlayer, target, damage, spell.DamageType);
        }

        if (targetPlayer != null)
        {
            Jewel.HandleCasterDefenderRampingQuestStamps(targetPlayer, sourceCreature);
            Jewel.HandlePlayerDefenderBonuses(targetPlayer, sourceCreature, damage);
        }
    }
}
