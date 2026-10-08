using System;
using System.Collections.Generic;
using System.Numerics;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using Position = ACE.Entity.Position;

namespace ACE.Server.WorldObjects;

public partial class SpellProjectile : WorldObject
{
    public Spell Spell;
    public ProjectileSpellType SpellType { get; set; }

    public Position SpawnPos { get; set; }
    public uint LifeProjectileDamage { get; set; }

    public int Strikethrough;
    public const int StrikethroughLimit = 3;
    private const double StrikethroughChance = 0.5f;

    private readonly List<uint> _strikethroughTargets = [];

    public int? WeaponSpellcraft;

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
    /// Assigned from emote CastSpellInstant/CastSpell (emote.Percent)
    /// </summary>
    public double DamageMultiplier = 1.0;

    /// <summary>
    /// COMBAT ABILITY - Reflect: the original caster of a reflected spell.
    /// The projectile is launched by the reflecting player (ProjectileSource),
    /// but its resist check and damage are based on this creature's stats.
    /// </summary>
    public Creature ReflectedCaster;

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
            PhysicsObj.Omega = new Vector3((float)(Math.PI * 2 * RotationSpeed), 0, 0);
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
        // a volley that struck through a target ignores it afterwards
        if (target != null && _strikethroughTargets.Contains(target.Guid.Full))
        {
            return;
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

        if (ShouldImpact())
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
        if (!VerifyPkStatus(creatureTarget))
        {
            return;
        }

        var targetPlayer = creatureTarget as Player;

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
            ref resisted,
            out var partialEvasion
        );

        // COMBAT ABILITY - Overload/Battery: charges on every hit, even a resisted one
        if (player is { OverloadStanceIsActive: true } or {BatteryStanceIsActive: true})
        {
            player.IncreaseChargedMeter(Spell, FromProc);
        }

        if (targetPlayer != null && damage != null)
        {
            damage = ApplySigilAbsorption(targetPlayer, target, damage.Value);
        }

        creatureTarget.OnAttackReceived(sourceCreature, CombatType.Magic, critical, resisted, (int)Spell.Level);

        if (damage != null)
        {
            OnSuccessfulHit(target, creatureTarget, damage.Value, critical, critDefended, overpower, partialEvasion);
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
                && (sourceCreature.AllowFactionCombat(creatureTarget) || sourceCreature.PotentialFoe(creatureTarget))
            )
            {
                sourceCreature.MonsterOnAttackMonster(creatureTarget);
            }
        }
    }

    /// <summary>
    /// Volleys can strike through targets: they stop at the strikethrough limit, or by chance.
    /// Every other projectile stops at the first thing it hits.
    /// </summary>
    private bool ShouldImpact()
    {
        return GetProjectileSpellType(Spell.Id) != ProjectileSpellType.Volley
            || Strikethrough == StrikethroughLimit
            || ThreadSafeRandom.Next(0.0f, 1.0f) < StrikethroughChance;
    }

    /// <summary>
    /// A player target must have a PK status the caster can attack. If not, tells both players and returns FALSE.
    /// </summary>
    private bool VerifyPkStatus(Creature creatureTarget)
    {
        var pkError = ProjectileSource?.CheckPKStatusVsTarget(creatureTarget, Spell);
        if (pkError == null)
        {
            return true;
        }

        if (ProjectileSource is Player player)
        {
            player.Session.Network.EnqueueSend(
                new GameEventWeenieErrorWithString(player.Session, pkError[0], creatureTarget.Name)
            );
        }

        if (creatureTarget is Player targetPlayer)
        {
            targetPlayer.Session.Network.EnqueueSend(
                new GameEventWeenieErrorWithString(targetPlayer.Session, pkError[1], ProjectileSource.Name)
            );
        }

        return false;
    }

    /// <summary>
    /// SIGIL TRINKET - Top of Absorption: the target player may convert part of the spell's damage into mana
    /// </summary>
    private float ApplySigilAbsorption(Player targetPlayer, WorldObject target, float damage)
    {
        var sigilDamageReductionMod = targetPlayer.CheckForSigilTrinketOnSpellHitReceivedEffects(this, Spell, (int)damage, Skill.MagicDefense,
            SigilTrinketMagicDefenseEffect.Absorption);

        if (damage < 0 || damage > uint.MaxValue)
        {
            _log.Error("OnCollideObject({Target}) - damage ({Damage}) could not be converted to uint.", target.Name, damage);
            return damage;
        }

        return Convert.ToUInt32(damage * sigilDamageReductionMod);
    }

    /// <summary>
    /// The spell landed on its target: applies its enchantment or damage, then proficiency and on-hit procs
    /// </summary>
    private void OnSuccessfulHit(
        WorldObject target,
        Creature creatureTarget,
        float damage,
        bool critical,
        bool critDefended,
        bool overpower,
        PartialEvasion partialEvasion
    )
    {
        var player = ProjectileSource as Player;
        var sourceCreature = ProjectileSource as Creature;

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
            DamageTarget(creatureTarget, damage, critical, critDefended, overpower, partialEvasion);
        }

        Strikethrough++;

        _strikethroughTargets.Add(creatureTarget.Guid.Full);

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
            player.CheckForSigilTrinketOnCastEffects(target, Spell, true, Skill.WarMagic, SigilTrinketWarMagicEffect.Detonate, creatureTarget);
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
                    player.CheckForSigilTrinketOnCastEffects(target, Spell, false, Skill.WarMagic, SigilTrinketWarMagicEffect.Detonate, creatureTarget);
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

    /// <summary>
    /// Sets what the projectile carries from its cast
    /// </summary>
    public void SetLaunchParameters(WorldObject source, SpellProjectileLaunch launch)
    {
        ProjectileSource = source;
        ProjectileLauncher = launch.Weapon;
        IsWeaponSpell = launch.IsWeaponSpell;
        FromProc = launch.FromProc;
        LifeProjectileDamage = launch.LifeProjectileDamage;
        WeaponSpellcraft = launch.WeaponSpellcraft;
        DamageMultiplier = launch.DamageMultiplier;
        ReflectedCaster = launch.ReflectedCaster;
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

        var pos = Location.Pos;
        var rotation = Location.Rotation;
        PhysicsObj.Position.Frame.Origin = pos;
        PhysicsObj.Position.Frame.Orientation = rotation;

        var velocity = Velocity;
        PhysicsObj.Velocity = velocity;

        if (target != null)
        {
            PhysicsObj.ProjectileTarget = target.PhysicsObj;
        }

        PhysicsObj.set_active(true);
    }
}
