using System;
using System.Collections.Generic;
using System.Numerics;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects.Entity;
using DamageType = ACE.Entity.Enum.DamageType;
using Position = ACE.Entity.Position;

namespace ACE.Server.WorldObjects;

public class SpellProjectile : WorldObject
{
    public Spell Spell;
    public ProjectileSpellType SpellType { get; set; }

    public Position SpawnPos { get; set; }
    public float DistanceToTarget { get; set; }
    public uint LifeProjectileDamage { get; set; }

    private PartialEvasion _partialEvasion;

    public int Strikethrough;
    public const int StrikethroughLimit = 3;
    private const double StrikethroughChance = 0.5f;

    private readonly List<uint> _strikethroughTargets = [];

    public SpellProjectileInfo Info { get; set; }

    /// <summary>
    /// Only set to true when this spell was launched by using the built-in spell on a caster
    /// </summary>
    public bool IsWeaponSpell { get; set; }

    /// <summary>
    /// If a spell projectile is from a proc source,
    /// make sure there is no attempt to re-proc again when the spell projectile hits
    /// </summary>
    public bool FromProc { get; set; }

    public int DebugVelocity;

    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public SpellProjectile(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public SpellProjectile(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues()
    {
        // Override weenie description defaults
        ValidLocations = null;
        DefaultScriptId = null;
    }

    /// <summary>
    /// Perfroms additional set up of the spell projectile based on the spell id or its derived type.
    /// </summary>
    public void Setup(Spell spell, ProjectileSpellType spellType)
    {
        Spell = spell;
        SpellType = spellType;

        InitPhysicsObj();

        // Runtime changes to default state
        ReportCollisions = true;
        Missile = true;
        AlignPath = true;
        PathClipped = true;
        IgnoreCollisions = false;

        // FIXME: use data here
        if (!Spell.Name.Equals("Rolling Death"))
        {
            Ethereal = false;
        }

        if (
            SpellType == ProjectileSpellType.Bolt
            || SpellType == ProjectileSpellType.Streak
            || SpellType == ProjectileSpellType.Arc
            || SpellType == ProjectileSpellType.Volley
            || SpellType == ProjectileSpellType.Blast
            || WeenieClassId == 7276
            || WeenieClassId == 7277
            || WeenieClassId == 7279
            || WeenieClassId == 7280
        )
        {
            DefaultScriptId = (uint)PlayScript.ProjectileCollision;
            DefaultScriptIntensity = 1.0f;
        }

        // Some wall spells don't have scripted collisions
        if (WeenieClassId == 7278 || WeenieClassId == 7281 || WeenieClassId == 7282 || WeenieClassId == 23144)
        {
            ScriptedCollision = false;
        }

        AllowEdgeSlide = false;

        // No need to send an ObjScale of 1.0f over the wire since that is the default value
        if (ObjScale == 1.0f)
        {
            ObjScale = null;
        }

        if (SpellType == ProjectileSpellType.Ring)
        {
            if (spell.Id == 3818)
            {
                DefaultScriptId = (uint)PlayScript.Explode;
                DefaultScriptIntensity = 1.0f;
                ScriptedCollision = true;
            }
            else
            {
                ScriptedCollision = false;
            }
        }

        // Projectiles with RotationSpeed get omega values and "align path" turned off which
        // creates the nice swirling animation
        if ((RotationSpeed ?? 0) != 0)
        {
            AlignPath = false;
            if (RotationSpeed != null)
            {
                PhysicsObj.Omega = new Vector3((float)(Math.PI * 2 * RotationSpeed), 0, 0);
            }
        }
    }

    public static ProjectileSpellType GetProjectileSpellType(uint spellID)
    {
        var spell = new Spell(spellID);

        if (spell.Wcid == 0)
        {
            return ProjectileSpellType.Undef;
        }

        if (spell.NumProjectiles == 1)
        {
            if (
                spell.Category >= SpellCategory.AcidStreak && spell.Category <= SpellCategory.SlashingStreak
                || spell.Category == SpellCategory.NetherStreak
                || spell.Category == SpellCategory.Fireworks
            )
            {
                return ProjectileSpellType.Streak;
            }
            else if (spell.NonTracking)
            {
                return ProjectileSpellType.Arc;
            }
            else
            {
                return ProjectileSpellType.Bolt;
            }
        }

        if (
            spell.Category >= SpellCategory.AcidRing && spell.Category <= SpellCategory.SlashingRing
            || Math.Abs(spell.SpreadAngle - 360) < 1
        )
        {
            return ProjectileSpellType.Ring;
        }

        if (
            spell.Category >= SpellCategory.AcidBurst && spell.Category <= SpellCategory.SlashingBurst
            || spell.Category == SpellCategory.NetherDamageOverTimeRaising3
        )
        {
            return ProjectileSpellType.Blast;
        }

        // 1481 - Flaming Missile Volley
        if (
            spell.Category >= SpellCategory.AcidVolley && spell.Category <= SpellCategory.BladeVolley
            || spell.Name.Contains("Volley")
        )
        {
            return ProjectileSpellType.Volley;
        }

        if (spell.Category >= SpellCategory.AcidWall && spell.Category <= SpellCategory.SlashingWall)
        {
            return ProjectileSpellType.Wall;
        }

        if (spell.Category >= SpellCategory.AcidStrike && spell.Category <= SpellCategory.SlashingStrike)
        {
            return ProjectileSpellType.Strike;
        }

        return ProjectileSpellType.Undef;
    }

    public float GetProjectileScriptIntensity(ProjectileSpellType spellType)
    {
        if (spellType == ProjectileSpellType.Wall)
        {
            return 0.4f;
        }
        if (spellType == ProjectileSpellType.Ring)
        {
            if (Spell.Level == 6 || Spell.Id == 3818)
            {
                return 0.4f;
            }

            if (Spell.Level == 7)
            {
                return 1.0f;
            }
        }

        // Bolt, Blast, Volley, Streak and Arc all seem to use this scale
        // TODO: should this be based on spell level, or power of first scarab?
        // ie. can this use Spell.Formula.ScarabScale?
        switch (Spell.Level)
        {
            case 1:
                return 0f;
            case 2:
                return 0.2f;
            case 3:
                return 0.4f;
            case 4:
                return 0.6f;
            case 5:
                return 0.8f;
            case 6:
            case 7:
            case 8:
                return 1.0f;
            default:
                return 0f;
        }
    }

    public bool WorldEntryCollision { get; set; }

    public void ProjectileImpact()
    {
        //Console.WriteLine($"{Name}.ProjectileImpact()");

        ReportCollisions = false;
        Ethereal = true;
        IgnoreCollisions = true;
        NoDraw = true;
        Cloaked = true;
        LightsStatus = false;

        PhysicsObj.set_active(false);

        if (PhysicsObj.entering_world)
        {
            // this path should only happen if spell_projectile_ethereal = false
            EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.Launch, GetProjectileScriptIntensity(SpellType)));
            WorldEntryCollision = true;
        }

        EnqueueBroadcast(new GameMessageSetState(this, PhysicsObj.State));
        EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.Explode, GetProjectileScriptIntensity(SpellType)));

        // this should only be needed for spell_projectile_ethereal = true,
        // however it can also fix a display issue on client in default mode,
        // where GameMessageSetState updates projectile to ethereal before it has actually collided on client,
        // causing a 'ghost' projectile to continue to sail through the target

        PhysicsObj.Velocity = Vector3.Zero;
        EnqueueBroadcast(new GameMessageVectorUpdate(this));

        var selfDestructChain = new ActionChain();
        selfDestructChain.AddDelaySeconds(5.0);
        selfDestructChain.AddAction(this, () => Destroy());
        selfDestructChain.EnqueueChain();
    }

    /// <summary>
    /// Handles collision with scenery or other static objects that would block a projectile from reaching its target,
    /// in which case the projectile should be removed with no further processing.
    /// </summary>
    public override void OnCollideEnvironment()
    {
        //Console.WriteLine($"{Name}.OnCollideEnvironment()");

        if (Info != null && ProjectileSource is Player player && player.DebugSpell)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"{Name}.OnCollideEnvironment()", ChatMessageType.Broadcast)
            );
            player.Session.Network.EnqueueSend(new GameMessageSystemChat(Info.ToString(), ChatMessageType.Broadcast));
        }

        ProjectileImpact();
    }

    public override void OnCollideObject(WorldObject target)
    {
        //Console.WriteLine($"{Name}.OnCollideObject({target.Name})");

        if (target != null && _strikethroughTargets != null)
        {
            if (_strikethroughTargets.Contains(target.Guid.Full))
            {
                return;
            }
        }

        var player = ProjectileSource as Player;

        if (Info != null && player != null && player.DebugSpell)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{Name}.OnCollideObject({target?.Name} ({target?.Guid}))",
                    ChatMessageType.Broadcast
                )
            );
            player.Session.Network.EnqueueSend(new GameMessageSystemChat(Info.ToString(), ChatMessageType.Broadcast));
        }

        var spellType = GetProjectileSpellType(Spell.Id);
        if (spellType != ProjectileSpellType.Volley)
        {
            ProjectileImpact();
        }

        if (
            spellType == ProjectileSpellType.Volley && Strikethrough == StrikethroughLimit
            || ThreadSafeRandom.Next(0.0f, 1.0f) < StrikethroughChance
        )
        {
            ProjectileImpact();
        }

        // ensure valid creature target
        var creatureTarget = target as Creature;
        if (creatureTarget == null || target == ProjectileSource)
        {
            return;
        }

        if (player != null)
        {
            player.LastHitSpellProjectile = Spell;
        }

        // ensure caster can damage target
        var sourceCreature = ProjectileSource as Creature;
        if (sourceCreature != null && !sourceCreature.CanDamage(creatureTarget))
        {
            return;
        }

        // if player target, ensure matching PK status
        var targetPlayer = creatureTarget as Player;

        var pkError = ProjectileSource?.CheckPKStatusVsTarget(creatureTarget, Spell);
        if (pkError != null)
        {
            if (player != null)
            {
                player.Session.Network.EnqueueSend(
                    new GameEventWeenieErrorWithString(player.Session, pkError[0], creatureTarget.Name)
                );
            }

            if (targetPlayer != null)
            {
                targetPlayer.Session.Network.EnqueueSend(
                    new GameEventWeenieErrorWithString(targetPlayer.Session, pkError[1], ProjectileSource.Name)
                );
            }

            return;
        }

        var critical = false;
        var critDefended = false;
        var overpower = false;
        var resisted = false;

        var damage = CalculateDamage(
            ProjectileSource,
            creatureTarget,
            ref critical,
            ref critDefended,
            ref overpower,
            ref resisted
        );

        if (targetPlayer != null && damage != null)
        {
            SigilTrinketSpellDamageReduction = 1.0f;
            targetPlayer.CheckForSigilTrinketOnSpellHitReceivedEffects(this, Spell, (int)damage, Skill.MagicDefense,
                (int)SigilTrinketMagicDefenseEffect.Absorption);
            damage = Convert.ToUInt32(damage * SigilTrinketSpellDamageReduction);
        }

        creatureTarget.OnAttackReceived(sourceCreature, CombatType.Magic, critical, resisted, (int)Spell.Level);

        if (damage != null)
        {
            if (Spell.MetaSpellType == ACE.Entity.Enum.SpellType.EnchantmentProjectile)
            {
                // handle EnchantmentProjectile successfully landing on target
                if (ProjectileSource != null)
                {
                    ProjectileSource.CreateEnchantment(creatureTarget, ProjectileSource, ProjectileLauncher, Spell, false, FromProc);
                }
            }
            else
            {
                DamageTarget(creatureTarget, damage.Value, critical, critDefended, overpower);
            }

            Strikethrough++;

            _strikethroughTargets?.Add(creatureTarget.Guid.Full);

            // if this SpellProjectile has a TargetEffect, play it on successful hit
            DoSpellEffects(Spell, ProjectileSource, creatureTarget, true);

            if (player != null)
            {
                Proficiency.OnSuccessUse(player, player.GetCreatureSkill(Spell.School), Spell.PowerMod);
            }

            // handle target procs
            // note that for untargeted multi-projectile spells,
            // ProjectileTarget will be null here, so procs will not apply

            // TODO: instead of ProjectileLauncher is Caster, perhaps a SpellProjectile.CanProc bool that defaults to true,
            // but is set to false if the source of a spell is from a proc, to prevent multi procs?

            // EMPOWERED SCARAB - Detonation Check for Cast-On-Strike
            if (player != null && FromProc)
            {
                player.CheckForSigilTrinketOnCastEffects(target, Spell, true, Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabDetonate, creatureTarget);
            }

            if (sourceCreature != null && ProjectileTarget != null && !FromProc)
            {
                // TODO figure out why cross-landblock group operations are happening here. We shouldn't need this code Mag-nus 2021-02-09
                var threadSafe = true;

                if (LandblockManager.CurrentlyTickingLandblockGroupsMultiThreaded)
                {
                    // Ok... if we got here, we're likely in the parallel landblock physics processing.
                    if (
                        sourceCreature.CurrentLandblock == null
                        || creatureTarget.CurrentLandblock == null
                        || sourceCreature.CurrentLandblock.CurrentLandblockGroup
                            != creatureTarget.CurrentLandblock.CurrentLandblockGroup
                    )
                    {
                        threadSafe = false;
                    }
                }

                if (threadSafe)
                {
                    // This can result in spell projectiles being added to either sourceCreature or creatureTargets landblock.
                    sourceCreature.TryProcEquippedItems(sourceCreature, creatureTarget, false, ProjectileLauncher);

                    // EMPOWERED SCARAB - Detonate
                    if (player != null)
                    {
                        player.CheckForSigilTrinketOnCastEffects(target, Spell, false, Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabDetonate, creatureTarget);
                    }
                }
                else
                {
                    // sourceCreature and creatureTarget are now in different landblock groups.
                    // What has likely happened is that sourceCreature sent a projectile toward creatureTarget. Before impact, sourceCreature was teleported away.
                    // To perform this fully thread safe, we would enqueue the work onto worldManager.
                    // WorldManager.EnqueueAction(new ActionEventDelegate(() => sourceCreature.TryProcEquippedItems(creatureTarget, false)));
                    // But, to keep it simple, we will just ignore it and not bother with TryProcEquippedItems for this particular impact.
                }
            }
        }

        // also called on resist
        if (player != null && targetPlayer == null)
        {
            player.OnAttackMonster(creatureTarget);
        }

        if (player == null && targetPlayer == null)
        {
            // check for faction combat
            if (
                sourceCreature != null
                && creatureTarget != null
                && (sourceCreature.AllowFactionCombat(creatureTarget) || sourceCreature.PotentialFoe(creatureTarget))
            )
            {
                sourceCreature.MonsterOnAttackMonster(creatureTarget);
            }
        }
    }

    /// <summary>
    /// Calculates the damage for a spell projectile
    /// Used by war magic, void magic, and life magic projectiles
    /// </summary>
    private float? CalculateDamage(
        WorldObject source,
        Creature target,
        ref bool criticalHit,
        ref bool critDefended,
        ref bool overpower,
        ref bool resisted
    )
    {
        var sourcePlayer = source as Player;
        var targetPlayer = target as Player;

        if (source == null || !target.IsAlive || targetPlayer != null && targetPlayer.Invincible)
        {
            return null;
        }

        // check lifestone protection
        if (targetPlayer != null && targetPlayer.UnderLifestoneProtection)
        {
            if (sourcePlayer != null)
            {
                sourcePlayer.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"The Lifestone's magic protects {targetPlayer.Name} from the attack!",
                        ChatMessageType.Magic
                    )
                );
            }

            targetPlayer.HandleLifestoneProtection();
            return null;
        }

        var critDamageBonus = 0.0f;
        var criticalDamageMod = 1.0f;
        var weaponCritDamageMod = 1.0f;
        var weaponResistanceMod = 1.0f;
        var resistanceMod = 1.0f;

        // life magic
        var lifeMagicDamage = 0.0f;

        // war/void magic
        var baseDamage = 0;
        var skillBonus = 0.0f;

        var finalDamage = 0.0f;

        var resistanceType = Creature.GetResistanceType(Spell.DamageType);

        var sourceCreature = source as Creature;
        if (sourceCreature?.Overpower != null)
        {
            overpower = Creature.GetOverpower(sourceCreature, target);
        }

        var weapon = ProjectileLauncher;

        var resistSource = IsWeaponSpell ? weapon : source;

        resisted = source.TryResistSpell(target, Spell, out var partialEvasion, resistSource, true);

        CheckForCombatAbilityReflectSpell(resisted, targetPlayer, sourceCreature);

        var resistedMod = 1.0f;
        _partialEvasion = partialEvasion;

        if (!overpower && sourcePlayer != null)
        {
            if (GetResistedMod(out resistedMod))
            {
                return null;
            }
        }
        else
        {
            if (resisted && !overpower)
            {
                return null;
            }
        }

        CreatureSkill attackSkill = null;
        if (sourceCreature != null)
        {
            attackSkill = sourceCreature.GetCreatureSkill(Spell.School);
        }

        // critical hit
        var criticalChance = GetWeaponMagicCritFrequency(weapon, sourceCreature, attackSkill, target);
        criticalChance += CheckForWarSpecCriticalChanceBonus(sourcePlayer, weapon);
        criticalChance = CheckForRatingReprisalAutoCrit(target, sourcePlayer, criticalChance);

        if (ThreadSafeRandom.Next(0.0f, 1.0f) < criticalChance)
        {
            if (targetPlayer != null && targetPlayer.AugmentationCriticalDefense > 0)
            {
                var criticalDefenseMod = sourcePlayer != null ? 0.05f : 0.25f;
                var criticalDefenseChance = targetPlayer.AugmentationCriticalDefense * criticalDefenseMod;

                if (criticalDefenseChance > ThreadSafeRandom.Next(0.0f, 1.0f))
                {
                    critDefended = true;
                }
            }

            var perceptionDefended = CheckForPerceptionSpecCriticalDefense(targetPlayer, attackSkill);

            if (!critDefended && perceptionDefended == false)
            {
                criticalHit = true;
            }

            if (CheckForRatingReprisalCritResist(criticalHit, ref resisted, targetPlayer, sourceCreature))
            {
                return null;
            }

            // EMPOWERED SCARAB - Crushing
            if (criticalHit && sourcePlayer != null && Spell.School == MagicSchool.WarMagic)
            {
                sourcePlayer.CheckForSigilTrinketOnCastEffects(target, Spell, false, Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabCrit, null, true);
            }
        }

        // ward mod, rend, and penetration
        var wardRendingMod = 1.0f;
        if (weapon != null && weapon.HasImbuedEffect(ImbuedEffectType.WardRending))
        {
            wardRendingMod = 1.0f - GetWardRendingMod(attackSkill);
        }

        var wardPenMod = 0.0f;
        if (sourcePlayer != null)
        {
            wardPenMod = sourcePlayer.GetIgnoreWardMod(weapon);
        }

        var ignoreWardMod = Math.Min(wardRendingMod, wardPenMod);

        ignoreWardMod *= 1.0f - CheckForWarSpecWardPenBonus(sourcePlayer, weapon);
        ignoreWardMod *= 1.0f - Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearWardPen, "WardPen");

        var wardMod = GetWardMod(sourceCreature, target, ignoreWardMod);

        //Console.WriteLine($"TargetWard: {target.WardLevel} WardRend: {wardRendingMod} Nullification: {NullificationMod} WardMod: {wardMod}");

        // absorb mod
        var isPVP = sourcePlayer != null && targetPlayer != null;
        var absorbMod = GetAbsorbMod(target, this);

        absorbMod *= 1.0f - Jewel.GetJewelEffectMod(targetPlayer, PropertyInt.GearNullification, "Nullification");

        //http://acpedia.org/wiki/Announcements_-_2014/01_-_Forces_of_Nature - Aegis is 72% effective in PvP
        if (isPVP && (target.CombatMode == CombatMode.Melee || target.CombatMode == CombatMode.Missile))
        {
            absorbMod = 1 - absorbMod;
            absorbMod *= 0.72f;
            absorbMod = 1 - absorbMod;
        }

        if (isPVP && Spell.IsHarmful)
        {
            Player.UpdatePKTimers(sourcePlayer, targetPlayer);
        }

        var attributeMod = 1f;
        if (sourcePlayer != null)
        {
            attributeMod = sourcePlayer.GetAttributeMod(weapon, true, target);
        }

        var elementalDamageMod = GetCasterElementalDamageModifier(weapon, sourceCreature, target, Spell.DamageType);

        // Possible 2x + damage bonus for the slayer property
        var slayerMod = GetWeaponCreatureSlayerModifier(weapon, sourceCreature, target);

        var combatAbilityDamageMod = 1.0f;
        combatAbilityDamageMod += CheckForCombatAbilityOverloadDamageMod(sourcePlayer);
        combatAbilityDamageMod -= CheckForCombatAbilityBatteryDamagePenalty(sourcePlayer);
        combatAbilityDamageMod += CheckForCombatAbilityEnchantedWeaponDamageBonus(sourcePlayer);

        var lethalityMod = 1.0f;
        if (sourceCreature is not null)
        {
            lethalityMod = Convert.ToSingle(sourceCreature.ArchetypeLethality ?? 1.0f);
        }

        var specDefenseMod = CheckForMagicDefenseSpecDefenseMod(targetPlayer, sourceCreature);

        var jewelRedFury = 1.0f + Jewel.GetJewelRedFury(sourcePlayer);
        var jewelBlueFury = 1.0f + Jewel.GetJewelBlueFury(sourcePlayer);
        var jewelSelfHarm = 1.0f + Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearSelfHarm);

        var levelScalingMod = GetLevelScalingMod(sourceCreature, target, targetPlayer);

        // life magic projectiles: ie., martyr's hecatomb
        if (Spell.MetaSpellType == ACE.Entity.Enum.SpellType.LifeProjectile)
        {
            lifeMagicDamage = LifeProjectileDamage * Spell.DamageRatio * combatAbilityDamageMod;

            // could life magic projectiles crit?
            // if so, did they use the same 1.5x formula as war magic, instead of 2.0x?
            if (criticalHit)
            {
                // verify: CriticalMultiplier only applied to the additional crit damage,
                // whereas CD/CDR applied to the total damage (base damage + additional crit damage)
                weaponCritDamageMod = GetWeaponCritDamageMod(weapon, sourceCreature, attackSkill, target);

                criticalDamageMod = 2.0f + weaponCritDamageMod;
            }

            weaponResistanceMod = GetWeaponResistanceModifier(
                weapon,
                sourceCreature,
                attackSkill,
                Spell.DamageType,
                target
            );

            // if attacker/weapon has IgnoreMagicResist directly, do not transfer to spell projectile
            // only pass if SpellProjectile has it directly, such as 2637 - Invoking Aun Tanua

            resistanceMod = (float)
                Math.Max(0.0f, target.GetResistanceMod(resistanceType, this, null, weaponResistanceMod));

            finalDamage =
                lifeMagicDamage
                * criticalDamageMod
                * elementalDamageMod
                * slayerMod
                * attributeMod
                * resistanceMod
                * absorbMod
                * wardMod
                * resistedMod
                * specDefenseMod
                * jewelRedFury
                * jewelBlueFury
                * jewelSelfHarm
                * lethalityMod
                * levelScalingMod;
        }
        // war/void magic projectiles
        else
        {
            if (criticalHit)
            {
                // Original:
                // http://acpedia.org/wiki/Announcements_-_2002/08_-_Atonement#Letter_to_the_Players

                // Critical Strikes: In addition to the skill-based damage bonus, each projectile spell has a 2% chance of causing a critical hit on the target and doing increased damage.
                // A magical critical hit is similar in some respects to melee critical hits (although the damage calculation is handled differently).
                // While a melee critical hit automatically does twice the maximum damage of the weapon, a magical critical hit will do an additional half the minimum damage of the spell.
                // For instance, a magical critical hit from a level 7 spell, which does 110-180 points of damage, would add an additional 55 points of damage to the spell.

                // Later updated for PvE only:

                // http://acpedia.org/wiki/Announcements_-_2004/07_-_Treaties_in_Stone#Letter_to_the_Players

                // Currently when a War Magic spell scores a critical hit, it adds a multiple of the base damage of the spell to a normal damage roll.
                // Starting in July, War Magic critical hits will instead add a multiple of the maximum damage of the spell.
                // No more crits that do less damage than non-crits!

                if (isPVP) // PvP: 50% of the MIN damage added to normal damage roll
                {
                    critDamageBonus = Spell.MinDamage * 0.5f;
                }
                else // PvE: 50% of the MAX damage added to normal damage roll
                {
                    critDamageBonus = Spell.MaxDamage * 0.5f;
                }

                // verify: CriticalMultiplier only applied to the additional crit damage,
                // whereas CD/CDR applied to the total damage (base damage + additional crit damage)
                weaponCritDamageMod = GetWeaponCritDamageMod(weapon, sourceCreature, attackSkill, target);
                weaponCritDamageMod += CheckForWarMagicSpecCriticalDamageBonus(sourcePlayer, weapon);

                critDamageBonus *= weaponCritDamageMod;

                criticalDamageMod = 2.0f + weaponCritDamageMod;
                criticalDamageMod *= 1.0f + Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearBludgeon, "Bludgeon");
            }

            baseDamage = ThreadSafeRandom.Next(Spell.MinDamage, Spell.MaxDamage);

            if (criticalHit && sourceCreature is not Player)
            {
                baseDamage = Spell.MedianDamage;
            }

            weaponResistanceMod = GetWeaponResistanceModifier(weapon, sourceCreature, attackSkill, Spell.DamageType, target);

            // if attacker/weapon has IgnoreMagicResist directly, do not transfer to spell projectile
            // only pass if SpellProjectile has it directly, such as 2637 - Invoking Aun Tanua

            resistanceMod = (float)Math.Max(0.0f, target.GetResistanceMod(resistanceType, this, null, weaponResistanceMod));

            if (sourcePlayer != null && targetPlayer != null && Spell.DamageType == DamageType.Nether)
            {
                // for direct damage from void spells in pvp,
                // apply void_pvp_modifier *on top of* the player's natural resistance to nether

                // this supposedly brings the direct damage from void spells in pvp closer to retail
                resistanceMod *= (float)PropertyManager.GetDouble("void_pvp_modifier").Item;
            }

            if (Spell.DamageType is DamageType.Pierce)
            {
                resistanceMod += Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearPierce, "Pierce");
            }

            var jewelElementalist = 1.0f + Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearElementalist, "Elementalist");
            var jewelElemental = Jewel.HandleElementalBonuses(sourcePlayer, Spell.DamageType);

            var ratingDamageTypeWard = Spell.DamageType switch
            {
                DamageType.Physical => 1.0f - Jewel.GetJewelEffectMod(targetPlayer, PropertyInt.GearPhysicalWard),
                DamageType.Elemental => 1.0f - Jewel.GetJewelEffectMod(targetPlayer, PropertyInt.GearElementalWard),
                _ => 1.0f
            };

            var strikethroughMod = 1.0f / (Strikethrough + 1);

            // ----- FINAL CALCULATION ------------
            var damageBeforeMitigation =
                baseDamage
                * criticalDamageMod
                * attributeMod
                * elementalDamageMod
                * slayerMod
                * combatAbilityDamageMod
                * jewelElementalist
                * jewelElemental
                * jewelSelfHarm
                * jewelRedFury
                * jewelBlueFury
                * strikethroughMod;

            finalDamage =
                damageBeforeMitigation
                * absorbMod
                * wardMod
                * resistanceMod
                * resistedMod
                * specDefenseMod
                * ratingDamageTypeWard
                * levelScalingMod;

            if (sourcePlayer != null)
            {
                //Console.WriteLine($"\n{sourcePlayer.Name} casted {Spell.Name} on {target.Name} for {Math.Round(finalDamage, 0)}.\n" +
                //    $" -baseDamage: {baseDamage}\n" +
                //    $" -critMultiplier: {criticalDamageMod}\n" +
                //    $" -attributeMod: {attributeMod}\n" +
                //    $" -elementalDamageMod: {elementalDamageMod}\n" +
                //    $" -slayerMod: {slayerMod}\n" +
                //    $" -absorbMod: {absorbMod}\n" +
                //    $" -wardMod: {wardMod}\n" +
                //    $" -resistanceMod: {resistanceMod}\n" +
                //    $" -resistedMod: {resistedMod}\n" +
                //    $" -specDefMod: {specDefenseMod}\n" +
                //    $" -FinalBeforeRatings: {finalDamage}");
            }
        }

        // show debug info
        if (sourceCreature != null && sourceCreature.DebugDamage.HasFlag(Creature.DebugDamageType.Attacker))
        {
            ShowInfo(
                sourceCreature,
                Spell,
                attackSkill,
                criticalChance,
                criticalHit,
                critDefended,
                overpower,
                weaponCritDamageMod,
                skillBonus,
                baseDamage,
                critDamageBonus,
                elementalDamageMod,
                slayerMod,
                weaponResistanceMod,
                resistanceMod,
                absorbMod,
                LifeProjectileDamage,
                lifeMagicDamage,
                finalDamage
            );
        }
        if (target.DebugDamage.HasFlag(Creature.DebugDamageType.Defender))
        {
            ShowInfo(
                target,
                Spell,
                attackSkill,
                criticalChance,
                criticalHit,
                critDefended,
                overpower,
                weaponCritDamageMod,
                skillBonus,
                baseDamage,
                critDamageBonus,
                elementalDamageMod,
                slayerMod,
                weaponResistanceMod,
                resistanceMod,
                absorbMod,
                LifeProjectileDamage,
                lifeMagicDamage,
                finalDamage
            );
        }
        return finalDamage;
    }

    private static float GetLevelScalingMod(Creature attacker, Creature defender, Player playerDefender)
    {
        var monsterHealthScalingMod = playerDefender != null
            ? LevelScaling.GetMonsterDamageDealtHealthScalar(playerDefender, attacker)
            : LevelScaling.GetMonsterDamageTakenHealthScalar(attacker, defender);

        var timeToKillMonsterScalingMod = playerDefender != null
            ? 1.0f
            : LevelScaling.GetMonsterDamageTakenTtkScalar(attacker, defender);

        var monsterDpsPerTierScalingMod = playerDefender != null
            ? 1.0f
            : LevelScaling.GetMonsterDamageDealtDpsPerTierScalar(attacker, defender);

        return monsterHealthScalingMod * timeToKillMonsterScalingMod * monsterDpsPerTierScalingMod;
    }

    /// <summary>
    /// SPEC BONUS - War Magic (Wand/Baton): +50% crit damage (additively)
    /// </summary>
    private static float CheckForWarMagicSpecCriticalDamageBonus(Player sourcePlayer, WorldObject weapon)
    {
        if (sourcePlayer == null || weapon == null)
        {
            return 0.0f;
        }

        if (
            weapon.WeaponSkill == Skill.WarMagic
            && sourcePlayer.GetCreatureSkill(Skill.WarMagic).AdvancementClass == SkillAdvancementClass.Specialized
            && LootGenerationFactory.GetCasterSubType(weapon) == 2
        )
        {
            return 0.5f;
        }

        return 0.0f;
    }

    /// <summary>
    /// SPEC BONUS - Magic Defense: Magic damage reduced by 10% + 1% per 50 skill level.
    /// </summary>
    private static float CheckForMagicDefenseSpecDefenseMod(Player targetPlayer, Creature sourceCreature)
    {
        if (targetPlayer == null || targetPlayer.GetCreatureSkill(Skill.MagicDefense).AdvancementClass != SkillAdvancementClass.Specialized)
        {
            return 1.0f;
        }

        var magicDefenseSkill = targetPlayer.GetModdedMagicDefSkill() * LevelScaling.GetPlayerDefenseSkillScalar(targetPlayer, sourceCreature);

        var bonusAmount = Math.Min(magicDefenseSkill, 500) / 50;

        return 0.9f - bonusAmount * 0.01f;

    }

    /// <summary>
    /// COMBAT ABILITY - Enchant: +25% increased damage with spells while a melee/missile weapon is equipped.
    /// </summary>
    private static float CheckForCombatAbilityEnchantedWeaponDamageBonus(Player sourcePlayer)
    {
        if (sourcePlayer == null)
        {
            return 0.0f;
        }

        if (sourcePlayer.EquippedCombatAbility != CombatAbility.EnchantedWeapon
            || !(sourcePlayer.LastEnchantedWeaponActivated
                 > Time.GetUnixTime() - sourcePlayer.EnchantedWeaponActivatedDuration))
        {
            return 0.0f;
        }

        if (sourcePlayer.GetEquippedMeleeWeapon() != null || sourcePlayer.GetEquippedMissileLauncher() != null || sourcePlayer.GetEquippedMissileWeapon() != null)
        {
            return 0.25f;
        }

        return 0.0f;
    }

    /// <summary>
    /// COMBAT ABILITY - Battery: Up to 25% damage penalty, if current mana drops below 75%. (-25% at 0 mana)
    /// </summary>
    private static float CheckForCombatAbilityBatteryDamagePenalty(Player sourcePlayer)
    {
        if (sourcePlayer == null)
        {
            return 0.0f;
        }

        if (sourcePlayer.EquippedCombatAbility != CombatAbility.Battery
            || !(sourcePlayer.LastBatteryActivated < Time.GetUnixTime() - sourcePlayer.BatteryActivatedDuration))
        {
            return 0.0f;
        }

        var maxMana = (float)sourcePlayer.Mana.MaxValue;
        var currentMana = (float)sourcePlayer.Mana.Current == 0 ? 1 : (float)sourcePlayer.Mana.Current;

        // If current mana is over 75% full, no penalty
        if ((currentMana / maxMana) > 0.75)
        {
            return 0.0f;
        }

        // Else, Up to 25% reduced damage depending on how low current mana is.
        var newMax = maxMana * 0.75;
        var manaMod = 0.25f * ((newMax - currentMana) / newMax);

        return (float)manaMod;
    }

    /// <summary>
    /// COMBAT ABILITY - Overload: Increased effectiveness up to 25%+ with Overload stacks, double bonus +
    /// erase stacks on activated ability.
    /// </summary>
    private static float CheckForCombatAbilityOverloadDamageMod(Player sourcePlayer)
    {
        if (sourcePlayer == null)
        {
            return 0.0f;
        }

        if (sourcePlayer.EquippedCombatAbility != CombatAbility.Overload || !sourcePlayer.QuestManager.HasQuest($"{sourcePlayer.Name},Overload"))
        {
            return 0.0f;
        }

        switch (sourcePlayer.OverloadActivated)
        {
            case false:
            {
                var overloadStacks = sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Overload");
                var overloadMod = (float)overloadStacks / 2000;

                return overloadMod;
            }
            case true
            when sourcePlayer.LastOverloadActivated > Time.GetUnixTime() - sourcePlayer.OverloadActivatedDuration:
            {
                sourcePlayer.OverloadActivated = false;
                sourcePlayer.OverloadDumped = true;

                var overloadStacks = sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Overload");
                var overloadMod = (float)overloadStacks / 1000;

                sourcePlayer.QuestManager.Erase($"{sourcePlayer.Name},Overload");

                return overloadMod;
            }
        }

        if (!sourcePlayer.OverloadActivated || !(sourcePlayer.LastOverloadActivated < Time.GetUnixTime() - sourcePlayer.OverloadActivatedDuration))
        {
            return 0.0f;
        }

        // reset if player didn't cast overload discharge within ten sec
        sourcePlayer.OverloadActivated = false;
        sourcePlayer.OverloadDumped = false;
        sourcePlayer.QuestManager.Erase($"{sourcePlayer.Name},Overload");

        return 0.0f;
    }

    /// <summary>
    /// SPEC BONUS - Perception: Up to 50% chance to prevent a critical hit
    /// </summary>
    private static bool CheckForPerceptionSpecCriticalDefense(Player targetPlayer, CreatureSkill attackSkill)
    {
        if (targetPlayer == null)
        {
            return false;
        }

        var perception = targetPlayer.GetCreatureSkill(Skill.Perception);
        if (perception.AdvancementClass != SkillAdvancementClass.Specialized)
        {
            return false;
        }

        var skillCheck = (float)targetPlayer.GetModdedPerceptionSkill() / attackSkill.Current;
        var criticalDefenseChance = skillCheck > 1f ? 0.5f : skillCheck * 0.5f;

        if (!(criticalDefenseChance > ThreadSafeRandom.Next(0f, 1f)))
        {
            return false;
        }

        targetPlayer.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Your perception skill allowed you to prevent a critical strike!",
                ChatMessageType.Magic
            )
        );

        return true;
    }

    /// <summary>
    /// COMBAT ABILITY - Reflect: Reflect resist spells back to the caster.
    /// </summary>
    private void CheckForCombatAbilityReflectSpell(bool resisted, Player targetPlayer, Creature sourceCreature)
    {
        if (!resisted || targetPlayer == null || sourceCreature == null)
        {
            return;
        }

        if (targetPlayer.EquippedCombatAbility == CombatAbility.Reflect)
        {
            targetPlayer.TryCastSpell(Spell, sourceCreature, null, null, false, false, false);
        }
    }

    /// <summary>
    /// SPEC BONUS - War Magic (Scepter): +5% critical chance (additively).
    /// </summary>
    private static float CheckForWarSpecCriticalChanceBonus(Player sourcePlayer, WorldObject weapon)
    {
        if (sourcePlayer == null || weapon == null)
        {
            return 0.0f;
        }

        if (
            weapon.WeaponSkill == Skill.WarMagic
            && sourcePlayer.GetCreatureSkill(Skill.WarMagic).AdvancementClass == SkillAdvancementClass.Specialized
            && LootGenerationFactory.GetCasterSubType(weapon) == 1
        )
        {
            return 0.05f;
        }

        return 0.0f;
    }

    /// <summary>
    /// SPEC BONUS - War Magic (Orb): +10% ward penetration (additively).
    /// </summary>
    private static float CheckForWarSpecWardPenBonus(Player sourcePlayer, WorldObject weapon)
    {
        if (sourcePlayer == null || weapon == null)
        {
            return 0.0f;
        }

        if (
            weapon.WeaponSkill == Skill.WarMagic
            && sourcePlayer.GetCreatureSkill(Skill.WarMagic).AdvancementClass == SkillAdvancementClass.Specialized
            && LootGenerationFactory.GetCasterSubType(weapon) == 0
        )
        {
            return 0.1f;
        }

        return 0.0f;
    }

    /// <summary>
    /// RATING - Reprisal: Crit resist.
    /// (JEWEL - Black Opal)
    /// </summary>
    private bool CheckForRatingReprisalCritResist(bool criticalHit, ref bool resisted, Player targetPlayer, Creature sourceCreature)
    {
        if (!criticalHit || targetPlayer is null || sourceCreature is null)
        {
            return false;
        }

        var chance = Jewel.GetJewelEffectMod(targetPlayer, PropertyInt.GearReprisal);

        if (ThreadSafeRandom.Next(0.0f, 1.0f) > chance)
        {
            return false;
        }

        targetPlayer.QuestManager.HandleReprisalQuest();
        targetPlayer.QuestManager.Stamp($"{sourceCreature.Guid}/Reprisal");

        resisted = true;
        targetPlayer.Reprisal = true;
        _partialEvasion = PartialEvasion.All;

        var msg = $"Reprisal! You resist the spell cast by {sourceCreature.Name}";
        targetPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.Magic));
        targetPlayer.Session.Network.EnqueueSend(new GameMessageSound(targetPlayer.Guid, Sound.ResistSpell));

        return true;
    }

    /// <summary>
    /// RATING - Reprisal: Auto-crit.
    /// (JEWEL - Black Opal)
    /// </summary>
    private static float CheckForRatingReprisalAutoCrit(Creature target, Player sourcePlayer, float criticalChance)
    {
        if (sourcePlayer == null)
        {
            return criticalChance;
        }

        if (sourcePlayer.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearReprisal) <= 0)
        {
            return criticalChance;
        }

        if (!sourcePlayer.QuestManager.HasQuest($"{target.Guid}/Reprisal"))
        {
            return criticalChance;
        }

        sourcePlayer.QuestManager.Erase($"{target.Guid}/Reprisal");

        return 1.0f;
    }

    private float GetAbsorbMod(Creature target, WorldObject source)
    {
        switch (target.CombatMode)
        {
            case CombatMode.Melee:

                // does target have shield equipped?
                var shield = target.GetEquippedShield();
                if (shield != null && shield.GetAbsorbMagicDamage() != null)
                {
                    return GetShieldMod(target, shield, source);
                }

                break;

            case CombatMode.Missile:

                var missileLauncherOrShield = target.GetEquippedMissileLauncher() ?? target.GetEquippedShield();
                if (missileLauncherOrShield != null && missileLauncherOrShield.GetAbsorbMagicDamage() != null)
                {
                    return AbsorbMagic(target, missileLauncherOrShield);
                }

                break;

            case CombatMode.Magic:

                var caster = target.GetEquippedWand();
                if (caster != null && caster.GetAbsorbMagicDamage() != null)
                {
                    return AbsorbMagic(target, caster);
                }

                break;
        }
        return 1.0f;
    }

    private float GetWardMod(Creature caster, Creature target, float ignoreWardMod)
    {
        var wardBuffDebuffMod = target.EnchantmentManager.GetWardMultiplicativeMod();

        var wardLevel = target.GetWardLevel() * wardBuffDebuffMod;

        if (caster is Player)
        {
            wardLevel = Convert.ToInt32(wardLevel * LevelScaling.GetMonsterArmorWardScalar(caster, target));
        }
        else if (target is Player)
        {
            wardLevel = Convert.ToInt32(wardLevel * LevelScaling.GetPlayerArmorWardScalar(target, caster));
        }

        return SkillFormula.CalcWardMod(wardLevel * ignoreWardMod);
    }

    /// <summary>
    /// Calculates the amount of damage a shield absorbs from magic projectile
    /// </summary>
    private static float GetShieldMod(Creature target, WorldObject shield, WorldObject source)
    {
        var bypassShieldAngleCheck = CheckForCombatAbilityPhalanxBypassSpellAngle(target);

        if (!bypassShieldAngleCheck)
        {
            // is spell projectile in front of creature target,
            // within shield effectiveness area?
            const float effectiveAngle = 180.0f;
            var angle = target.GetAngle(source);
            if (Math.Abs(angle) > effectiveAngle / 2.0f)
            {
                return 1.0f;
            }
        }

        // https://asheron.fandom.com/wiki/Shield
        // The formula to determine magic absorption for shields is:
        // Reduction Percent = (cap * specMod * baseSkill * 0.003f) - (cap * specMod * 0.3f)
        // Cap = Maximum reduction
        // SpecMod = 1.0 for spec, 0.8 for trained
        // BaseSkill = 100 to 433 (above 433 base shield you always achieve the maximum %)

        var shieldSkill = target.GetCreatureSkill(Skill.Shield);
        // ensure trained?
        if (shieldSkill.AdvancementClass < SkillAdvancementClass.Trained || shieldSkill.Base < 100)
        {
            return 1.0f;
        }

        var baseSkill = Math.Min(shieldSkill.Base, 433);
        var specMod = shieldSkill.AdvancementClass == SkillAdvancementClass.Specialized ? 1.0f : 0.8f;
        var cap = (float)(shield.GetAbsorbMagicDamage() ?? 0.0f);

        // speced, 100 skill = 0%
        // trained, 100 skill = 0%
        // speced, 200 skill = 30%
        // trained, 200 skill = 24%
        // speced, 300 skill = 60%
        // trained, 300 skill = 48%
        // speced, 433 skill = 100%
        // trained, 433 skill = 80%

        var reduction = (cap * specMod * baseSkill * 0.003f) - (cap * specMod * 0.3f);

        var shieldMod = Math.Min(1.0f, 1.0f - reduction);
        return shieldMod;
    }

    /// <summary>
    /// COMBAT ABILITY - Phalanx: Apply shield ward damage reduction to spells from behind player.
    /// </summary>
    private static bool CheckForCombatAbilityPhalanxBypassSpellAngle(Creature target)
    {
        var combatAbilityTrinket = target.GetEquippedTrinket();

        return combatAbilityTrinket is { CombatAbilityId: (int)CombatAbility.Phalanx };
    }

    /// <summary>
    /// Calculates the damage reduction modifier for bows and casters
    /// with 'Magic Absorbing' property
    /// </summary>
    private static float AbsorbMagic(Creature target, WorldObject item)
    {
        // https://asheron.fandom.com/wiki/Category:Magic_Absorbing

        // Tomes and Bows
        // The formula to determine magic absorption for Tomes and the Fetish of the Dark Idols:
        // - For a 25% maximum item: (magic absorbing %) = 25 - (0.1 * (319 - base magic defense))
        // - For a 10% maximum item: (magic absorbing %) = 10 - (0.04 * (319 - base magic defense))

        // wiki currently has what is likely a typo for the 10% formula,
        // where it has a factor of 0.4 instead of 0.04
        // with 0.4, the 10% items would not start to become effective until base magic defense 294
        // with 0.04, both formulas start to become effective at base magic defense 69

        // using an equivalent formula that produces the correct results for 10% and 25%,
        // and also produces the correct results for any %

        var absorbMagicDamage = item.GetAbsorbMagicDamage();

        if (absorbMagicDamage == null)
        {
            return 1.0f;
        }

        var maxPercent = absorbMagicDamage.Value;

        var baseCap = 319;
        var magicDefBase = target.GetCreatureSkill(Skill.MagicDefense).Base;
        var diff = Math.Max(0, baseCap - magicDefBase);

        var percent = maxPercent - maxPercent * diff * 0.004f;

        return Math.Min(1.0f, 1.0f - (float)percent);
    }

    /// <summary>
    /// Called for a spell projectile to damage its target
    /// </summary>
    private void DamageTarget(Creature target, float damage, bool critical, bool critDefended, bool overpower)
    {
        var targetPlayer = target as Player;

        if (targetPlayer != null && targetPlayer.Invincible || target.IsDead)
        {
            return;
        }

        var sourceCreature = ProjectileSource as Creature;
        var sourcePlayer = ProjectileSource as Player;

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
            if (sourcePlayer != null)
            {
                // TODO: use target direction vs. projectile position, instead of player position
                // could sneak attack be applied to void DoTs?
                sneakAttackMod = sourcePlayer.GetSneakAttackMod(target);
                //Console.WriteLine("Magic sneak attack:  + sneakAttackMod);
                heritageMod = sourcePlayer.GetHeritageBonus(sourcePlayer.GetEquippedWand()) ? 1.05f : 1.0f;
            }
            // Calc sneak bonus for monsters
            if (targetPlayer != null && sourceCreature != null)
            {
                sneakAttackMod = sourceCreature.GetSneakAttackMod(targetPlayer);
            }

            var damageRating = sourceCreature?.GetDamageRating() ?? 0;
            damageRatingMod = Creature.AdditiveCombine(
                Creature.GetPositiveRatingMod(damageRating),
                heritageMod,
                sneakAttackMod
            );

            damageResistRatingMod = target.GetDamageResistRatingMod(CombatType.Magic);

            if (critical)
            {
                damageRatingMod = Creature.GetPositiveRatingMod(sourceCreature?.GetCritDamageRating() ?? 0);
                damageResistRatingMod = Creature.GetNegativeRatingMod(target.GetCritDamageResistRating());
            }
            if (pkBattle)
            {
                pkDamageRatingMod = Creature.GetPositiveRatingMod(sourceCreature.GetPKDamageRating());
                pkDamageResistRatingMod = Creature.GetNegativeRatingMod(target.GetPKDamageResistRating());

                damageRatingMod = Creature.AdditiveCombine(damageRatingMod, pkDamageRatingMod);
                damageResistRatingMod = Creature.AdditiveCombine(damageResistRatingMod, pkDamageResistRatingMod);
            }

            damage *= damageRatingMod * damageResistRatingMod;

            percent = damage / target.Health.MaxValue;

            //Console.WriteLine($"Damage rating: " + Creature.ModToRating(damageRatingMod));

            equippedCloak = target.EquippedCloak;

            if (equippedCloak != null && Cloak.HasDamageProc(equippedCloak) && Cloak.RollProc(equippedCloak, percent))
            {
                var reducedDamage = Cloak.GetReducedAmount(ProjectileSource, damage);

                Cloak.ShowMessage(target, ProjectileSource, damage, reducedDamage);

                damage = reducedDamage;
                percent = damage / target.Health.MaxValue;
            }

            amount = Convert.ToUInt32(damage);

            amount = CheckForCombatAbilityManaBarrier(target, damage, targetPlayer, amount);
            amount = CheckForCombatAbilityEvasiveStance(target, damage, targetPlayer, amount);

            target.UpdateVitalDelta(target.Health, (int)-Math.Round(damage));
            target.DamageHistory.Add(ProjectileSource, Spell.DamageType, amount);
        }

        var overloadPercent = HandleCombatAbilityOverloadStamps(sourcePlayer, sourceCreature, out var overload);

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
            var overloadMsg = overload ? $"{overloadPercent}% Overload! " : "";
            var resistSome = _partialEvasion == PartialEvasion.Some ? "Partial resist! " : "";
            var strikeThrough = Strikethrough > 0 ? "Strikethrough! " : "";

            var nonHealth = Spell.Category is SpellCategory.StaminaLowering or SpellCategory.ManaLowering;

            if (sourcePlayer != null)
            {
                var critProt = critDefended ? " Your critical hit was avoided with their augmentation!" : "";

                if (sourcePlayer.OverloadDumped)
                {
                    sourcePlayer.OverloadDumped = false;
                    overloadMsg = "Overload Discharged! ";
                }

                var attackerMsg = $"{resistSome}{strikeThrough}{critMsg}{overpowerMsg}{overloadMsg}{sneakMsg}{elementalistMsg}You {verb} {target.Name} for {amount} points with {Spell.Name}.{critProt}";

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

    /// <summary>
    /// COMBAT ABILITY - Overload: Handle overload stamps.
    /// </summary>
    private int HandleCombatAbilityOverloadStamps(Player sourcePlayer, Creature sourceCreature, out bool overload)
    {
        // Overload Stamps + Messages
        var overloadPercent = 0;
        overload = false;

        if (sourcePlayer == null)
        {
            return overloadPercent;
        }

        var combatAbility = CombatAbility.None;
        var combatFocus = sourceCreature.GetEquippedCombatFocus();
        if (combatFocus != null)
        {
            combatAbility = combatFocus.GetCombatAbility();
        }

        if (combatAbility != CombatAbility.Overload)
        {
            return overloadPercent;
        }

        overload = true;
        var projectileScaler = 1;
        switch (SpellType)
        {
            case ProjectileSpellType.Streak:
                projectileScaler = 5;
                break;
            case ProjectileSpellType.Blast:
                projectileScaler = 3;
                break;
            case ProjectileSpellType.Volley:
            case ProjectileSpellType.Ring:
            case ProjectileSpellType.Wall:
                projectileScaler = 6;
                break;
        }

        overloadPercent = Player.HandleOverloadStamps(sourcePlayer, projectileScaler, Spell.Level);

        return overloadPercent;
    }

    /// <summary>
    /// COMBAT ABILITY - Mana Barrier: Some damage taken from mana instead of health.
    /// </summary>
    private uint CheckForCombatAbilityManaBarrier(Creature target, float damage, Player targetPlayer, uint amount)
    {
        if (targetPlayer is not { ManaBarrierToggle: true })
        {
            return amount;
        }

        if (targetPlayer.Level == null || !targetPlayer.ManaBarrierToggle)
        {
            return amount;
        }

        var skill = targetPlayer.GetCreatureSkill(Skill.ManaConversion);

        var expectedSkill = (float)(targetPlayer.Level * 5);
        var currentSkill = (float)skill.Current;

        // Create a scaling mod. If expected skill is much higher than currentSkill, you will be multiplying the amount
        // of mana damage singificantly, so low skill players will not get much benefit before bubble bursts.
        // Capped at 1.0f so high skill gets the proper ratio of health-to-mana, but no better than that.

        var skillModifier = Math.Max(expectedSkill / currentSkill, 1.0f);

        var manaDamage = (damage * 0.25) * 3 * skillModifier;
        if (skill.AdvancementClass == SkillAdvancementClass.Specialized)
        {
            manaDamage = (damage * 0.25) * 1.5 * skillModifier;
        }

        if (targetPlayer.Mana.Current >= manaDamage)
        {
            amount = (uint)(damage * 0.75);

            targetPlayer.PlayParticleEffect(PlayScript.RestrictionEffectBlue, targetPlayer.Guid);
            targetPlayer.UpdateVitalDelta(targetPlayer.Mana, (int)-Math.Round(manaDamage));
        }
        // if not enough mana, barrier falls and player takes remainder of damage as health
        else
        {
            targetPlayer.ToggleManaBarrierSetting();
            targetPlayer.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"Your mana barrier fails and collapses!", ChatMessageType.Magic)
            );

            var manaBarrier = targetPlayer.GetInventoryItemsOfWCID(1051110); // Mana Barrier
            if (manaBarrier[0] is not null)
            {
                targetPlayer.EnchantmentManager.StartCooldown(manaBarrier[0]);
            }

            targetPlayer.PlayParticleEffect(PlayScript.HealthDownBlue, targetPlayer.Guid);

            // find mana damage overage and reconvert to HP damage
            var manaRemainder = (manaDamage - targetPlayer.Mana.Current) / skillModifier / 1.5;

            if (skill.AdvancementClass == SkillAdvancementClass.Specialized)
            {
                manaRemainder = (manaDamage - targetPlayer.Mana.Current) / skillModifier / 3;
            }

            amount = (uint)((damage * 0.75) + manaRemainder);

            targetPlayer.UpdateVitalDelta(targetPlayer.Mana, (int)-(targetPlayer.Mana.Current - 1));
        }

        return amount;
    }

    /// <summary>
    /// COMBAT ABILITY - Mana Barrier: Some damage taken from mana instead of health.
    /// </summary>
    private uint CheckForCombatAbilityEvasiveStance(Creature target, float damage, Player targetPlayer, uint amount)
    {
        if (targetPlayer is not { EvasiveStanceToggle: true })
        {
            return amount;
        }

        if (targetPlayer.Level == null || !targetPlayer.EvasiveStanceToggle)
        {
            return amount;
        }

        var runSkill = targetPlayer.GetCreatureSkill(Skill.Run);
        var jumpSkill = targetPlayer.GetCreatureSkill(Skill.Jump);

        var expectedSkill = (float)(targetPlayer.Level * 5);
        var currentSkill = runSkill.Current > jumpSkill.Current ? (float)runSkill.Current : (float)jumpSkill.Current;

        var skillSpecialized = runSkill.AdvancementClass == SkillAdvancementClass.Specialized || jumpSkill.AdvancementClass == SkillAdvancementClass.Specialized;

        // Create a scaling mod. If expected skill is much higher than currentSkill, you will be multiplying the amount
        // of stamina damage singificantly, so low skill players will not get much benefit before bubble bursts.
        // Capped at 1.0f so high skill gets the proper ratio of health-to-stamina, but no better than that.

        var skillModifier = Math.Max(expectedSkill / currentSkill, 1.0f);

        var staminaDamage = (damage * 0.25) * 3 * skillModifier;
        if (skillSpecialized)
        {
            staminaDamage = (damage * 0.25) * 1.5 * skillModifier;
        }

        if (targetPlayer.Stamina.Current >= staminaDamage)
        {
            amount = (uint)(damage * 0.75);

            targetPlayer.PlayParticleEffect(PlayScript.RestrictionEffectGold, targetPlayer.Guid);
            targetPlayer.UpdateVitalDelta(targetPlayer.Stamina, (int)-Math.Round(staminaDamage));
        }
        // if not enough mana, barrier falls and player takes remainder of damage as health
        else
        {
            targetPlayer.ToggleEvasiveStanceSetting();
            targetPlayer.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"You run out of stamina and are fall out of your Evasive Stance!", ChatMessageType.Magic)
            );

            var evasiveStance = targetPlayer.GetInventoryItemsOfWCID(1051114); // Evasive Stance
            if (evasiveStance[0] is not null)
            {
                targetPlayer.EnchantmentManager.StartCooldown(evasiveStance[0]);
            }

            targetPlayer.PlayParticleEffect(PlayScript.HealthDownYellow, targetPlayer.Guid);

            // find stamina damage overage and reconvert to HP damage
            var staminaRemainder = (staminaDamage - targetPlayer.Stamina.Current) / skillModifier / 1.5;

            if (skillSpecialized)
            {
                staminaRemainder = (staminaDamage - targetPlayer.Stamina.Current) / skillModifier / 3;
            }

            amount = (uint)((damage * 0.75) + staminaRemainder);

            targetPlayer.UpdateVitalDelta(targetPlayer.Stamina, (int)-(targetPlayer.Stamina.Current - 1));
        }

        return amount;
    }

    /// <summary>
    /// Sets the physics state for a launched projectile
    /// </summary>
    public void SetProjectilePhysicsState(WorldObject target, bool useGravity)
    {
        if (useGravity)
        {
            GravityStatus = true;
        }

        CurrentMotionState = null;
        Placement = null;

        // TODO: Physics description timestamps (sequence numbers) don't seem to be getting updated

        //Console.WriteLine("SpellProjectile PhysicsState: " + PhysicsObj.State);

        var pos = Location.Pos;
        var rotation = Location.Rotation;
        PhysicsObj.Position.Frame.Origin = pos;
        PhysicsObj.Position.Frame.Orientation = rotation;

        var velocity = Velocity;
        //velocity = Vector3.Transform(velocity, Matrix4x4.Transpose(Matrix4x4.CreateFromQuaternion(rotation)));
        PhysicsObj.Velocity = velocity;

        if (target != null)
        {
            PhysicsObj.ProjectileTarget = target.PhysicsObj;
        }

        PhysicsObj.set_active(true);
    }

    private static void ShowInfo(
        Creature observed,
        Spell spell,
        CreatureSkill skill,
        float criticalChance,
        bool criticalHit,
        bool critDefended,
        bool overpower,
        float weaponCritDamageMod,
        float magicSkillBonus,
        int baseDamage,
        float critDamageBonus,
        float elementalDamageMod,
        float slayerMod,
        float weaponResistanceMod,
        float resistanceMod,
        float absorbMod,
        float lifeProjectileDamage,
        float lifeMagicDamage,
        float finalDamage
    )
    {
        var observer = PlayerManager.GetOnlinePlayer(observed.DebugDamageTarget);
        if (observer == null)
        {
            observed.DebugDamage = Creature.DebugDamageType.None;
            return;
        }

        var info = $"Skill: {skill.Skill.ToSentence()}\n";
        info += $"CriticalChance: {criticalChance}\n";
        info += $"CriticalHit: {criticalHit}\n";

        if (critDefended)
        {
            info += $"CriticalDefended: {critDefended}\n";
        }

        info += $"Overpower: {overpower}\n";

        if (spell.MetaSpellType == ACE.Entity.Enum.SpellType.LifeProjectile)
        {
            // life magic projectile
            info += $"LifeProjectileDamage: {lifeProjectileDamage}\n";
            info += $"DamageRatio: {spell.DamageRatio}\n";
            info += $"LifeMagicDamage: {lifeMagicDamage}\n";
        }
        else
        {
            // war/void projectile
            var difficulty = Math.Min(spell.Power, 350);
            info += $"Difficulty: {difficulty}\n";

            if (magicSkillBonus != 0.0f)
            {
                info += $"SkillBonus: {magicSkillBonus}\n";
            }

            info += $"BaseDamageRange: {spell.MinDamage} - {spell.MaxDamage}\n";
            info += $"BaseDamage: {baseDamage}\n";
            info += $"DamageType: {spell.DamageType}\n";
        }

        if (weaponCritDamageMod != 1.0f)
        {
            info += $"WeaponCritDamageMod: {weaponCritDamageMod}\n";
        }

        if (critDamageBonus != 0)
        {
            info += $"CritDamageBonus: {critDamageBonus}\n";
        }

        if (elementalDamageMod != 1.0f)
        {
            info += $"ElementalDamageMod: {elementalDamageMod}\n";
        }

        if (slayerMod != 1.0f)
        {
            info += $"SlayerMod: {slayerMod}\n";
        }

        if (weaponResistanceMod != 1.0f)
        {
            info += $"WeaponResistanceMod: {weaponResistanceMod}\n";
        }

        if (resistanceMod != 1.0f)
        {
            info += $"ResistanceMod: {resistanceMod}\n";
        }

        if (absorbMod != 1.0f)
        {
            info += $"AbsorbMod: {absorbMod}\n";
        }

        //observer.Session.Network.EnqueueSend(new GameMessageSystemChat(info, ChatMessageType.Broadcast));
        observer.DebugDamageBuffer += info;
    }

    private static void ShowInfo(
        Creature observed,
        float heritageMod,
        float sneakAttackMod,
        float damageRatingMod,
        float damageResistRatingMod,
        float critDamageRatingMod,
        float critDamageResistRatingMod,
        float pkDamageRatingMod,
        float pkDamageResistRatingMod,
        float damage
    )
    {
        var observer = PlayerManager.GetOnlinePlayer(observed.DebugDamageTarget);
        if (observer == null)
        {
            observed.DebugDamage = Creature.DebugDamageType.None;
            return;
        }
        var info = "";

        if (heritageMod != 1.0f)
        {
            info += $"HeritageMod: {heritageMod}\n";
        }

        if (sneakAttackMod != 1.0f)
        {
            info += $"SneakAttackMod: {sneakAttackMod}\n";
        }

        if (critDamageRatingMod != 1.0f)
        {
            info += $"CritDamageRatingMod: {critDamageRatingMod}\n";
        }

        if (pkDamageRatingMod != 1.0f)
        {
            info += $"PkDamageRatingMod: {pkDamageRatingMod}\n";
        }

        if (damageRatingMod != 1.0f)
        {
            info += $"DamageRatingMod: {damageRatingMod}\n";
        }

        if (critDamageResistRatingMod != 1.0f)
        {
            info += $"CritDamageResistRatingMod: {critDamageResistRatingMod}\n";
        }

        if (pkDamageResistRatingMod != 1.0f)
        {
            info += $"PkDamageResistRatingMod: {pkDamageResistRatingMod}\n";
        }

        if (damageResistRatingMod != 1.0f)
        {
            info += $"DamageResistRatingMod: {damageResistRatingMod}\n";
        }

        info += $"Final damage: {damage}";

        observer.Session.Network.EnqueueSend(
            new GameMessageSystemChat(observer.DebugDamageBuffer + info, ChatMessageType.Broadcast)
        );

        observer.DebugDamageBuffer = null;
    }

    /// <summary>
    /// If resist succeeded, determine if resist was partial or full.
    /// </summary>
    private bool GetResistedMod(out float resistedMod)
    {
        switch (_partialEvasion)
        {
            case PartialEvasion.None:
                resistedMod = 1.0f;
                return false;
            case PartialEvasion.Some:
                resistedMod = 0.5f;
                return false;
            case PartialEvasion.All:
            default:
                resistedMod = 0.0f;
                return true;
        }
    }
}
