using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class SpellProjectile
{
    /// <summary>
    /// The damage and damage resistance rating multipliers for a spell projectile hit.
    /// Each one is 1.0 when it has no effect.
    /// </summary>
    private struct SpellRatingModifiers
    {
        public float Heritage;
        public float SneakAttack;

        /// <summary>
        /// The damage rating, combined with the heritage and sneak attack bonuses,
        /// the critical damage rating on critical hits, and the PK damage rating in PK battles
        /// </summary>
        public float DamageRating;

        public float CritDamageRating;
        public float PkDamageRating;

        /// <summary>
        /// The target's damage resistance rating, combined with the critical and PK damage resistance ratings when they apply
        /// </summary>
        public float DamageResistRating;

        public float CritDamageResistRating;
        public float PkDamageResistRating;

        public static SpellRatingModifiers None =>
            new()
            {
                Heritage = 1.0f,
                SneakAttack = 1.0f,
                DamageRating = 1.0f,
                CritDamageRating = 1.0f,
                PkDamageRating = 1.0f,
                DamageResistRating = 1.0f,
                CritDamageResistRating = 1.0f,
                PkDamageResistRating = 1.0f,
            };
    }

    /// <summary>
    /// Called for a spell projectile to damage its target
    /// </summary>
    private void DamageTarget(
        Creature target,
        float damage,
        bool critical,
        bool critDefended,
        bool overpower,
        PartialEvasion partialEvasion
    )
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

        var ratings = SpellRatingModifiers.None;
        WorldObject equippedCloak = null;
        uint amount;
        float percent;

        // life projectiles that drain stamina or mana
        var drainsVital = Spell.Category is SpellCategory.StaminaLowering or SpellCategory.ManaLowering;

        if (drainsVital)
        {
            amount = DrainVital(target, damage, out percent);
        }
        else
        {
            ratings = GetRatingModifiers(target, critical);

            damage *= ratings.DamageRating * ratings.DamageResistRating;

            equippedCloak = target.EquippedCloak;

            amount = ApplyHealthDamage(target, ref damage, equippedCloak, out percent);
        }

        // add threat to damaged targets
        if (target.IsMonster && sourcePlayer != null)
        {
            var percentOfTargetMaxHealth = (float)amount / target.Health.MaxValue;
            target.IncreaseTargetThreatLevel(sourcePlayer, (int)(percentOfTargetMaxHealth * 1000));
        }

        ShowRatingDebugInfo(target, sourceCreature, ratings, damage);

        if (target.IsAlive)
        {
            SendHitMessages(
                target,
                amount,
                percent,
                critical,
                critDefended,
                overpower,
                partialEvasion,
                ratings.SneakAttack
            );

            if (!drainsVital)
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
        else
        {
            HandleTargetDeath(target, critical);
        }

        HandlePostDamageRatingEffects(target, damage, sourcePlayer, targetPlayer, sourceCreature, Spell, SpellType); // (jewel effects)
    }

    /// <summary>
    /// Drains the target's stamina or mana, returning the amount drained
    /// </summary>
    private uint DrainVital(Creature target, float damage, out float percent)
    {
        var vital = Spell.Category == SpellCategory.StaminaLowering ? target.Stamina : target.Mana;

        percent = damage / vital.MaxValue;

        return (uint)-target.UpdateVitalDelta(vital, (int)-Math.Round(damage));
    }

    private SpellRatingModifiers GetRatingModifiers(Creature target, bool critical)
    {
        var ratings = SpellRatingModifiers.None;

        var targetPlayer = target as Player;
        var sourceCreature = ProjectileSource as Creature;
        var sourcePlayer = ProjectileSource as Player;

        // COMBAT ABILITY - Reflect: damage ratings for a reflected spell come from the original caster
        var damageSource = ReflectedCaster ?? sourceCreature;

        var pkBattle = sourcePlayer != null && targetPlayer != null;

        // for possibly applying sneak attack to magic projectiles,
        // only do this for health-damaging projectiles?
        if (sourcePlayer != null && ReflectedCaster == null)
        {
            // TODO: use target direction vs. projectile position, instead of player position
            // could sneak attack be applied to void DoTs?
            ratings.SneakAttack = sourcePlayer.GetSneakAttackMod(target);
            ratings.Heritage = sourcePlayer.GetHeritageBonus(sourcePlayer.GetEquippedWand()) ? 1.05f : 1.0f;
        }
        // Calc sneak bonus for monsters
        else if (targetPlayer != null && sourceCreature != null && ReflectedCaster == null)
        {
            ratings.SneakAttack = sourceCreature.GetSneakAttackMod(targetPlayer);
        }

        var damageRating = damageSource?.GetDamageRating() ?? 0;
        ratings.DamageRating = Creature.AdditiveCombine(
            Creature.GetPositiveRatingMod(damageRating),
            ratings.Heritage,
            ratings.SneakAttack
        );

        if (critical)
        {
            ratings.CritDamageRating = Creature.GetPositiveRatingMod(damageSource?.GetCritDamageRating() ?? 0);
            ratings.CritDamageResistRating = Creature.GetNegativeRatingMod(target.GetCritDamageResistRating());

            ratings.DamageRating = Creature.AdditiveCombine(ratings.DamageRating, ratings.CritDamageRating);
        }

        if (pkBattle)
        {
            ratings.PkDamageRating = Creature.GetPositiveRatingMod(damageSource.GetPKDamageRating());
            ratings.PkDamageResistRating = Creature.GetNegativeRatingMod(target.GetPKDamageResistRating());

            ratings.DamageRating = Creature.AdditiveCombine(ratings.DamageRating, ratings.PkDamageRating);
        }

        ratings.DamageResistRating = DamageFormulas.CombineDamageResistRatings(
            target.GetDamageResistRatingMod(CombatType.Magic),
            critical ? ratings.CritDamageResistRating : null,
            pkBattle ? ratings.PkDamageResistRating : null
        );

        return ratings;
    }

    /// <summary>
    /// Damages the target's health, after the target's cloak may proc to reduce the damage.
    /// COMBAT ABILITY - Mana Barrier: a player may take part of the damage as mana instead.
    /// Returns the damage dealt.
    /// </summary>
    /// <param name="damage">The damage, reduced by a cloak proc</param>
    /// <param name="percent">The damage as a fraction of the target's max health</param>
    private uint ApplyHealthDamage(Creature target, ref float damage, WorldObject equippedCloak, out float percent)
    {
        percent = damage / target.Health.MaxValue;

        if (equippedCloak != null && Cloak.HasDamageProc(equippedCloak) && Cloak.RollProc(equippedCloak, percent))
        {
            var reducedDamage = Cloak.GetReducedAmount(ProjectileSource, damage);

            Cloak.ShowMessage(target, ProjectileSource, damage, reducedDamage);

            damage = reducedDamage;
            percent = damage / target.Health.MaxValue;
        }

        var amount = Convert.ToUInt32(damage);

        if (target is Player { ManaBarrierIsActive: true } targetPlayer)
        {
            return Player.CombatAbilityManaBarrier(targetPlayer, amount, ProjectileSource, Spell.DamageType);
        }

        target.UpdateVitalDelta(target.Health, (int)-Math.Round(damage));
        target.DamageHistory.Add(ProjectileSource, Spell.DamageType, amount);

        return amount;
    }

    /// <summary>
    /// Tells the caster and the target how much damage the spell did
    /// </summary>
    private void SendHitMessages(
        Creature target,
        uint amount,
        float percent,
        bool critical,
        bool critDefended,
        bool overpower,
        PartialEvasion partialEvasion,
        float sneakAttackMod
    )
    {
        var sourceCreature = ProjectileSource as Creature;
        var sourcePlayer = ProjectileSource as Player;
        var targetPlayer = target as Player;

        string verb = null,
            plural = null;
        Strings.GetAttackVerb(Spell.DamageType, percent, ref verb, ref plural);

        var elementalistRating = Math.Round(
            Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearElementalist, "Elementalist") * 100
        );
        var elementalistMsg = elementalistRating > 0.0f ? $"Elementalist {elementalistRating}%! " : "";

        var critMsg = critical ? "Critical hit! " : "";
        var sneakMsg = sneakAttackMod > 1.0f ? "Sneak Attack! " : "";
        var overpowerMsg = overpower ? "Overpower! " : "";

        var resistSome = partialEvasion == PartialEvasion.Some ? "Partial Resist! " : "";
        var strikeThrough = Strikethrough > 0 ? "Strikethrough! " : "";

        var drainsVital = Spell.Category is SpellCategory.StaminaLowering or SpellCategory.ManaLowering;
        var vital = Spell.Category == SpellCategory.StaminaLowering ? "stamina" : "mana";

        if (sourcePlayer != null)
        {
            var critProt = critDefended ? " Your critical hit was avoided with their augmentation!" : "";

            var chargedMsg = sourcePlayer.GetChargedMessage();

            var attackerMsg =
                $"{resistSome}{strikeThrough}{critMsg}{overpowerMsg}{chargedMsg}{sneakMsg}{elementalistMsg}You {verb} {target.Name} for {amount} points with {Spell.Name}.{critProt}";

            // could these crit / sneak attack?
            if (drainsVital)
            {
                attackerMsg = $"With {Spell.Name} you drain {amount} points of {vital} from {target.Name}.";
            }

            if (!sourcePlayer.SquelchManager.Squelches.Contains(target, ChatMessageType.Magic))
            {
                sourcePlayer.Session.Network.EnqueueSend(new GameMessageSystemChat(attackerMsg, ChatMessageType.Magic));
            }
        }

        if (targetPlayer != null)
        {
            var critProt = critDefended ? " Your augmentation allows you to avoid a critical hit!" : "";

            var defenderMsg =
                $"{resistSome}{critMsg}{overpowerMsg}{sneakMsg}{ProjectileSource.Name} {plural} you for {amount} points with {Spell.Name}.{critProt}";

            if (drainsVital)
            {
                defenderMsg = $"{ProjectileSource.Name} casts {Spell.Name} and drains {amount} points of your {vital}.";
            }

            if (!targetPlayer.SquelchManager.Squelches.Contains(ProjectileSource, ChatMessageType.Magic))
            {
                targetPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat(defenderMsg, ChatMessageType.Magic));
            }

            if (sourceCreature != null)
            {
                targetPlayer.SetCurrentAttacker(sourceCreature);
            }
        }
    }

    private void HandleTargetDeath(Creature target, bool critical)
    {
        if (target is Player { IsInDeathProcess: false } targetPlayer)
        {
            targetPlayer.IsInDeathProcess = true;
        }

        var lastDamager = ProjectileSource != null ? new DamageHistoryInfo(ProjectileSource) : null;
        target.OnDeath(lastDamager, Spell.DamageType, critical);
        target.Die();
    }
}
