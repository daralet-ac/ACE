using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using ACE.Common;
using ACE.Database;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Physics;
using ACE.Server.Physics.Extensions;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    /// <summary>
    /// Creates and launches the projectiles for a spell
    /// </summary>
    /// <param name="reflectedCaster">COMBAT ABILITY - Reflect: the original caster of a reflected spell</param>
    protected List<SpellProjectile> CreateSpellProjectiles(
        Spell spell,
        WorldObject target,
        WorldObject weapon,
        bool isWeaponSpell = false,
        bool fromProc = false,
        uint lifeProjectileDamage = 0,
        bool castAtTarget = false,
        int? weaponSpellcraft = null,
        double damageMultiplier = 1.0,
        Creature reflectedCaster = null
    )
    {
        if (spell.NumProjectiles == 0)
        {
            _log.Error($"{Name} ({Guid}).CreateSpellProjectiles({spell.Id} - {spell.Name}) - spell.NumProjectiles == 0");
            return new List<SpellProjectile>();
        }
        var spellType = SpellProjectile.GetProjectileSpellType(spell.Id);

        var origins = CalculateProjectileOrigins(spell, spellType, target);

        var velocity = CalculateProjectileVelocity(spell, target, spellType, origins[0]);

        // EMPOWERED SCARAB - Crushing
        // (not consumed by a reflected spell)
        var fireAllProjectilesFromCenter = false;
        var propertiesEnchantmentRegistry = EnchantmentManager.GetEnchantment(
            (uint)SpellId.GauntletCriticalDamageBoostI,
            null
        );
        if (propertiesEnchantmentRegistry != null && spellType == ProjectileSpellType.Blast && reflectedCaster == null)
        {
            EnchantmentManager.Dispel(propertiesEnchantmentRegistry);
            fireAllProjectilesFromCenter = true;
        }

        return LaunchSpellProjectiles(
            spell,
            target,
            spellType,
            weapon,
            isWeaponSpell,
            fromProc,
            origins,
            velocity,
            lifeProjectileDamage,
            castAtTarget,
            fireAllProjectilesFromCenter,
            weaponSpellcraft,
            damageMultiplier,
            reflectedCaster
        );
    }

    private const float ProjHeight = 2.0f / 3.0f;

    private Vector3 CalculatePreOffset(Spell spell, ProjectileSpellType spellType, WorldObject target)
    {
        var startFactor = spellType == ProjectileSpellType.Arc ? 1.0f : ProjHeight;

        var preOffset = new Vector3(0, 0, Height * startFactor);

        if (target == null)
        {
            return preOffset;
        }

        var startPos = new Physics.Common.Position(PhysicsObj.Position);
        startPos.Frame.Origin.Z += Height * startFactor;

        var endFactor = spellType == ProjectileSpellType.Arc ? ProjHeightArc : ProjHeight;

        var endPos = new Physics.Common.Position(target.PhysicsObj.Position);
        endPos.Frame.Origin.Z += target.Height * endFactor;

        var globOffset = startPos.GetOffset(endPos);

        // align in x
        var rotate = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)Math.Atan2(globOffset.X, globOffset.Y));

        var offset = Vector3.Transform(globOffset, rotate);

        var localDir = Vector3.Normalize(offset);

        var radsum = PhysicsObj.GetPhysicsRadius() + GetProjectileRadius(spell);

        var defaultSpawnPos = Vector3.UnitY * radsum;

        var spawnPos = localDir * radsum;

        var spawnOffset = spawnPos - defaultSpawnPos;

        return preOffset + spawnOffset;
    }

    /// <summary>
    /// Returns a list of positions to spawn projectiles for a spell,
    /// in local space relative to the caster
    /// </summary>
    private List<Vector3> CalculateProjectileOrigins(
        Spell spell,
        ProjectileSpellType spellType,
        WorldObject target
    )
    {
        var numProjectiles = spell.NumProjectiles;
        if (spellType == ProjectileSpellType.Blast)
        {
            numProjectiles = 1;
        }

        var origins = new List<Vector3>();

        var radius = GetProjectileRadius(spell);

        var vRadius = Vector3.One * radius;

        var baseOffset = spell.CreateOffset;

        var radsum = PhysicsObj.GetPhysicsRadius() * 2.0f + radius * 2.0f;

        var heightOffset = CalculatePreOffset(spell, spellType, target);

        if (target != null)
        {
            var cylDist = GetCylinderDistance(target);
            if (cylDist < 0.6f)
            {
                radsum = PhysicsObj.GetPhysicsRadius() + radius;
            }
        }

        if (Math.Abs(spell.SpreadAngle - 360) < 1)
        {
            radsum *= 0.6f;
        }

        baseOffset.Y += radsum;

        baseOffset += heightOffset;

        var anglePerStep = GetSpreadAnglePerStep(spell);

        // TODO: normalize data
        var dims = new Vector3(
            spell._spell.DimsOriginX ?? numProjectiles,
            spell._spell.DimsOriginY ?? 1,
            spell._spell.DimsOriginZ ?? 1
        );

        var i = 0;
        for (var z = 0; z < dims.Z; z++)
        {
            for (var y = 0; y < dims.Y; y++)
            {
                var oddRow = (int)Math.Min(dims.X, numProjectiles - i) % 2 == 1;

                for (var x = 0; x < dims.X; x++)
                {
                    if (i >= numProjectiles)
                    {
                        break;
                    }

                    var curOffset = baseOffset;

                    if (spell.Peturbation != Vector3.Zero)
                    {
                        var rng = new Vector3(
                            (float)ThreadSafeRandom.Next(-1.0f, 1.0f),
                            (float)ThreadSafeRandom.Next(-1.0f, 1.0f),
                            (float)ThreadSafeRandom.Next(-1.0f, 1.0f)
                        );

                        curOffset += rng * spell.Peturbation * spell.Padding;
                    }

                    if (!oddRow && spell.SpreadAngle == 0)
                    {
                        curOffset.X += spell.Padding.X * 0.5f + radius;
                    }

                    var xFactor =
                        spell.SpreadAngle == 0
                            ? oddRow
                                ? (float)Math.Ceiling(x * 0.5f)
                                : (float)Math.Floor(x * 0.5f)
                            : 0;

                    var origin = curOffset + (vRadius * 2.0f + spell.Padding) * new Vector3(xFactor, y, z);

                    if (spell.SpreadAngle == 0)
                    {
                        if (x % 2 == (oddRow ? 1 : 0))
                        {
                            origin.X *= -1.0f;
                        }
                    }
                    else
                    {
                        // get the rotation matrix to apply to x
                        var numSteps = (x + 1) / 2;
                        if (x % 2 == 0)
                        {
                            numSteps *= -1;
                        }

                        var curAngle = anglePerStep * numSteps;
                        var rads = curAngle.ToRadians();

                        var rot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rads);
                        origin = Vector3.Transform(origin, rot);
                    }

                    origins.Add(origin);
                    i++;
                }

                if (i >= numProjectiles)
                {
                    break;
                }
            }

            if (i >= numProjectiles)
            {
                break;
            }
        }

        return origins;
    }

    /// <summary>
    /// Returns the angle in degrees between projectiles
    /// for spells with SpreadAngle
    /// </summary>
    private static float GetSpreadAnglePerStep(Spell spell)
    {
        if (spell.SpreadAngle == 0.0f || spell.NumProjectiles == 1)
        {
            return 0.0f;
        }

        var numProjectiles = spell.NumProjectiles;

        if (numProjectiles % 2 == 1)
        {
            numProjectiles--;
        }

        return spell.SpreadAngle / numProjectiles;
    }

    private static readonly Quaternion OneEighty = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)Math.PI);

    private const float ProjHeightArc = 5.0f / 6.0f;

    /// <summary>
    /// Calculates the spell projectile velocity in global space
    /// </summary>
    private Vector3 CalculateProjectileVelocity(
        Spell spell,
        WorldObject target,
        ProjectileSpellType spellType,
        Vector3 origin
    )
    {
        var casterLoc = PhysicsObj.Position.ACEPosition();

        var speed = GetProjectileSpeed(spell);

        if (target == null && this is Creature creature && !(this is Player))
        {
            target = creature.AttackTarget;
        }

        if (target == null)
        {
            // launch along forward vector
            return Vector3.Transform(Vector3.UnitY, casterLoc.Rotation) * speed;
        }

        var targetLoc = target.PhysicsObj.Position.ACEPosition();

        var strikeSpell = spellType == ProjectileSpellType.Strike;

        var crossLandblock = !strikeSpell && casterLoc.Landblock != targetLoc.Landblock;

        var qDir = PhysicsObj.Position.GetOffset(target.PhysicsObj.Position);
        var rotate = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)Math.Atan2(-qDir.X, qDir.Y));

        var startPos = strikeSpell
            ? targetLoc.Pos
            : crossLandblock
                ? casterLoc.ToGlobal(false)
                : casterLoc.Pos;
        startPos += Vector3.Transform(origin, strikeSpell ? rotate * OneEighty : rotate);

        var endPos = crossLandblock ? targetLoc.ToGlobal(false) : targetLoc.Pos;

        endPos.Z += target.Height * (spellType == ProjectileSpellType.Arc ? ProjHeightArc : ProjHeight);

        var dir = Vector3.Normalize(endPos - startPos);

        var targetVelocity = spell.IsTracking ? target.PhysicsObj.CachedVelocity : Vector3.Zero;

        var useGravity = spellType == ProjectileSpellType.Arc;

        if (useGravity || targetVelocity != Vector3.Zero)
        {
            var gravity = useGravity ? PhysicsGlobals.Gravity : 0.0f;

            Vector3 velocity;
            if (!PropertyManager.GetBool("trajectory_alt_solver").Item)
            {
                Trajectory.solve_ballistic_arc_lateral(
                    startPos,
                    speed,
                    endPos,
                    targetVelocity,
                    gravity,
                    out velocity,
                    out var time,
                    out var impactPoint
                );
            }
            else
            {
                velocity = Trajectory2.CalculateTrajectory(startPos, endPos, targetVelocity, speed, useGravity);
            }

            if (velocity == Vector3.Zero && useGravity && targetVelocity != Vector3.Zero)
            {
                // intractable?
                // try to solve w/ zero velocity
                if (!PropertyManager.GetBool("trajectory_alt_solver").Item)
                {
                    Trajectory.solve_ballistic_arc_lateral(
                        startPos,
                        speed,
                        endPos,
                        Vector3.Zero,
                        gravity,
                        out velocity,
                        out var time,
                        out var impactPoint
                    );
                }
                else
                {
                    velocity = Trajectory2.CalculateTrajectory(startPos, endPos, Vector3.Zero, speed, useGravity);
                }
            }
            if (velocity != Vector3.Zero)
            {
                return velocity;
            }
        }

        return dir * speed;
    }

    private List<SpellProjectile> LaunchSpellProjectiles(
        Spell spell,
        WorldObject target,
        ProjectileSpellType spellType,
        WorldObject weapon,
        bool isWeaponSpell,
        bool fromProc,
        List<Vector3> origins,
        Vector3 velocity,
        uint lifeProjectileDamage = 0,
        bool castAtTarget = false,
        bool fireAllProjectilesFromCenter = false,
        int? weaponSpellcraft = null,
        double damageMultiplier = 1.0,
        Creature reflectedCaster = null
    )
    {
        var useGravity = spellType == ProjectileSpellType.Arc;

        var strikeSpell = target != null && spellType == ProjectileSpellType.Strike;

        var spellProjectiles = new List<SpellProjectile>();

        var casterLoc = castAtTarget ? target?.PhysicsObj.Position.ACEPosition() : PhysicsObj.Position.ACEPosition();
        var targetLoc = target?.PhysicsObj.Position.ACEPosition();

        for (var i = 0; i < origins.Count; i++)
        {
            var origin = origins[i];

            if (fireAllProjectilesFromCenter)
            {
                origin = origins[0];
            }

            var sp = WorldObjectFactory.CreateNewWorldObject(spell.Wcid) as SpellProjectile;

            if (sp == null)
            {
                _log.Error(
                    $"{Name} ({Guid}).LaunchSpellProjectiles({spell.Id} - {spell.Name}) - failed to create spell projectile from wcid {spell.Wcid}"
                );
                break;
            }

            sp.Setup(spell, spellType);

            if (casterLoc != null)
            {
                var rotate = casterLoc.Rotation;
                if (target != null)
                {
                    var qDir = PhysicsObj.Position.GetOffset(target.PhysicsObj.Position);
                    rotate = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)Math.Atan2(-qDir.X, qDir.Y));
                }

                sp.Location = strikeSpell ? new Position(targetLoc) : new Position(casterLoc);
                sp.Location.Pos += Vector3.Transform(origin, strikeSpell ? rotate * OneEighty : rotate);
            }

            sp.PhysicsObj.Velocity = velocity;

            if (spell.SpreadAngle > 0 && !fireAllProjectilesFromCenter)
            {
                var n = Vector3.Normalize(origin);
                var angle = Math.Atan2(-n.X, n.Y);
                var q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)angle);
                sp.PhysicsObj.Velocity = Vector3.Transform(velocity, q);
            }

            // set orientation
            var dir = Vector3.Normalize(sp.Velocity);
            sp.PhysicsObj.Position.Frame.set_vector_heading(dir);
            sp.Location.Rotation = sp.PhysicsObj.Position.Frame.Orientation;

            sp.ProjectileSource = this;
            sp.FromProc = fromProc;

            // side projectiles always untargeted?
            if (i == 0)
            {
                sp.ProjectileTarget = target;
            }

            sp.ProjectileLauncher = weapon;
            sp.IsWeaponSpell = isWeaponSpell;

            sp.SetProjectilePhysicsState(sp.ProjectileTarget, useGravity);
            sp.SpawnPos = new Position(sp.Location);

            sp.LifeProjectileDamage = lifeProjectileDamage;

            sp.WeaponSpellcraft = weaponSpellcraft;

            sp.DamageMultiplier = damageMultiplier;

            // set before entering the world, a projectile can collide with its target on world entry
            sp.ReflectedCaster = reflectedCaster;

            sp.InstanceId = InstanceId;

            if (!LandblockManager.AddObject(sp))
            {
                sp.Destroy();
                continue;
            }

            if (sp.WorldEntryCollision)
            {
                continue;
            }

            sp.EnqueueBroadcast(
                new GameMessageScript(sp.Guid, PlayScript.Launch, sp.GetProjectileScriptIntensity(spellType))
            );

            if (!IsProjectileVisible(sp))
            {
                sp.OnCollideEnvironment();
                continue;
            }

            spellProjectiles.Add(sp);
        }

        return spellProjectiles;
    }

    public static void ClearSpellCache()
    {
        ProjectileRadiusCache.Clear();
        ProjectileSpeedCache.Clear();
    }

    protected static readonly ConcurrentDictionary<uint, float> ProjectileRadiusCache =
        new ConcurrentDictionary<uint, float>();

    private float GetProjectileRadius(Spell spell)
    {
        var projectileWcid = spell.WeenieClassId;

        if (ProjectileRadiusCache.TryGetValue(projectileWcid, out var radius))
        {
            return radius;
        }

        var weenie = DatabaseManager.World.GetCachedWeenie(projectileWcid);

        if (weenie == null)
        {
            _log.Error(
                $"{Name} ({Guid}).GetSetupRadius({spell.Id} - {spell.Name}): couldn't find weenie {projectileWcid}"
            );
            return 0.0f;
        }

        if (!weenie.PropertiesDID.TryGetValue(PropertyDataId.Setup, out var setupId))
        {
            _log.Error(
                $"{Name} ({Guid}).GetSetupRadius({spell.Id} - {spell.Name}): couldn't find setup ID for {weenie.WeenieClassId} - {weenie.ClassName}"
            );
            return 0.0f;
        }

        var setup = DatManager.PortalDat.ReadFromDat<SetupModel>(setupId);

        if (!weenie.PropertiesFloat.TryGetValue(PropertyFloat.DefaultScale, out var scale))
        {
            scale = 1.0f;
        }

        var result = (float)(setup.Spheres[0].Radius * scale);

        ProjectileRadiusCache.TryAdd(projectileWcid, result);

        return result;
    }

    private static readonly ConcurrentDictionary<uint, float> ProjectileSpeedCache =
        new ConcurrentDictionary<uint, float>();

    /// <summary>
    /// Gets the speed of a spell's projectile (its weenie's MaximumVelocity)
    /// </summary>
    private float GetProjectileSpeed(Spell spell)
    {
        var projectileWcid = spell.WeenieClassId;

        if (!ProjectileSpeedCache.TryGetValue(projectileWcid, out var baseSpeed))
        {
            var weenie = DatabaseManager.World.GetCachedWeenie(projectileWcid);

            if (weenie == null)
            {
                _log.Error(
                    $"{Name} ({Guid}).GetSpellProjectileSpeed({spell.Id} - {spell.Name}): couldn't find weenie {projectileWcid}"
                );
                return 0.0f;
            }

            if (!weenie.PropertiesFloat.TryGetValue(PropertyFloat.MaximumVelocity, out var maxVelocity))
            {
                _log.Error(
                    $"{Name} ({Guid}).GetSpellProjectileSpeed({spell.Id} - {spell.Name}): couldn't find MaxVelocity for {weenie.WeenieClassId} - {weenie.ClassName}"
                );
                return 0.0f;
            }

            baseSpeed = (float)maxVelocity;

            ProjectileSpeedCache.TryAdd(projectileWcid, baseSpeed);
        }

        return baseSpeed;
    }
}
