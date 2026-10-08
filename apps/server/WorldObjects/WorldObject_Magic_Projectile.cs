using System;
using System.Collections.Generic;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    /// <summary>
    /// Handles casting SpellType.Projectile / LifeProjectile / EnchantmentProjectile spells
    /// </summary>
    /// <param name="reflectedCaster">
    /// COMBAT ABILITY - Reflect: when set, this spell was reflected back at its original caster,
    /// and the projectiles use the original caster's stats for their resist check and damage.
    /// </param>
    /// <param name="reflectedLifeProjectileDamage">
    /// For a reflected life projectile, the damage the original caster paid for it.
    /// The reflecting player's own vitals are not drained.
    /// </param>
    private void HandleCastSpell_Projectile(
        Spell spell,
        WorldObject target,
        WorldObject itemCaster,
        WorldObject weapon,
        bool isWeaponSpell,
        bool fromProc,
        int? weaponSpellcraft = null,
        double damageMultiplier = 1.0,
        Creature reflectedCaster = null,
        uint reflectedLifeProjectileDamage = 0
    )
    {
        uint damage = 0;
        var caster = this as Creature;

        var damageType = DamageType.Undef;

        if (reflectedCaster != null)
        {
            damage = reflectedLifeProjectileDamage;
        }
        else if (spell.School == MagicSchool.LifeMagic && caster != null)
        {
            if (spell.DamageType.HasFlag(DamageType.Mana))
            {
                var tryDamage = (int)
                    Math.Round(caster.GetCreatureVital(PropertyAttribute2nd.Mana).Current * spell.DrainPercentage);
                damage = (uint)-caster.UpdateVitalDelta(caster.Mana, -tryDamage);
                damageType = DamageType.Mana;
            }
            else if (spell.DamageType.HasFlag(DamageType.Stamina))
            {
                var tryDamage = (int)
                    Math.Round(caster.GetCreatureVital(PropertyAttribute2nd.Stamina).Current * spell.DrainPercentage);
                damage = (uint)-caster.UpdateVitalDelta(caster.Stamina, -tryDamage);
                damageType = DamageType.Stamina;
            }
            else if (spell.DamageType.HasFlag(DamageType.Health))
            {
                var tryDamage = (int)
                    Math.Round(caster.GetCreatureVital(PropertyAttribute2nd.Health).Current * spell.DrainPercentage);
                damage = (uint)-caster.UpdateVitalDelta(caster.Health, -tryDamage);
                caster.DamageHistory.Add(this, DamageType.Health, damage);
                damageType = DamageType.Health;
            }
            else if (spell.DamageType != DamageType.Undef)
            {
                // Handle rare case where some of these "Life Magic" spells do physical damage e.g. Hunter's Lash 2970 and Thorn Valley 6159
                damageType = spell.DamageType;
            }
            else
            {
                _log.Warning(
                    "Unknown DamageType ({DamageType}) for LifeProjectile {SpellName} - {SpellId}",
                    spell.DamageType,
                    spell.Name,
                    spell.Id
                );
                return;
            }

            if (caster is Player playerCaster and ({ OverloadStanceIsActive: true } or {BatteryStanceIsActive: true}))
            {
                playerCaster.IncreaseChargedMeter(spell, fromProc);
            }
        }

        var projectileSpellType = SpellProjectile.GetProjectileSpellType(spell.Id);

        if (projectileSpellType != ProjectileSpellType.Blast)
        {
            CreateSpellProjectiles(spell, target, weapon, isWeaponSpell, fromProc, damage, false, weaponSpellcraft, damageMultiplier, reflectedCaster);
        }

        var targetCreature = target as Creature;

        if (targetCreature != null && projectileSpellType == ProjectileSpellType.Blast)
        {
            var blastRadius = spell.Id == (uint)SpellId.OlthoiQueenAcidSpray ? 1000 : 10;
            List<Creature> nearbyTargets;
            nearbyTargets = this is Player ? targetCreature.GetNearbyMonsters(blastRadius) : targetCreature.GetNearbyPlayers(blastRadius);

            var blastTargets = new List<Creature> { targetCreature };

            // Max ADDITIONAL targets beyond the primary - i.e. total targets hit caps at
            // spell.NumProjectiles (the DB-set projectile count), not a fixed retail value.
            var maxExtraBlastTargets = Math.Max(0, spell.NumProjectiles - 1);

            if (nearbyTargets != null)
            {
                var blastCount = 0;
                foreach (var nearbyTarget in nearbyTargets)
                {
                    if (blastCount >= maxExtraBlastTargets)
                    {
                        break;
                    }

                    if (nearbyTarget.Translucency == 1 || nearbyTarget.Visibility)
                    {
                        continue;
                    }

                    // In front of the CASTER's own facing, not the primary target's facing - a
                    // player's orientation relative to their neighbors has nothing to do with
                    // where the caster is aiming, which was making this look like it fired in
                    // random directions once the radius was widened for Acid Spray.
                    var angle = (caster ?? targetCreature).GetAngle(nearbyTarget);
                    if (Math.Abs(angle) > Creature.CleaveAngle / 4.0f)
                    {
                        continue;
                    }

                    blastTargets.Add(nearbyTarget);
                    blastCount++;
                }
            }

            foreach (var blastTarget in blastTargets)
            {
                CreateSpellProjectiles(spell, blastTarget, weapon, isWeaponSpell, fromProc, damage, false, weaponSpellcraft, damageMultiplier, reflectedCaster);
            }
        }

        if (reflectedCaster != null)
        {
            return;
        }

        CheckForRatingSlashCleaveBonus(spell, weapon, isWeaponSpell, fromProc, caster, targetCreature, damage);

        if (spell.School == MagicSchool.LifeMagic && caster != null)
        {
            if (caster.Health.Current <= 0)
            {
                // should this be possible?
                var lastDamager = new DamageHistoryInfo(caster);

                caster.OnDeath(lastDamager, damageType, false);
                caster.Die();
            }
        }
    }

    /// <summary>
    /// RATING - Slash: Chance for bonus cleave target with slashing damage.
    /// (JEWEL - Imperial Topaz)
    /// </summary>
    private void CheckForRatingSlashCleaveBonus(Spell spell, WorldObject weapon, bool isWeaponSpell, bool fromProc,
        Creature caster, Creature targetCreature, uint damage)
    {
        // JEWEL - Imperial Topaz - Bonus cleave chance
        if (caster is not Player playerCaster || targetCreature == null)
        {
            return;
        }

        if (spell.DamageType != DamageType.Slash)
        {
            return;
        }

        if (spell.NumProjectiles > 1)
        {
            return;
        }

        var rating = playerCaster.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearSlash);

        if (rating <= 0)
        {
            return;
        }

        var chance = Jewel.GetJewelEffectMod(playerCaster, PropertyInt.GearSlash);

        if (ThreadSafeRandom.Next(0.0f, 1.0f) > chance)
        {
            return;
        }

        var cleave = targetCreature.GetNearbyMonsters(10);

        // the nearest visible monster in front of the caster, other than the target
        foreach (var cleaveHit in cleave)
        {
            if (cleaveHit == targetCreature || cleaveHit.Translucency == 1 || cleaveHit.Visibility)
            {
                continue;
            }

            if (Math.Abs(caster.GetAngle(cleaveHit)) > 90.0f)
            {
                continue;
            }

            CreateSpellProjectiles(spell, cleaveHit, weapon, isWeaponSpell, fromProc, damage);
            break;
        }
    }
}
