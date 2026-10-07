using System;
using System.Collections.Generic;
using ACE.DatLoader.Entity.AnimationHooks;
using ACE.Entity.Enum;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// WeaponTime is the time each hit takes, in 1/60ths of a second, for a wielder with 200 Quickness.
/// Swing animations are sped up or slowed down to match it, so a lower WeaponTime is always a faster weapon,
/// whatever its animation.
/// </summary>
public static class WeaponSpeed
{
    public const float TicksPerSecond = 60.0f;
    public const float ReferenceQuickness = 200.0f;

    /// <summary>
    /// The WeaponTime used when nothing is wielded (a punch at the old default speed of 40)
    /// </summary>
    public const int UnarmedWeaponTime = 34;

    /// <summary>
    /// The highest WeaponTime sent to the client for display
    /// </summary>
    public const int MaxDisplayWeaponTime = 150;

    public const float MinAnimSpeed = 0.5f;
    public const float MaxAnimSpeed = 2.5f;

    /// <summary>
    /// WeaponTime enchantments (Swift Killer, Leaden Weapon, auras) are percentages of the weapon's WeaponTime.
    /// They can make a weapon at most this much faster.
    /// </summary>
    private const int MaxSpeedBonusPercent = 75;

    public static float GetQuicknessMod(float quickness)
    {
        return 1.0f + Math.Max(0.0f, quickness) / 600.0f;
    }

    /// <summary>
    /// Returns the seconds each hit takes with this WeaponTime at this Quickness
    /// </summary>
    public static float GetSecondsPerHit(float weaponTime, float quickness = ReferenceQuickness)
    {
        return weaponTime / TicksPerSecond * GetQuicknessMod(ReferenceQuickness) / GetQuicknessMod(quickness);
    }

    /// <summary>
    /// Applies a WeaponTime enchantment percentage (negative is faster) to a WeaponTime
    /// </summary>
    public static int ApplySpeedPercent(int weaponTime, int percent)
    {
        percent = Math.Max(percent, -MaxSpeedBonusPercent);

        return Math.Max(1, (int)Math.Round(weaponTime * (1.0f + percent / 100.0f), MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Returns the animation speed that plays an animation of baseAnimLength in targetLength seconds
    /// </summary>
    public static float GetAnimSpeed(float baseAnimLength, float targetLength)
    {
        if (baseAnimLength <= 0)
        {
            return 1.0f;
        }

        if (targetLength <= 0)
        {
            return MaxAnimSpeed;
        }

        return Math.Clamp(baseAnimLength / targetLength, MinAnimSpeed, MaxAnimSpeed);
    }

    /// <summary>
    /// Returns how many hits a swing with this weapon deals.
    /// Two-handed weapons deal one hit, though their animation strikes twice.
    /// </summary>
    public static int GetHitsPerSwing(WorldObject weapon)
    {
        if (weapon == null || weapon.IsTwoHanded || weapon.IsRanged)
        {
            return 1;
        }

        var attackType = weapon.W_AttackType;

        if ((attackType & AttackType.TripleStrike) != 0)
        {
            return 3;
        }

        if ((attackType & AttackType.DoubleStrike) != 0)
        {
            return 2;
        }

        return 1;
    }

    /// <summary>
    /// Two-handed swings play two strikes but deal one hit.
    /// Returns the attack frames that deal damage for this swing.
    /// </summary>
    public static List<(float time, AttackHook attackHook)> GetTwoHandedHitFrames(
        List<(float time, AttackHook attackHook)> attackFrames
    )
    {
        if (attackFrames.Count <= 1)
        {
            return attackFrames;
        }

        var useLastFrame = PropertyManager.GetLong("two_handed_hit_frame").Item == 1;

        return [useLastFrame ? attackFrames[^1] : attackFrames[0]];
    }
}
