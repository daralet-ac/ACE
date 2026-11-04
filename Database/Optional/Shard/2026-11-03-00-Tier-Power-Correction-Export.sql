/*
 * Tier power correction: export from the shard BACKUP taken before migrate-tier-power.
 *
 * Read-only. Lists every loot melee, thrown and launcher weapon with the Damage (or DamageMod) it rolled, for the
 * correct-tier-power-damage admin command, which puts back the damage the migration placed too low.
 *
 * Run it against the backup, not the live shard, as a tab separated file with a header:
 *   mariadb -B ace_shard_backup < 2026-11-03-00-Tier-Power-Correction-Export.sql > tier-power-backup.tsv
 * Then copy the file to the server and run, with the world closed for the fix:
 *   correct-tier-power-damage <path to tier-power-backup.tsv>
 *   correct-tier-power-damage <path to tier-power-backup.tsv> fix
 */

SELECT b.id,
       b.weenie_Class_Id AS wcid,
       tier.value AS tier,
       damage.value AS damage,
       base_damage.value AS base_damage,
       damage_mod.value AS damage_mod,
       base_damage_mod.value AS base_damage_mod
FROM biota b
JOIN biota_properties_int tier ON tier.object_Id = b.id AND tier.`type` = 10007 /* Tier */ AND tier.value >= 1
LEFT JOIN biota_properties_int damage ON damage.object_Id = b.id AND damage.`type` = 44 /* Damage */
LEFT JOIN biota_properties_int base_damage ON base_damage.object_Id = b.id AND base_damage.`type` = 452 /* BaseDamage */
LEFT JOIN biota_properties_float damage_mod ON damage_mod.object_Id = b.id AND damage_mod.`type` = 63 /* DamageMod */
LEFT JOIN biota_properties_float base_damage_mod ON base_damage_mod.object_Id = b.id AND base_damage_mod.`type` = 20050 /* BaseDamageMod */
WHERE (b.weenie_Type IN (1 /* Generic */, 4 /* Missile */, 6 /* MeleeWeapon */) AND damage.value > 0)
   OR (b.weenie_Type = 3 /* MissileLauncher */ AND damage_mod.value > 0)
ORDER BY b.id;
