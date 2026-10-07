/*
 * Weapon Time rework.
 *
 * WeaponTime (PropertyInt 49) is now the time each hit takes, in 1/60ths of a second, for a wielder with
 * 200 Quickness. Swing animations are sped up or slowed down to match it, so a lower WeaponTime is always
 * a faster weapon. Every weapon keeps its old attack speed at 200 Quickness.
 *
 * Two-handed weapons now deal one hit per swing (their animation still strikes twice), so their Damage
 * and BaseDamage are doubled.
 *
 * WeaponTime enchantments (Swift Killer, Leaden Weapon, Swift Hunter, auras) are now percentages of the
 * weapon's WeaponTime. world-db converted the spells, which only affects NEWLY cast enchantments.
 *
 * This script retroactively converts already-persisted weapons (WeaponTime, BaseWeaponTime, two-handed
 * Damage/BaseDamage) and active WeaponTime enchantments to match. It uses the same conversion as world-db.
 *
 * RUN THIS ONCE. Running it again converts the converted values again.
 * Back up the shard database before applying this.
 */

-- Preview: how many weapons will be converted
-- SELECT COUNT(*) FROM biota_properties_int WHERE `type` = 49;

START TRANSACTION;

DROP TEMPORARY TABLE IF EXISTS weapon_time_rework;

CREATE TEMPORARY TABLE weapon_time_rework (
    object_Id INT UNSIGNED NOT NULL PRIMARY KEY,
    weenie_type INT NOT NULL,
    combat_style INT NULL,
    attack_type INT NOT NULL,
    weapon_skill INT NULL,
    weapon_time INT NOT NULL,
    base_weapon_time INT NULL,
    kind VARCHAR(8) NULL,
    anim DOUBLE NULL,
    reload_or_hits DOUBLE NULL,
    new_weapon_time INT NULL,
    new_base_weapon_time INT NULL
);

INSERT INTO weapon_time_rework (object_Id, weenie_type, combat_style, attack_type, weapon_skill, weapon_time, base_weapon_time)
SELECT b.id, b.weenie_Type, style.value, COALESCE(atk.value, 0), skill.value, wt.value, bwt.value
FROM biota b
JOIN biota_properties_int wt ON wt.object_Id = b.id AND wt.`type` = 49 /* WeaponTime */
LEFT JOIN biota_properties_int style ON style.object_Id = b.id AND style.`type` = 46 /* DefaultCombatStyle */
LEFT JOIN biota_properties_int atk ON atk.object_Id = b.id AND atk.`type` = 47 /* AttackType */
LEFT JOIN biota_properties_int skill ON skill.object_Id = b.id AND skill.`type` = 48 /* WeaponSkill */
LEFT JOIN biota_properties_int bwt ON bwt.object_Id = b.id AND bwt.`type` = 454 /* BaseWeaponTime */;

/* old animation length of each weapon: two-handed, launcher (bow / crossbow / atlatl), thrown, or one-handed melee by AttackType */
UPDATE weapon_time_rework r SET
    r.kind = CASE
        WHEN r.weenie_type = 6 /* MeleeWeapon */ AND r.combat_style = 8 /* TwoHanded */ THEN '2H'
        WHEN r.weenie_type = 6 /* MeleeWeapon */ THEN 'melee'
        WHEN r.weenie_type = 3 /* MissileLauncher */ AND r.combat_style = 1 /* Unarmed */ THEN 'punch'
        WHEN r.weenie_type = 1 /* Generic */ AND r.combat_style = 128 /* ThrownWeapon */ THEN 'thrown'
        WHEN r.weenie_type = 3 /* MissileLauncher */ AND r.combat_style = 16 /* Bow */ THEN 'bow'
        WHEN r.weenie_type = 3 /* MissileLauncher */ AND r.combat_style = 32 /* Crossbow */ THEN 'crossbow'
        WHEN r.weenie_type = 3 /* MissileLauncher */ AND r.combat_style = 1024 /* Atlatl */ THEN 'atlatl'
        WHEN r.weenie_type = 3 /* MissileLauncher */ AND r.weapon_skill = 2 /* Bow */ THEN 'bow'
        WHEN r.weenie_type = 3 /* MissileLauncher */ AND r.weapon_skill = 3 /* Crossbow */ THEN 'crossbow'
        WHEN r.weenie_type = 3 /* MissileLauncher */ AND r.weapon_skill IN (12 /* ThrownWeapon */, 47 /* MissileWeapons */) THEN 'atlatl'
        WHEN r.weenie_type = 4 /* Missile */ THEN 'thrown'
        ELSE NULL
    END;

UPDATE weapon_time_rework r SET
    r.anim = CASE r.kind
        WHEN '2H' THEN 1.85
        WHEN 'punch' THEN 1.10
        WHEN 'bow' THEN 1.057
        WHEN 'crossbow' THEN 1.59
        WHEN 'atlatl' THEN 1.85
        WHEN 'thrown' THEN 2.33
        ELSE NULL
    END,
    r.reload_or_hits = CASE r.kind
        WHEN '2H' THEN 1
        WHEN 'punch' THEN 1
        WHEN 'bow' THEN 0.32
        WHEN 'crossbow' THEN 0.26
        WHEN 'atlatl' THEN 0.73
        WHEN 'thrown' THEN 0.9777778
        ELSE NULL
    END;

UPDATE weapon_time_rework r SET
    r.anim = CASE
        WHEN r.attack_type = 0x20 THEN 2.62
        WHEN r.attack_type = 0x80 THEN 3.05
        WHEN r.attack_type = 0x8 THEN 1.1
        WHEN r.attack_type = 0x10 THEN 0.87
        WHEN r.attack_type = 0x400 THEN 1.2
        WHEN r.attack_type = 0x200 THEN 1.2
        WHEN r.attack_type = 0x1 THEN 1.1
        WHEN r.attack_type = 0x11 THEN 1.1
        WHEN r.attack_type = 0x4 THEN 1.33
        WHEN r.attack_type = 0x2 THEN 1.38
        WHEN r.attack_type = 0x6 THEN 1.33
        WHEN r.attack_type = 0x40 THEN 3.18
        WHEN r.attack_type = 0x100 THEN 4.64
        WHEN (r.attack_type & 0x5140) != 0 AND (r.attack_type & 0x1040) != 0 THEN 3.18
        WHEN (r.attack_type & 0x5140) != 0 THEN 4.64
        WHEN (r.attack_type & 0x28A0) != 0 AND (r.attack_type & 0x820) != 0 THEN 2.62
        WHEN (r.attack_type & 0x28A0) != 0 THEN 3.05
        WHEN (r.attack_type & 0x4) != 0 THEN 1.33
        WHEN (r.attack_type & 0x2) != 0 THEN 1.38
        WHEN (r.attack_type & 0x11) != 0 THEN 1.1
        ELSE NULL
    END,
    r.reload_or_hits = CASE
        WHEN r.attack_type = 0x20 THEN 2
        WHEN r.attack_type = 0x80 THEN 2
        WHEN r.attack_type = 0x8 THEN 1
        WHEN r.attack_type = 0x10 THEN 1
        WHEN r.attack_type = 0x400 THEN 1
        WHEN r.attack_type = 0x200 THEN 1
        WHEN r.attack_type = 0x1 THEN 1
        WHEN r.attack_type = 0x11 THEN 1
        WHEN r.attack_type = 0x4 THEN 1
        WHEN r.attack_type = 0x2 THEN 1
        WHEN r.attack_type = 0x6 THEN 1
        WHEN r.attack_type = 0x40 THEN 3
        WHEN r.attack_type = 0x100 THEN 3
        WHEN (r.attack_type & 0x5140) != 0 AND (r.attack_type & 0x1040) != 0 THEN 3
        WHEN (r.attack_type & 0x5140) != 0 THEN 3
        WHEN (r.attack_type & 0x28A0) != 0 AND (r.attack_type & 0x820) != 0 THEN 2
        WHEN (r.attack_type & 0x28A0) != 0 THEN 2
        WHEN (r.attack_type & 0x4) != 0 THEN 1
        WHEN (r.attack_type & 0x2) != 0 THEN 1
        WHEN (r.attack_type & 0x11) != 0 THEN 1
        ELSE NULL
    END
WHERE r.kind = 'melee';

/*
 * old attack speed: animSpeed = clamp(1 + (1 - WeaponTime / 100) + 200 / 600, 1, 2.5)
 * launchers and thrown weapons only sped up their reload animation; melee sped up the whole swing
 * new WeaponTime = seconds per hit x 60, rounded half up
 */
UPDATE weapon_time_rework r SET
    r.new_weapon_time = GREATEST(1, FLOOR(60 * CASE
        WHEN r.kind IN ('bow', 'crossbow', 'atlatl', 'thrown')
            THEN r.anim - r.reload_or_hits + r.reload_or_hits / LEAST(GREATEST(1 + (1 - r.weapon_time / 100e0) + 200e0 / 600e0, 1), 2.5)
        ELSE r.anim / LEAST(GREATEST(1 + (1 - r.weapon_time / 100e0) + 200e0 / 600e0, 1), 2.5) / r.reload_or_hits
    END + 0.5e0)),
    r.new_base_weapon_time = CASE WHEN r.base_weapon_time IS NULL OR r.base_weapon_time = 0 THEN r.base_weapon_time
        ELSE GREATEST(1, FLOOR(60 * CASE
            WHEN r.kind IN ('bow', 'crossbow', 'atlatl', 'thrown')
                THEN r.anim - r.reload_or_hits + r.reload_or_hits / LEAST(GREATEST(1 + (1 - r.base_weapon_time / 100e0) + 200e0 / 600e0, 1), 2.5)
            ELSE r.anim / LEAST(GREATEST(1 + (1 - r.base_weapon_time / 100e0) + 200e0 / 600e0, 1), 2.5) / r.reload_or_hits
        END + 0.5e0)) END
WHERE r.anim IS NOT NULL;

UPDATE biota_properties_int p
JOIN weapon_time_rework r ON r.object_Id = p.object_Id
SET p.value = r.new_weapon_time
WHERE p.`type` = 49 /* WeaponTime */ AND r.new_weapon_time IS NOT NULL;

UPDATE biota_properties_int p
JOIN weapon_time_rework r ON r.object_Id = p.object_Id
SET p.value = r.new_base_weapon_time
WHERE p.`type` = 454 /* BaseWeaponTime */ AND r.new_base_weapon_time IS NOT NULL;

/* two-handed weapons: one hit per swing, so double the damage of each hit */
UPDATE biota_properties_int p
JOIN biota b ON b.id = p.object_Id AND b.weenie_Type = 6 /* MeleeWeapon */
JOIN biota_properties_int style ON style.object_Id = p.object_Id AND style.`type` = 46 /* DefaultCombatStyle */ AND style.value = 8 /* TwoHanded */
SET p.value = p.value * 2
WHERE p.`type` IN (44 /* Damage */, 452 /* BaseDamage */);

/*
 * active WeaponTime enchantments (Int, Additive): flat points become a percentage of the WeaponTime,
 * matching the old effect on an average weapon (WeaponTime 50) at 200 Quickness
 */
UPDATE biota_properties_enchantment_registry e
SET e.stat_Mod_Value = CASE
    WHEN e.stat_Mod_Value <= -500 THEN -75
    WHEN e.stat_Mod_Value < 0 THEN -FLOOR((1 - (11e0 / 6e0) / ((11e0 / 6e0) - e.stat_Mod_Value / 100e0)) * 100 + 0.5)
    ELSE FLOOR(((11e0 / 6e0) / ((11e0 / 6e0) - e.stat_Mod_Value / 100e0) - 1) * 100 + 0.5)
END
WHERE e.stat_Mod_Key IN (49 /* WeaponTime */, 361 /* WeaponAuraSpeed */)
    AND (e.stat_Mod_Type & 0x4) != 0 /* Int */
    AND (e.stat_Mod_Type & 0x10) = 0 /* not Skill */
    AND (e.stat_Mod_Type & 0x8000) != 0 /* Additive */;

DROP TEMPORARY TABLE weapon_time_rework;

COMMIT;
