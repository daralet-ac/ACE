/*
 * Aegis (WCID 1051128) was reworked into a weapon damage reduction ability: for 10 seconds, damage
 * taken from weapon attacks is reduced by 40%, full evades become full hits, and each hit restores
 * stamina and mana equal to 10% of the damage prevented. It costs mana equal to character level.
 *
 * world-db lowered the Aegis weenie's CooldownDuration from 20 to 1 second, so it can be recast to
 * refresh its duration, and replaced its description. Those template changes only affect NEWLY
 * created items.
 *
 * This script retroactively updates already-persisted Aegis biotas (player inventories, houses,
 * mail, corpses, etc.) so existing copies match. Without it, existing copies keep the old 20 second
 * cooldown and the old description.
 *
 * Run the preview SELECT COUNT(*) query first to see how many rows will be touched.
 * Back up the shard database before applying this.
 */

-- Preview: how many existing Aegis items will be updated
-- SELECT COUNT(*) FROM biota WHERE weenie_Class_Id = 1051128;

START TRANSACTION;

/* CooldownDuration: 20 -> 1 */
INSERT INTO biota_properties_float (object_Id, `type`, value)
SELECT id, 167, 1 FROM biota WHERE weenie_Class_Id = 1051128
ON DUPLICATE KEY UPDATE value = 1;

/* Use text */
INSERT INTO biota_properties_string (object_Id, `type`, value)
SELECT id, 14, 'Use to reduce the damage you take from weapon attacks by 40%, for 10 seconds. While active, attacks you would have fully evaded become full hits, and each hit you take restores stamina and mana equal to 10% of the damage prevented.\n\nUsing Aegis again while it is active refreshes its duration.\n\nCost: Mana equal to your character level.\n\nRequired Focus: Spellsword, Warrior, or Sorcerer.'
FROM biota WHERE weenie_Class_Id = 1051128
ON DUPLICATE KEY UPDATE value = 'Use to reduce the damage you take from weapon attacks by 40%, for 10 seconds. While active, attacks you would have fully evaded become full hits, and each hit you take restores stamina and mana equal to 10% of the damage prevented.\n\nUsing Aegis again while it is active refreshes its duration.\n\nCost: Mana equal to your character level.\n\nRequired Focus: Spellsword, Warrior, or Sorcerer.';

COMMIT;
