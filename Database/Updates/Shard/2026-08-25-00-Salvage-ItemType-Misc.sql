/*
 * Salvage bags (WeenieType.Salvage = 76) were reclassified from ItemType.TinkeringMaterial (1073741824)
 * to ItemType.Misc (128) at the weenie-template level, to avoid an AC-client-side "may be destroyed"
 * confirmation and a vendor-window display quirk both tied to ItemType.TinkeringMaterial.
 *
 * That template change only affects NEWLY created salvage bags. This script retroactively updates
 * already-persisted biotas (anywhere: player inventories, houses, mail, corpses, active market
 * listings' underlying items, etc.) so existing bags match.
 *
 * Run the two SELECT counts first to see how many rows will be touched before running the UPDATEs.
 * Back up the shard database before applying this.
 */

-- Preview: how many existing salvage ItemType rows will change
-- SELECT COUNT(*) FROM biota_properties_int bint
-- INNER JOIN biota ON biota.id = bint.object_Id
-- WHERE bint.`type` = 1 AND bint.value = 1073741824 AND biota.weenie_Type = 76;

-- Preview: how many existing salvage TargetType rows will change
-- SELECT COUNT(*) FROM biota_properties_int bint
-- INNER JOIN biota ON biota.id = bint.object_Id
-- WHERE bint.`type` = 94 AND (bint.value & 1073741824) = 1073741824 AND biota.weenie_Type = 76;

START TRANSACTION;

/* ItemType: TinkeringMaterial (1073741824) -> Misc (128) */
UPDATE biota_properties_int bint
INNER JOIN biota ON biota.id = bint.object_Id
SET bint.value = 128
WHERE bint.`type` = 1 AND bint.value = 1073741824 AND biota.weenie_Type = 76;

/* TargetType: clear the TinkeringMaterial bit, set the Misc bit. Bitwise so it applies
   correctly regardless of which other TargetType flags (Weapon, Armor, Caster, etc.) a
   given salvage bag also carries. */
UPDATE biota_properties_int bint
INNER JOIN biota ON biota.id = bint.object_Id
SET bint.value = bint.value - 1073741824 + 128
WHERE bint.`type` = 94 AND (bint.value & 1073741824) = 1073741824 AND biota.weenie_Type = 76;

COMMIT;
