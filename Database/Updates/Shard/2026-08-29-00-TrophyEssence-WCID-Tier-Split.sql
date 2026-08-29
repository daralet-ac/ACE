/*
 * TrophyEssence crafted outputs (Alchemy/Cooking essence-enhanced potions and food) previously
 * shared one WCID across all 10 quality tiers (1-10) of a given spell/base-item combination,
 * with only the per-instance Spell (PropertyDataId 28) or BoostValue (PropertyInt 90) property
 * distinguishing quality. This caused client-side display/stacking problems when items of the
 * same WCID carried different embedded quality data.
 *
 * world-db was changed to mint one distinct WCID per quality tier (396 combinations x 10 tiers
 * = 3960 new WCIDs, replacing the 396 old shared WCIDs), and TrophyEssence.cs was updated to
 * select the tier-specific WCID at craft time. That only affects NEWLY crafted items.
 *
 * This script retroactively migrates already-persisted biotas (anywhere: player inventories,
 * houses, mail, corpses, active market listings' underlying items, etc.) from the old shared
 * per-spell WCID to the correct new per-tier WCID, based on each instance's own embedded
 * Spell (spell-bearing outputs) or BoostValue (non-spell 'Sudden' vital-boost outputs) value.
 *
 * Old and new WCID ranges are entirely disjoint (old: 1054600-1054995, new: 1060000-1063959),
 * so statement order does not matter and nothing here can collide with itself.
 *
 * Run the preview SELECT COUNT(*) queries first to see how many rows will be touched.
 * Back up the shard database before applying this.
 */

-- Preview: how many spell-bearing crafted-essence biotas (food + potion) will be migrated
-- SELECT COUNT(*) FROM biota b
-- INNER JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28
-- WHERE b.weenie_Class_Id BETWEEN 1054600 AND 1054959;

-- Preview: how many non-spell 'Sudden' vital-boost crafted-essence biotas will be migrated
-- SELECT COUNT(*) FROM biota b
-- INNER JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90
-- WHERE b.weenie_Class_Id BETWEEN 1054960 AND 1054995;

START TRANSACTION;

-- Gristly Steak of Strength: WCID 1054600 -> 1060000-1060009 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060000 WHERE b.weenie_Class_Id = 1054600 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060001 WHERE b.weenie_Class_Id = 1054600 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060002 WHERE b.weenie_Class_Id = 1054600 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060003 WHERE b.weenie_Class_Id = 1054600 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060004 WHERE b.weenie_Class_Id = 1054600 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060005 WHERE b.weenie_Class_Id = 1054600 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060006 WHERE b.weenie_Class_Id = 1054600 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060007 WHERE b.weenie_Class_Id = 1054600 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060008 WHERE b.weenie_Class_Id = 1054600 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060009 WHERE b.weenie_Class_Id = 1054600 AND s.value = 6532;

-- Gristly Steak of Endurance: WCID 1054601 -> 1060010-1060019 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060010 WHERE b.weenie_Class_Id = 1054601 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060011 WHERE b.weenie_Class_Id = 1054601 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060012 WHERE b.weenie_Class_Id = 1054601 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060013 WHERE b.weenie_Class_Id = 1054601 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060014 WHERE b.weenie_Class_Id = 1054601 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060015 WHERE b.weenie_Class_Id = 1054601 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060016 WHERE b.weenie_Class_Id = 1054601 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060017 WHERE b.weenie_Class_Id = 1054601 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060018 WHERE b.weenie_Class_Id = 1054601 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060019 WHERE b.weenie_Class_Id = 1054601 AND s.value = 6542;

-- Gristly Steak of Coordination: WCID 1054602 -> 1060020-1060029 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060020 WHERE b.weenie_Class_Id = 1054602 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060021 WHERE b.weenie_Class_Id = 1054602 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060022 WHERE b.weenie_Class_Id = 1054602 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060023 WHERE b.weenie_Class_Id = 1054602 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060024 WHERE b.weenie_Class_Id = 1054602 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060025 WHERE b.weenie_Class_Id = 1054602 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060026 WHERE b.weenie_Class_Id = 1054602 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060027 WHERE b.weenie_Class_Id = 1054602 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060028 WHERE b.weenie_Class_Id = 1054602 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060029 WHERE b.weenie_Class_Id = 1054602 AND s.value = 6552;

-- Gristly Steak of Quickness: WCID 1054603 -> 1060030-1060039 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060030 WHERE b.weenie_Class_Id = 1054603 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060031 WHERE b.weenie_Class_Id = 1054603 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060032 WHERE b.weenie_Class_Id = 1054603 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060033 WHERE b.weenie_Class_Id = 1054603 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060034 WHERE b.weenie_Class_Id = 1054603 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060035 WHERE b.weenie_Class_Id = 1054603 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060036 WHERE b.weenie_Class_Id = 1054603 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060037 WHERE b.weenie_Class_Id = 1054603 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060038 WHERE b.weenie_Class_Id = 1054603 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060039 WHERE b.weenie_Class_Id = 1054603 AND s.value = 6562;

-- Gristly Steak of Focus: WCID 1054604 -> 1060040-1060049 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060040 WHERE b.weenie_Class_Id = 1054604 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060041 WHERE b.weenie_Class_Id = 1054604 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060042 WHERE b.weenie_Class_Id = 1054604 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060043 WHERE b.weenie_Class_Id = 1054604 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060044 WHERE b.weenie_Class_Id = 1054604 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060045 WHERE b.weenie_Class_Id = 1054604 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060046 WHERE b.weenie_Class_Id = 1054604 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060047 WHERE b.weenie_Class_Id = 1054604 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060048 WHERE b.weenie_Class_Id = 1054604 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060049 WHERE b.weenie_Class_Id = 1054604 AND s.value = 6572;

-- Gristly Steak of Self: WCID 1054605 -> 1060050-1060059 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060050 WHERE b.weenie_Class_Id = 1054605 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060051 WHERE b.weenie_Class_Id = 1054605 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060052 WHERE b.weenie_Class_Id = 1054605 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060053 WHERE b.weenie_Class_Id = 1054605 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060054 WHERE b.weenie_Class_Id = 1054605 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060055 WHERE b.weenie_Class_Id = 1054605 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060056 WHERE b.weenie_Class_Id = 1054605 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060057 WHERE b.weenie_Class_Id = 1054605 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060058 WHERE b.weenie_Class_Id = 1054605 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060059 WHERE b.weenie_Class_Id = 1054605 AND s.value = 6582;

-- Gristly Steak of War Magic: WCID 1054606 -> 1060060-1060069 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060060 WHERE b.weenie_Class_Id = 1054606 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060061 WHERE b.weenie_Class_Id = 1054606 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060062 WHERE b.weenie_Class_Id = 1054606 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060063 WHERE b.weenie_Class_Id = 1054606 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060064 WHERE b.weenie_Class_Id = 1054606 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060065 WHERE b.weenie_Class_Id = 1054606 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060066 WHERE b.weenie_Class_Id = 1054606 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060067 WHERE b.weenie_Class_Id = 1054606 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060068 WHERE b.weenie_Class_Id = 1054606 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060069 WHERE b.weenie_Class_Id = 1054606 AND s.value = 6824;

-- Gristly Steak of Life Magic: WCID 1054607 -> 1060070-1060079 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060070 WHERE b.weenie_Class_Id = 1054607 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060071 WHERE b.weenie_Class_Id = 1054607 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060072 WHERE b.weenie_Class_Id = 1054607 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060073 WHERE b.weenie_Class_Id = 1054607 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060074 WHERE b.weenie_Class_Id = 1054607 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060075 WHERE b.weenie_Class_Id = 1054607 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060076 WHERE b.weenie_Class_Id = 1054607 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060077 WHERE b.weenie_Class_Id = 1054607 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060078 WHERE b.weenie_Class_Id = 1054607 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060079 WHERE b.weenie_Class_Id = 1054607 AND s.value = 6814;

-- Gristly Steak of Sprint: WCID 1054608 -> 1060080-1060089 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060080 WHERE b.weenie_Class_Id = 1054608 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060081 WHERE b.weenie_Class_Id = 1054608 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060082 WHERE b.weenie_Class_Id = 1054608 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060083 WHERE b.weenie_Class_Id = 1054608 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060084 WHERE b.weenie_Class_Id = 1054608 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060085 WHERE b.weenie_Class_Id = 1054608 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060086 WHERE b.weenie_Class_Id = 1054608 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060087 WHERE b.weenie_Class_Id = 1054608 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060088 WHERE b.weenie_Class_Id = 1054608 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060089 WHERE b.weenie_Class_Id = 1054608 AND s.value = 6804;

-- Gristly Steak of Jump: WCID 1054609 -> 1060090-1060099 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060090 WHERE b.weenie_Class_Id = 1054609 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060091 WHERE b.weenie_Class_Id = 1054609 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060092 WHERE b.weenie_Class_Id = 1054609 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060093 WHERE b.weenie_Class_Id = 1054609 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060094 WHERE b.weenie_Class_Id = 1054609 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060095 WHERE b.weenie_Class_Id = 1054609 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060096 WHERE b.weenie_Class_Id = 1054609 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060097 WHERE b.weenie_Class_Id = 1054609 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060098 WHERE b.weenie_Class_Id = 1054609 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060099 WHERE b.weenie_Class_Id = 1054609 AND s.value = 6794;

-- Gristly Steak of Thievery: WCID 1054610 -> 1060100-1060109 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060100 WHERE b.weenie_Class_Id = 1054610 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060101 WHERE b.weenie_Class_Id = 1054610 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060102 WHERE b.weenie_Class_Id = 1054610 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060103 WHERE b.weenie_Class_Id = 1054610 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060104 WHERE b.weenie_Class_Id = 1054610 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060105 WHERE b.weenie_Class_Id = 1054610 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060106 WHERE b.weenie_Class_Id = 1054610 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060107 WHERE b.weenie_Class_Id = 1054610 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060108 WHERE b.weenie_Class_Id = 1054610 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060109 WHERE b.weenie_Class_Id = 1054610 AND s.value = 6834;

-- Gristly Pepper Steak of Strength: WCID 1054611 -> 1060110-1060119 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060110 WHERE b.weenie_Class_Id = 1054611 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060111 WHERE b.weenie_Class_Id = 1054611 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060112 WHERE b.weenie_Class_Id = 1054611 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060113 WHERE b.weenie_Class_Id = 1054611 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060114 WHERE b.weenie_Class_Id = 1054611 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060115 WHERE b.weenie_Class_Id = 1054611 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060116 WHERE b.weenie_Class_Id = 1054611 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060117 WHERE b.weenie_Class_Id = 1054611 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060118 WHERE b.weenie_Class_Id = 1054611 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060119 WHERE b.weenie_Class_Id = 1054611 AND s.value = 6532;

-- Gristly Pepper Steak of Endurance: WCID 1054612 -> 1060120-1060129 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060120 WHERE b.weenie_Class_Id = 1054612 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060121 WHERE b.weenie_Class_Id = 1054612 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060122 WHERE b.weenie_Class_Id = 1054612 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060123 WHERE b.weenie_Class_Id = 1054612 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060124 WHERE b.weenie_Class_Id = 1054612 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060125 WHERE b.weenie_Class_Id = 1054612 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060126 WHERE b.weenie_Class_Id = 1054612 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060127 WHERE b.weenie_Class_Id = 1054612 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060128 WHERE b.weenie_Class_Id = 1054612 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060129 WHERE b.weenie_Class_Id = 1054612 AND s.value = 6542;

-- Gristly Pepper Steak of Coordination: WCID 1054613 -> 1060130-1060139 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060130 WHERE b.weenie_Class_Id = 1054613 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060131 WHERE b.weenie_Class_Id = 1054613 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060132 WHERE b.weenie_Class_Id = 1054613 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060133 WHERE b.weenie_Class_Id = 1054613 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060134 WHERE b.weenie_Class_Id = 1054613 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060135 WHERE b.weenie_Class_Id = 1054613 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060136 WHERE b.weenie_Class_Id = 1054613 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060137 WHERE b.weenie_Class_Id = 1054613 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060138 WHERE b.weenie_Class_Id = 1054613 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060139 WHERE b.weenie_Class_Id = 1054613 AND s.value = 6552;

-- Gristly Pepper Steak of Quickness: WCID 1054614 -> 1060140-1060149 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060140 WHERE b.weenie_Class_Id = 1054614 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060141 WHERE b.weenie_Class_Id = 1054614 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060142 WHERE b.weenie_Class_Id = 1054614 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060143 WHERE b.weenie_Class_Id = 1054614 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060144 WHERE b.weenie_Class_Id = 1054614 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060145 WHERE b.weenie_Class_Id = 1054614 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060146 WHERE b.weenie_Class_Id = 1054614 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060147 WHERE b.weenie_Class_Id = 1054614 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060148 WHERE b.weenie_Class_Id = 1054614 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060149 WHERE b.weenie_Class_Id = 1054614 AND s.value = 6562;

-- Gristly Pepper Steak of Focus: WCID 1054615 -> 1060150-1060159 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060150 WHERE b.weenie_Class_Id = 1054615 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060151 WHERE b.weenie_Class_Id = 1054615 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060152 WHERE b.weenie_Class_Id = 1054615 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060153 WHERE b.weenie_Class_Id = 1054615 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060154 WHERE b.weenie_Class_Id = 1054615 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060155 WHERE b.weenie_Class_Id = 1054615 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060156 WHERE b.weenie_Class_Id = 1054615 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060157 WHERE b.weenie_Class_Id = 1054615 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060158 WHERE b.weenie_Class_Id = 1054615 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060159 WHERE b.weenie_Class_Id = 1054615 AND s.value = 6572;

-- Gristly Pepper Steak of Self: WCID 1054616 -> 1060160-1060169 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060160 WHERE b.weenie_Class_Id = 1054616 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060161 WHERE b.weenie_Class_Id = 1054616 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060162 WHERE b.weenie_Class_Id = 1054616 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060163 WHERE b.weenie_Class_Id = 1054616 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060164 WHERE b.weenie_Class_Id = 1054616 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060165 WHERE b.weenie_Class_Id = 1054616 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060166 WHERE b.weenie_Class_Id = 1054616 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060167 WHERE b.weenie_Class_Id = 1054616 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060168 WHERE b.weenie_Class_Id = 1054616 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060169 WHERE b.weenie_Class_Id = 1054616 AND s.value = 6582;

-- Gristly Pepper Steak of War Magic: WCID 1054617 -> 1060170-1060179 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060170 WHERE b.weenie_Class_Id = 1054617 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060171 WHERE b.weenie_Class_Id = 1054617 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060172 WHERE b.weenie_Class_Id = 1054617 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060173 WHERE b.weenie_Class_Id = 1054617 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060174 WHERE b.weenie_Class_Id = 1054617 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060175 WHERE b.weenie_Class_Id = 1054617 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060176 WHERE b.weenie_Class_Id = 1054617 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060177 WHERE b.weenie_Class_Id = 1054617 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060178 WHERE b.weenie_Class_Id = 1054617 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060179 WHERE b.weenie_Class_Id = 1054617 AND s.value = 6824;

-- Gristly Pepper Steak of Life Magic: WCID 1054618 -> 1060180-1060189 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060180 WHERE b.weenie_Class_Id = 1054618 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060181 WHERE b.weenie_Class_Id = 1054618 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060182 WHERE b.weenie_Class_Id = 1054618 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060183 WHERE b.weenie_Class_Id = 1054618 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060184 WHERE b.weenie_Class_Id = 1054618 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060185 WHERE b.weenie_Class_Id = 1054618 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060186 WHERE b.weenie_Class_Id = 1054618 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060187 WHERE b.weenie_Class_Id = 1054618 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060188 WHERE b.weenie_Class_Id = 1054618 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060189 WHERE b.weenie_Class_Id = 1054618 AND s.value = 6814;

-- Gristly Pepper Steak of Sprint: WCID 1054619 -> 1060190-1060199 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060190 WHERE b.weenie_Class_Id = 1054619 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060191 WHERE b.weenie_Class_Id = 1054619 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060192 WHERE b.weenie_Class_Id = 1054619 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060193 WHERE b.weenie_Class_Id = 1054619 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060194 WHERE b.weenie_Class_Id = 1054619 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060195 WHERE b.weenie_Class_Id = 1054619 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060196 WHERE b.weenie_Class_Id = 1054619 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060197 WHERE b.weenie_Class_Id = 1054619 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060198 WHERE b.weenie_Class_Id = 1054619 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060199 WHERE b.weenie_Class_Id = 1054619 AND s.value = 6804;

-- Gristly Pepper Steak of Jump: WCID 1054620 -> 1060200-1060209 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060200 WHERE b.weenie_Class_Id = 1054620 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060201 WHERE b.weenie_Class_Id = 1054620 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060202 WHERE b.weenie_Class_Id = 1054620 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060203 WHERE b.weenie_Class_Id = 1054620 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060204 WHERE b.weenie_Class_Id = 1054620 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060205 WHERE b.weenie_Class_Id = 1054620 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060206 WHERE b.weenie_Class_Id = 1054620 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060207 WHERE b.weenie_Class_Id = 1054620 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060208 WHERE b.weenie_Class_Id = 1054620 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060209 WHERE b.weenie_Class_Id = 1054620 AND s.value = 6794;

-- Gristly Pepper Steak of Thievery: WCID 1054621 -> 1060210-1060219 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060210 WHERE b.weenie_Class_Id = 1054621 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060211 WHERE b.weenie_Class_Id = 1054621 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060212 WHERE b.weenie_Class_Id = 1054621 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060213 WHERE b.weenie_Class_Id = 1054621 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060214 WHERE b.weenie_Class_Id = 1054621 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060215 WHERE b.weenie_Class_Id = 1054621 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060216 WHERE b.weenie_Class_Id = 1054621 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060217 WHERE b.weenie_Class_Id = 1054621 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060218 WHERE b.weenie_Class_Id = 1054621 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060219 WHERE b.weenie_Class_Id = 1054621 AND s.value = 6834;

-- Gristly Brined Steak of Strength: WCID 1054622 -> 1060220-1060229 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060220 WHERE b.weenie_Class_Id = 1054622 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060221 WHERE b.weenie_Class_Id = 1054622 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060222 WHERE b.weenie_Class_Id = 1054622 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060223 WHERE b.weenie_Class_Id = 1054622 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060224 WHERE b.weenie_Class_Id = 1054622 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060225 WHERE b.weenie_Class_Id = 1054622 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060226 WHERE b.weenie_Class_Id = 1054622 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060227 WHERE b.weenie_Class_Id = 1054622 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060228 WHERE b.weenie_Class_Id = 1054622 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060229 WHERE b.weenie_Class_Id = 1054622 AND s.value = 6532;

-- Gristly Brined Steak of Endurance: WCID 1054623 -> 1060230-1060239 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060230 WHERE b.weenie_Class_Id = 1054623 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060231 WHERE b.weenie_Class_Id = 1054623 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060232 WHERE b.weenie_Class_Id = 1054623 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060233 WHERE b.weenie_Class_Id = 1054623 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060234 WHERE b.weenie_Class_Id = 1054623 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060235 WHERE b.weenie_Class_Id = 1054623 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060236 WHERE b.weenie_Class_Id = 1054623 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060237 WHERE b.weenie_Class_Id = 1054623 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060238 WHERE b.weenie_Class_Id = 1054623 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060239 WHERE b.weenie_Class_Id = 1054623 AND s.value = 6542;

-- Gristly Brined Steak of Coordination: WCID 1054624 -> 1060240-1060249 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060240 WHERE b.weenie_Class_Id = 1054624 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060241 WHERE b.weenie_Class_Id = 1054624 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060242 WHERE b.weenie_Class_Id = 1054624 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060243 WHERE b.weenie_Class_Id = 1054624 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060244 WHERE b.weenie_Class_Id = 1054624 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060245 WHERE b.weenie_Class_Id = 1054624 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060246 WHERE b.weenie_Class_Id = 1054624 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060247 WHERE b.weenie_Class_Id = 1054624 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060248 WHERE b.weenie_Class_Id = 1054624 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060249 WHERE b.weenie_Class_Id = 1054624 AND s.value = 6552;

-- Gristly Brined Steak of Quickness: WCID 1054625 -> 1060250-1060259 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060250 WHERE b.weenie_Class_Id = 1054625 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060251 WHERE b.weenie_Class_Id = 1054625 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060252 WHERE b.weenie_Class_Id = 1054625 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060253 WHERE b.weenie_Class_Id = 1054625 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060254 WHERE b.weenie_Class_Id = 1054625 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060255 WHERE b.weenie_Class_Id = 1054625 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060256 WHERE b.weenie_Class_Id = 1054625 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060257 WHERE b.weenie_Class_Id = 1054625 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060258 WHERE b.weenie_Class_Id = 1054625 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060259 WHERE b.weenie_Class_Id = 1054625 AND s.value = 6562;

-- Gristly Brined Steak of Focus: WCID 1054626 -> 1060260-1060269 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060260 WHERE b.weenie_Class_Id = 1054626 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060261 WHERE b.weenie_Class_Id = 1054626 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060262 WHERE b.weenie_Class_Id = 1054626 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060263 WHERE b.weenie_Class_Id = 1054626 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060264 WHERE b.weenie_Class_Id = 1054626 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060265 WHERE b.weenie_Class_Id = 1054626 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060266 WHERE b.weenie_Class_Id = 1054626 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060267 WHERE b.weenie_Class_Id = 1054626 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060268 WHERE b.weenie_Class_Id = 1054626 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060269 WHERE b.weenie_Class_Id = 1054626 AND s.value = 6572;

-- Gristly Brined Steak of Self: WCID 1054627 -> 1060270-1060279 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060270 WHERE b.weenie_Class_Id = 1054627 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060271 WHERE b.weenie_Class_Id = 1054627 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060272 WHERE b.weenie_Class_Id = 1054627 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060273 WHERE b.weenie_Class_Id = 1054627 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060274 WHERE b.weenie_Class_Id = 1054627 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060275 WHERE b.weenie_Class_Id = 1054627 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060276 WHERE b.weenie_Class_Id = 1054627 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060277 WHERE b.weenie_Class_Id = 1054627 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060278 WHERE b.weenie_Class_Id = 1054627 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060279 WHERE b.weenie_Class_Id = 1054627 AND s.value = 6582;

-- Gristly Brined Steak of War Magic: WCID 1054628 -> 1060280-1060289 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060280 WHERE b.weenie_Class_Id = 1054628 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060281 WHERE b.weenie_Class_Id = 1054628 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060282 WHERE b.weenie_Class_Id = 1054628 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060283 WHERE b.weenie_Class_Id = 1054628 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060284 WHERE b.weenie_Class_Id = 1054628 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060285 WHERE b.weenie_Class_Id = 1054628 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060286 WHERE b.weenie_Class_Id = 1054628 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060287 WHERE b.weenie_Class_Id = 1054628 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060288 WHERE b.weenie_Class_Id = 1054628 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060289 WHERE b.weenie_Class_Id = 1054628 AND s.value = 6824;

-- Gristly Brined Steak of Life Magic: WCID 1054629 -> 1060290-1060299 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060290 WHERE b.weenie_Class_Id = 1054629 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060291 WHERE b.weenie_Class_Id = 1054629 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060292 WHERE b.weenie_Class_Id = 1054629 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060293 WHERE b.weenie_Class_Id = 1054629 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060294 WHERE b.weenie_Class_Id = 1054629 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060295 WHERE b.weenie_Class_Id = 1054629 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060296 WHERE b.weenie_Class_Id = 1054629 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060297 WHERE b.weenie_Class_Id = 1054629 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060298 WHERE b.weenie_Class_Id = 1054629 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060299 WHERE b.weenie_Class_Id = 1054629 AND s.value = 6814;

-- Gristly Brined Steak of Sprint: WCID 1054630 -> 1060300-1060309 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060300 WHERE b.weenie_Class_Id = 1054630 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060301 WHERE b.weenie_Class_Id = 1054630 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060302 WHERE b.weenie_Class_Id = 1054630 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060303 WHERE b.weenie_Class_Id = 1054630 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060304 WHERE b.weenie_Class_Id = 1054630 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060305 WHERE b.weenie_Class_Id = 1054630 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060306 WHERE b.weenie_Class_Id = 1054630 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060307 WHERE b.weenie_Class_Id = 1054630 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060308 WHERE b.weenie_Class_Id = 1054630 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060309 WHERE b.weenie_Class_Id = 1054630 AND s.value = 6804;

-- Gristly Brined Steak of Jump: WCID 1054631 -> 1060310-1060319 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060310 WHERE b.weenie_Class_Id = 1054631 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060311 WHERE b.weenie_Class_Id = 1054631 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060312 WHERE b.weenie_Class_Id = 1054631 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060313 WHERE b.weenie_Class_Id = 1054631 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060314 WHERE b.weenie_Class_Id = 1054631 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060315 WHERE b.weenie_Class_Id = 1054631 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060316 WHERE b.weenie_Class_Id = 1054631 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060317 WHERE b.weenie_Class_Id = 1054631 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060318 WHERE b.weenie_Class_Id = 1054631 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060319 WHERE b.weenie_Class_Id = 1054631 AND s.value = 6794;

-- Gristly Brined Steak of Thievery: WCID 1054632 -> 1060320-1060329 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060320 WHERE b.weenie_Class_Id = 1054632 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060321 WHERE b.weenie_Class_Id = 1054632 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060322 WHERE b.weenie_Class_Id = 1054632 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060323 WHERE b.weenie_Class_Id = 1054632 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060324 WHERE b.weenie_Class_Id = 1054632 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060325 WHERE b.weenie_Class_Id = 1054632 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060326 WHERE b.weenie_Class_Id = 1054632 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060327 WHERE b.weenie_Class_Id = 1054632 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060328 WHERE b.weenie_Class_Id = 1054632 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060329 WHERE b.weenie_Class_Id = 1054632 AND s.value = 6834;

-- Steak of Strength: WCID 1054633 -> 1060330-1060339 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060330 WHERE b.weenie_Class_Id = 1054633 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060331 WHERE b.weenie_Class_Id = 1054633 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060332 WHERE b.weenie_Class_Id = 1054633 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060333 WHERE b.weenie_Class_Id = 1054633 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060334 WHERE b.weenie_Class_Id = 1054633 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060335 WHERE b.weenie_Class_Id = 1054633 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060336 WHERE b.weenie_Class_Id = 1054633 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060337 WHERE b.weenie_Class_Id = 1054633 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060338 WHERE b.weenie_Class_Id = 1054633 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060339 WHERE b.weenie_Class_Id = 1054633 AND s.value = 6532;

-- Steak of Endurance: WCID 1054634 -> 1060340-1060349 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060340 WHERE b.weenie_Class_Id = 1054634 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060341 WHERE b.weenie_Class_Id = 1054634 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060342 WHERE b.weenie_Class_Id = 1054634 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060343 WHERE b.weenie_Class_Id = 1054634 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060344 WHERE b.weenie_Class_Id = 1054634 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060345 WHERE b.weenie_Class_Id = 1054634 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060346 WHERE b.weenie_Class_Id = 1054634 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060347 WHERE b.weenie_Class_Id = 1054634 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060348 WHERE b.weenie_Class_Id = 1054634 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060349 WHERE b.weenie_Class_Id = 1054634 AND s.value = 6542;

-- Steak of Coordination: WCID 1054635 -> 1060350-1060359 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060350 WHERE b.weenie_Class_Id = 1054635 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060351 WHERE b.weenie_Class_Id = 1054635 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060352 WHERE b.weenie_Class_Id = 1054635 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060353 WHERE b.weenie_Class_Id = 1054635 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060354 WHERE b.weenie_Class_Id = 1054635 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060355 WHERE b.weenie_Class_Id = 1054635 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060356 WHERE b.weenie_Class_Id = 1054635 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060357 WHERE b.weenie_Class_Id = 1054635 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060358 WHERE b.weenie_Class_Id = 1054635 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060359 WHERE b.weenie_Class_Id = 1054635 AND s.value = 6552;

-- Steak of Quickness: WCID 1054636 -> 1060360-1060369 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060360 WHERE b.weenie_Class_Id = 1054636 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060361 WHERE b.weenie_Class_Id = 1054636 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060362 WHERE b.weenie_Class_Id = 1054636 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060363 WHERE b.weenie_Class_Id = 1054636 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060364 WHERE b.weenie_Class_Id = 1054636 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060365 WHERE b.weenie_Class_Id = 1054636 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060366 WHERE b.weenie_Class_Id = 1054636 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060367 WHERE b.weenie_Class_Id = 1054636 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060368 WHERE b.weenie_Class_Id = 1054636 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060369 WHERE b.weenie_Class_Id = 1054636 AND s.value = 6562;

-- Steak of Focus: WCID 1054637 -> 1060370-1060379 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060370 WHERE b.weenie_Class_Id = 1054637 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060371 WHERE b.weenie_Class_Id = 1054637 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060372 WHERE b.weenie_Class_Id = 1054637 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060373 WHERE b.weenie_Class_Id = 1054637 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060374 WHERE b.weenie_Class_Id = 1054637 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060375 WHERE b.weenie_Class_Id = 1054637 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060376 WHERE b.weenie_Class_Id = 1054637 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060377 WHERE b.weenie_Class_Id = 1054637 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060378 WHERE b.weenie_Class_Id = 1054637 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060379 WHERE b.weenie_Class_Id = 1054637 AND s.value = 6572;

-- Steak of Self: WCID 1054638 -> 1060380-1060389 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060380 WHERE b.weenie_Class_Id = 1054638 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060381 WHERE b.weenie_Class_Id = 1054638 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060382 WHERE b.weenie_Class_Id = 1054638 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060383 WHERE b.weenie_Class_Id = 1054638 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060384 WHERE b.weenie_Class_Id = 1054638 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060385 WHERE b.weenie_Class_Id = 1054638 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060386 WHERE b.weenie_Class_Id = 1054638 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060387 WHERE b.weenie_Class_Id = 1054638 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060388 WHERE b.weenie_Class_Id = 1054638 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060389 WHERE b.weenie_Class_Id = 1054638 AND s.value = 6582;

-- Steak of War Magic: WCID 1054639 -> 1060390-1060399 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060390 WHERE b.weenie_Class_Id = 1054639 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060391 WHERE b.weenie_Class_Id = 1054639 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060392 WHERE b.weenie_Class_Id = 1054639 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060393 WHERE b.weenie_Class_Id = 1054639 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060394 WHERE b.weenie_Class_Id = 1054639 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060395 WHERE b.weenie_Class_Id = 1054639 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060396 WHERE b.weenie_Class_Id = 1054639 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060397 WHERE b.weenie_Class_Id = 1054639 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060398 WHERE b.weenie_Class_Id = 1054639 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060399 WHERE b.weenie_Class_Id = 1054639 AND s.value = 6824;

-- Steak of Life Magic: WCID 1054640 -> 1060400-1060409 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060400 WHERE b.weenie_Class_Id = 1054640 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060401 WHERE b.weenie_Class_Id = 1054640 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060402 WHERE b.weenie_Class_Id = 1054640 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060403 WHERE b.weenie_Class_Id = 1054640 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060404 WHERE b.weenie_Class_Id = 1054640 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060405 WHERE b.weenie_Class_Id = 1054640 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060406 WHERE b.weenie_Class_Id = 1054640 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060407 WHERE b.weenie_Class_Id = 1054640 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060408 WHERE b.weenie_Class_Id = 1054640 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060409 WHERE b.weenie_Class_Id = 1054640 AND s.value = 6814;

-- Steak of Sprint: WCID 1054641 -> 1060410-1060419 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060410 WHERE b.weenie_Class_Id = 1054641 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060411 WHERE b.weenie_Class_Id = 1054641 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060412 WHERE b.weenie_Class_Id = 1054641 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060413 WHERE b.weenie_Class_Id = 1054641 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060414 WHERE b.weenie_Class_Id = 1054641 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060415 WHERE b.weenie_Class_Id = 1054641 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060416 WHERE b.weenie_Class_Id = 1054641 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060417 WHERE b.weenie_Class_Id = 1054641 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060418 WHERE b.weenie_Class_Id = 1054641 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060419 WHERE b.weenie_Class_Id = 1054641 AND s.value = 6804;

-- Steak of Jump: WCID 1054642 -> 1060420-1060429 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060420 WHERE b.weenie_Class_Id = 1054642 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060421 WHERE b.weenie_Class_Id = 1054642 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060422 WHERE b.weenie_Class_Id = 1054642 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060423 WHERE b.weenie_Class_Id = 1054642 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060424 WHERE b.weenie_Class_Id = 1054642 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060425 WHERE b.weenie_Class_Id = 1054642 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060426 WHERE b.weenie_Class_Id = 1054642 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060427 WHERE b.weenie_Class_Id = 1054642 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060428 WHERE b.weenie_Class_Id = 1054642 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060429 WHERE b.weenie_Class_Id = 1054642 AND s.value = 6794;

-- Steak of Thievery: WCID 1054643 -> 1060430-1060439 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060430 WHERE b.weenie_Class_Id = 1054643 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060431 WHERE b.weenie_Class_Id = 1054643 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060432 WHERE b.weenie_Class_Id = 1054643 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060433 WHERE b.weenie_Class_Id = 1054643 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060434 WHERE b.weenie_Class_Id = 1054643 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060435 WHERE b.weenie_Class_Id = 1054643 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060436 WHERE b.weenie_Class_Id = 1054643 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060437 WHERE b.weenie_Class_Id = 1054643 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060438 WHERE b.weenie_Class_Id = 1054643 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060439 WHERE b.weenie_Class_Id = 1054643 AND s.value = 6834;

-- Pepper Steak of Strength: WCID 1054644 -> 1060440-1060449 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060440 WHERE b.weenie_Class_Id = 1054644 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060441 WHERE b.weenie_Class_Id = 1054644 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060442 WHERE b.weenie_Class_Id = 1054644 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060443 WHERE b.weenie_Class_Id = 1054644 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060444 WHERE b.weenie_Class_Id = 1054644 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060445 WHERE b.weenie_Class_Id = 1054644 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060446 WHERE b.weenie_Class_Id = 1054644 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060447 WHERE b.weenie_Class_Id = 1054644 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060448 WHERE b.weenie_Class_Id = 1054644 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060449 WHERE b.weenie_Class_Id = 1054644 AND s.value = 6532;

-- Pepper Steak of Endurance: WCID 1054645 -> 1060450-1060459 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060450 WHERE b.weenie_Class_Id = 1054645 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060451 WHERE b.weenie_Class_Id = 1054645 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060452 WHERE b.weenie_Class_Id = 1054645 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060453 WHERE b.weenie_Class_Id = 1054645 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060454 WHERE b.weenie_Class_Id = 1054645 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060455 WHERE b.weenie_Class_Id = 1054645 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060456 WHERE b.weenie_Class_Id = 1054645 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060457 WHERE b.weenie_Class_Id = 1054645 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060458 WHERE b.weenie_Class_Id = 1054645 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060459 WHERE b.weenie_Class_Id = 1054645 AND s.value = 6542;

-- Pepper Steak of Coordination: WCID 1054646 -> 1060460-1060469 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060460 WHERE b.weenie_Class_Id = 1054646 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060461 WHERE b.weenie_Class_Id = 1054646 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060462 WHERE b.weenie_Class_Id = 1054646 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060463 WHERE b.weenie_Class_Id = 1054646 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060464 WHERE b.weenie_Class_Id = 1054646 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060465 WHERE b.weenie_Class_Id = 1054646 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060466 WHERE b.weenie_Class_Id = 1054646 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060467 WHERE b.weenie_Class_Id = 1054646 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060468 WHERE b.weenie_Class_Id = 1054646 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060469 WHERE b.weenie_Class_Id = 1054646 AND s.value = 6552;

-- Pepper Steak of Quickness: WCID 1054647 -> 1060470-1060479 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060470 WHERE b.weenie_Class_Id = 1054647 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060471 WHERE b.weenie_Class_Id = 1054647 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060472 WHERE b.weenie_Class_Id = 1054647 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060473 WHERE b.weenie_Class_Id = 1054647 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060474 WHERE b.weenie_Class_Id = 1054647 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060475 WHERE b.weenie_Class_Id = 1054647 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060476 WHERE b.weenie_Class_Id = 1054647 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060477 WHERE b.weenie_Class_Id = 1054647 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060478 WHERE b.weenie_Class_Id = 1054647 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060479 WHERE b.weenie_Class_Id = 1054647 AND s.value = 6562;

-- Pepper Steak of Focus: WCID 1054648 -> 1060480-1060489 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060480 WHERE b.weenie_Class_Id = 1054648 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060481 WHERE b.weenie_Class_Id = 1054648 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060482 WHERE b.weenie_Class_Id = 1054648 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060483 WHERE b.weenie_Class_Id = 1054648 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060484 WHERE b.weenie_Class_Id = 1054648 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060485 WHERE b.weenie_Class_Id = 1054648 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060486 WHERE b.weenie_Class_Id = 1054648 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060487 WHERE b.weenie_Class_Id = 1054648 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060488 WHERE b.weenie_Class_Id = 1054648 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060489 WHERE b.weenie_Class_Id = 1054648 AND s.value = 6572;

-- Pepper Steak of Self: WCID 1054649 -> 1060490-1060499 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060490 WHERE b.weenie_Class_Id = 1054649 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060491 WHERE b.weenie_Class_Id = 1054649 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060492 WHERE b.weenie_Class_Id = 1054649 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060493 WHERE b.weenie_Class_Id = 1054649 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060494 WHERE b.weenie_Class_Id = 1054649 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060495 WHERE b.weenie_Class_Id = 1054649 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060496 WHERE b.weenie_Class_Id = 1054649 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060497 WHERE b.weenie_Class_Id = 1054649 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060498 WHERE b.weenie_Class_Id = 1054649 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060499 WHERE b.weenie_Class_Id = 1054649 AND s.value = 6582;

-- Pepper Steak of War Magic: WCID 1054650 -> 1060500-1060509 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060500 WHERE b.weenie_Class_Id = 1054650 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060501 WHERE b.weenie_Class_Id = 1054650 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060502 WHERE b.weenie_Class_Id = 1054650 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060503 WHERE b.weenie_Class_Id = 1054650 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060504 WHERE b.weenie_Class_Id = 1054650 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060505 WHERE b.weenie_Class_Id = 1054650 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060506 WHERE b.weenie_Class_Id = 1054650 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060507 WHERE b.weenie_Class_Id = 1054650 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060508 WHERE b.weenie_Class_Id = 1054650 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060509 WHERE b.weenie_Class_Id = 1054650 AND s.value = 6824;

-- Pepper Steak of Life Magic: WCID 1054651 -> 1060510-1060519 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060510 WHERE b.weenie_Class_Id = 1054651 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060511 WHERE b.weenie_Class_Id = 1054651 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060512 WHERE b.weenie_Class_Id = 1054651 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060513 WHERE b.weenie_Class_Id = 1054651 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060514 WHERE b.weenie_Class_Id = 1054651 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060515 WHERE b.weenie_Class_Id = 1054651 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060516 WHERE b.weenie_Class_Id = 1054651 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060517 WHERE b.weenie_Class_Id = 1054651 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060518 WHERE b.weenie_Class_Id = 1054651 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060519 WHERE b.weenie_Class_Id = 1054651 AND s.value = 6814;

-- Pepper Steak of Sprint: WCID 1054652 -> 1060520-1060529 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060520 WHERE b.weenie_Class_Id = 1054652 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060521 WHERE b.weenie_Class_Id = 1054652 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060522 WHERE b.weenie_Class_Id = 1054652 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060523 WHERE b.weenie_Class_Id = 1054652 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060524 WHERE b.weenie_Class_Id = 1054652 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060525 WHERE b.weenie_Class_Id = 1054652 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060526 WHERE b.weenie_Class_Id = 1054652 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060527 WHERE b.weenie_Class_Id = 1054652 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060528 WHERE b.weenie_Class_Id = 1054652 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060529 WHERE b.weenie_Class_Id = 1054652 AND s.value = 6804;

-- Pepper Steak of Jump: WCID 1054653 -> 1060530-1060539 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060530 WHERE b.weenie_Class_Id = 1054653 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060531 WHERE b.weenie_Class_Id = 1054653 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060532 WHERE b.weenie_Class_Id = 1054653 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060533 WHERE b.weenie_Class_Id = 1054653 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060534 WHERE b.weenie_Class_Id = 1054653 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060535 WHERE b.weenie_Class_Id = 1054653 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060536 WHERE b.weenie_Class_Id = 1054653 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060537 WHERE b.weenie_Class_Id = 1054653 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060538 WHERE b.weenie_Class_Id = 1054653 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060539 WHERE b.weenie_Class_Id = 1054653 AND s.value = 6794;

-- Pepper Steak of Thievery: WCID 1054654 -> 1060540-1060549 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060540 WHERE b.weenie_Class_Id = 1054654 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060541 WHERE b.weenie_Class_Id = 1054654 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060542 WHERE b.weenie_Class_Id = 1054654 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060543 WHERE b.weenie_Class_Id = 1054654 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060544 WHERE b.weenie_Class_Id = 1054654 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060545 WHERE b.weenie_Class_Id = 1054654 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060546 WHERE b.weenie_Class_Id = 1054654 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060547 WHERE b.weenie_Class_Id = 1054654 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060548 WHERE b.weenie_Class_Id = 1054654 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060549 WHERE b.weenie_Class_Id = 1054654 AND s.value = 6834;

-- Brined Steak of Strength: WCID 1054655 -> 1060550-1060559 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060550 WHERE b.weenie_Class_Id = 1054655 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060551 WHERE b.weenie_Class_Id = 1054655 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060552 WHERE b.weenie_Class_Id = 1054655 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060553 WHERE b.weenie_Class_Id = 1054655 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060554 WHERE b.weenie_Class_Id = 1054655 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060555 WHERE b.weenie_Class_Id = 1054655 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060556 WHERE b.weenie_Class_Id = 1054655 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060557 WHERE b.weenie_Class_Id = 1054655 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060558 WHERE b.weenie_Class_Id = 1054655 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060559 WHERE b.weenie_Class_Id = 1054655 AND s.value = 6532;

-- Brined Steak of Endurance: WCID 1054656 -> 1060560-1060569 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060560 WHERE b.weenie_Class_Id = 1054656 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060561 WHERE b.weenie_Class_Id = 1054656 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060562 WHERE b.weenie_Class_Id = 1054656 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060563 WHERE b.weenie_Class_Id = 1054656 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060564 WHERE b.weenie_Class_Id = 1054656 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060565 WHERE b.weenie_Class_Id = 1054656 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060566 WHERE b.weenie_Class_Id = 1054656 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060567 WHERE b.weenie_Class_Id = 1054656 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060568 WHERE b.weenie_Class_Id = 1054656 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060569 WHERE b.weenie_Class_Id = 1054656 AND s.value = 6542;

-- Brined Steak of Coordination: WCID 1054657 -> 1060570-1060579 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060570 WHERE b.weenie_Class_Id = 1054657 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060571 WHERE b.weenie_Class_Id = 1054657 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060572 WHERE b.weenie_Class_Id = 1054657 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060573 WHERE b.weenie_Class_Id = 1054657 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060574 WHERE b.weenie_Class_Id = 1054657 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060575 WHERE b.weenie_Class_Id = 1054657 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060576 WHERE b.weenie_Class_Id = 1054657 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060577 WHERE b.weenie_Class_Id = 1054657 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060578 WHERE b.weenie_Class_Id = 1054657 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060579 WHERE b.weenie_Class_Id = 1054657 AND s.value = 6552;

-- Brined Steak of Quickness: WCID 1054658 -> 1060580-1060589 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060580 WHERE b.weenie_Class_Id = 1054658 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060581 WHERE b.weenie_Class_Id = 1054658 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060582 WHERE b.weenie_Class_Id = 1054658 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060583 WHERE b.weenie_Class_Id = 1054658 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060584 WHERE b.weenie_Class_Id = 1054658 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060585 WHERE b.weenie_Class_Id = 1054658 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060586 WHERE b.weenie_Class_Id = 1054658 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060587 WHERE b.weenie_Class_Id = 1054658 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060588 WHERE b.weenie_Class_Id = 1054658 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060589 WHERE b.weenie_Class_Id = 1054658 AND s.value = 6562;

-- Brined Steak of Focus: WCID 1054659 -> 1060590-1060599 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060590 WHERE b.weenie_Class_Id = 1054659 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060591 WHERE b.weenie_Class_Id = 1054659 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060592 WHERE b.weenie_Class_Id = 1054659 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060593 WHERE b.weenie_Class_Id = 1054659 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060594 WHERE b.weenie_Class_Id = 1054659 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060595 WHERE b.weenie_Class_Id = 1054659 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060596 WHERE b.weenie_Class_Id = 1054659 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060597 WHERE b.weenie_Class_Id = 1054659 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060598 WHERE b.weenie_Class_Id = 1054659 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060599 WHERE b.weenie_Class_Id = 1054659 AND s.value = 6572;

-- Brined Steak of Self: WCID 1054660 -> 1060600-1060609 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060600 WHERE b.weenie_Class_Id = 1054660 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060601 WHERE b.weenie_Class_Id = 1054660 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060602 WHERE b.weenie_Class_Id = 1054660 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060603 WHERE b.weenie_Class_Id = 1054660 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060604 WHERE b.weenie_Class_Id = 1054660 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060605 WHERE b.weenie_Class_Id = 1054660 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060606 WHERE b.weenie_Class_Id = 1054660 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060607 WHERE b.weenie_Class_Id = 1054660 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060608 WHERE b.weenie_Class_Id = 1054660 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060609 WHERE b.weenie_Class_Id = 1054660 AND s.value = 6582;

-- Brined Steak of War Magic: WCID 1054661 -> 1060610-1060619 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060610 WHERE b.weenie_Class_Id = 1054661 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060611 WHERE b.weenie_Class_Id = 1054661 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060612 WHERE b.weenie_Class_Id = 1054661 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060613 WHERE b.weenie_Class_Id = 1054661 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060614 WHERE b.weenie_Class_Id = 1054661 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060615 WHERE b.weenie_Class_Id = 1054661 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060616 WHERE b.weenie_Class_Id = 1054661 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060617 WHERE b.weenie_Class_Id = 1054661 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060618 WHERE b.weenie_Class_Id = 1054661 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060619 WHERE b.weenie_Class_Id = 1054661 AND s.value = 6824;

-- Brined Steak of Life Magic: WCID 1054662 -> 1060620-1060629 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060620 WHERE b.weenie_Class_Id = 1054662 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060621 WHERE b.weenie_Class_Id = 1054662 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060622 WHERE b.weenie_Class_Id = 1054662 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060623 WHERE b.weenie_Class_Id = 1054662 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060624 WHERE b.weenie_Class_Id = 1054662 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060625 WHERE b.weenie_Class_Id = 1054662 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060626 WHERE b.weenie_Class_Id = 1054662 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060627 WHERE b.weenie_Class_Id = 1054662 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060628 WHERE b.weenie_Class_Id = 1054662 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060629 WHERE b.weenie_Class_Id = 1054662 AND s.value = 6814;

-- Brined Steak of Sprint: WCID 1054663 -> 1060630-1060639 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060630 WHERE b.weenie_Class_Id = 1054663 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060631 WHERE b.weenie_Class_Id = 1054663 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060632 WHERE b.weenie_Class_Id = 1054663 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060633 WHERE b.weenie_Class_Id = 1054663 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060634 WHERE b.weenie_Class_Id = 1054663 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060635 WHERE b.weenie_Class_Id = 1054663 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060636 WHERE b.weenie_Class_Id = 1054663 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060637 WHERE b.weenie_Class_Id = 1054663 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060638 WHERE b.weenie_Class_Id = 1054663 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060639 WHERE b.weenie_Class_Id = 1054663 AND s.value = 6804;

-- Brined Steak of Jump: WCID 1054664 -> 1060640-1060649 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060640 WHERE b.weenie_Class_Id = 1054664 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060641 WHERE b.weenie_Class_Id = 1054664 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060642 WHERE b.weenie_Class_Id = 1054664 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060643 WHERE b.weenie_Class_Id = 1054664 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060644 WHERE b.weenie_Class_Id = 1054664 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060645 WHERE b.weenie_Class_Id = 1054664 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060646 WHERE b.weenie_Class_Id = 1054664 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060647 WHERE b.weenie_Class_Id = 1054664 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060648 WHERE b.weenie_Class_Id = 1054664 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060649 WHERE b.weenie_Class_Id = 1054664 AND s.value = 6794;

-- Brined Steak of Thievery: WCID 1054665 -> 1060650-1060659 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060650 WHERE b.weenie_Class_Id = 1054665 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060651 WHERE b.weenie_Class_Id = 1054665 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060652 WHERE b.weenie_Class_Id = 1054665 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060653 WHERE b.weenie_Class_Id = 1054665 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060654 WHERE b.weenie_Class_Id = 1054665 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060655 WHERE b.weenie_Class_Id = 1054665 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060656 WHERE b.weenie_Class_Id = 1054665 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060657 WHERE b.weenie_Class_Id = 1054665 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060658 WHERE b.weenie_Class_Id = 1054665 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060659 WHERE b.weenie_Class_Id = 1054665 AND s.value = 6834;

-- Tender Steak of Strength: WCID 1054666 -> 1060660-1060669 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060660 WHERE b.weenie_Class_Id = 1054666 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060661 WHERE b.weenie_Class_Id = 1054666 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060662 WHERE b.weenie_Class_Id = 1054666 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060663 WHERE b.weenie_Class_Id = 1054666 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060664 WHERE b.weenie_Class_Id = 1054666 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060665 WHERE b.weenie_Class_Id = 1054666 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060666 WHERE b.weenie_Class_Id = 1054666 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060667 WHERE b.weenie_Class_Id = 1054666 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060668 WHERE b.weenie_Class_Id = 1054666 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060669 WHERE b.weenie_Class_Id = 1054666 AND s.value = 6532;

-- Tender Steak of Endurance: WCID 1054667 -> 1060670-1060679 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060670 WHERE b.weenie_Class_Id = 1054667 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060671 WHERE b.weenie_Class_Id = 1054667 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060672 WHERE b.weenie_Class_Id = 1054667 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060673 WHERE b.weenie_Class_Id = 1054667 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060674 WHERE b.weenie_Class_Id = 1054667 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060675 WHERE b.weenie_Class_Id = 1054667 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060676 WHERE b.weenie_Class_Id = 1054667 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060677 WHERE b.weenie_Class_Id = 1054667 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060678 WHERE b.weenie_Class_Id = 1054667 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060679 WHERE b.weenie_Class_Id = 1054667 AND s.value = 6542;

-- Tender Steak of Coordination: WCID 1054668 -> 1060680-1060689 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060680 WHERE b.weenie_Class_Id = 1054668 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060681 WHERE b.weenie_Class_Id = 1054668 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060682 WHERE b.weenie_Class_Id = 1054668 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060683 WHERE b.weenie_Class_Id = 1054668 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060684 WHERE b.weenie_Class_Id = 1054668 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060685 WHERE b.weenie_Class_Id = 1054668 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060686 WHERE b.weenie_Class_Id = 1054668 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060687 WHERE b.weenie_Class_Id = 1054668 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060688 WHERE b.weenie_Class_Id = 1054668 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060689 WHERE b.weenie_Class_Id = 1054668 AND s.value = 6552;

-- Tender Steak of Quickness: WCID 1054669 -> 1060690-1060699 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060690 WHERE b.weenie_Class_Id = 1054669 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060691 WHERE b.weenie_Class_Id = 1054669 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060692 WHERE b.weenie_Class_Id = 1054669 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060693 WHERE b.weenie_Class_Id = 1054669 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060694 WHERE b.weenie_Class_Id = 1054669 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060695 WHERE b.weenie_Class_Id = 1054669 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060696 WHERE b.weenie_Class_Id = 1054669 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060697 WHERE b.weenie_Class_Id = 1054669 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060698 WHERE b.weenie_Class_Id = 1054669 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060699 WHERE b.weenie_Class_Id = 1054669 AND s.value = 6562;

-- Tender Steak of Focus: WCID 1054670 -> 1060700-1060709 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060700 WHERE b.weenie_Class_Id = 1054670 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060701 WHERE b.weenie_Class_Id = 1054670 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060702 WHERE b.weenie_Class_Id = 1054670 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060703 WHERE b.weenie_Class_Id = 1054670 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060704 WHERE b.weenie_Class_Id = 1054670 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060705 WHERE b.weenie_Class_Id = 1054670 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060706 WHERE b.weenie_Class_Id = 1054670 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060707 WHERE b.weenie_Class_Id = 1054670 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060708 WHERE b.weenie_Class_Id = 1054670 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060709 WHERE b.weenie_Class_Id = 1054670 AND s.value = 6572;

-- Tender Steak of Self: WCID 1054671 -> 1060710-1060719 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060710 WHERE b.weenie_Class_Id = 1054671 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060711 WHERE b.weenie_Class_Id = 1054671 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060712 WHERE b.weenie_Class_Id = 1054671 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060713 WHERE b.weenie_Class_Id = 1054671 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060714 WHERE b.weenie_Class_Id = 1054671 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060715 WHERE b.weenie_Class_Id = 1054671 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060716 WHERE b.weenie_Class_Id = 1054671 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060717 WHERE b.weenie_Class_Id = 1054671 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060718 WHERE b.weenie_Class_Id = 1054671 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060719 WHERE b.weenie_Class_Id = 1054671 AND s.value = 6582;

-- Tender Steak of War Magic: WCID 1054672 -> 1060720-1060729 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060720 WHERE b.weenie_Class_Id = 1054672 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060721 WHERE b.weenie_Class_Id = 1054672 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060722 WHERE b.weenie_Class_Id = 1054672 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060723 WHERE b.weenie_Class_Id = 1054672 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060724 WHERE b.weenie_Class_Id = 1054672 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060725 WHERE b.weenie_Class_Id = 1054672 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060726 WHERE b.weenie_Class_Id = 1054672 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060727 WHERE b.weenie_Class_Id = 1054672 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060728 WHERE b.weenie_Class_Id = 1054672 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060729 WHERE b.weenie_Class_Id = 1054672 AND s.value = 6824;

-- Tender Steak of Life Magic: WCID 1054673 -> 1060730-1060739 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060730 WHERE b.weenie_Class_Id = 1054673 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060731 WHERE b.weenie_Class_Id = 1054673 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060732 WHERE b.weenie_Class_Id = 1054673 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060733 WHERE b.weenie_Class_Id = 1054673 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060734 WHERE b.weenie_Class_Id = 1054673 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060735 WHERE b.weenie_Class_Id = 1054673 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060736 WHERE b.weenie_Class_Id = 1054673 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060737 WHERE b.weenie_Class_Id = 1054673 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060738 WHERE b.weenie_Class_Id = 1054673 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060739 WHERE b.weenie_Class_Id = 1054673 AND s.value = 6814;

-- Tender Steak of Sprint: WCID 1054674 -> 1060740-1060749 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060740 WHERE b.weenie_Class_Id = 1054674 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060741 WHERE b.weenie_Class_Id = 1054674 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060742 WHERE b.weenie_Class_Id = 1054674 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060743 WHERE b.weenie_Class_Id = 1054674 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060744 WHERE b.weenie_Class_Id = 1054674 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060745 WHERE b.weenie_Class_Id = 1054674 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060746 WHERE b.weenie_Class_Id = 1054674 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060747 WHERE b.weenie_Class_Id = 1054674 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060748 WHERE b.weenie_Class_Id = 1054674 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060749 WHERE b.weenie_Class_Id = 1054674 AND s.value = 6804;

-- Tender Steak of Jump: WCID 1054675 -> 1060750-1060759 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060750 WHERE b.weenie_Class_Id = 1054675 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060751 WHERE b.weenie_Class_Id = 1054675 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060752 WHERE b.weenie_Class_Id = 1054675 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060753 WHERE b.weenie_Class_Id = 1054675 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060754 WHERE b.weenie_Class_Id = 1054675 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060755 WHERE b.weenie_Class_Id = 1054675 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060756 WHERE b.weenie_Class_Id = 1054675 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060757 WHERE b.weenie_Class_Id = 1054675 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060758 WHERE b.weenie_Class_Id = 1054675 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060759 WHERE b.weenie_Class_Id = 1054675 AND s.value = 6794;

-- Tender Steak of Thievery: WCID 1054676 -> 1060760-1060769 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060760 WHERE b.weenie_Class_Id = 1054676 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060761 WHERE b.weenie_Class_Id = 1054676 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060762 WHERE b.weenie_Class_Id = 1054676 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060763 WHERE b.weenie_Class_Id = 1054676 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060764 WHERE b.weenie_Class_Id = 1054676 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060765 WHERE b.weenie_Class_Id = 1054676 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060766 WHERE b.weenie_Class_Id = 1054676 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060767 WHERE b.weenie_Class_Id = 1054676 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060768 WHERE b.weenie_Class_Id = 1054676 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060769 WHERE b.weenie_Class_Id = 1054676 AND s.value = 6834;

-- Tender Pepper Steak of Strength: WCID 1054677 -> 1060770-1060779 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060770 WHERE b.weenie_Class_Id = 1054677 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060771 WHERE b.weenie_Class_Id = 1054677 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060772 WHERE b.weenie_Class_Id = 1054677 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060773 WHERE b.weenie_Class_Id = 1054677 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060774 WHERE b.weenie_Class_Id = 1054677 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060775 WHERE b.weenie_Class_Id = 1054677 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060776 WHERE b.weenie_Class_Id = 1054677 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060777 WHERE b.weenie_Class_Id = 1054677 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060778 WHERE b.weenie_Class_Id = 1054677 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060779 WHERE b.weenie_Class_Id = 1054677 AND s.value = 6532;

-- Tender Pepper Steak of Endurance: WCID 1054678 -> 1060780-1060789 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060780 WHERE b.weenie_Class_Id = 1054678 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060781 WHERE b.weenie_Class_Id = 1054678 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060782 WHERE b.weenie_Class_Id = 1054678 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060783 WHERE b.weenie_Class_Id = 1054678 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060784 WHERE b.weenie_Class_Id = 1054678 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060785 WHERE b.weenie_Class_Id = 1054678 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060786 WHERE b.weenie_Class_Id = 1054678 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060787 WHERE b.weenie_Class_Id = 1054678 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060788 WHERE b.weenie_Class_Id = 1054678 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060789 WHERE b.weenie_Class_Id = 1054678 AND s.value = 6542;

-- Tender Pepper Steak of Coordination: WCID 1054679 -> 1060790-1060799 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060790 WHERE b.weenie_Class_Id = 1054679 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060791 WHERE b.weenie_Class_Id = 1054679 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060792 WHERE b.weenie_Class_Id = 1054679 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060793 WHERE b.weenie_Class_Id = 1054679 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060794 WHERE b.weenie_Class_Id = 1054679 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060795 WHERE b.weenie_Class_Id = 1054679 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060796 WHERE b.weenie_Class_Id = 1054679 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060797 WHERE b.weenie_Class_Id = 1054679 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060798 WHERE b.weenie_Class_Id = 1054679 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060799 WHERE b.weenie_Class_Id = 1054679 AND s.value = 6552;

-- Tender Pepper Steak of Quickness: WCID 1054680 -> 1060800-1060809 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060800 WHERE b.weenie_Class_Id = 1054680 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060801 WHERE b.weenie_Class_Id = 1054680 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060802 WHERE b.weenie_Class_Id = 1054680 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060803 WHERE b.weenie_Class_Id = 1054680 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060804 WHERE b.weenie_Class_Id = 1054680 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060805 WHERE b.weenie_Class_Id = 1054680 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060806 WHERE b.weenie_Class_Id = 1054680 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060807 WHERE b.weenie_Class_Id = 1054680 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060808 WHERE b.weenie_Class_Id = 1054680 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060809 WHERE b.weenie_Class_Id = 1054680 AND s.value = 6562;

-- Tender Pepper Steak of Focus: WCID 1054681 -> 1060810-1060819 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060810 WHERE b.weenie_Class_Id = 1054681 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060811 WHERE b.weenie_Class_Id = 1054681 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060812 WHERE b.weenie_Class_Id = 1054681 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060813 WHERE b.weenie_Class_Id = 1054681 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060814 WHERE b.weenie_Class_Id = 1054681 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060815 WHERE b.weenie_Class_Id = 1054681 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060816 WHERE b.weenie_Class_Id = 1054681 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060817 WHERE b.weenie_Class_Id = 1054681 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060818 WHERE b.weenie_Class_Id = 1054681 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060819 WHERE b.weenie_Class_Id = 1054681 AND s.value = 6572;

-- Tender Pepper Steak of Self: WCID 1054682 -> 1060820-1060829 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060820 WHERE b.weenie_Class_Id = 1054682 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060821 WHERE b.weenie_Class_Id = 1054682 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060822 WHERE b.weenie_Class_Id = 1054682 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060823 WHERE b.weenie_Class_Id = 1054682 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060824 WHERE b.weenie_Class_Id = 1054682 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060825 WHERE b.weenie_Class_Id = 1054682 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060826 WHERE b.weenie_Class_Id = 1054682 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060827 WHERE b.weenie_Class_Id = 1054682 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060828 WHERE b.weenie_Class_Id = 1054682 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060829 WHERE b.weenie_Class_Id = 1054682 AND s.value = 6582;

-- Tender Pepper Steak of War Magic: WCID 1054683 -> 1060830-1060839 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060830 WHERE b.weenie_Class_Id = 1054683 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060831 WHERE b.weenie_Class_Id = 1054683 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060832 WHERE b.weenie_Class_Id = 1054683 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060833 WHERE b.weenie_Class_Id = 1054683 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060834 WHERE b.weenie_Class_Id = 1054683 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060835 WHERE b.weenie_Class_Id = 1054683 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060836 WHERE b.weenie_Class_Id = 1054683 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060837 WHERE b.weenie_Class_Id = 1054683 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060838 WHERE b.weenie_Class_Id = 1054683 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060839 WHERE b.weenie_Class_Id = 1054683 AND s.value = 6824;

-- Tender Pepper Steak of Life Magic: WCID 1054684 -> 1060840-1060849 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060840 WHERE b.weenie_Class_Id = 1054684 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060841 WHERE b.weenie_Class_Id = 1054684 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060842 WHERE b.weenie_Class_Id = 1054684 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060843 WHERE b.weenie_Class_Id = 1054684 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060844 WHERE b.weenie_Class_Id = 1054684 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060845 WHERE b.weenie_Class_Id = 1054684 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060846 WHERE b.weenie_Class_Id = 1054684 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060847 WHERE b.weenie_Class_Id = 1054684 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060848 WHERE b.weenie_Class_Id = 1054684 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060849 WHERE b.weenie_Class_Id = 1054684 AND s.value = 6814;

-- Tender Pepper Steak of Sprint: WCID 1054685 -> 1060850-1060859 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060850 WHERE b.weenie_Class_Id = 1054685 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060851 WHERE b.weenie_Class_Id = 1054685 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060852 WHERE b.weenie_Class_Id = 1054685 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060853 WHERE b.weenie_Class_Id = 1054685 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060854 WHERE b.weenie_Class_Id = 1054685 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060855 WHERE b.weenie_Class_Id = 1054685 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060856 WHERE b.weenie_Class_Id = 1054685 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060857 WHERE b.weenie_Class_Id = 1054685 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060858 WHERE b.weenie_Class_Id = 1054685 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060859 WHERE b.weenie_Class_Id = 1054685 AND s.value = 6804;

-- Tender Pepper Steak of Jump: WCID 1054686 -> 1060860-1060869 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060860 WHERE b.weenie_Class_Id = 1054686 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060861 WHERE b.weenie_Class_Id = 1054686 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060862 WHERE b.weenie_Class_Id = 1054686 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060863 WHERE b.weenie_Class_Id = 1054686 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060864 WHERE b.weenie_Class_Id = 1054686 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060865 WHERE b.weenie_Class_Id = 1054686 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060866 WHERE b.weenie_Class_Id = 1054686 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060867 WHERE b.weenie_Class_Id = 1054686 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060868 WHERE b.weenie_Class_Id = 1054686 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060869 WHERE b.weenie_Class_Id = 1054686 AND s.value = 6794;

-- Tender Pepper Steak of Thievery: WCID 1054687 -> 1060870-1060879 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060870 WHERE b.weenie_Class_Id = 1054687 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060871 WHERE b.weenie_Class_Id = 1054687 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060872 WHERE b.weenie_Class_Id = 1054687 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060873 WHERE b.weenie_Class_Id = 1054687 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060874 WHERE b.weenie_Class_Id = 1054687 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060875 WHERE b.weenie_Class_Id = 1054687 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060876 WHERE b.weenie_Class_Id = 1054687 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060877 WHERE b.weenie_Class_Id = 1054687 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060878 WHERE b.weenie_Class_Id = 1054687 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060879 WHERE b.weenie_Class_Id = 1054687 AND s.value = 6834;

-- Tender Brined Steak of Strength: WCID 1054688 -> 1060880-1060889 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060880 WHERE b.weenie_Class_Id = 1054688 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060881 WHERE b.weenie_Class_Id = 1054688 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060882 WHERE b.weenie_Class_Id = 1054688 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060883 WHERE b.weenie_Class_Id = 1054688 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060884 WHERE b.weenie_Class_Id = 1054688 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060885 WHERE b.weenie_Class_Id = 1054688 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060886 WHERE b.weenie_Class_Id = 1054688 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060887 WHERE b.weenie_Class_Id = 1054688 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060888 WHERE b.weenie_Class_Id = 1054688 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060889 WHERE b.weenie_Class_Id = 1054688 AND s.value = 6532;

-- Tender Brined Steak of Endurance: WCID 1054689 -> 1060890-1060899 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060890 WHERE b.weenie_Class_Id = 1054689 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060891 WHERE b.weenie_Class_Id = 1054689 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060892 WHERE b.weenie_Class_Id = 1054689 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060893 WHERE b.weenie_Class_Id = 1054689 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060894 WHERE b.weenie_Class_Id = 1054689 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060895 WHERE b.weenie_Class_Id = 1054689 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060896 WHERE b.weenie_Class_Id = 1054689 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060897 WHERE b.weenie_Class_Id = 1054689 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060898 WHERE b.weenie_Class_Id = 1054689 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060899 WHERE b.weenie_Class_Id = 1054689 AND s.value = 6542;

-- Tender Brined Steak of Coordination: WCID 1054690 -> 1060900-1060909 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060900 WHERE b.weenie_Class_Id = 1054690 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060901 WHERE b.weenie_Class_Id = 1054690 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060902 WHERE b.weenie_Class_Id = 1054690 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060903 WHERE b.weenie_Class_Id = 1054690 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060904 WHERE b.weenie_Class_Id = 1054690 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060905 WHERE b.weenie_Class_Id = 1054690 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060906 WHERE b.weenie_Class_Id = 1054690 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060907 WHERE b.weenie_Class_Id = 1054690 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060908 WHERE b.weenie_Class_Id = 1054690 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060909 WHERE b.weenie_Class_Id = 1054690 AND s.value = 6552;

-- Tender Brined Steak of Quickness: WCID 1054691 -> 1060910-1060919 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060910 WHERE b.weenie_Class_Id = 1054691 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060911 WHERE b.weenie_Class_Id = 1054691 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060912 WHERE b.weenie_Class_Id = 1054691 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060913 WHERE b.weenie_Class_Id = 1054691 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060914 WHERE b.weenie_Class_Id = 1054691 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060915 WHERE b.weenie_Class_Id = 1054691 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060916 WHERE b.weenie_Class_Id = 1054691 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060917 WHERE b.weenie_Class_Id = 1054691 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060918 WHERE b.weenie_Class_Id = 1054691 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060919 WHERE b.weenie_Class_Id = 1054691 AND s.value = 6562;

-- Tender Brined Steak of Focus: WCID 1054692 -> 1060920-1060929 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060920 WHERE b.weenie_Class_Id = 1054692 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060921 WHERE b.weenie_Class_Id = 1054692 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060922 WHERE b.weenie_Class_Id = 1054692 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060923 WHERE b.weenie_Class_Id = 1054692 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060924 WHERE b.weenie_Class_Id = 1054692 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060925 WHERE b.weenie_Class_Id = 1054692 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060926 WHERE b.weenie_Class_Id = 1054692 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060927 WHERE b.weenie_Class_Id = 1054692 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060928 WHERE b.weenie_Class_Id = 1054692 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060929 WHERE b.weenie_Class_Id = 1054692 AND s.value = 6572;

-- Tender Brined Steak of Self: WCID 1054693 -> 1060930-1060939 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060930 WHERE b.weenie_Class_Id = 1054693 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060931 WHERE b.weenie_Class_Id = 1054693 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060932 WHERE b.weenie_Class_Id = 1054693 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060933 WHERE b.weenie_Class_Id = 1054693 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060934 WHERE b.weenie_Class_Id = 1054693 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060935 WHERE b.weenie_Class_Id = 1054693 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060936 WHERE b.weenie_Class_Id = 1054693 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060937 WHERE b.weenie_Class_Id = 1054693 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060938 WHERE b.weenie_Class_Id = 1054693 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060939 WHERE b.weenie_Class_Id = 1054693 AND s.value = 6582;

-- Tender Brined Steak of War Magic: WCID 1054694 -> 1060940-1060949 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060940 WHERE b.weenie_Class_Id = 1054694 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060941 WHERE b.weenie_Class_Id = 1054694 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060942 WHERE b.weenie_Class_Id = 1054694 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060943 WHERE b.weenie_Class_Id = 1054694 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060944 WHERE b.weenie_Class_Id = 1054694 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060945 WHERE b.weenie_Class_Id = 1054694 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060946 WHERE b.weenie_Class_Id = 1054694 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060947 WHERE b.weenie_Class_Id = 1054694 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060948 WHERE b.weenie_Class_Id = 1054694 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060949 WHERE b.weenie_Class_Id = 1054694 AND s.value = 6824;

-- Tender Brined Steak of Life Magic: WCID 1054695 -> 1060950-1060959 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060950 WHERE b.weenie_Class_Id = 1054695 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060951 WHERE b.weenie_Class_Id = 1054695 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060952 WHERE b.weenie_Class_Id = 1054695 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060953 WHERE b.weenie_Class_Id = 1054695 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060954 WHERE b.weenie_Class_Id = 1054695 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060955 WHERE b.weenie_Class_Id = 1054695 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060956 WHERE b.weenie_Class_Id = 1054695 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060957 WHERE b.weenie_Class_Id = 1054695 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060958 WHERE b.weenie_Class_Id = 1054695 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060959 WHERE b.weenie_Class_Id = 1054695 AND s.value = 6814;

-- Tender Brined Steak of Sprint: WCID 1054696 -> 1060960-1060969 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060960 WHERE b.weenie_Class_Id = 1054696 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060961 WHERE b.weenie_Class_Id = 1054696 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060962 WHERE b.weenie_Class_Id = 1054696 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060963 WHERE b.weenie_Class_Id = 1054696 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060964 WHERE b.weenie_Class_Id = 1054696 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060965 WHERE b.weenie_Class_Id = 1054696 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060966 WHERE b.weenie_Class_Id = 1054696 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060967 WHERE b.weenie_Class_Id = 1054696 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060968 WHERE b.weenie_Class_Id = 1054696 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060969 WHERE b.weenie_Class_Id = 1054696 AND s.value = 6804;

-- Tender Brined Steak of Jump: WCID 1054697 -> 1060970-1060979 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060970 WHERE b.weenie_Class_Id = 1054697 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060971 WHERE b.weenie_Class_Id = 1054697 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060972 WHERE b.weenie_Class_Id = 1054697 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060973 WHERE b.weenie_Class_Id = 1054697 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060974 WHERE b.weenie_Class_Id = 1054697 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060975 WHERE b.weenie_Class_Id = 1054697 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060976 WHERE b.weenie_Class_Id = 1054697 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060977 WHERE b.weenie_Class_Id = 1054697 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060978 WHERE b.weenie_Class_Id = 1054697 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060979 WHERE b.weenie_Class_Id = 1054697 AND s.value = 6794;

-- Tender Brined Steak of Thievery: WCID 1054698 -> 1060980-1060989 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060980 WHERE b.weenie_Class_Id = 1054698 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060981 WHERE b.weenie_Class_Id = 1054698 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060982 WHERE b.weenie_Class_Id = 1054698 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060983 WHERE b.weenie_Class_Id = 1054698 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060984 WHERE b.weenie_Class_Id = 1054698 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060985 WHERE b.weenie_Class_Id = 1054698 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060986 WHERE b.weenie_Class_Id = 1054698 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060987 WHERE b.weenie_Class_Id = 1054698 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060988 WHERE b.weenie_Class_Id = 1054698 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060989 WHERE b.weenie_Class_Id = 1054698 AND s.value = 6834;

-- Choice Steak of Strength: WCID 1054699 -> 1060990-1060999 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060990 WHERE b.weenie_Class_Id = 1054699 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060991 WHERE b.weenie_Class_Id = 1054699 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060992 WHERE b.weenie_Class_Id = 1054699 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060993 WHERE b.weenie_Class_Id = 1054699 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060994 WHERE b.weenie_Class_Id = 1054699 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060995 WHERE b.weenie_Class_Id = 1054699 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060996 WHERE b.weenie_Class_Id = 1054699 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060997 WHERE b.weenie_Class_Id = 1054699 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060998 WHERE b.weenie_Class_Id = 1054699 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1060999 WHERE b.weenie_Class_Id = 1054699 AND s.value = 6532;

-- Choice Steak of Endurance: WCID 1054700 -> 1061000-1061009 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061000 WHERE b.weenie_Class_Id = 1054700 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061001 WHERE b.weenie_Class_Id = 1054700 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061002 WHERE b.weenie_Class_Id = 1054700 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061003 WHERE b.weenie_Class_Id = 1054700 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061004 WHERE b.weenie_Class_Id = 1054700 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061005 WHERE b.weenie_Class_Id = 1054700 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061006 WHERE b.weenie_Class_Id = 1054700 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061007 WHERE b.weenie_Class_Id = 1054700 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061008 WHERE b.weenie_Class_Id = 1054700 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061009 WHERE b.weenie_Class_Id = 1054700 AND s.value = 6542;

-- Choice Steak of Coordination: WCID 1054701 -> 1061010-1061019 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061010 WHERE b.weenie_Class_Id = 1054701 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061011 WHERE b.weenie_Class_Id = 1054701 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061012 WHERE b.weenie_Class_Id = 1054701 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061013 WHERE b.weenie_Class_Id = 1054701 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061014 WHERE b.weenie_Class_Id = 1054701 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061015 WHERE b.weenie_Class_Id = 1054701 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061016 WHERE b.weenie_Class_Id = 1054701 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061017 WHERE b.weenie_Class_Id = 1054701 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061018 WHERE b.weenie_Class_Id = 1054701 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061019 WHERE b.weenie_Class_Id = 1054701 AND s.value = 6552;

-- Choice Steak of Quickness: WCID 1054702 -> 1061020-1061029 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061020 WHERE b.weenie_Class_Id = 1054702 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061021 WHERE b.weenie_Class_Id = 1054702 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061022 WHERE b.weenie_Class_Id = 1054702 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061023 WHERE b.weenie_Class_Id = 1054702 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061024 WHERE b.weenie_Class_Id = 1054702 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061025 WHERE b.weenie_Class_Id = 1054702 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061026 WHERE b.weenie_Class_Id = 1054702 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061027 WHERE b.weenie_Class_Id = 1054702 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061028 WHERE b.weenie_Class_Id = 1054702 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061029 WHERE b.weenie_Class_Id = 1054702 AND s.value = 6562;

-- Choice Steak of Focus: WCID 1054703 -> 1061030-1061039 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061030 WHERE b.weenie_Class_Id = 1054703 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061031 WHERE b.weenie_Class_Id = 1054703 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061032 WHERE b.weenie_Class_Id = 1054703 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061033 WHERE b.weenie_Class_Id = 1054703 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061034 WHERE b.weenie_Class_Id = 1054703 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061035 WHERE b.weenie_Class_Id = 1054703 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061036 WHERE b.weenie_Class_Id = 1054703 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061037 WHERE b.weenie_Class_Id = 1054703 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061038 WHERE b.weenie_Class_Id = 1054703 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061039 WHERE b.weenie_Class_Id = 1054703 AND s.value = 6572;

-- Choice Steak of Self: WCID 1054704 -> 1061040-1061049 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061040 WHERE b.weenie_Class_Id = 1054704 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061041 WHERE b.weenie_Class_Id = 1054704 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061042 WHERE b.weenie_Class_Id = 1054704 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061043 WHERE b.weenie_Class_Id = 1054704 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061044 WHERE b.weenie_Class_Id = 1054704 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061045 WHERE b.weenie_Class_Id = 1054704 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061046 WHERE b.weenie_Class_Id = 1054704 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061047 WHERE b.weenie_Class_Id = 1054704 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061048 WHERE b.weenie_Class_Id = 1054704 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061049 WHERE b.weenie_Class_Id = 1054704 AND s.value = 6582;

-- Choice Steak of War Magic: WCID 1054705 -> 1061050-1061059 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061050 WHERE b.weenie_Class_Id = 1054705 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061051 WHERE b.weenie_Class_Id = 1054705 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061052 WHERE b.weenie_Class_Id = 1054705 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061053 WHERE b.weenie_Class_Id = 1054705 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061054 WHERE b.weenie_Class_Id = 1054705 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061055 WHERE b.weenie_Class_Id = 1054705 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061056 WHERE b.weenie_Class_Id = 1054705 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061057 WHERE b.weenie_Class_Id = 1054705 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061058 WHERE b.weenie_Class_Id = 1054705 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061059 WHERE b.weenie_Class_Id = 1054705 AND s.value = 6824;

-- Choice Steak of Life Magic: WCID 1054706 -> 1061060-1061069 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061060 WHERE b.weenie_Class_Id = 1054706 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061061 WHERE b.weenie_Class_Id = 1054706 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061062 WHERE b.weenie_Class_Id = 1054706 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061063 WHERE b.weenie_Class_Id = 1054706 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061064 WHERE b.weenie_Class_Id = 1054706 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061065 WHERE b.weenie_Class_Id = 1054706 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061066 WHERE b.weenie_Class_Id = 1054706 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061067 WHERE b.weenie_Class_Id = 1054706 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061068 WHERE b.weenie_Class_Id = 1054706 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061069 WHERE b.weenie_Class_Id = 1054706 AND s.value = 6814;

-- Choice Steak of Sprint: WCID 1054707 -> 1061070-1061079 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061070 WHERE b.weenie_Class_Id = 1054707 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061071 WHERE b.weenie_Class_Id = 1054707 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061072 WHERE b.weenie_Class_Id = 1054707 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061073 WHERE b.weenie_Class_Id = 1054707 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061074 WHERE b.weenie_Class_Id = 1054707 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061075 WHERE b.weenie_Class_Id = 1054707 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061076 WHERE b.weenie_Class_Id = 1054707 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061077 WHERE b.weenie_Class_Id = 1054707 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061078 WHERE b.weenie_Class_Id = 1054707 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061079 WHERE b.weenie_Class_Id = 1054707 AND s.value = 6804;

-- Choice Steak of Jump: WCID 1054708 -> 1061080-1061089 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061080 WHERE b.weenie_Class_Id = 1054708 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061081 WHERE b.weenie_Class_Id = 1054708 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061082 WHERE b.weenie_Class_Id = 1054708 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061083 WHERE b.weenie_Class_Id = 1054708 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061084 WHERE b.weenie_Class_Id = 1054708 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061085 WHERE b.weenie_Class_Id = 1054708 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061086 WHERE b.weenie_Class_Id = 1054708 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061087 WHERE b.weenie_Class_Id = 1054708 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061088 WHERE b.weenie_Class_Id = 1054708 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061089 WHERE b.weenie_Class_Id = 1054708 AND s.value = 6794;

-- Choice Steak of Thievery: WCID 1054709 -> 1061090-1061099 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061090 WHERE b.weenie_Class_Id = 1054709 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061091 WHERE b.weenie_Class_Id = 1054709 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061092 WHERE b.weenie_Class_Id = 1054709 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061093 WHERE b.weenie_Class_Id = 1054709 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061094 WHERE b.weenie_Class_Id = 1054709 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061095 WHERE b.weenie_Class_Id = 1054709 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061096 WHERE b.weenie_Class_Id = 1054709 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061097 WHERE b.weenie_Class_Id = 1054709 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061098 WHERE b.weenie_Class_Id = 1054709 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061099 WHERE b.weenie_Class_Id = 1054709 AND s.value = 6834;

-- Choice Pepper Steak of Strength: WCID 1054710 -> 1061100-1061109 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061100 WHERE b.weenie_Class_Id = 1054710 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061101 WHERE b.weenie_Class_Id = 1054710 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061102 WHERE b.weenie_Class_Id = 1054710 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061103 WHERE b.weenie_Class_Id = 1054710 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061104 WHERE b.weenie_Class_Id = 1054710 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061105 WHERE b.weenie_Class_Id = 1054710 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061106 WHERE b.weenie_Class_Id = 1054710 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061107 WHERE b.weenie_Class_Id = 1054710 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061108 WHERE b.weenie_Class_Id = 1054710 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061109 WHERE b.weenie_Class_Id = 1054710 AND s.value = 6532;

-- Choice Pepper Steak of Endurance: WCID 1054711 -> 1061110-1061119 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061110 WHERE b.weenie_Class_Id = 1054711 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061111 WHERE b.weenie_Class_Id = 1054711 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061112 WHERE b.weenie_Class_Id = 1054711 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061113 WHERE b.weenie_Class_Id = 1054711 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061114 WHERE b.weenie_Class_Id = 1054711 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061115 WHERE b.weenie_Class_Id = 1054711 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061116 WHERE b.weenie_Class_Id = 1054711 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061117 WHERE b.weenie_Class_Id = 1054711 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061118 WHERE b.weenie_Class_Id = 1054711 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061119 WHERE b.weenie_Class_Id = 1054711 AND s.value = 6542;

-- Choice Pepper Steak of Coordination: WCID 1054712 -> 1061120-1061129 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061120 WHERE b.weenie_Class_Id = 1054712 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061121 WHERE b.weenie_Class_Id = 1054712 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061122 WHERE b.weenie_Class_Id = 1054712 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061123 WHERE b.weenie_Class_Id = 1054712 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061124 WHERE b.weenie_Class_Id = 1054712 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061125 WHERE b.weenie_Class_Id = 1054712 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061126 WHERE b.weenie_Class_Id = 1054712 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061127 WHERE b.weenie_Class_Id = 1054712 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061128 WHERE b.weenie_Class_Id = 1054712 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061129 WHERE b.weenie_Class_Id = 1054712 AND s.value = 6552;

-- Choice Pepper Steak of Quickness: WCID 1054713 -> 1061130-1061139 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061130 WHERE b.weenie_Class_Id = 1054713 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061131 WHERE b.weenie_Class_Id = 1054713 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061132 WHERE b.weenie_Class_Id = 1054713 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061133 WHERE b.weenie_Class_Id = 1054713 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061134 WHERE b.weenie_Class_Id = 1054713 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061135 WHERE b.weenie_Class_Id = 1054713 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061136 WHERE b.weenie_Class_Id = 1054713 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061137 WHERE b.weenie_Class_Id = 1054713 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061138 WHERE b.weenie_Class_Id = 1054713 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061139 WHERE b.weenie_Class_Id = 1054713 AND s.value = 6562;

-- Choice Pepper Steak of Focus: WCID 1054714 -> 1061140-1061149 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061140 WHERE b.weenie_Class_Id = 1054714 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061141 WHERE b.weenie_Class_Id = 1054714 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061142 WHERE b.weenie_Class_Id = 1054714 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061143 WHERE b.weenie_Class_Id = 1054714 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061144 WHERE b.weenie_Class_Id = 1054714 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061145 WHERE b.weenie_Class_Id = 1054714 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061146 WHERE b.weenie_Class_Id = 1054714 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061147 WHERE b.weenie_Class_Id = 1054714 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061148 WHERE b.weenie_Class_Id = 1054714 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061149 WHERE b.weenie_Class_Id = 1054714 AND s.value = 6572;

-- Choice Pepper Steak of Self: WCID 1054715 -> 1061150-1061159 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061150 WHERE b.weenie_Class_Id = 1054715 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061151 WHERE b.weenie_Class_Id = 1054715 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061152 WHERE b.weenie_Class_Id = 1054715 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061153 WHERE b.weenie_Class_Id = 1054715 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061154 WHERE b.weenie_Class_Id = 1054715 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061155 WHERE b.weenie_Class_Id = 1054715 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061156 WHERE b.weenie_Class_Id = 1054715 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061157 WHERE b.weenie_Class_Id = 1054715 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061158 WHERE b.weenie_Class_Id = 1054715 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061159 WHERE b.weenie_Class_Id = 1054715 AND s.value = 6582;

-- Choice Pepper Steak of War Magic: WCID 1054716 -> 1061160-1061169 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061160 WHERE b.weenie_Class_Id = 1054716 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061161 WHERE b.weenie_Class_Id = 1054716 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061162 WHERE b.weenie_Class_Id = 1054716 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061163 WHERE b.weenie_Class_Id = 1054716 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061164 WHERE b.weenie_Class_Id = 1054716 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061165 WHERE b.weenie_Class_Id = 1054716 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061166 WHERE b.weenie_Class_Id = 1054716 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061167 WHERE b.weenie_Class_Id = 1054716 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061168 WHERE b.weenie_Class_Id = 1054716 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061169 WHERE b.weenie_Class_Id = 1054716 AND s.value = 6824;

-- Choice Pepper Steak of Life Magic: WCID 1054717 -> 1061170-1061179 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061170 WHERE b.weenie_Class_Id = 1054717 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061171 WHERE b.weenie_Class_Id = 1054717 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061172 WHERE b.weenie_Class_Id = 1054717 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061173 WHERE b.weenie_Class_Id = 1054717 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061174 WHERE b.weenie_Class_Id = 1054717 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061175 WHERE b.weenie_Class_Id = 1054717 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061176 WHERE b.weenie_Class_Id = 1054717 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061177 WHERE b.weenie_Class_Id = 1054717 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061178 WHERE b.weenie_Class_Id = 1054717 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061179 WHERE b.weenie_Class_Id = 1054717 AND s.value = 6814;

-- Choice Pepper Steak of Sprint: WCID 1054718 -> 1061180-1061189 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061180 WHERE b.weenie_Class_Id = 1054718 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061181 WHERE b.weenie_Class_Id = 1054718 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061182 WHERE b.weenie_Class_Id = 1054718 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061183 WHERE b.weenie_Class_Id = 1054718 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061184 WHERE b.weenie_Class_Id = 1054718 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061185 WHERE b.weenie_Class_Id = 1054718 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061186 WHERE b.weenie_Class_Id = 1054718 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061187 WHERE b.weenie_Class_Id = 1054718 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061188 WHERE b.weenie_Class_Id = 1054718 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061189 WHERE b.weenie_Class_Id = 1054718 AND s.value = 6804;

-- Choice Pepper Steak of Jump: WCID 1054719 -> 1061190-1061199 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061190 WHERE b.weenie_Class_Id = 1054719 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061191 WHERE b.weenie_Class_Id = 1054719 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061192 WHERE b.weenie_Class_Id = 1054719 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061193 WHERE b.weenie_Class_Id = 1054719 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061194 WHERE b.weenie_Class_Id = 1054719 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061195 WHERE b.weenie_Class_Id = 1054719 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061196 WHERE b.weenie_Class_Id = 1054719 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061197 WHERE b.weenie_Class_Id = 1054719 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061198 WHERE b.weenie_Class_Id = 1054719 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061199 WHERE b.weenie_Class_Id = 1054719 AND s.value = 6794;

-- Choice Pepper Steak of Thievery: WCID 1054720 -> 1061200-1061209 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061200 WHERE b.weenie_Class_Id = 1054720 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061201 WHERE b.weenie_Class_Id = 1054720 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061202 WHERE b.weenie_Class_Id = 1054720 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061203 WHERE b.weenie_Class_Id = 1054720 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061204 WHERE b.weenie_Class_Id = 1054720 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061205 WHERE b.weenie_Class_Id = 1054720 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061206 WHERE b.weenie_Class_Id = 1054720 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061207 WHERE b.weenie_Class_Id = 1054720 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061208 WHERE b.weenie_Class_Id = 1054720 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061209 WHERE b.weenie_Class_Id = 1054720 AND s.value = 6834;

-- Choice Brined Steak of Strength: WCID 1054721 -> 1061210-1061219 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061210 WHERE b.weenie_Class_Id = 1054721 AND s.value = 6523;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061211 WHERE b.weenie_Class_Id = 1054721 AND s.value = 6524;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061212 WHERE b.weenie_Class_Id = 1054721 AND s.value = 6525;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061213 WHERE b.weenie_Class_Id = 1054721 AND s.value = 6526;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061214 WHERE b.weenie_Class_Id = 1054721 AND s.value = 6527;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061215 WHERE b.weenie_Class_Id = 1054721 AND s.value = 6528;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061216 WHERE b.weenie_Class_Id = 1054721 AND s.value = 6529;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061217 WHERE b.weenie_Class_Id = 1054721 AND s.value = 6530;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061218 WHERE b.weenie_Class_Id = 1054721 AND s.value = 6531;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061219 WHERE b.weenie_Class_Id = 1054721 AND s.value = 6532;

-- Choice Brined Steak of Endurance: WCID 1054722 -> 1061220-1061229 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061220 WHERE b.weenie_Class_Id = 1054722 AND s.value = 6533;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061221 WHERE b.weenie_Class_Id = 1054722 AND s.value = 6534;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061222 WHERE b.weenie_Class_Id = 1054722 AND s.value = 6535;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061223 WHERE b.weenie_Class_Id = 1054722 AND s.value = 6536;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061224 WHERE b.weenie_Class_Id = 1054722 AND s.value = 6537;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061225 WHERE b.weenie_Class_Id = 1054722 AND s.value = 6538;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061226 WHERE b.weenie_Class_Id = 1054722 AND s.value = 6539;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061227 WHERE b.weenie_Class_Id = 1054722 AND s.value = 6540;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061228 WHERE b.weenie_Class_Id = 1054722 AND s.value = 6541;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061229 WHERE b.weenie_Class_Id = 1054722 AND s.value = 6542;

-- Choice Brined Steak of Coordination: WCID 1054723 -> 1061230-1061239 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061230 WHERE b.weenie_Class_Id = 1054723 AND s.value = 6543;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061231 WHERE b.weenie_Class_Id = 1054723 AND s.value = 6544;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061232 WHERE b.weenie_Class_Id = 1054723 AND s.value = 6545;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061233 WHERE b.weenie_Class_Id = 1054723 AND s.value = 6546;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061234 WHERE b.weenie_Class_Id = 1054723 AND s.value = 6547;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061235 WHERE b.weenie_Class_Id = 1054723 AND s.value = 6548;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061236 WHERE b.weenie_Class_Id = 1054723 AND s.value = 6549;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061237 WHERE b.weenie_Class_Id = 1054723 AND s.value = 6550;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061238 WHERE b.weenie_Class_Id = 1054723 AND s.value = 6551;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061239 WHERE b.weenie_Class_Id = 1054723 AND s.value = 6552;

-- Choice Brined Steak of Quickness: WCID 1054724 -> 1061240-1061249 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061240 WHERE b.weenie_Class_Id = 1054724 AND s.value = 6553;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061241 WHERE b.weenie_Class_Id = 1054724 AND s.value = 6554;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061242 WHERE b.weenie_Class_Id = 1054724 AND s.value = 6555;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061243 WHERE b.weenie_Class_Id = 1054724 AND s.value = 6556;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061244 WHERE b.weenie_Class_Id = 1054724 AND s.value = 6557;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061245 WHERE b.weenie_Class_Id = 1054724 AND s.value = 6558;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061246 WHERE b.weenie_Class_Id = 1054724 AND s.value = 6559;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061247 WHERE b.weenie_Class_Id = 1054724 AND s.value = 6560;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061248 WHERE b.weenie_Class_Id = 1054724 AND s.value = 6561;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061249 WHERE b.weenie_Class_Id = 1054724 AND s.value = 6562;

-- Choice Brined Steak of Focus: WCID 1054725 -> 1061250-1061259 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061250 WHERE b.weenie_Class_Id = 1054725 AND s.value = 6563;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061251 WHERE b.weenie_Class_Id = 1054725 AND s.value = 6564;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061252 WHERE b.weenie_Class_Id = 1054725 AND s.value = 6565;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061253 WHERE b.weenie_Class_Id = 1054725 AND s.value = 6566;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061254 WHERE b.weenie_Class_Id = 1054725 AND s.value = 6567;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061255 WHERE b.weenie_Class_Id = 1054725 AND s.value = 6568;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061256 WHERE b.weenie_Class_Id = 1054725 AND s.value = 6569;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061257 WHERE b.weenie_Class_Id = 1054725 AND s.value = 6570;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061258 WHERE b.weenie_Class_Id = 1054725 AND s.value = 6571;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061259 WHERE b.weenie_Class_Id = 1054725 AND s.value = 6572;

-- Choice Brined Steak of Self: WCID 1054726 -> 1061260-1061269 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061260 WHERE b.weenie_Class_Id = 1054726 AND s.value = 6573;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061261 WHERE b.weenie_Class_Id = 1054726 AND s.value = 6574;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061262 WHERE b.weenie_Class_Id = 1054726 AND s.value = 6575;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061263 WHERE b.weenie_Class_Id = 1054726 AND s.value = 6576;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061264 WHERE b.weenie_Class_Id = 1054726 AND s.value = 6577;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061265 WHERE b.weenie_Class_Id = 1054726 AND s.value = 6578;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061266 WHERE b.weenie_Class_Id = 1054726 AND s.value = 6579;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061267 WHERE b.weenie_Class_Id = 1054726 AND s.value = 6580;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061268 WHERE b.weenie_Class_Id = 1054726 AND s.value = 6581;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061269 WHERE b.weenie_Class_Id = 1054726 AND s.value = 6582;

-- Choice Brined Steak of War Magic: WCID 1054727 -> 1061270-1061279 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061270 WHERE b.weenie_Class_Id = 1054727 AND s.value = 6815;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061271 WHERE b.weenie_Class_Id = 1054727 AND s.value = 6816;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061272 WHERE b.weenie_Class_Id = 1054727 AND s.value = 6817;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061273 WHERE b.weenie_Class_Id = 1054727 AND s.value = 6818;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061274 WHERE b.weenie_Class_Id = 1054727 AND s.value = 6819;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061275 WHERE b.weenie_Class_Id = 1054727 AND s.value = 6820;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061276 WHERE b.weenie_Class_Id = 1054727 AND s.value = 6821;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061277 WHERE b.weenie_Class_Id = 1054727 AND s.value = 6822;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061278 WHERE b.weenie_Class_Id = 1054727 AND s.value = 6823;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061279 WHERE b.weenie_Class_Id = 1054727 AND s.value = 6824;

-- Choice Brined Steak of Life Magic: WCID 1054728 -> 1061280-1061289 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061280 WHERE b.weenie_Class_Id = 1054728 AND s.value = 6805;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061281 WHERE b.weenie_Class_Id = 1054728 AND s.value = 6806;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061282 WHERE b.weenie_Class_Id = 1054728 AND s.value = 6807;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061283 WHERE b.weenie_Class_Id = 1054728 AND s.value = 6808;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061284 WHERE b.weenie_Class_Id = 1054728 AND s.value = 6809;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061285 WHERE b.weenie_Class_Id = 1054728 AND s.value = 6810;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061286 WHERE b.weenie_Class_Id = 1054728 AND s.value = 6811;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061287 WHERE b.weenie_Class_Id = 1054728 AND s.value = 6812;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061288 WHERE b.weenie_Class_Id = 1054728 AND s.value = 6813;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061289 WHERE b.weenie_Class_Id = 1054728 AND s.value = 6814;

-- Choice Brined Steak of Sprint: WCID 1054729 -> 1061290-1061299 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061290 WHERE b.weenie_Class_Id = 1054729 AND s.value = 6795;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061291 WHERE b.weenie_Class_Id = 1054729 AND s.value = 6796;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061292 WHERE b.weenie_Class_Id = 1054729 AND s.value = 6797;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061293 WHERE b.weenie_Class_Id = 1054729 AND s.value = 6798;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061294 WHERE b.weenie_Class_Id = 1054729 AND s.value = 6799;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061295 WHERE b.weenie_Class_Id = 1054729 AND s.value = 6800;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061296 WHERE b.weenie_Class_Id = 1054729 AND s.value = 6801;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061297 WHERE b.weenie_Class_Id = 1054729 AND s.value = 6802;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061298 WHERE b.weenie_Class_Id = 1054729 AND s.value = 6803;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061299 WHERE b.weenie_Class_Id = 1054729 AND s.value = 6804;

-- Choice Brined Steak of Jump: WCID 1054730 -> 1061300-1061309 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061300 WHERE b.weenie_Class_Id = 1054730 AND s.value = 6785;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061301 WHERE b.weenie_Class_Id = 1054730 AND s.value = 6786;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061302 WHERE b.weenie_Class_Id = 1054730 AND s.value = 6787;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061303 WHERE b.weenie_Class_Id = 1054730 AND s.value = 6788;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061304 WHERE b.weenie_Class_Id = 1054730 AND s.value = 6789;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061305 WHERE b.weenie_Class_Id = 1054730 AND s.value = 6790;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061306 WHERE b.weenie_Class_Id = 1054730 AND s.value = 6791;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061307 WHERE b.weenie_Class_Id = 1054730 AND s.value = 6792;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061308 WHERE b.weenie_Class_Id = 1054730 AND s.value = 6793;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061309 WHERE b.weenie_Class_Id = 1054730 AND s.value = 6794;

-- Choice Brined Steak of Thievery: WCID 1054731 -> 1061310-1061319 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061310 WHERE b.weenie_Class_Id = 1054731 AND s.value = 6825;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061311 WHERE b.weenie_Class_Id = 1054731 AND s.value = 6826;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061312 WHERE b.weenie_Class_Id = 1054731 AND s.value = 6827;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061313 WHERE b.weenie_Class_Id = 1054731 AND s.value = 6828;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061314 WHERE b.weenie_Class_Id = 1054731 AND s.value = 6829;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061315 WHERE b.weenie_Class_Id = 1054731 AND s.value = 6830;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061316 WHERE b.weenie_Class_Id = 1054731 AND s.value = 6831;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061317 WHERE b.weenie_Class_Id = 1054731 AND s.value = 6832;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061318 WHERE b.weenie_Class_Id = 1054731 AND s.value = 6833;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061319 WHERE b.weenie_Class_Id = 1054731 AND s.value = 6834;

-- Health Draught of Armor: WCID 1054732 -> 1061320-1061329 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061320 WHERE b.weenie_Class_Id = 1054732 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061321 WHERE b.weenie_Class_Id = 1054732 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061322 WHERE b.weenie_Class_Id = 1054732 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061323 WHERE b.weenie_Class_Id = 1054732 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061324 WHERE b.weenie_Class_Id = 1054732 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061325 WHERE b.weenie_Class_Id = 1054732 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061326 WHERE b.weenie_Class_Id = 1054732 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061327 WHERE b.weenie_Class_Id = 1054732 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061328 WHERE b.weenie_Class_Id = 1054732 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061329 WHERE b.weenie_Class_Id = 1054732 AND s.value = 6592;

-- Health Draught of Warding: WCID 1054733 -> 1061330-1061339 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061330 WHERE b.weenie_Class_Id = 1054733 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061331 WHERE b.weenie_Class_Id = 1054733 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061332 WHERE b.weenie_Class_Id = 1054733 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061333 WHERE b.weenie_Class_Id = 1054733 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061334 WHERE b.weenie_Class_Id = 1054733 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061335 WHERE b.weenie_Class_Id = 1054733 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061336 WHERE b.weenie_Class_Id = 1054733 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061337 WHERE b.weenie_Class_Id = 1054733 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061338 WHERE b.weenie_Class_Id = 1054733 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061339 WHERE b.weenie_Class_Id = 1054733 AND s.value = 6602;

-- Health Draught of Regeneration: WCID 1054734 -> 1061340-1061349 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061340 WHERE b.weenie_Class_Id = 1054734 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061341 WHERE b.weenie_Class_Id = 1054734 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061342 WHERE b.weenie_Class_Id = 1054734 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061343 WHERE b.weenie_Class_Id = 1054734 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061344 WHERE b.weenie_Class_Id = 1054734 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061345 WHERE b.weenie_Class_Id = 1054734 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061346 WHERE b.weenie_Class_Id = 1054734 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061347 WHERE b.weenie_Class_Id = 1054734 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061348 WHERE b.weenie_Class_Id = 1054734 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061349 WHERE b.weenie_Class_Id = 1054734 AND s.value = 6694;

-- Health Draught of Rejuvenation: WCID 1054735 -> 1061350-1061359 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061350 WHERE b.weenie_Class_Id = 1054735 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061351 WHERE b.weenie_Class_Id = 1054735 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061352 WHERE b.weenie_Class_Id = 1054735 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061353 WHERE b.weenie_Class_Id = 1054735 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061354 WHERE b.weenie_Class_Id = 1054735 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061355 WHERE b.weenie_Class_Id = 1054735 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061356 WHERE b.weenie_Class_Id = 1054735 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061357 WHERE b.weenie_Class_Id = 1054735 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061358 WHERE b.weenie_Class_Id = 1054735 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061359 WHERE b.weenie_Class_Id = 1054735 AND s.value = 6704;

-- Health Draught of Clarity: WCID 1054736 -> 1061360-1061369 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061360 WHERE b.weenie_Class_Id = 1054736 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061361 WHERE b.weenie_Class_Id = 1054736 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061362 WHERE b.weenie_Class_Id = 1054736 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061363 WHERE b.weenie_Class_Id = 1054736 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061364 WHERE b.weenie_Class_Id = 1054736 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061365 WHERE b.weenie_Class_Id = 1054736 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061366 WHERE b.weenie_Class_Id = 1054736 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061367 WHERE b.weenie_Class_Id = 1054736 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061368 WHERE b.weenie_Class_Id = 1054736 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061369 WHERE b.weenie_Class_Id = 1054736 AND s.value = 6714;

-- Health Draught of Blood Drinker: WCID 1054737 -> 1061370-1061379 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061370 WHERE b.weenie_Class_Id = 1054737 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061371 WHERE b.weenie_Class_Id = 1054737 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061372 WHERE b.weenie_Class_Id = 1054737 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061373 WHERE b.weenie_Class_Id = 1054737 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061374 WHERE b.weenie_Class_Id = 1054737 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061375 WHERE b.weenie_Class_Id = 1054737 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061376 WHERE b.weenie_Class_Id = 1054737 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061377 WHERE b.weenie_Class_Id = 1054737 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061378 WHERE b.weenie_Class_Id = 1054737 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061379 WHERE b.weenie_Class_Id = 1054737 AND s.value = 6744;

-- Health Draught of Spirit Drinker: WCID 1054738 -> 1061380-1061389 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061380 WHERE b.weenie_Class_Id = 1054738 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061381 WHERE b.weenie_Class_Id = 1054738 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061382 WHERE b.weenie_Class_Id = 1054738 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061383 WHERE b.weenie_Class_Id = 1054738 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061384 WHERE b.weenie_Class_Id = 1054738 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061385 WHERE b.weenie_Class_Id = 1054738 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061386 WHERE b.weenie_Class_Id = 1054738 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061387 WHERE b.weenie_Class_Id = 1054738 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061388 WHERE b.weenie_Class_Id = 1054738 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061389 WHERE b.weenie_Class_Id = 1054738 AND s.value = 6774;

-- Health Draught of Heart Seeker: WCID 1054739 -> 1061390-1061399 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061390 WHERE b.weenie_Class_Id = 1054739 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061391 WHERE b.weenie_Class_Id = 1054739 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061392 WHERE b.weenie_Class_Id = 1054739 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061393 WHERE b.weenie_Class_Id = 1054739 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061394 WHERE b.weenie_Class_Id = 1054739 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061395 WHERE b.weenie_Class_Id = 1054739 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061396 WHERE b.weenie_Class_Id = 1054739 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061397 WHERE b.weenie_Class_Id = 1054739 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061398 WHERE b.weenie_Class_Id = 1054739 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061399 WHERE b.weenie_Class_Id = 1054739 AND s.value = 6754;

-- Health Draught of Swift Killer: WCID 1054740 -> 1061400-1061409 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061400 WHERE b.weenie_Class_Id = 1054740 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061401 WHERE b.weenie_Class_Id = 1054740 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061402 WHERE b.weenie_Class_Id = 1054740 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061403 WHERE b.weenie_Class_Id = 1054740 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061404 WHERE b.weenie_Class_Id = 1054740 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061405 WHERE b.weenie_Class_Id = 1054740 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061406 WHERE b.weenie_Class_Id = 1054740 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061407 WHERE b.weenie_Class_Id = 1054740 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061408 WHERE b.weenie_Class_Id = 1054740 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061409 WHERE b.weenie_Class_Id = 1054740 AND s.value = 6764;

-- Health Draught of Defender: WCID 1054741 -> 1061410-1061419 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061410 WHERE b.weenie_Class_Id = 1054741 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061411 WHERE b.weenie_Class_Id = 1054741 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061412 WHERE b.weenie_Class_Id = 1054741 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061413 WHERE b.weenie_Class_Id = 1054741 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061414 WHERE b.weenie_Class_Id = 1054741 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061415 WHERE b.weenie_Class_Id = 1054741 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061416 WHERE b.weenie_Class_Id = 1054741 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061417 WHERE b.weenie_Class_Id = 1054741 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061418 WHERE b.weenie_Class_Id = 1054741 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061419 WHERE b.weenie_Class_Id = 1054741 AND s.value = 6784;

-- Health Draught of Critical Chance: WCID 1054742 -> 1061420-1061429 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061420 WHERE b.weenie_Class_Id = 1054742 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061421 WHERE b.weenie_Class_Id = 1054742 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061422 WHERE b.weenie_Class_Id = 1054742 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061423 WHERE b.weenie_Class_Id = 1054742 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061424 WHERE b.weenie_Class_Id = 1054742 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061425 WHERE b.weenie_Class_Id = 1054742 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061426 WHERE b.weenie_Class_Id = 1054742 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061427 WHERE b.weenie_Class_Id = 1054742 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061428 WHERE b.weenie_Class_Id = 1054742 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061429 WHERE b.weenie_Class_Id = 1054742 AND s.value = 6724;

-- Health Draught of Critical Damage: WCID 1054743 -> 1061430-1061439 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061430 WHERE b.weenie_Class_Id = 1054743 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061431 WHERE b.weenie_Class_Id = 1054743 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061432 WHERE b.weenie_Class_Id = 1054743 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061433 WHERE b.weenie_Class_Id = 1054743 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061434 WHERE b.weenie_Class_Id = 1054743 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061435 WHERE b.weenie_Class_Id = 1054743 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061436 WHERE b.weenie_Class_Id = 1054743 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061437 WHERE b.weenie_Class_Id = 1054743 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061438 WHERE b.weenie_Class_Id = 1054743 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061439 WHERE b.weenie_Class_Id = 1054743 AND s.value = 6734;

-- Health Draught of Slashing Protection: WCID 1054744 -> 1061440-1061449 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061440 WHERE b.weenie_Class_Id = 1054744 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061441 WHERE b.weenie_Class_Id = 1054744 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061442 WHERE b.weenie_Class_Id = 1054744 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061443 WHERE b.weenie_Class_Id = 1054744 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061444 WHERE b.weenie_Class_Id = 1054744 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061445 WHERE b.weenie_Class_Id = 1054744 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061446 WHERE b.weenie_Class_Id = 1054744 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061447 WHERE b.weenie_Class_Id = 1054744 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061448 WHERE b.weenie_Class_Id = 1054744 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061449 WHERE b.weenie_Class_Id = 1054744 AND s.value = 6612;

-- Health Draught of Piercing Protection: WCID 1054745 -> 1061450-1061459 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061450 WHERE b.weenie_Class_Id = 1054745 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061451 WHERE b.weenie_Class_Id = 1054745 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061452 WHERE b.weenie_Class_Id = 1054745 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061453 WHERE b.weenie_Class_Id = 1054745 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061454 WHERE b.weenie_Class_Id = 1054745 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061455 WHERE b.weenie_Class_Id = 1054745 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061456 WHERE b.weenie_Class_Id = 1054745 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061457 WHERE b.weenie_Class_Id = 1054745 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061458 WHERE b.weenie_Class_Id = 1054745 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061459 WHERE b.weenie_Class_Id = 1054745 AND s.value = 6622;

-- Health Draught of Bludgeoning Protection: WCID 1054746 -> 1061460-1061469 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061460 WHERE b.weenie_Class_Id = 1054746 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061461 WHERE b.weenie_Class_Id = 1054746 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061462 WHERE b.weenie_Class_Id = 1054746 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061463 WHERE b.weenie_Class_Id = 1054746 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061464 WHERE b.weenie_Class_Id = 1054746 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061465 WHERE b.weenie_Class_Id = 1054746 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061466 WHERE b.weenie_Class_Id = 1054746 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061467 WHERE b.weenie_Class_Id = 1054746 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061468 WHERE b.weenie_Class_Id = 1054746 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061469 WHERE b.weenie_Class_Id = 1054746 AND s.value = 6632;

-- Health Draught of Acid Protection: WCID 1054747 -> 1061470-1061479 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061470 WHERE b.weenie_Class_Id = 1054747 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061471 WHERE b.weenie_Class_Id = 1054747 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061472 WHERE b.weenie_Class_Id = 1054747 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061473 WHERE b.weenie_Class_Id = 1054747 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061474 WHERE b.weenie_Class_Id = 1054747 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061475 WHERE b.weenie_Class_Id = 1054747 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061476 WHERE b.weenie_Class_Id = 1054747 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061477 WHERE b.weenie_Class_Id = 1054747 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061478 WHERE b.weenie_Class_Id = 1054747 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061479 WHERE b.weenie_Class_Id = 1054747 AND s.value = 6642;

-- Health Draught of Fire Protection: WCID 1054748 -> 1061480-1061489 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061480 WHERE b.weenie_Class_Id = 1054748 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061481 WHERE b.weenie_Class_Id = 1054748 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061482 WHERE b.weenie_Class_Id = 1054748 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061483 WHERE b.weenie_Class_Id = 1054748 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061484 WHERE b.weenie_Class_Id = 1054748 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061485 WHERE b.weenie_Class_Id = 1054748 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061486 WHERE b.weenie_Class_Id = 1054748 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061487 WHERE b.weenie_Class_Id = 1054748 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061488 WHERE b.weenie_Class_Id = 1054748 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061489 WHERE b.weenie_Class_Id = 1054748 AND s.value = 6652;

-- Health Draught of Cold Protection: WCID 1054749 -> 1061490-1061499 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061490 WHERE b.weenie_Class_Id = 1054749 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061491 WHERE b.weenie_Class_Id = 1054749 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061492 WHERE b.weenie_Class_Id = 1054749 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061493 WHERE b.weenie_Class_Id = 1054749 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061494 WHERE b.weenie_Class_Id = 1054749 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061495 WHERE b.weenie_Class_Id = 1054749 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061496 WHERE b.weenie_Class_Id = 1054749 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061497 WHERE b.weenie_Class_Id = 1054749 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061498 WHERE b.weenie_Class_Id = 1054749 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061499 WHERE b.weenie_Class_Id = 1054749 AND s.value = 6662;

-- Health Draught of Lightning Protection: WCID 1054750 -> 1061500-1061509 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061500 WHERE b.weenie_Class_Id = 1054750 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061501 WHERE b.weenie_Class_Id = 1054750 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061502 WHERE b.weenie_Class_Id = 1054750 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061503 WHERE b.weenie_Class_Id = 1054750 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061504 WHERE b.weenie_Class_Id = 1054750 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061505 WHERE b.weenie_Class_Id = 1054750 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061506 WHERE b.weenie_Class_Id = 1054750 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061507 WHERE b.weenie_Class_Id = 1054750 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061508 WHERE b.weenie_Class_Id = 1054750 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061509 WHERE b.weenie_Class_Id = 1054750 AND s.value = 6672;

-- Stamina Draught of Armor: WCID 1054751 -> 1061510-1061519 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061510 WHERE b.weenie_Class_Id = 1054751 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061511 WHERE b.weenie_Class_Id = 1054751 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061512 WHERE b.weenie_Class_Id = 1054751 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061513 WHERE b.weenie_Class_Id = 1054751 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061514 WHERE b.weenie_Class_Id = 1054751 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061515 WHERE b.weenie_Class_Id = 1054751 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061516 WHERE b.weenie_Class_Id = 1054751 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061517 WHERE b.weenie_Class_Id = 1054751 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061518 WHERE b.weenie_Class_Id = 1054751 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061519 WHERE b.weenie_Class_Id = 1054751 AND s.value = 6592;

-- Stamina Draught of Warding: WCID 1054752 -> 1061520-1061529 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061520 WHERE b.weenie_Class_Id = 1054752 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061521 WHERE b.weenie_Class_Id = 1054752 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061522 WHERE b.weenie_Class_Id = 1054752 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061523 WHERE b.weenie_Class_Id = 1054752 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061524 WHERE b.weenie_Class_Id = 1054752 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061525 WHERE b.weenie_Class_Id = 1054752 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061526 WHERE b.weenie_Class_Id = 1054752 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061527 WHERE b.weenie_Class_Id = 1054752 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061528 WHERE b.weenie_Class_Id = 1054752 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061529 WHERE b.weenie_Class_Id = 1054752 AND s.value = 6602;

-- Stamina Draught of Regeneration: WCID 1054753 -> 1061530-1061539 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061530 WHERE b.weenie_Class_Id = 1054753 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061531 WHERE b.weenie_Class_Id = 1054753 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061532 WHERE b.weenie_Class_Id = 1054753 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061533 WHERE b.weenie_Class_Id = 1054753 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061534 WHERE b.weenie_Class_Id = 1054753 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061535 WHERE b.weenie_Class_Id = 1054753 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061536 WHERE b.weenie_Class_Id = 1054753 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061537 WHERE b.weenie_Class_Id = 1054753 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061538 WHERE b.weenie_Class_Id = 1054753 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061539 WHERE b.weenie_Class_Id = 1054753 AND s.value = 6694;

-- Stamina Draught of Rejuvenation: WCID 1054754 -> 1061540-1061549 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061540 WHERE b.weenie_Class_Id = 1054754 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061541 WHERE b.weenie_Class_Id = 1054754 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061542 WHERE b.weenie_Class_Id = 1054754 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061543 WHERE b.weenie_Class_Id = 1054754 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061544 WHERE b.weenie_Class_Id = 1054754 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061545 WHERE b.weenie_Class_Id = 1054754 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061546 WHERE b.weenie_Class_Id = 1054754 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061547 WHERE b.weenie_Class_Id = 1054754 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061548 WHERE b.weenie_Class_Id = 1054754 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061549 WHERE b.weenie_Class_Id = 1054754 AND s.value = 6704;

-- Stamina Draught of Clarity: WCID 1054755 -> 1061550-1061559 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061550 WHERE b.weenie_Class_Id = 1054755 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061551 WHERE b.weenie_Class_Id = 1054755 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061552 WHERE b.weenie_Class_Id = 1054755 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061553 WHERE b.weenie_Class_Id = 1054755 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061554 WHERE b.weenie_Class_Id = 1054755 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061555 WHERE b.weenie_Class_Id = 1054755 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061556 WHERE b.weenie_Class_Id = 1054755 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061557 WHERE b.weenie_Class_Id = 1054755 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061558 WHERE b.weenie_Class_Id = 1054755 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061559 WHERE b.weenie_Class_Id = 1054755 AND s.value = 6714;

-- Stamina Draught of Blood Drinker: WCID 1054756 -> 1061560-1061569 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061560 WHERE b.weenie_Class_Id = 1054756 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061561 WHERE b.weenie_Class_Id = 1054756 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061562 WHERE b.weenie_Class_Id = 1054756 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061563 WHERE b.weenie_Class_Id = 1054756 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061564 WHERE b.weenie_Class_Id = 1054756 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061565 WHERE b.weenie_Class_Id = 1054756 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061566 WHERE b.weenie_Class_Id = 1054756 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061567 WHERE b.weenie_Class_Id = 1054756 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061568 WHERE b.weenie_Class_Id = 1054756 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061569 WHERE b.weenie_Class_Id = 1054756 AND s.value = 6744;

-- Stamina Draught of Spirit Drinker: WCID 1054757 -> 1061570-1061579 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061570 WHERE b.weenie_Class_Id = 1054757 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061571 WHERE b.weenie_Class_Id = 1054757 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061572 WHERE b.weenie_Class_Id = 1054757 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061573 WHERE b.weenie_Class_Id = 1054757 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061574 WHERE b.weenie_Class_Id = 1054757 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061575 WHERE b.weenie_Class_Id = 1054757 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061576 WHERE b.weenie_Class_Id = 1054757 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061577 WHERE b.weenie_Class_Id = 1054757 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061578 WHERE b.weenie_Class_Id = 1054757 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061579 WHERE b.weenie_Class_Id = 1054757 AND s.value = 6774;

-- Stamina Draught of Heart Seeker: WCID 1054758 -> 1061580-1061589 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061580 WHERE b.weenie_Class_Id = 1054758 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061581 WHERE b.weenie_Class_Id = 1054758 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061582 WHERE b.weenie_Class_Id = 1054758 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061583 WHERE b.weenie_Class_Id = 1054758 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061584 WHERE b.weenie_Class_Id = 1054758 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061585 WHERE b.weenie_Class_Id = 1054758 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061586 WHERE b.weenie_Class_Id = 1054758 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061587 WHERE b.weenie_Class_Id = 1054758 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061588 WHERE b.weenie_Class_Id = 1054758 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061589 WHERE b.weenie_Class_Id = 1054758 AND s.value = 6754;

-- Stamina Draught of Swift Killer: WCID 1054759 -> 1061590-1061599 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061590 WHERE b.weenie_Class_Id = 1054759 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061591 WHERE b.weenie_Class_Id = 1054759 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061592 WHERE b.weenie_Class_Id = 1054759 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061593 WHERE b.weenie_Class_Id = 1054759 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061594 WHERE b.weenie_Class_Id = 1054759 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061595 WHERE b.weenie_Class_Id = 1054759 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061596 WHERE b.weenie_Class_Id = 1054759 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061597 WHERE b.weenie_Class_Id = 1054759 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061598 WHERE b.weenie_Class_Id = 1054759 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061599 WHERE b.weenie_Class_Id = 1054759 AND s.value = 6764;

-- Stamina Draught of Defender: WCID 1054760 -> 1061600-1061609 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061600 WHERE b.weenie_Class_Id = 1054760 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061601 WHERE b.weenie_Class_Id = 1054760 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061602 WHERE b.weenie_Class_Id = 1054760 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061603 WHERE b.weenie_Class_Id = 1054760 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061604 WHERE b.weenie_Class_Id = 1054760 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061605 WHERE b.weenie_Class_Id = 1054760 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061606 WHERE b.weenie_Class_Id = 1054760 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061607 WHERE b.weenie_Class_Id = 1054760 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061608 WHERE b.weenie_Class_Id = 1054760 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061609 WHERE b.weenie_Class_Id = 1054760 AND s.value = 6784;

-- Stamina Draught of Critical Chance: WCID 1054761 -> 1061610-1061619 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061610 WHERE b.weenie_Class_Id = 1054761 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061611 WHERE b.weenie_Class_Id = 1054761 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061612 WHERE b.weenie_Class_Id = 1054761 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061613 WHERE b.weenie_Class_Id = 1054761 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061614 WHERE b.weenie_Class_Id = 1054761 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061615 WHERE b.weenie_Class_Id = 1054761 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061616 WHERE b.weenie_Class_Id = 1054761 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061617 WHERE b.weenie_Class_Id = 1054761 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061618 WHERE b.weenie_Class_Id = 1054761 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061619 WHERE b.weenie_Class_Id = 1054761 AND s.value = 6724;

-- Stamina Draught of Critical Damage: WCID 1054762 -> 1061620-1061629 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061620 WHERE b.weenie_Class_Id = 1054762 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061621 WHERE b.weenie_Class_Id = 1054762 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061622 WHERE b.weenie_Class_Id = 1054762 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061623 WHERE b.weenie_Class_Id = 1054762 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061624 WHERE b.weenie_Class_Id = 1054762 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061625 WHERE b.weenie_Class_Id = 1054762 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061626 WHERE b.weenie_Class_Id = 1054762 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061627 WHERE b.weenie_Class_Id = 1054762 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061628 WHERE b.weenie_Class_Id = 1054762 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061629 WHERE b.weenie_Class_Id = 1054762 AND s.value = 6734;

-- Stamina Draught of Slashing Protection: WCID 1054763 -> 1061630-1061639 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061630 WHERE b.weenie_Class_Id = 1054763 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061631 WHERE b.weenie_Class_Id = 1054763 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061632 WHERE b.weenie_Class_Id = 1054763 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061633 WHERE b.weenie_Class_Id = 1054763 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061634 WHERE b.weenie_Class_Id = 1054763 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061635 WHERE b.weenie_Class_Id = 1054763 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061636 WHERE b.weenie_Class_Id = 1054763 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061637 WHERE b.weenie_Class_Id = 1054763 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061638 WHERE b.weenie_Class_Id = 1054763 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061639 WHERE b.weenie_Class_Id = 1054763 AND s.value = 6612;

-- Stamina Draught of Piercing Protection: WCID 1054764 -> 1061640-1061649 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061640 WHERE b.weenie_Class_Id = 1054764 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061641 WHERE b.weenie_Class_Id = 1054764 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061642 WHERE b.weenie_Class_Id = 1054764 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061643 WHERE b.weenie_Class_Id = 1054764 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061644 WHERE b.weenie_Class_Id = 1054764 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061645 WHERE b.weenie_Class_Id = 1054764 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061646 WHERE b.weenie_Class_Id = 1054764 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061647 WHERE b.weenie_Class_Id = 1054764 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061648 WHERE b.weenie_Class_Id = 1054764 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061649 WHERE b.weenie_Class_Id = 1054764 AND s.value = 6622;

-- Stamina Draught of Bludgeoning Protection: WCID 1054765 -> 1061650-1061659 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061650 WHERE b.weenie_Class_Id = 1054765 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061651 WHERE b.weenie_Class_Id = 1054765 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061652 WHERE b.weenie_Class_Id = 1054765 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061653 WHERE b.weenie_Class_Id = 1054765 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061654 WHERE b.weenie_Class_Id = 1054765 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061655 WHERE b.weenie_Class_Id = 1054765 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061656 WHERE b.weenie_Class_Id = 1054765 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061657 WHERE b.weenie_Class_Id = 1054765 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061658 WHERE b.weenie_Class_Id = 1054765 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061659 WHERE b.weenie_Class_Id = 1054765 AND s.value = 6632;

-- Stamina Draught of Acid Protection: WCID 1054766 -> 1061660-1061669 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061660 WHERE b.weenie_Class_Id = 1054766 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061661 WHERE b.weenie_Class_Id = 1054766 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061662 WHERE b.weenie_Class_Id = 1054766 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061663 WHERE b.weenie_Class_Id = 1054766 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061664 WHERE b.weenie_Class_Id = 1054766 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061665 WHERE b.weenie_Class_Id = 1054766 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061666 WHERE b.weenie_Class_Id = 1054766 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061667 WHERE b.weenie_Class_Id = 1054766 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061668 WHERE b.weenie_Class_Id = 1054766 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061669 WHERE b.weenie_Class_Id = 1054766 AND s.value = 6642;

-- Stamina Draught of Fire Protection: WCID 1054767 -> 1061670-1061679 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061670 WHERE b.weenie_Class_Id = 1054767 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061671 WHERE b.weenie_Class_Id = 1054767 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061672 WHERE b.weenie_Class_Id = 1054767 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061673 WHERE b.weenie_Class_Id = 1054767 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061674 WHERE b.weenie_Class_Id = 1054767 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061675 WHERE b.weenie_Class_Id = 1054767 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061676 WHERE b.weenie_Class_Id = 1054767 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061677 WHERE b.weenie_Class_Id = 1054767 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061678 WHERE b.weenie_Class_Id = 1054767 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061679 WHERE b.weenie_Class_Id = 1054767 AND s.value = 6652;

-- Stamina Draught of Cold Protection: WCID 1054768 -> 1061680-1061689 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061680 WHERE b.weenie_Class_Id = 1054768 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061681 WHERE b.weenie_Class_Id = 1054768 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061682 WHERE b.weenie_Class_Id = 1054768 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061683 WHERE b.weenie_Class_Id = 1054768 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061684 WHERE b.weenie_Class_Id = 1054768 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061685 WHERE b.weenie_Class_Id = 1054768 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061686 WHERE b.weenie_Class_Id = 1054768 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061687 WHERE b.weenie_Class_Id = 1054768 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061688 WHERE b.weenie_Class_Id = 1054768 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061689 WHERE b.weenie_Class_Id = 1054768 AND s.value = 6662;

-- Stamina Draught of Lightning Protection: WCID 1054769 -> 1061690-1061699 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061690 WHERE b.weenie_Class_Id = 1054769 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061691 WHERE b.weenie_Class_Id = 1054769 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061692 WHERE b.weenie_Class_Id = 1054769 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061693 WHERE b.weenie_Class_Id = 1054769 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061694 WHERE b.weenie_Class_Id = 1054769 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061695 WHERE b.weenie_Class_Id = 1054769 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061696 WHERE b.weenie_Class_Id = 1054769 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061697 WHERE b.weenie_Class_Id = 1054769 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061698 WHERE b.weenie_Class_Id = 1054769 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061699 WHERE b.weenie_Class_Id = 1054769 AND s.value = 6672;

-- Mana Draught of Armor: WCID 1054770 -> 1061700-1061709 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061700 WHERE b.weenie_Class_Id = 1054770 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061701 WHERE b.weenie_Class_Id = 1054770 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061702 WHERE b.weenie_Class_Id = 1054770 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061703 WHERE b.weenie_Class_Id = 1054770 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061704 WHERE b.weenie_Class_Id = 1054770 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061705 WHERE b.weenie_Class_Id = 1054770 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061706 WHERE b.weenie_Class_Id = 1054770 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061707 WHERE b.weenie_Class_Id = 1054770 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061708 WHERE b.weenie_Class_Id = 1054770 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061709 WHERE b.weenie_Class_Id = 1054770 AND s.value = 6592;

-- Mana Draught of Warding: WCID 1054771 -> 1061710-1061719 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061710 WHERE b.weenie_Class_Id = 1054771 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061711 WHERE b.weenie_Class_Id = 1054771 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061712 WHERE b.weenie_Class_Id = 1054771 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061713 WHERE b.weenie_Class_Id = 1054771 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061714 WHERE b.weenie_Class_Id = 1054771 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061715 WHERE b.weenie_Class_Id = 1054771 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061716 WHERE b.weenie_Class_Id = 1054771 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061717 WHERE b.weenie_Class_Id = 1054771 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061718 WHERE b.weenie_Class_Id = 1054771 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061719 WHERE b.weenie_Class_Id = 1054771 AND s.value = 6602;

-- Mana Draught of Regeneration: WCID 1054772 -> 1061720-1061729 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061720 WHERE b.weenie_Class_Id = 1054772 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061721 WHERE b.weenie_Class_Id = 1054772 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061722 WHERE b.weenie_Class_Id = 1054772 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061723 WHERE b.weenie_Class_Id = 1054772 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061724 WHERE b.weenie_Class_Id = 1054772 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061725 WHERE b.weenie_Class_Id = 1054772 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061726 WHERE b.weenie_Class_Id = 1054772 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061727 WHERE b.weenie_Class_Id = 1054772 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061728 WHERE b.weenie_Class_Id = 1054772 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061729 WHERE b.weenie_Class_Id = 1054772 AND s.value = 6694;

-- Mana Draught of Rejuvenation: WCID 1054773 -> 1061730-1061739 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061730 WHERE b.weenie_Class_Id = 1054773 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061731 WHERE b.weenie_Class_Id = 1054773 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061732 WHERE b.weenie_Class_Id = 1054773 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061733 WHERE b.weenie_Class_Id = 1054773 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061734 WHERE b.weenie_Class_Id = 1054773 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061735 WHERE b.weenie_Class_Id = 1054773 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061736 WHERE b.weenie_Class_Id = 1054773 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061737 WHERE b.weenie_Class_Id = 1054773 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061738 WHERE b.weenie_Class_Id = 1054773 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061739 WHERE b.weenie_Class_Id = 1054773 AND s.value = 6704;

-- Mana Draught of Clarity: WCID 1054774 -> 1061740-1061749 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061740 WHERE b.weenie_Class_Id = 1054774 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061741 WHERE b.weenie_Class_Id = 1054774 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061742 WHERE b.weenie_Class_Id = 1054774 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061743 WHERE b.weenie_Class_Id = 1054774 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061744 WHERE b.weenie_Class_Id = 1054774 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061745 WHERE b.weenie_Class_Id = 1054774 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061746 WHERE b.weenie_Class_Id = 1054774 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061747 WHERE b.weenie_Class_Id = 1054774 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061748 WHERE b.weenie_Class_Id = 1054774 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061749 WHERE b.weenie_Class_Id = 1054774 AND s.value = 6714;

-- Mana Draught of Blood Drinker: WCID 1054775 -> 1061750-1061759 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061750 WHERE b.weenie_Class_Id = 1054775 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061751 WHERE b.weenie_Class_Id = 1054775 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061752 WHERE b.weenie_Class_Id = 1054775 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061753 WHERE b.weenie_Class_Id = 1054775 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061754 WHERE b.weenie_Class_Id = 1054775 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061755 WHERE b.weenie_Class_Id = 1054775 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061756 WHERE b.weenie_Class_Id = 1054775 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061757 WHERE b.weenie_Class_Id = 1054775 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061758 WHERE b.weenie_Class_Id = 1054775 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061759 WHERE b.weenie_Class_Id = 1054775 AND s.value = 6744;

-- Mana Draught of Spirit Drinker: WCID 1054776 -> 1061760-1061769 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061760 WHERE b.weenie_Class_Id = 1054776 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061761 WHERE b.weenie_Class_Id = 1054776 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061762 WHERE b.weenie_Class_Id = 1054776 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061763 WHERE b.weenie_Class_Id = 1054776 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061764 WHERE b.weenie_Class_Id = 1054776 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061765 WHERE b.weenie_Class_Id = 1054776 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061766 WHERE b.weenie_Class_Id = 1054776 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061767 WHERE b.weenie_Class_Id = 1054776 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061768 WHERE b.weenie_Class_Id = 1054776 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061769 WHERE b.weenie_Class_Id = 1054776 AND s.value = 6774;

-- Mana Draught of Heart Seeker: WCID 1054777 -> 1061770-1061779 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061770 WHERE b.weenie_Class_Id = 1054777 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061771 WHERE b.weenie_Class_Id = 1054777 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061772 WHERE b.weenie_Class_Id = 1054777 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061773 WHERE b.weenie_Class_Id = 1054777 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061774 WHERE b.weenie_Class_Id = 1054777 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061775 WHERE b.weenie_Class_Id = 1054777 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061776 WHERE b.weenie_Class_Id = 1054777 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061777 WHERE b.weenie_Class_Id = 1054777 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061778 WHERE b.weenie_Class_Id = 1054777 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061779 WHERE b.weenie_Class_Id = 1054777 AND s.value = 6754;

-- Mana Draught of Swift Killer: WCID 1054778 -> 1061780-1061789 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061780 WHERE b.weenie_Class_Id = 1054778 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061781 WHERE b.weenie_Class_Id = 1054778 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061782 WHERE b.weenie_Class_Id = 1054778 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061783 WHERE b.weenie_Class_Id = 1054778 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061784 WHERE b.weenie_Class_Id = 1054778 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061785 WHERE b.weenie_Class_Id = 1054778 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061786 WHERE b.weenie_Class_Id = 1054778 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061787 WHERE b.weenie_Class_Id = 1054778 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061788 WHERE b.weenie_Class_Id = 1054778 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061789 WHERE b.weenie_Class_Id = 1054778 AND s.value = 6764;

-- Mana Draught of Defender: WCID 1054779 -> 1061790-1061799 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061790 WHERE b.weenie_Class_Id = 1054779 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061791 WHERE b.weenie_Class_Id = 1054779 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061792 WHERE b.weenie_Class_Id = 1054779 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061793 WHERE b.weenie_Class_Id = 1054779 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061794 WHERE b.weenie_Class_Id = 1054779 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061795 WHERE b.weenie_Class_Id = 1054779 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061796 WHERE b.weenie_Class_Id = 1054779 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061797 WHERE b.weenie_Class_Id = 1054779 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061798 WHERE b.weenie_Class_Id = 1054779 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061799 WHERE b.weenie_Class_Id = 1054779 AND s.value = 6784;

-- Mana Draught of Critical Chance: WCID 1054780 -> 1061800-1061809 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061800 WHERE b.weenie_Class_Id = 1054780 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061801 WHERE b.weenie_Class_Id = 1054780 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061802 WHERE b.weenie_Class_Id = 1054780 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061803 WHERE b.weenie_Class_Id = 1054780 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061804 WHERE b.weenie_Class_Id = 1054780 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061805 WHERE b.weenie_Class_Id = 1054780 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061806 WHERE b.weenie_Class_Id = 1054780 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061807 WHERE b.weenie_Class_Id = 1054780 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061808 WHERE b.weenie_Class_Id = 1054780 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061809 WHERE b.weenie_Class_Id = 1054780 AND s.value = 6724;

-- Mana Draught of Critical Damage: WCID 1054781 -> 1061810-1061819 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061810 WHERE b.weenie_Class_Id = 1054781 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061811 WHERE b.weenie_Class_Id = 1054781 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061812 WHERE b.weenie_Class_Id = 1054781 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061813 WHERE b.weenie_Class_Id = 1054781 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061814 WHERE b.weenie_Class_Id = 1054781 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061815 WHERE b.weenie_Class_Id = 1054781 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061816 WHERE b.weenie_Class_Id = 1054781 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061817 WHERE b.weenie_Class_Id = 1054781 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061818 WHERE b.weenie_Class_Id = 1054781 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061819 WHERE b.weenie_Class_Id = 1054781 AND s.value = 6734;

-- Mana Draught of Slashing Protection: WCID 1054782 -> 1061820-1061829 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061820 WHERE b.weenie_Class_Id = 1054782 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061821 WHERE b.weenie_Class_Id = 1054782 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061822 WHERE b.weenie_Class_Id = 1054782 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061823 WHERE b.weenie_Class_Id = 1054782 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061824 WHERE b.weenie_Class_Id = 1054782 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061825 WHERE b.weenie_Class_Id = 1054782 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061826 WHERE b.weenie_Class_Id = 1054782 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061827 WHERE b.weenie_Class_Id = 1054782 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061828 WHERE b.weenie_Class_Id = 1054782 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061829 WHERE b.weenie_Class_Id = 1054782 AND s.value = 6612;

-- Mana Draught of Piercing Protection: WCID 1054783 -> 1061830-1061839 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061830 WHERE b.weenie_Class_Id = 1054783 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061831 WHERE b.weenie_Class_Id = 1054783 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061832 WHERE b.weenie_Class_Id = 1054783 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061833 WHERE b.weenie_Class_Id = 1054783 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061834 WHERE b.weenie_Class_Id = 1054783 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061835 WHERE b.weenie_Class_Id = 1054783 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061836 WHERE b.weenie_Class_Id = 1054783 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061837 WHERE b.weenie_Class_Id = 1054783 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061838 WHERE b.weenie_Class_Id = 1054783 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061839 WHERE b.weenie_Class_Id = 1054783 AND s.value = 6622;

-- Mana Draught of Bludgeoning Protection: WCID 1054784 -> 1061840-1061849 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061840 WHERE b.weenie_Class_Id = 1054784 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061841 WHERE b.weenie_Class_Id = 1054784 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061842 WHERE b.weenie_Class_Id = 1054784 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061843 WHERE b.weenie_Class_Id = 1054784 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061844 WHERE b.weenie_Class_Id = 1054784 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061845 WHERE b.weenie_Class_Id = 1054784 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061846 WHERE b.weenie_Class_Id = 1054784 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061847 WHERE b.weenie_Class_Id = 1054784 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061848 WHERE b.weenie_Class_Id = 1054784 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061849 WHERE b.weenie_Class_Id = 1054784 AND s.value = 6632;

-- Mana Draught of Acid Protection: WCID 1054785 -> 1061850-1061859 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061850 WHERE b.weenie_Class_Id = 1054785 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061851 WHERE b.weenie_Class_Id = 1054785 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061852 WHERE b.weenie_Class_Id = 1054785 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061853 WHERE b.weenie_Class_Id = 1054785 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061854 WHERE b.weenie_Class_Id = 1054785 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061855 WHERE b.weenie_Class_Id = 1054785 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061856 WHERE b.weenie_Class_Id = 1054785 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061857 WHERE b.weenie_Class_Id = 1054785 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061858 WHERE b.weenie_Class_Id = 1054785 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061859 WHERE b.weenie_Class_Id = 1054785 AND s.value = 6642;

-- Mana Draught of Fire Protection: WCID 1054786 -> 1061860-1061869 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061860 WHERE b.weenie_Class_Id = 1054786 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061861 WHERE b.weenie_Class_Id = 1054786 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061862 WHERE b.weenie_Class_Id = 1054786 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061863 WHERE b.weenie_Class_Id = 1054786 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061864 WHERE b.weenie_Class_Id = 1054786 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061865 WHERE b.weenie_Class_Id = 1054786 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061866 WHERE b.weenie_Class_Id = 1054786 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061867 WHERE b.weenie_Class_Id = 1054786 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061868 WHERE b.weenie_Class_Id = 1054786 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061869 WHERE b.weenie_Class_Id = 1054786 AND s.value = 6652;

-- Mana Draught of Cold Protection: WCID 1054787 -> 1061870-1061879 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061870 WHERE b.weenie_Class_Id = 1054787 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061871 WHERE b.weenie_Class_Id = 1054787 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061872 WHERE b.weenie_Class_Id = 1054787 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061873 WHERE b.weenie_Class_Id = 1054787 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061874 WHERE b.weenie_Class_Id = 1054787 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061875 WHERE b.weenie_Class_Id = 1054787 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061876 WHERE b.weenie_Class_Id = 1054787 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061877 WHERE b.weenie_Class_Id = 1054787 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061878 WHERE b.weenie_Class_Id = 1054787 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061879 WHERE b.weenie_Class_Id = 1054787 AND s.value = 6662;

-- Mana Draught of Lightning Protection: WCID 1054788 -> 1061880-1061889 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061880 WHERE b.weenie_Class_Id = 1054788 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061881 WHERE b.weenie_Class_Id = 1054788 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061882 WHERE b.weenie_Class_Id = 1054788 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061883 WHERE b.weenie_Class_Id = 1054788 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061884 WHERE b.weenie_Class_Id = 1054788 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061885 WHERE b.weenie_Class_Id = 1054788 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061886 WHERE b.weenie_Class_Id = 1054788 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061887 WHERE b.weenie_Class_Id = 1054788 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061888 WHERE b.weenie_Class_Id = 1054788 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061889 WHERE b.weenie_Class_Id = 1054788 AND s.value = 6672;

-- Health Potion of Armor: WCID 1054789 -> 1061890-1061899 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061890 WHERE b.weenie_Class_Id = 1054789 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061891 WHERE b.weenie_Class_Id = 1054789 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061892 WHERE b.weenie_Class_Id = 1054789 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061893 WHERE b.weenie_Class_Id = 1054789 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061894 WHERE b.weenie_Class_Id = 1054789 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061895 WHERE b.weenie_Class_Id = 1054789 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061896 WHERE b.weenie_Class_Id = 1054789 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061897 WHERE b.weenie_Class_Id = 1054789 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061898 WHERE b.weenie_Class_Id = 1054789 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061899 WHERE b.weenie_Class_Id = 1054789 AND s.value = 6592;

-- Health Potion of Warding: WCID 1054790 -> 1061900-1061909 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061900 WHERE b.weenie_Class_Id = 1054790 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061901 WHERE b.weenie_Class_Id = 1054790 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061902 WHERE b.weenie_Class_Id = 1054790 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061903 WHERE b.weenie_Class_Id = 1054790 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061904 WHERE b.weenie_Class_Id = 1054790 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061905 WHERE b.weenie_Class_Id = 1054790 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061906 WHERE b.weenie_Class_Id = 1054790 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061907 WHERE b.weenie_Class_Id = 1054790 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061908 WHERE b.weenie_Class_Id = 1054790 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061909 WHERE b.weenie_Class_Id = 1054790 AND s.value = 6602;

-- Health Potion of Regeneration: WCID 1054791 -> 1061910-1061919 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061910 WHERE b.weenie_Class_Id = 1054791 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061911 WHERE b.weenie_Class_Id = 1054791 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061912 WHERE b.weenie_Class_Id = 1054791 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061913 WHERE b.weenie_Class_Id = 1054791 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061914 WHERE b.weenie_Class_Id = 1054791 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061915 WHERE b.weenie_Class_Id = 1054791 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061916 WHERE b.weenie_Class_Id = 1054791 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061917 WHERE b.weenie_Class_Id = 1054791 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061918 WHERE b.weenie_Class_Id = 1054791 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061919 WHERE b.weenie_Class_Id = 1054791 AND s.value = 6694;

-- Health Potion of Rejuvenation: WCID 1054792 -> 1061920-1061929 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061920 WHERE b.weenie_Class_Id = 1054792 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061921 WHERE b.weenie_Class_Id = 1054792 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061922 WHERE b.weenie_Class_Id = 1054792 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061923 WHERE b.weenie_Class_Id = 1054792 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061924 WHERE b.weenie_Class_Id = 1054792 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061925 WHERE b.weenie_Class_Id = 1054792 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061926 WHERE b.weenie_Class_Id = 1054792 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061927 WHERE b.weenie_Class_Id = 1054792 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061928 WHERE b.weenie_Class_Id = 1054792 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061929 WHERE b.weenie_Class_Id = 1054792 AND s.value = 6704;

-- Health Potion of Clarity: WCID 1054793 -> 1061930-1061939 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061930 WHERE b.weenie_Class_Id = 1054793 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061931 WHERE b.weenie_Class_Id = 1054793 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061932 WHERE b.weenie_Class_Id = 1054793 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061933 WHERE b.weenie_Class_Id = 1054793 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061934 WHERE b.weenie_Class_Id = 1054793 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061935 WHERE b.weenie_Class_Id = 1054793 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061936 WHERE b.weenie_Class_Id = 1054793 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061937 WHERE b.weenie_Class_Id = 1054793 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061938 WHERE b.weenie_Class_Id = 1054793 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061939 WHERE b.weenie_Class_Id = 1054793 AND s.value = 6714;

-- Health Potion of Blood Drinker: WCID 1054794 -> 1061940-1061949 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061940 WHERE b.weenie_Class_Id = 1054794 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061941 WHERE b.weenie_Class_Id = 1054794 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061942 WHERE b.weenie_Class_Id = 1054794 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061943 WHERE b.weenie_Class_Id = 1054794 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061944 WHERE b.weenie_Class_Id = 1054794 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061945 WHERE b.weenie_Class_Id = 1054794 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061946 WHERE b.weenie_Class_Id = 1054794 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061947 WHERE b.weenie_Class_Id = 1054794 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061948 WHERE b.weenie_Class_Id = 1054794 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061949 WHERE b.weenie_Class_Id = 1054794 AND s.value = 6744;

-- Health Potion of Spirit Drinker: WCID 1054795 -> 1061950-1061959 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061950 WHERE b.weenie_Class_Id = 1054795 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061951 WHERE b.weenie_Class_Id = 1054795 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061952 WHERE b.weenie_Class_Id = 1054795 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061953 WHERE b.weenie_Class_Id = 1054795 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061954 WHERE b.weenie_Class_Id = 1054795 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061955 WHERE b.weenie_Class_Id = 1054795 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061956 WHERE b.weenie_Class_Id = 1054795 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061957 WHERE b.weenie_Class_Id = 1054795 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061958 WHERE b.weenie_Class_Id = 1054795 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061959 WHERE b.weenie_Class_Id = 1054795 AND s.value = 6774;

-- Health Potion of Heart Seeker: WCID 1054796 -> 1061960-1061969 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061960 WHERE b.weenie_Class_Id = 1054796 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061961 WHERE b.weenie_Class_Id = 1054796 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061962 WHERE b.weenie_Class_Id = 1054796 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061963 WHERE b.weenie_Class_Id = 1054796 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061964 WHERE b.weenie_Class_Id = 1054796 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061965 WHERE b.weenie_Class_Id = 1054796 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061966 WHERE b.weenie_Class_Id = 1054796 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061967 WHERE b.weenie_Class_Id = 1054796 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061968 WHERE b.weenie_Class_Id = 1054796 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061969 WHERE b.weenie_Class_Id = 1054796 AND s.value = 6754;

-- Health Potion of Swift Killer: WCID 1054797 -> 1061970-1061979 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061970 WHERE b.weenie_Class_Id = 1054797 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061971 WHERE b.weenie_Class_Id = 1054797 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061972 WHERE b.weenie_Class_Id = 1054797 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061973 WHERE b.weenie_Class_Id = 1054797 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061974 WHERE b.weenie_Class_Id = 1054797 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061975 WHERE b.weenie_Class_Id = 1054797 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061976 WHERE b.weenie_Class_Id = 1054797 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061977 WHERE b.weenie_Class_Id = 1054797 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061978 WHERE b.weenie_Class_Id = 1054797 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061979 WHERE b.weenie_Class_Id = 1054797 AND s.value = 6764;

-- Health Potion of Defender: WCID 1054798 -> 1061980-1061989 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061980 WHERE b.weenie_Class_Id = 1054798 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061981 WHERE b.weenie_Class_Id = 1054798 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061982 WHERE b.weenie_Class_Id = 1054798 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061983 WHERE b.weenie_Class_Id = 1054798 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061984 WHERE b.weenie_Class_Id = 1054798 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061985 WHERE b.weenie_Class_Id = 1054798 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061986 WHERE b.weenie_Class_Id = 1054798 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061987 WHERE b.weenie_Class_Id = 1054798 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061988 WHERE b.weenie_Class_Id = 1054798 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061989 WHERE b.weenie_Class_Id = 1054798 AND s.value = 6784;

-- Health Potion of Critical Chance: WCID 1054799 -> 1061990-1061999 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061990 WHERE b.weenie_Class_Id = 1054799 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061991 WHERE b.weenie_Class_Id = 1054799 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061992 WHERE b.weenie_Class_Id = 1054799 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061993 WHERE b.weenie_Class_Id = 1054799 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061994 WHERE b.weenie_Class_Id = 1054799 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061995 WHERE b.weenie_Class_Id = 1054799 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061996 WHERE b.weenie_Class_Id = 1054799 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061997 WHERE b.weenie_Class_Id = 1054799 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061998 WHERE b.weenie_Class_Id = 1054799 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1061999 WHERE b.weenie_Class_Id = 1054799 AND s.value = 6724;

-- Health Potion of Critical Damage: WCID 1054800 -> 1062000-1062009 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062000 WHERE b.weenie_Class_Id = 1054800 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062001 WHERE b.weenie_Class_Id = 1054800 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062002 WHERE b.weenie_Class_Id = 1054800 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062003 WHERE b.weenie_Class_Id = 1054800 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062004 WHERE b.weenie_Class_Id = 1054800 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062005 WHERE b.weenie_Class_Id = 1054800 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062006 WHERE b.weenie_Class_Id = 1054800 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062007 WHERE b.weenie_Class_Id = 1054800 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062008 WHERE b.weenie_Class_Id = 1054800 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062009 WHERE b.weenie_Class_Id = 1054800 AND s.value = 6734;

-- Health Potion of Slashing Protection: WCID 1054801 -> 1062010-1062019 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062010 WHERE b.weenie_Class_Id = 1054801 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062011 WHERE b.weenie_Class_Id = 1054801 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062012 WHERE b.weenie_Class_Id = 1054801 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062013 WHERE b.weenie_Class_Id = 1054801 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062014 WHERE b.weenie_Class_Id = 1054801 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062015 WHERE b.weenie_Class_Id = 1054801 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062016 WHERE b.weenie_Class_Id = 1054801 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062017 WHERE b.weenie_Class_Id = 1054801 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062018 WHERE b.weenie_Class_Id = 1054801 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062019 WHERE b.weenie_Class_Id = 1054801 AND s.value = 6612;

-- Health Potion of Piercing Protection: WCID 1054802 -> 1062020-1062029 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062020 WHERE b.weenie_Class_Id = 1054802 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062021 WHERE b.weenie_Class_Id = 1054802 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062022 WHERE b.weenie_Class_Id = 1054802 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062023 WHERE b.weenie_Class_Id = 1054802 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062024 WHERE b.weenie_Class_Id = 1054802 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062025 WHERE b.weenie_Class_Id = 1054802 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062026 WHERE b.weenie_Class_Id = 1054802 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062027 WHERE b.weenie_Class_Id = 1054802 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062028 WHERE b.weenie_Class_Id = 1054802 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062029 WHERE b.weenie_Class_Id = 1054802 AND s.value = 6622;

-- Health Potion of Bludgeoning Protection: WCID 1054803 -> 1062030-1062039 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062030 WHERE b.weenie_Class_Id = 1054803 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062031 WHERE b.weenie_Class_Id = 1054803 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062032 WHERE b.weenie_Class_Id = 1054803 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062033 WHERE b.weenie_Class_Id = 1054803 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062034 WHERE b.weenie_Class_Id = 1054803 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062035 WHERE b.weenie_Class_Id = 1054803 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062036 WHERE b.weenie_Class_Id = 1054803 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062037 WHERE b.weenie_Class_Id = 1054803 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062038 WHERE b.weenie_Class_Id = 1054803 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062039 WHERE b.weenie_Class_Id = 1054803 AND s.value = 6632;

-- Health Potion of Acid Protection: WCID 1054804 -> 1062040-1062049 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062040 WHERE b.weenie_Class_Id = 1054804 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062041 WHERE b.weenie_Class_Id = 1054804 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062042 WHERE b.weenie_Class_Id = 1054804 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062043 WHERE b.weenie_Class_Id = 1054804 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062044 WHERE b.weenie_Class_Id = 1054804 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062045 WHERE b.weenie_Class_Id = 1054804 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062046 WHERE b.weenie_Class_Id = 1054804 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062047 WHERE b.weenie_Class_Id = 1054804 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062048 WHERE b.weenie_Class_Id = 1054804 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062049 WHERE b.weenie_Class_Id = 1054804 AND s.value = 6642;

-- Health Potion of Fire Protection: WCID 1054805 -> 1062050-1062059 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062050 WHERE b.weenie_Class_Id = 1054805 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062051 WHERE b.weenie_Class_Id = 1054805 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062052 WHERE b.weenie_Class_Id = 1054805 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062053 WHERE b.weenie_Class_Id = 1054805 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062054 WHERE b.weenie_Class_Id = 1054805 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062055 WHERE b.weenie_Class_Id = 1054805 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062056 WHERE b.weenie_Class_Id = 1054805 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062057 WHERE b.weenie_Class_Id = 1054805 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062058 WHERE b.weenie_Class_Id = 1054805 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062059 WHERE b.weenie_Class_Id = 1054805 AND s.value = 6652;

-- Health Potion of Cold Protection: WCID 1054806 -> 1062060-1062069 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062060 WHERE b.weenie_Class_Id = 1054806 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062061 WHERE b.weenie_Class_Id = 1054806 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062062 WHERE b.weenie_Class_Id = 1054806 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062063 WHERE b.weenie_Class_Id = 1054806 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062064 WHERE b.weenie_Class_Id = 1054806 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062065 WHERE b.weenie_Class_Id = 1054806 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062066 WHERE b.weenie_Class_Id = 1054806 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062067 WHERE b.weenie_Class_Id = 1054806 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062068 WHERE b.weenie_Class_Id = 1054806 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062069 WHERE b.weenie_Class_Id = 1054806 AND s.value = 6662;

-- Health Potion of Lightning Protection: WCID 1054807 -> 1062070-1062079 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062070 WHERE b.weenie_Class_Id = 1054807 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062071 WHERE b.weenie_Class_Id = 1054807 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062072 WHERE b.weenie_Class_Id = 1054807 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062073 WHERE b.weenie_Class_Id = 1054807 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062074 WHERE b.weenie_Class_Id = 1054807 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062075 WHERE b.weenie_Class_Id = 1054807 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062076 WHERE b.weenie_Class_Id = 1054807 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062077 WHERE b.weenie_Class_Id = 1054807 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062078 WHERE b.weenie_Class_Id = 1054807 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062079 WHERE b.weenie_Class_Id = 1054807 AND s.value = 6672;

-- Stamina Potion of Armor: WCID 1054808 -> 1062080-1062089 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062080 WHERE b.weenie_Class_Id = 1054808 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062081 WHERE b.weenie_Class_Id = 1054808 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062082 WHERE b.weenie_Class_Id = 1054808 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062083 WHERE b.weenie_Class_Id = 1054808 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062084 WHERE b.weenie_Class_Id = 1054808 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062085 WHERE b.weenie_Class_Id = 1054808 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062086 WHERE b.weenie_Class_Id = 1054808 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062087 WHERE b.weenie_Class_Id = 1054808 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062088 WHERE b.weenie_Class_Id = 1054808 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062089 WHERE b.weenie_Class_Id = 1054808 AND s.value = 6592;

-- Stamina Potion of Warding: WCID 1054809 -> 1062090-1062099 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062090 WHERE b.weenie_Class_Id = 1054809 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062091 WHERE b.weenie_Class_Id = 1054809 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062092 WHERE b.weenie_Class_Id = 1054809 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062093 WHERE b.weenie_Class_Id = 1054809 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062094 WHERE b.weenie_Class_Id = 1054809 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062095 WHERE b.weenie_Class_Id = 1054809 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062096 WHERE b.weenie_Class_Id = 1054809 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062097 WHERE b.weenie_Class_Id = 1054809 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062098 WHERE b.weenie_Class_Id = 1054809 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062099 WHERE b.weenie_Class_Id = 1054809 AND s.value = 6602;

-- Stamina Potion of Regeneration: WCID 1054810 -> 1062100-1062109 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062100 WHERE b.weenie_Class_Id = 1054810 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062101 WHERE b.weenie_Class_Id = 1054810 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062102 WHERE b.weenie_Class_Id = 1054810 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062103 WHERE b.weenie_Class_Id = 1054810 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062104 WHERE b.weenie_Class_Id = 1054810 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062105 WHERE b.weenie_Class_Id = 1054810 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062106 WHERE b.weenie_Class_Id = 1054810 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062107 WHERE b.weenie_Class_Id = 1054810 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062108 WHERE b.weenie_Class_Id = 1054810 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062109 WHERE b.weenie_Class_Id = 1054810 AND s.value = 6694;

-- Stamina Potion of Rejuvenation: WCID 1054811 -> 1062110-1062119 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062110 WHERE b.weenie_Class_Id = 1054811 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062111 WHERE b.weenie_Class_Id = 1054811 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062112 WHERE b.weenie_Class_Id = 1054811 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062113 WHERE b.weenie_Class_Id = 1054811 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062114 WHERE b.weenie_Class_Id = 1054811 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062115 WHERE b.weenie_Class_Id = 1054811 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062116 WHERE b.weenie_Class_Id = 1054811 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062117 WHERE b.weenie_Class_Id = 1054811 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062118 WHERE b.weenie_Class_Id = 1054811 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062119 WHERE b.weenie_Class_Id = 1054811 AND s.value = 6704;

-- Stamina Potion of Clarity: WCID 1054812 -> 1062120-1062129 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062120 WHERE b.weenie_Class_Id = 1054812 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062121 WHERE b.weenie_Class_Id = 1054812 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062122 WHERE b.weenie_Class_Id = 1054812 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062123 WHERE b.weenie_Class_Id = 1054812 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062124 WHERE b.weenie_Class_Id = 1054812 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062125 WHERE b.weenie_Class_Id = 1054812 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062126 WHERE b.weenie_Class_Id = 1054812 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062127 WHERE b.weenie_Class_Id = 1054812 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062128 WHERE b.weenie_Class_Id = 1054812 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062129 WHERE b.weenie_Class_Id = 1054812 AND s.value = 6714;

-- Stamina Potion of Blood Drinker: WCID 1054813 -> 1062130-1062139 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062130 WHERE b.weenie_Class_Id = 1054813 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062131 WHERE b.weenie_Class_Id = 1054813 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062132 WHERE b.weenie_Class_Id = 1054813 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062133 WHERE b.weenie_Class_Id = 1054813 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062134 WHERE b.weenie_Class_Id = 1054813 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062135 WHERE b.weenie_Class_Id = 1054813 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062136 WHERE b.weenie_Class_Id = 1054813 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062137 WHERE b.weenie_Class_Id = 1054813 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062138 WHERE b.weenie_Class_Id = 1054813 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062139 WHERE b.weenie_Class_Id = 1054813 AND s.value = 6744;

-- Stamina Potion of Spirit Drinker: WCID 1054814 -> 1062140-1062149 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062140 WHERE b.weenie_Class_Id = 1054814 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062141 WHERE b.weenie_Class_Id = 1054814 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062142 WHERE b.weenie_Class_Id = 1054814 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062143 WHERE b.weenie_Class_Id = 1054814 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062144 WHERE b.weenie_Class_Id = 1054814 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062145 WHERE b.weenie_Class_Id = 1054814 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062146 WHERE b.weenie_Class_Id = 1054814 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062147 WHERE b.weenie_Class_Id = 1054814 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062148 WHERE b.weenie_Class_Id = 1054814 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062149 WHERE b.weenie_Class_Id = 1054814 AND s.value = 6774;

-- Stamina Potion of Heart Seeker: WCID 1054815 -> 1062150-1062159 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062150 WHERE b.weenie_Class_Id = 1054815 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062151 WHERE b.weenie_Class_Id = 1054815 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062152 WHERE b.weenie_Class_Id = 1054815 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062153 WHERE b.weenie_Class_Id = 1054815 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062154 WHERE b.weenie_Class_Id = 1054815 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062155 WHERE b.weenie_Class_Id = 1054815 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062156 WHERE b.weenie_Class_Id = 1054815 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062157 WHERE b.weenie_Class_Id = 1054815 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062158 WHERE b.weenie_Class_Id = 1054815 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062159 WHERE b.weenie_Class_Id = 1054815 AND s.value = 6754;

-- Stamina Potion of Swift Killer: WCID 1054816 -> 1062160-1062169 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062160 WHERE b.weenie_Class_Id = 1054816 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062161 WHERE b.weenie_Class_Id = 1054816 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062162 WHERE b.weenie_Class_Id = 1054816 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062163 WHERE b.weenie_Class_Id = 1054816 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062164 WHERE b.weenie_Class_Id = 1054816 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062165 WHERE b.weenie_Class_Id = 1054816 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062166 WHERE b.weenie_Class_Id = 1054816 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062167 WHERE b.weenie_Class_Id = 1054816 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062168 WHERE b.weenie_Class_Id = 1054816 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062169 WHERE b.weenie_Class_Id = 1054816 AND s.value = 6764;

-- Stamina Potion of Defender: WCID 1054817 -> 1062170-1062179 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062170 WHERE b.weenie_Class_Id = 1054817 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062171 WHERE b.weenie_Class_Id = 1054817 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062172 WHERE b.weenie_Class_Id = 1054817 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062173 WHERE b.weenie_Class_Id = 1054817 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062174 WHERE b.weenie_Class_Id = 1054817 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062175 WHERE b.weenie_Class_Id = 1054817 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062176 WHERE b.weenie_Class_Id = 1054817 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062177 WHERE b.weenie_Class_Id = 1054817 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062178 WHERE b.weenie_Class_Id = 1054817 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062179 WHERE b.weenie_Class_Id = 1054817 AND s.value = 6784;

-- Stamina Potion of Critical Chance: WCID 1054818 -> 1062180-1062189 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062180 WHERE b.weenie_Class_Id = 1054818 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062181 WHERE b.weenie_Class_Id = 1054818 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062182 WHERE b.weenie_Class_Id = 1054818 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062183 WHERE b.weenie_Class_Id = 1054818 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062184 WHERE b.weenie_Class_Id = 1054818 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062185 WHERE b.weenie_Class_Id = 1054818 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062186 WHERE b.weenie_Class_Id = 1054818 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062187 WHERE b.weenie_Class_Id = 1054818 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062188 WHERE b.weenie_Class_Id = 1054818 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062189 WHERE b.weenie_Class_Id = 1054818 AND s.value = 6724;

-- Stamina Potion of Critical Damage: WCID 1054819 -> 1062190-1062199 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062190 WHERE b.weenie_Class_Id = 1054819 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062191 WHERE b.weenie_Class_Id = 1054819 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062192 WHERE b.weenie_Class_Id = 1054819 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062193 WHERE b.weenie_Class_Id = 1054819 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062194 WHERE b.weenie_Class_Id = 1054819 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062195 WHERE b.weenie_Class_Id = 1054819 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062196 WHERE b.weenie_Class_Id = 1054819 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062197 WHERE b.weenie_Class_Id = 1054819 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062198 WHERE b.weenie_Class_Id = 1054819 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062199 WHERE b.weenie_Class_Id = 1054819 AND s.value = 6734;

-- Stamina Potion of Slashing Protection: WCID 1054820 -> 1062200-1062209 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062200 WHERE b.weenie_Class_Id = 1054820 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062201 WHERE b.weenie_Class_Id = 1054820 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062202 WHERE b.weenie_Class_Id = 1054820 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062203 WHERE b.weenie_Class_Id = 1054820 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062204 WHERE b.weenie_Class_Id = 1054820 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062205 WHERE b.weenie_Class_Id = 1054820 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062206 WHERE b.weenie_Class_Id = 1054820 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062207 WHERE b.weenie_Class_Id = 1054820 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062208 WHERE b.weenie_Class_Id = 1054820 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062209 WHERE b.weenie_Class_Id = 1054820 AND s.value = 6612;

-- Stamina Potion of Piercing Protection: WCID 1054821 -> 1062210-1062219 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062210 WHERE b.weenie_Class_Id = 1054821 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062211 WHERE b.weenie_Class_Id = 1054821 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062212 WHERE b.weenie_Class_Id = 1054821 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062213 WHERE b.weenie_Class_Id = 1054821 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062214 WHERE b.weenie_Class_Id = 1054821 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062215 WHERE b.weenie_Class_Id = 1054821 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062216 WHERE b.weenie_Class_Id = 1054821 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062217 WHERE b.weenie_Class_Id = 1054821 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062218 WHERE b.weenie_Class_Id = 1054821 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062219 WHERE b.weenie_Class_Id = 1054821 AND s.value = 6622;

-- Stamina Potion of Bludgeoning Protection: WCID 1054822 -> 1062220-1062229 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062220 WHERE b.weenie_Class_Id = 1054822 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062221 WHERE b.weenie_Class_Id = 1054822 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062222 WHERE b.weenie_Class_Id = 1054822 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062223 WHERE b.weenie_Class_Id = 1054822 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062224 WHERE b.weenie_Class_Id = 1054822 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062225 WHERE b.weenie_Class_Id = 1054822 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062226 WHERE b.weenie_Class_Id = 1054822 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062227 WHERE b.weenie_Class_Id = 1054822 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062228 WHERE b.weenie_Class_Id = 1054822 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062229 WHERE b.weenie_Class_Id = 1054822 AND s.value = 6632;

-- Stamina Potion of Acid Protection: WCID 1054823 -> 1062230-1062239 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062230 WHERE b.weenie_Class_Id = 1054823 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062231 WHERE b.weenie_Class_Id = 1054823 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062232 WHERE b.weenie_Class_Id = 1054823 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062233 WHERE b.weenie_Class_Id = 1054823 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062234 WHERE b.weenie_Class_Id = 1054823 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062235 WHERE b.weenie_Class_Id = 1054823 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062236 WHERE b.weenie_Class_Id = 1054823 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062237 WHERE b.weenie_Class_Id = 1054823 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062238 WHERE b.weenie_Class_Id = 1054823 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062239 WHERE b.weenie_Class_Id = 1054823 AND s.value = 6642;

-- Stamina Potion of Fire Protection: WCID 1054824 -> 1062240-1062249 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062240 WHERE b.weenie_Class_Id = 1054824 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062241 WHERE b.weenie_Class_Id = 1054824 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062242 WHERE b.weenie_Class_Id = 1054824 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062243 WHERE b.weenie_Class_Id = 1054824 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062244 WHERE b.weenie_Class_Id = 1054824 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062245 WHERE b.weenie_Class_Id = 1054824 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062246 WHERE b.weenie_Class_Id = 1054824 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062247 WHERE b.weenie_Class_Id = 1054824 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062248 WHERE b.weenie_Class_Id = 1054824 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062249 WHERE b.weenie_Class_Id = 1054824 AND s.value = 6652;

-- Stamina Potion of Cold Protection: WCID 1054825 -> 1062250-1062259 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062250 WHERE b.weenie_Class_Id = 1054825 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062251 WHERE b.weenie_Class_Id = 1054825 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062252 WHERE b.weenie_Class_Id = 1054825 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062253 WHERE b.weenie_Class_Id = 1054825 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062254 WHERE b.weenie_Class_Id = 1054825 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062255 WHERE b.weenie_Class_Id = 1054825 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062256 WHERE b.weenie_Class_Id = 1054825 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062257 WHERE b.weenie_Class_Id = 1054825 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062258 WHERE b.weenie_Class_Id = 1054825 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062259 WHERE b.weenie_Class_Id = 1054825 AND s.value = 6662;

-- Stamina Potion of Lightning Protection: WCID 1054826 -> 1062260-1062269 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062260 WHERE b.weenie_Class_Id = 1054826 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062261 WHERE b.weenie_Class_Id = 1054826 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062262 WHERE b.weenie_Class_Id = 1054826 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062263 WHERE b.weenie_Class_Id = 1054826 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062264 WHERE b.weenie_Class_Id = 1054826 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062265 WHERE b.weenie_Class_Id = 1054826 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062266 WHERE b.weenie_Class_Id = 1054826 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062267 WHERE b.weenie_Class_Id = 1054826 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062268 WHERE b.weenie_Class_Id = 1054826 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062269 WHERE b.weenie_Class_Id = 1054826 AND s.value = 6672;

-- Mana Potion of Armor: WCID 1054827 -> 1062270-1062279 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062270 WHERE b.weenie_Class_Id = 1054827 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062271 WHERE b.weenie_Class_Id = 1054827 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062272 WHERE b.weenie_Class_Id = 1054827 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062273 WHERE b.weenie_Class_Id = 1054827 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062274 WHERE b.weenie_Class_Id = 1054827 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062275 WHERE b.weenie_Class_Id = 1054827 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062276 WHERE b.weenie_Class_Id = 1054827 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062277 WHERE b.weenie_Class_Id = 1054827 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062278 WHERE b.weenie_Class_Id = 1054827 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062279 WHERE b.weenie_Class_Id = 1054827 AND s.value = 6592;

-- Mana Potion of Warding: WCID 1054828 -> 1062280-1062289 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062280 WHERE b.weenie_Class_Id = 1054828 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062281 WHERE b.weenie_Class_Id = 1054828 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062282 WHERE b.weenie_Class_Id = 1054828 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062283 WHERE b.weenie_Class_Id = 1054828 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062284 WHERE b.weenie_Class_Id = 1054828 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062285 WHERE b.weenie_Class_Id = 1054828 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062286 WHERE b.weenie_Class_Id = 1054828 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062287 WHERE b.weenie_Class_Id = 1054828 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062288 WHERE b.weenie_Class_Id = 1054828 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062289 WHERE b.weenie_Class_Id = 1054828 AND s.value = 6602;

-- Mana Potion of Regeneration: WCID 1054829 -> 1062290-1062299 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062290 WHERE b.weenie_Class_Id = 1054829 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062291 WHERE b.weenie_Class_Id = 1054829 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062292 WHERE b.weenie_Class_Id = 1054829 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062293 WHERE b.weenie_Class_Id = 1054829 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062294 WHERE b.weenie_Class_Id = 1054829 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062295 WHERE b.weenie_Class_Id = 1054829 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062296 WHERE b.weenie_Class_Id = 1054829 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062297 WHERE b.weenie_Class_Id = 1054829 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062298 WHERE b.weenie_Class_Id = 1054829 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062299 WHERE b.weenie_Class_Id = 1054829 AND s.value = 6694;

-- Mana Potion of Rejuvenation: WCID 1054830 -> 1062300-1062309 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062300 WHERE b.weenie_Class_Id = 1054830 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062301 WHERE b.weenie_Class_Id = 1054830 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062302 WHERE b.weenie_Class_Id = 1054830 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062303 WHERE b.weenie_Class_Id = 1054830 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062304 WHERE b.weenie_Class_Id = 1054830 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062305 WHERE b.weenie_Class_Id = 1054830 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062306 WHERE b.weenie_Class_Id = 1054830 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062307 WHERE b.weenie_Class_Id = 1054830 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062308 WHERE b.weenie_Class_Id = 1054830 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062309 WHERE b.weenie_Class_Id = 1054830 AND s.value = 6704;

-- Mana Potion of Clarity: WCID 1054831 -> 1062310-1062319 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062310 WHERE b.weenie_Class_Id = 1054831 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062311 WHERE b.weenie_Class_Id = 1054831 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062312 WHERE b.weenie_Class_Id = 1054831 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062313 WHERE b.weenie_Class_Id = 1054831 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062314 WHERE b.weenie_Class_Id = 1054831 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062315 WHERE b.weenie_Class_Id = 1054831 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062316 WHERE b.weenie_Class_Id = 1054831 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062317 WHERE b.weenie_Class_Id = 1054831 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062318 WHERE b.weenie_Class_Id = 1054831 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062319 WHERE b.weenie_Class_Id = 1054831 AND s.value = 6714;

-- Mana Potion of Blood Drinker: WCID 1054832 -> 1062320-1062329 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062320 WHERE b.weenie_Class_Id = 1054832 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062321 WHERE b.weenie_Class_Id = 1054832 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062322 WHERE b.weenie_Class_Id = 1054832 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062323 WHERE b.weenie_Class_Id = 1054832 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062324 WHERE b.weenie_Class_Id = 1054832 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062325 WHERE b.weenie_Class_Id = 1054832 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062326 WHERE b.weenie_Class_Id = 1054832 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062327 WHERE b.weenie_Class_Id = 1054832 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062328 WHERE b.weenie_Class_Id = 1054832 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062329 WHERE b.weenie_Class_Id = 1054832 AND s.value = 6744;

-- Mana Potion of Spirit Drinker: WCID 1054833 -> 1062330-1062339 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062330 WHERE b.weenie_Class_Id = 1054833 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062331 WHERE b.weenie_Class_Id = 1054833 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062332 WHERE b.weenie_Class_Id = 1054833 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062333 WHERE b.weenie_Class_Id = 1054833 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062334 WHERE b.weenie_Class_Id = 1054833 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062335 WHERE b.weenie_Class_Id = 1054833 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062336 WHERE b.weenie_Class_Id = 1054833 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062337 WHERE b.weenie_Class_Id = 1054833 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062338 WHERE b.weenie_Class_Id = 1054833 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062339 WHERE b.weenie_Class_Id = 1054833 AND s.value = 6774;

-- Mana Potion of Heart Seeker: WCID 1054834 -> 1062340-1062349 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062340 WHERE b.weenie_Class_Id = 1054834 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062341 WHERE b.weenie_Class_Id = 1054834 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062342 WHERE b.weenie_Class_Id = 1054834 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062343 WHERE b.weenie_Class_Id = 1054834 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062344 WHERE b.weenie_Class_Id = 1054834 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062345 WHERE b.weenie_Class_Id = 1054834 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062346 WHERE b.weenie_Class_Id = 1054834 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062347 WHERE b.weenie_Class_Id = 1054834 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062348 WHERE b.weenie_Class_Id = 1054834 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062349 WHERE b.weenie_Class_Id = 1054834 AND s.value = 6754;

-- Mana Potion of Swift Killer: WCID 1054835 -> 1062350-1062359 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062350 WHERE b.weenie_Class_Id = 1054835 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062351 WHERE b.weenie_Class_Id = 1054835 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062352 WHERE b.weenie_Class_Id = 1054835 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062353 WHERE b.weenie_Class_Id = 1054835 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062354 WHERE b.weenie_Class_Id = 1054835 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062355 WHERE b.weenie_Class_Id = 1054835 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062356 WHERE b.weenie_Class_Id = 1054835 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062357 WHERE b.weenie_Class_Id = 1054835 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062358 WHERE b.weenie_Class_Id = 1054835 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062359 WHERE b.weenie_Class_Id = 1054835 AND s.value = 6764;

-- Mana Potion of Defender: WCID 1054836 -> 1062360-1062369 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062360 WHERE b.weenie_Class_Id = 1054836 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062361 WHERE b.weenie_Class_Id = 1054836 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062362 WHERE b.weenie_Class_Id = 1054836 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062363 WHERE b.weenie_Class_Id = 1054836 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062364 WHERE b.weenie_Class_Id = 1054836 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062365 WHERE b.weenie_Class_Id = 1054836 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062366 WHERE b.weenie_Class_Id = 1054836 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062367 WHERE b.weenie_Class_Id = 1054836 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062368 WHERE b.weenie_Class_Id = 1054836 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062369 WHERE b.weenie_Class_Id = 1054836 AND s.value = 6784;

-- Mana Potion of Critical Chance: WCID 1054837 -> 1062370-1062379 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062370 WHERE b.weenie_Class_Id = 1054837 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062371 WHERE b.weenie_Class_Id = 1054837 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062372 WHERE b.weenie_Class_Id = 1054837 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062373 WHERE b.weenie_Class_Id = 1054837 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062374 WHERE b.weenie_Class_Id = 1054837 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062375 WHERE b.weenie_Class_Id = 1054837 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062376 WHERE b.weenie_Class_Id = 1054837 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062377 WHERE b.weenie_Class_Id = 1054837 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062378 WHERE b.weenie_Class_Id = 1054837 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062379 WHERE b.weenie_Class_Id = 1054837 AND s.value = 6724;

-- Mana Potion of Critical Damage: WCID 1054838 -> 1062380-1062389 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062380 WHERE b.weenie_Class_Id = 1054838 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062381 WHERE b.weenie_Class_Id = 1054838 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062382 WHERE b.weenie_Class_Id = 1054838 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062383 WHERE b.weenie_Class_Id = 1054838 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062384 WHERE b.weenie_Class_Id = 1054838 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062385 WHERE b.weenie_Class_Id = 1054838 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062386 WHERE b.weenie_Class_Id = 1054838 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062387 WHERE b.weenie_Class_Id = 1054838 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062388 WHERE b.weenie_Class_Id = 1054838 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062389 WHERE b.weenie_Class_Id = 1054838 AND s.value = 6734;

-- Mana Potion of Slashing Protection: WCID 1054839 -> 1062390-1062399 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062390 WHERE b.weenie_Class_Id = 1054839 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062391 WHERE b.weenie_Class_Id = 1054839 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062392 WHERE b.weenie_Class_Id = 1054839 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062393 WHERE b.weenie_Class_Id = 1054839 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062394 WHERE b.weenie_Class_Id = 1054839 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062395 WHERE b.weenie_Class_Id = 1054839 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062396 WHERE b.weenie_Class_Id = 1054839 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062397 WHERE b.weenie_Class_Id = 1054839 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062398 WHERE b.weenie_Class_Id = 1054839 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062399 WHERE b.weenie_Class_Id = 1054839 AND s.value = 6612;

-- Mana Potion of Piercing Protection: WCID 1054840 -> 1062400-1062409 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062400 WHERE b.weenie_Class_Id = 1054840 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062401 WHERE b.weenie_Class_Id = 1054840 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062402 WHERE b.weenie_Class_Id = 1054840 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062403 WHERE b.weenie_Class_Id = 1054840 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062404 WHERE b.weenie_Class_Id = 1054840 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062405 WHERE b.weenie_Class_Id = 1054840 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062406 WHERE b.weenie_Class_Id = 1054840 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062407 WHERE b.weenie_Class_Id = 1054840 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062408 WHERE b.weenie_Class_Id = 1054840 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062409 WHERE b.weenie_Class_Id = 1054840 AND s.value = 6622;

-- Mana Potion of Bludgeoning Protection: WCID 1054841 -> 1062410-1062419 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062410 WHERE b.weenie_Class_Id = 1054841 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062411 WHERE b.weenie_Class_Id = 1054841 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062412 WHERE b.weenie_Class_Id = 1054841 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062413 WHERE b.weenie_Class_Id = 1054841 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062414 WHERE b.weenie_Class_Id = 1054841 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062415 WHERE b.weenie_Class_Id = 1054841 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062416 WHERE b.weenie_Class_Id = 1054841 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062417 WHERE b.weenie_Class_Id = 1054841 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062418 WHERE b.weenie_Class_Id = 1054841 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062419 WHERE b.weenie_Class_Id = 1054841 AND s.value = 6632;

-- Mana Potion of Acid Protection: WCID 1054842 -> 1062420-1062429 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062420 WHERE b.weenie_Class_Id = 1054842 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062421 WHERE b.weenie_Class_Id = 1054842 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062422 WHERE b.weenie_Class_Id = 1054842 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062423 WHERE b.weenie_Class_Id = 1054842 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062424 WHERE b.weenie_Class_Id = 1054842 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062425 WHERE b.weenie_Class_Id = 1054842 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062426 WHERE b.weenie_Class_Id = 1054842 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062427 WHERE b.weenie_Class_Id = 1054842 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062428 WHERE b.weenie_Class_Id = 1054842 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062429 WHERE b.weenie_Class_Id = 1054842 AND s.value = 6642;

-- Mana Potion of Fire Protection: WCID 1054843 -> 1062430-1062439 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062430 WHERE b.weenie_Class_Id = 1054843 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062431 WHERE b.weenie_Class_Id = 1054843 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062432 WHERE b.weenie_Class_Id = 1054843 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062433 WHERE b.weenie_Class_Id = 1054843 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062434 WHERE b.weenie_Class_Id = 1054843 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062435 WHERE b.weenie_Class_Id = 1054843 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062436 WHERE b.weenie_Class_Id = 1054843 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062437 WHERE b.weenie_Class_Id = 1054843 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062438 WHERE b.weenie_Class_Id = 1054843 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062439 WHERE b.weenie_Class_Id = 1054843 AND s.value = 6652;

-- Mana Potion of Cold Protection: WCID 1054844 -> 1062440-1062449 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062440 WHERE b.weenie_Class_Id = 1054844 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062441 WHERE b.weenie_Class_Id = 1054844 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062442 WHERE b.weenie_Class_Id = 1054844 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062443 WHERE b.weenie_Class_Id = 1054844 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062444 WHERE b.weenie_Class_Id = 1054844 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062445 WHERE b.weenie_Class_Id = 1054844 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062446 WHERE b.weenie_Class_Id = 1054844 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062447 WHERE b.weenie_Class_Id = 1054844 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062448 WHERE b.weenie_Class_Id = 1054844 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062449 WHERE b.weenie_Class_Id = 1054844 AND s.value = 6662;

-- Mana Potion of Lightning Protection: WCID 1054845 -> 1062450-1062459 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062450 WHERE b.weenie_Class_Id = 1054845 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062451 WHERE b.weenie_Class_Id = 1054845 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062452 WHERE b.weenie_Class_Id = 1054845 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062453 WHERE b.weenie_Class_Id = 1054845 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062454 WHERE b.weenie_Class_Id = 1054845 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062455 WHERE b.weenie_Class_Id = 1054845 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062456 WHERE b.weenie_Class_Id = 1054845 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062457 WHERE b.weenie_Class_Id = 1054845 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062458 WHERE b.weenie_Class_Id = 1054845 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062459 WHERE b.weenie_Class_Id = 1054845 AND s.value = 6672;

-- Health Tonic of Armor: WCID 1054846 -> 1062460-1062469 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062460 WHERE b.weenie_Class_Id = 1054846 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062461 WHERE b.weenie_Class_Id = 1054846 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062462 WHERE b.weenie_Class_Id = 1054846 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062463 WHERE b.weenie_Class_Id = 1054846 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062464 WHERE b.weenie_Class_Id = 1054846 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062465 WHERE b.weenie_Class_Id = 1054846 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062466 WHERE b.weenie_Class_Id = 1054846 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062467 WHERE b.weenie_Class_Id = 1054846 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062468 WHERE b.weenie_Class_Id = 1054846 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062469 WHERE b.weenie_Class_Id = 1054846 AND s.value = 6592;

-- Health Tonic of Warding: WCID 1054847 -> 1062470-1062479 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062470 WHERE b.weenie_Class_Id = 1054847 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062471 WHERE b.weenie_Class_Id = 1054847 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062472 WHERE b.weenie_Class_Id = 1054847 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062473 WHERE b.weenie_Class_Id = 1054847 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062474 WHERE b.weenie_Class_Id = 1054847 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062475 WHERE b.weenie_Class_Id = 1054847 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062476 WHERE b.weenie_Class_Id = 1054847 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062477 WHERE b.weenie_Class_Id = 1054847 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062478 WHERE b.weenie_Class_Id = 1054847 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062479 WHERE b.weenie_Class_Id = 1054847 AND s.value = 6602;

-- Health Tonic of Regeneration: WCID 1054848 -> 1062480-1062489 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062480 WHERE b.weenie_Class_Id = 1054848 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062481 WHERE b.weenie_Class_Id = 1054848 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062482 WHERE b.weenie_Class_Id = 1054848 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062483 WHERE b.weenie_Class_Id = 1054848 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062484 WHERE b.weenie_Class_Id = 1054848 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062485 WHERE b.weenie_Class_Id = 1054848 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062486 WHERE b.weenie_Class_Id = 1054848 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062487 WHERE b.weenie_Class_Id = 1054848 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062488 WHERE b.weenie_Class_Id = 1054848 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062489 WHERE b.weenie_Class_Id = 1054848 AND s.value = 6694;

-- Health Tonic of Rejuvenation: WCID 1054849 -> 1062490-1062499 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062490 WHERE b.weenie_Class_Id = 1054849 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062491 WHERE b.weenie_Class_Id = 1054849 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062492 WHERE b.weenie_Class_Id = 1054849 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062493 WHERE b.weenie_Class_Id = 1054849 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062494 WHERE b.weenie_Class_Id = 1054849 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062495 WHERE b.weenie_Class_Id = 1054849 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062496 WHERE b.weenie_Class_Id = 1054849 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062497 WHERE b.weenie_Class_Id = 1054849 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062498 WHERE b.weenie_Class_Id = 1054849 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062499 WHERE b.weenie_Class_Id = 1054849 AND s.value = 6704;

-- Health Tonic of Clarity: WCID 1054850 -> 1062500-1062509 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062500 WHERE b.weenie_Class_Id = 1054850 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062501 WHERE b.weenie_Class_Id = 1054850 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062502 WHERE b.weenie_Class_Id = 1054850 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062503 WHERE b.weenie_Class_Id = 1054850 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062504 WHERE b.weenie_Class_Id = 1054850 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062505 WHERE b.weenie_Class_Id = 1054850 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062506 WHERE b.weenie_Class_Id = 1054850 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062507 WHERE b.weenie_Class_Id = 1054850 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062508 WHERE b.weenie_Class_Id = 1054850 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062509 WHERE b.weenie_Class_Id = 1054850 AND s.value = 6714;

-- Health Tonic of Blood Drinker: WCID 1054851 -> 1062510-1062519 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062510 WHERE b.weenie_Class_Id = 1054851 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062511 WHERE b.weenie_Class_Id = 1054851 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062512 WHERE b.weenie_Class_Id = 1054851 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062513 WHERE b.weenie_Class_Id = 1054851 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062514 WHERE b.weenie_Class_Id = 1054851 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062515 WHERE b.weenie_Class_Id = 1054851 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062516 WHERE b.weenie_Class_Id = 1054851 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062517 WHERE b.weenie_Class_Id = 1054851 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062518 WHERE b.weenie_Class_Id = 1054851 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062519 WHERE b.weenie_Class_Id = 1054851 AND s.value = 6744;

-- Health Tonic of Spirit Drinker: WCID 1054852 -> 1062520-1062529 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062520 WHERE b.weenie_Class_Id = 1054852 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062521 WHERE b.weenie_Class_Id = 1054852 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062522 WHERE b.weenie_Class_Id = 1054852 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062523 WHERE b.weenie_Class_Id = 1054852 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062524 WHERE b.weenie_Class_Id = 1054852 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062525 WHERE b.weenie_Class_Id = 1054852 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062526 WHERE b.weenie_Class_Id = 1054852 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062527 WHERE b.weenie_Class_Id = 1054852 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062528 WHERE b.weenie_Class_Id = 1054852 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062529 WHERE b.weenie_Class_Id = 1054852 AND s.value = 6774;

-- Health Tonic of Heart Seeker: WCID 1054853 -> 1062530-1062539 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062530 WHERE b.weenie_Class_Id = 1054853 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062531 WHERE b.weenie_Class_Id = 1054853 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062532 WHERE b.weenie_Class_Id = 1054853 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062533 WHERE b.weenie_Class_Id = 1054853 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062534 WHERE b.weenie_Class_Id = 1054853 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062535 WHERE b.weenie_Class_Id = 1054853 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062536 WHERE b.weenie_Class_Id = 1054853 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062537 WHERE b.weenie_Class_Id = 1054853 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062538 WHERE b.weenie_Class_Id = 1054853 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062539 WHERE b.weenie_Class_Id = 1054853 AND s.value = 6754;

-- Health Tonic of Swift Killer: WCID 1054854 -> 1062540-1062549 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062540 WHERE b.weenie_Class_Id = 1054854 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062541 WHERE b.weenie_Class_Id = 1054854 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062542 WHERE b.weenie_Class_Id = 1054854 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062543 WHERE b.weenie_Class_Id = 1054854 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062544 WHERE b.weenie_Class_Id = 1054854 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062545 WHERE b.weenie_Class_Id = 1054854 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062546 WHERE b.weenie_Class_Id = 1054854 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062547 WHERE b.weenie_Class_Id = 1054854 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062548 WHERE b.weenie_Class_Id = 1054854 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062549 WHERE b.weenie_Class_Id = 1054854 AND s.value = 6764;

-- Health Tonic of Defender: WCID 1054855 -> 1062550-1062559 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062550 WHERE b.weenie_Class_Id = 1054855 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062551 WHERE b.weenie_Class_Id = 1054855 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062552 WHERE b.weenie_Class_Id = 1054855 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062553 WHERE b.weenie_Class_Id = 1054855 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062554 WHERE b.weenie_Class_Id = 1054855 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062555 WHERE b.weenie_Class_Id = 1054855 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062556 WHERE b.weenie_Class_Id = 1054855 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062557 WHERE b.weenie_Class_Id = 1054855 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062558 WHERE b.weenie_Class_Id = 1054855 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062559 WHERE b.weenie_Class_Id = 1054855 AND s.value = 6784;

-- Health Tonic of Critical Chance: WCID 1054856 -> 1062560-1062569 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062560 WHERE b.weenie_Class_Id = 1054856 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062561 WHERE b.weenie_Class_Id = 1054856 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062562 WHERE b.weenie_Class_Id = 1054856 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062563 WHERE b.weenie_Class_Id = 1054856 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062564 WHERE b.weenie_Class_Id = 1054856 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062565 WHERE b.weenie_Class_Id = 1054856 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062566 WHERE b.weenie_Class_Id = 1054856 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062567 WHERE b.weenie_Class_Id = 1054856 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062568 WHERE b.weenie_Class_Id = 1054856 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062569 WHERE b.weenie_Class_Id = 1054856 AND s.value = 6724;

-- Health Tonic of Critical Damage: WCID 1054857 -> 1062570-1062579 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062570 WHERE b.weenie_Class_Id = 1054857 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062571 WHERE b.weenie_Class_Id = 1054857 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062572 WHERE b.weenie_Class_Id = 1054857 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062573 WHERE b.weenie_Class_Id = 1054857 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062574 WHERE b.weenie_Class_Id = 1054857 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062575 WHERE b.weenie_Class_Id = 1054857 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062576 WHERE b.weenie_Class_Id = 1054857 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062577 WHERE b.weenie_Class_Id = 1054857 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062578 WHERE b.weenie_Class_Id = 1054857 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062579 WHERE b.weenie_Class_Id = 1054857 AND s.value = 6734;

-- Health Tonic of Slashing Protection: WCID 1054858 -> 1062580-1062589 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062580 WHERE b.weenie_Class_Id = 1054858 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062581 WHERE b.weenie_Class_Id = 1054858 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062582 WHERE b.weenie_Class_Id = 1054858 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062583 WHERE b.weenie_Class_Id = 1054858 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062584 WHERE b.weenie_Class_Id = 1054858 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062585 WHERE b.weenie_Class_Id = 1054858 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062586 WHERE b.weenie_Class_Id = 1054858 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062587 WHERE b.weenie_Class_Id = 1054858 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062588 WHERE b.weenie_Class_Id = 1054858 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062589 WHERE b.weenie_Class_Id = 1054858 AND s.value = 6612;

-- Health Tonic of Piercing Protection: WCID 1054859 -> 1062590-1062599 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062590 WHERE b.weenie_Class_Id = 1054859 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062591 WHERE b.weenie_Class_Id = 1054859 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062592 WHERE b.weenie_Class_Id = 1054859 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062593 WHERE b.weenie_Class_Id = 1054859 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062594 WHERE b.weenie_Class_Id = 1054859 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062595 WHERE b.weenie_Class_Id = 1054859 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062596 WHERE b.weenie_Class_Id = 1054859 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062597 WHERE b.weenie_Class_Id = 1054859 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062598 WHERE b.weenie_Class_Id = 1054859 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062599 WHERE b.weenie_Class_Id = 1054859 AND s.value = 6622;

-- Health Tonic of Bludgeoning Protection: WCID 1054860 -> 1062600-1062609 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062600 WHERE b.weenie_Class_Id = 1054860 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062601 WHERE b.weenie_Class_Id = 1054860 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062602 WHERE b.weenie_Class_Id = 1054860 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062603 WHERE b.weenie_Class_Id = 1054860 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062604 WHERE b.weenie_Class_Id = 1054860 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062605 WHERE b.weenie_Class_Id = 1054860 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062606 WHERE b.weenie_Class_Id = 1054860 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062607 WHERE b.weenie_Class_Id = 1054860 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062608 WHERE b.weenie_Class_Id = 1054860 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062609 WHERE b.weenie_Class_Id = 1054860 AND s.value = 6632;

-- Health Tonic of Acid Protection: WCID 1054861 -> 1062610-1062619 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062610 WHERE b.weenie_Class_Id = 1054861 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062611 WHERE b.weenie_Class_Id = 1054861 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062612 WHERE b.weenie_Class_Id = 1054861 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062613 WHERE b.weenie_Class_Id = 1054861 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062614 WHERE b.weenie_Class_Id = 1054861 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062615 WHERE b.weenie_Class_Id = 1054861 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062616 WHERE b.weenie_Class_Id = 1054861 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062617 WHERE b.weenie_Class_Id = 1054861 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062618 WHERE b.weenie_Class_Id = 1054861 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062619 WHERE b.weenie_Class_Id = 1054861 AND s.value = 6642;

-- Health Tonic of Fire Protection: WCID 1054862 -> 1062620-1062629 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062620 WHERE b.weenie_Class_Id = 1054862 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062621 WHERE b.weenie_Class_Id = 1054862 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062622 WHERE b.weenie_Class_Id = 1054862 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062623 WHERE b.weenie_Class_Id = 1054862 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062624 WHERE b.weenie_Class_Id = 1054862 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062625 WHERE b.weenie_Class_Id = 1054862 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062626 WHERE b.weenie_Class_Id = 1054862 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062627 WHERE b.weenie_Class_Id = 1054862 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062628 WHERE b.weenie_Class_Id = 1054862 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062629 WHERE b.weenie_Class_Id = 1054862 AND s.value = 6652;

-- Health Tonic of Cold Protection: WCID 1054863 -> 1062630-1062639 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062630 WHERE b.weenie_Class_Id = 1054863 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062631 WHERE b.weenie_Class_Id = 1054863 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062632 WHERE b.weenie_Class_Id = 1054863 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062633 WHERE b.weenie_Class_Id = 1054863 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062634 WHERE b.weenie_Class_Id = 1054863 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062635 WHERE b.weenie_Class_Id = 1054863 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062636 WHERE b.weenie_Class_Id = 1054863 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062637 WHERE b.weenie_Class_Id = 1054863 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062638 WHERE b.weenie_Class_Id = 1054863 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062639 WHERE b.weenie_Class_Id = 1054863 AND s.value = 6662;

-- Health Tonic of Lightning Protection: WCID 1054864 -> 1062640-1062649 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062640 WHERE b.weenie_Class_Id = 1054864 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062641 WHERE b.weenie_Class_Id = 1054864 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062642 WHERE b.weenie_Class_Id = 1054864 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062643 WHERE b.weenie_Class_Id = 1054864 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062644 WHERE b.weenie_Class_Id = 1054864 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062645 WHERE b.weenie_Class_Id = 1054864 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062646 WHERE b.weenie_Class_Id = 1054864 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062647 WHERE b.weenie_Class_Id = 1054864 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062648 WHERE b.weenie_Class_Id = 1054864 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062649 WHERE b.weenie_Class_Id = 1054864 AND s.value = 6672;

-- Stamina Tonic of Armor: WCID 1054865 -> 1062650-1062659 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062650 WHERE b.weenie_Class_Id = 1054865 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062651 WHERE b.weenie_Class_Id = 1054865 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062652 WHERE b.weenie_Class_Id = 1054865 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062653 WHERE b.weenie_Class_Id = 1054865 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062654 WHERE b.weenie_Class_Id = 1054865 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062655 WHERE b.weenie_Class_Id = 1054865 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062656 WHERE b.weenie_Class_Id = 1054865 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062657 WHERE b.weenie_Class_Id = 1054865 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062658 WHERE b.weenie_Class_Id = 1054865 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062659 WHERE b.weenie_Class_Id = 1054865 AND s.value = 6592;

-- Stamina Tonic of Warding: WCID 1054866 -> 1062660-1062669 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062660 WHERE b.weenie_Class_Id = 1054866 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062661 WHERE b.weenie_Class_Id = 1054866 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062662 WHERE b.weenie_Class_Id = 1054866 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062663 WHERE b.weenie_Class_Id = 1054866 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062664 WHERE b.weenie_Class_Id = 1054866 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062665 WHERE b.weenie_Class_Id = 1054866 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062666 WHERE b.weenie_Class_Id = 1054866 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062667 WHERE b.weenie_Class_Id = 1054866 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062668 WHERE b.weenie_Class_Id = 1054866 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062669 WHERE b.weenie_Class_Id = 1054866 AND s.value = 6602;

-- Stamina Tonic of Regeneration: WCID 1054867 -> 1062670-1062679 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062670 WHERE b.weenie_Class_Id = 1054867 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062671 WHERE b.weenie_Class_Id = 1054867 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062672 WHERE b.weenie_Class_Id = 1054867 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062673 WHERE b.weenie_Class_Id = 1054867 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062674 WHERE b.weenie_Class_Id = 1054867 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062675 WHERE b.weenie_Class_Id = 1054867 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062676 WHERE b.weenie_Class_Id = 1054867 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062677 WHERE b.weenie_Class_Id = 1054867 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062678 WHERE b.weenie_Class_Id = 1054867 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062679 WHERE b.weenie_Class_Id = 1054867 AND s.value = 6694;

-- Stamina Tonic of Rejuvenation: WCID 1054868 -> 1062680-1062689 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062680 WHERE b.weenie_Class_Id = 1054868 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062681 WHERE b.weenie_Class_Id = 1054868 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062682 WHERE b.weenie_Class_Id = 1054868 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062683 WHERE b.weenie_Class_Id = 1054868 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062684 WHERE b.weenie_Class_Id = 1054868 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062685 WHERE b.weenie_Class_Id = 1054868 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062686 WHERE b.weenie_Class_Id = 1054868 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062687 WHERE b.weenie_Class_Id = 1054868 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062688 WHERE b.weenie_Class_Id = 1054868 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062689 WHERE b.weenie_Class_Id = 1054868 AND s.value = 6704;

-- Stamina Tonic of Clarity: WCID 1054869 -> 1062690-1062699 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062690 WHERE b.weenie_Class_Id = 1054869 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062691 WHERE b.weenie_Class_Id = 1054869 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062692 WHERE b.weenie_Class_Id = 1054869 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062693 WHERE b.weenie_Class_Id = 1054869 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062694 WHERE b.weenie_Class_Id = 1054869 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062695 WHERE b.weenie_Class_Id = 1054869 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062696 WHERE b.weenie_Class_Id = 1054869 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062697 WHERE b.weenie_Class_Id = 1054869 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062698 WHERE b.weenie_Class_Id = 1054869 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062699 WHERE b.weenie_Class_Id = 1054869 AND s.value = 6714;

-- Stamina Tonic of Blood Drinker: WCID 1054870 -> 1062700-1062709 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062700 WHERE b.weenie_Class_Id = 1054870 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062701 WHERE b.weenie_Class_Id = 1054870 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062702 WHERE b.weenie_Class_Id = 1054870 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062703 WHERE b.weenie_Class_Id = 1054870 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062704 WHERE b.weenie_Class_Id = 1054870 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062705 WHERE b.weenie_Class_Id = 1054870 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062706 WHERE b.weenie_Class_Id = 1054870 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062707 WHERE b.weenie_Class_Id = 1054870 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062708 WHERE b.weenie_Class_Id = 1054870 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062709 WHERE b.weenie_Class_Id = 1054870 AND s.value = 6744;

-- Stamina Tonic of Spirit Drinker: WCID 1054871 -> 1062710-1062719 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062710 WHERE b.weenie_Class_Id = 1054871 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062711 WHERE b.weenie_Class_Id = 1054871 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062712 WHERE b.weenie_Class_Id = 1054871 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062713 WHERE b.weenie_Class_Id = 1054871 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062714 WHERE b.weenie_Class_Id = 1054871 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062715 WHERE b.weenie_Class_Id = 1054871 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062716 WHERE b.weenie_Class_Id = 1054871 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062717 WHERE b.weenie_Class_Id = 1054871 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062718 WHERE b.weenie_Class_Id = 1054871 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062719 WHERE b.weenie_Class_Id = 1054871 AND s.value = 6774;

-- Stamina Tonic of Heart Seeker: WCID 1054872 -> 1062720-1062729 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062720 WHERE b.weenie_Class_Id = 1054872 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062721 WHERE b.weenie_Class_Id = 1054872 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062722 WHERE b.weenie_Class_Id = 1054872 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062723 WHERE b.weenie_Class_Id = 1054872 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062724 WHERE b.weenie_Class_Id = 1054872 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062725 WHERE b.weenie_Class_Id = 1054872 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062726 WHERE b.weenie_Class_Id = 1054872 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062727 WHERE b.weenie_Class_Id = 1054872 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062728 WHERE b.weenie_Class_Id = 1054872 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062729 WHERE b.weenie_Class_Id = 1054872 AND s.value = 6754;

-- Stamina Tonic of Swift Killer: WCID 1054873 -> 1062730-1062739 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062730 WHERE b.weenie_Class_Id = 1054873 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062731 WHERE b.weenie_Class_Id = 1054873 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062732 WHERE b.weenie_Class_Id = 1054873 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062733 WHERE b.weenie_Class_Id = 1054873 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062734 WHERE b.weenie_Class_Id = 1054873 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062735 WHERE b.weenie_Class_Id = 1054873 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062736 WHERE b.weenie_Class_Id = 1054873 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062737 WHERE b.weenie_Class_Id = 1054873 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062738 WHERE b.weenie_Class_Id = 1054873 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062739 WHERE b.weenie_Class_Id = 1054873 AND s.value = 6764;

-- Stamina Tonic of Defender: WCID 1054874 -> 1062740-1062749 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062740 WHERE b.weenie_Class_Id = 1054874 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062741 WHERE b.weenie_Class_Id = 1054874 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062742 WHERE b.weenie_Class_Id = 1054874 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062743 WHERE b.weenie_Class_Id = 1054874 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062744 WHERE b.weenie_Class_Id = 1054874 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062745 WHERE b.weenie_Class_Id = 1054874 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062746 WHERE b.weenie_Class_Id = 1054874 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062747 WHERE b.weenie_Class_Id = 1054874 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062748 WHERE b.weenie_Class_Id = 1054874 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062749 WHERE b.weenie_Class_Id = 1054874 AND s.value = 6784;

-- Stamina Tonic of Critical Chance: WCID 1054875 -> 1062750-1062759 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062750 WHERE b.weenie_Class_Id = 1054875 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062751 WHERE b.weenie_Class_Id = 1054875 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062752 WHERE b.weenie_Class_Id = 1054875 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062753 WHERE b.weenie_Class_Id = 1054875 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062754 WHERE b.weenie_Class_Id = 1054875 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062755 WHERE b.weenie_Class_Id = 1054875 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062756 WHERE b.weenie_Class_Id = 1054875 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062757 WHERE b.weenie_Class_Id = 1054875 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062758 WHERE b.weenie_Class_Id = 1054875 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062759 WHERE b.weenie_Class_Id = 1054875 AND s.value = 6724;

-- Stamina Tonic of Critical Damage: WCID 1054876 -> 1062760-1062769 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062760 WHERE b.weenie_Class_Id = 1054876 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062761 WHERE b.weenie_Class_Id = 1054876 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062762 WHERE b.weenie_Class_Id = 1054876 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062763 WHERE b.weenie_Class_Id = 1054876 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062764 WHERE b.weenie_Class_Id = 1054876 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062765 WHERE b.weenie_Class_Id = 1054876 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062766 WHERE b.weenie_Class_Id = 1054876 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062767 WHERE b.weenie_Class_Id = 1054876 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062768 WHERE b.weenie_Class_Id = 1054876 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062769 WHERE b.weenie_Class_Id = 1054876 AND s.value = 6734;

-- Stamina Tonic of Slashing Protection: WCID 1054877 -> 1062770-1062779 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062770 WHERE b.weenie_Class_Id = 1054877 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062771 WHERE b.weenie_Class_Id = 1054877 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062772 WHERE b.weenie_Class_Id = 1054877 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062773 WHERE b.weenie_Class_Id = 1054877 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062774 WHERE b.weenie_Class_Id = 1054877 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062775 WHERE b.weenie_Class_Id = 1054877 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062776 WHERE b.weenie_Class_Id = 1054877 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062777 WHERE b.weenie_Class_Id = 1054877 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062778 WHERE b.weenie_Class_Id = 1054877 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062779 WHERE b.weenie_Class_Id = 1054877 AND s.value = 6612;

-- Stamina Tonic of Piercing Protection: WCID 1054878 -> 1062780-1062789 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062780 WHERE b.weenie_Class_Id = 1054878 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062781 WHERE b.weenie_Class_Id = 1054878 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062782 WHERE b.weenie_Class_Id = 1054878 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062783 WHERE b.weenie_Class_Id = 1054878 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062784 WHERE b.weenie_Class_Id = 1054878 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062785 WHERE b.weenie_Class_Id = 1054878 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062786 WHERE b.weenie_Class_Id = 1054878 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062787 WHERE b.weenie_Class_Id = 1054878 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062788 WHERE b.weenie_Class_Id = 1054878 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062789 WHERE b.weenie_Class_Id = 1054878 AND s.value = 6622;

-- Stamina Tonic of Bludgeoning Protection: WCID 1054879 -> 1062790-1062799 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062790 WHERE b.weenie_Class_Id = 1054879 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062791 WHERE b.weenie_Class_Id = 1054879 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062792 WHERE b.weenie_Class_Id = 1054879 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062793 WHERE b.weenie_Class_Id = 1054879 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062794 WHERE b.weenie_Class_Id = 1054879 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062795 WHERE b.weenie_Class_Id = 1054879 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062796 WHERE b.weenie_Class_Id = 1054879 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062797 WHERE b.weenie_Class_Id = 1054879 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062798 WHERE b.weenie_Class_Id = 1054879 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062799 WHERE b.weenie_Class_Id = 1054879 AND s.value = 6632;

-- Stamina Tonic of Acid Protection: WCID 1054880 -> 1062800-1062809 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062800 WHERE b.weenie_Class_Id = 1054880 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062801 WHERE b.weenie_Class_Id = 1054880 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062802 WHERE b.weenie_Class_Id = 1054880 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062803 WHERE b.weenie_Class_Id = 1054880 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062804 WHERE b.weenie_Class_Id = 1054880 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062805 WHERE b.weenie_Class_Id = 1054880 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062806 WHERE b.weenie_Class_Id = 1054880 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062807 WHERE b.weenie_Class_Id = 1054880 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062808 WHERE b.weenie_Class_Id = 1054880 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062809 WHERE b.weenie_Class_Id = 1054880 AND s.value = 6642;

-- Stamina Tonic of Fire Protection: WCID 1054881 -> 1062810-1062819 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062810 WHERE b.weenie_Class_Id = 1054881 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062811 WHERE b.weenie_Class_Id = 1054881 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062812 WHERE b.weenie_Class_Id = 1054881 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062813 WHERE b.weenie_Class_Id = 1054881 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062814 WHERE b.weenie_Class_Id = 1054881 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062815 WHERE b.weenie_Class_Id = 1054881 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062816 WHERE b.weenie_Class_Id = 1054881 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062817 WHERE b.weenie_Class_Id = 1054881 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062818 WHERE b.weenie_Class_Id = 1054881 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062819 WHERE b.weenie_Class_Id = 1054881 AND s.value = 6652;

-- Stamina Tonic of Cold Protection: WCID 1054882 -> 1062820-1062829 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062820 WHERE b.weenie_Class_Id = 1054882 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062821 WHERE b.weenie_Class_Id = 1054882 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062822 WHERE b.weenie_Class_Id = 1054882 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062823 WHERE b.weenie_Class_Id = 1054882 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062824 WHERE b.weenie_Class_Id = 1054882 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062825 WHERE b.weenie_Class_Id = 1054882 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062826 WHERE b.weenie_Class_Id = 1054882 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062827 WHERE b.weenie_Class_Id = 1054882 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062828 WHERE b.weenie_Class_Id = 1054882 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062829 WHERE b.weenie_Class_Id = 1054882 AND s.value = 6662;

-- Stamina Tonic of Lightning Protection: WCID 1054883 -> 1062830-1062839 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062830 WHERE b.weenie_Class_Id = 1054883 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062831 WHERE b.weenie_Class_Id = 1054883 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062832 WHERE b.weenie_Class_Id = 1054883 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062833 WHERE b.weenie_Class_Id = 1054883 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062834 WHERE b.weenie_Class_Id = 1054883 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062835 WHERE b.weenie_Class_Id = 1054883 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062836 WHERE b.weenie_Class_Id = 1054883 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062837 WHERE b.weenie_Class_Id = 1054883 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062838 WHERE b.weenie_Class_Id = 1054883 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062839 WHERE b.weenie_Class_Id = 1054883 AND s.value = 6672;

-- Mana Tonic of Armor: WCID 1054884 -> 1062840-1062849 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062840 WHERE b.weenie_Class_Id = 1054884 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062841 WHERE b.weenie_Class_Id = 1054884 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062842 WHERE b.weenie_Class_Id = 1054884 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062843 WHERE b.weenie_Class_Id = 1054884 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062844 WHERE b.weenie_Class_Id = 1054884 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062845 WHERE b.weenie_Class_Id = 1054884 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062846 WHERE b.weenie_Class_Id = 1054884 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062847 WHERE b.weenie_Class_Id = 1054884 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062848 WHERE b.weenie_Class_Id = 1054884 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062849 WHERE b.weenie_Class_Id = 1054884 AND s.value = 6592;

-- Mana Tonic of Warding: WCID 1054885 -> 1062850-1062859 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062850 WHERE b.weenie_Class_Id = 1054885 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062851 WHERE b.weenie_Class_Id = 1054885 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062852 WHERE b.weenie_Class_Id = 1054885 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062853 WHERE b.weenie_Class_Id = 1054885 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062854 WHERE b.weenie_Class_Id = 1054885 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062855 WHERE b.weenie_Class_Id = 1054885 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062856 WHERE b.weenie_Class_Id = 1054885 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062857 WHERE b.weenie_Class_Id = 1054885 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062858 WHERE b.weenie_Class_Id = 1054885 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062859 WHERE b.weenie_Class_Id = 1054885 AND s.value = 6602;

-- Mana Tonic of Regeneration: WCID 1054886 -> 1062860-1062869 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062860 WHERE b.weenie_Class_Id = 1054886 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062861 WHERE b.weenie_Class_Id = 1054886 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062862 WHERE b.weenie_Class_Id = 1054886 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062863 WHERE b.weenie_Class_Id = 1054886 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062864 WHERE b.weenie_Class_Id = 1054886 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062865 WHERE b.weenie_Class_Id = 1054886 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062866 WHERE b.weenie_Class_Id = 1054886 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062867 WHERE b.weenie_Class_Id = 1054886 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062868 WHERE b.weenie_Class_Id = 1054886 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062869 WHERE b.weenie_Class_Id = 1054886 AND s.value = 6694;

-- Mana Tonic of Rejuvenation: WCID 1054887 -> 1062870-1062879 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062870 WHERE b.weenie_Class_Id = 1054887 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062871 WHERE b.weenie_Class_Id = 1054887 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062872 WHERE b.weenie_Class_Id = 1054887 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062873 WHERE b.weenie_Class_Id = 1054887 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062874 WHERE b.weenie_Class_Id = 1054887 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062875 WHERE b.weenie_Class_Id = 1054887 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062876 WHERE b.weenie_Class_Id = 1054887 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062877 WHERE b.weenie_Class_Id = 1054887 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062878 WHERE b.weenie_Class_Id = 1054887 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062879 WHERE b.weenie_Class_Id = 1054887 AND s.value = 6704;

-- Mana Tonic of Clarity: WCID 1054888 -> 1062880-1062889 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062880 WHERE b.weenie_Class_Id = 1054888 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062881 WHERE b.weenie_Class_Id = 1054888 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062882 WHERE b.weenie_Class_Id = 1054888 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062883 WHERE b.weenie_Class_Id = 1054888 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062884 WHERE b.weenie_Class_Id = 1054888 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062885 WHERE b.weenie_Class_Id = 1054888 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062886 WHERE b.weenie_Class_Id = 1054888 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062887 WHERE b.weenie_Class_Id = 1054888 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062888 WHERE b.weenie_Class_Id = 1054888 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062889 WHERE b.weenie_Class_Id = 1054888 AND s.value = 6714;

-- Mana Tonic of Blood Drinker: WCID 1054889 -> 1062890-1062899 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062890 WHERE b.weenie_Class_Id = 1054889 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062891 WHERE b.weenie_Class_Id = 1054889 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062892 WHERE b.weenie_Class_Id = 1054889 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062893 WHERE b.weenie_Class_Id = 1054889 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062894 WHERE b.weenie_Class_Id = 1054889 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062895 WHERE b.weenie_Class_Id = 1054889 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062896 WHERE b.weenie_Class_Id = 1054889 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062897 WHERE b.weenie_Class_Id = 1054889 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062898 WHERE b.weenie_Class_Id = 1054889 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062899 WHERE b.weenie_Class_Id = 1054889 AND s.value = 6744;

-- Mana Tonic of Spirit Drinker: WCID 1054890 -> 1062900-1062909 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062900 WHERE b.weenie_Class_Id = 1054890 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062901 WHERE b.weenie_Class_Id = 1054890 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062902 WHERE b.weenie_Class_Id = 1054890 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062903 WHERE b.weenie_Class_Id = 1054890 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062904 WHERE b.weenie_Class_Id = 1054890 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062905 WHERE b.weenie_Class_Id = 1054890 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062906 WHERE b.weenie_Class_Id = 1054890 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062907 WHERE b.weenie_Class_Id = 1054890 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062908 WHERE b.weenie_Class_Id = 1054890 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062909 WHERE b.weenie_Class_Id = 1054890 AND s.value = 6774;

-- Mana Tonic of Heart Seeker: WCID 1054891 -> 1062910-1062919 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062910 WHERE b.weenie_Class_Id = 1054891 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062911 WHERE b.weenie_Class_Id = 1054891 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062912 WHERE b.weenie_Class_Id = 1054891 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062913 WHERE b.weenie_Class_Id = 1054891 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062914 WHERE b.weenie_Class_Id = 1054891 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062915 WHERE b.weenie_Class_Id = 1054891 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062916 WHERE b.weenie_Class_Id = 1054891 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062917 WHERE b.weenie_Class_Id = 1054891 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062918 WHERE b.weenie_Class_Id = 1054891 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062919 WHERE b.weenie_Class_Id = 1054891 AND s.value = 6754;

-- Mana Tonic of Swift Killer: WCID 1054892 -> 1062920-1062929 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062920 WHERE b.weenie_Class_Id = 1054892 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062921 WHERE b.weenie_Class_Id = 1054892 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062922 WHERE b.weenie_Class_Id = 1054892 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062923 WHERE b.weenie_Class_Id = 1054892 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062924 WHERE b.weenie_Class_Id = 1054892 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062925 WHERE b.weenie_Class_Id = 1054892 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062926 WHERE b.weenie_Class_Id = 1054892 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062927 WHERE b.weenie_Class_Id = 1054892 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062928 WHERE b.weenie_Class_Id = 1054892 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062929 WHERE b.weenie_Class_Id = 1054892 AND s.value = 6764;

-- Mana Tonic of Defender: WCID 1054893 -> 1062930-1062939 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062930 WHERE b.weenie_Class_Id = 1054893 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062931 WHERE b.weenie_Class_Id = 1054893 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062932 WHERE b.weenie_Class_Id = 1054893 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062933 WHERE b.weenie_Class_Id = 1054893 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062934 WHERE b.weenie_Class_Id = 1054893 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062935 WHERE b.weenie_Class_Id = 1054893 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062936 WHERE b.weenie_Class_Id = 1054893 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062937 WHERE b.weenie_Class_Id = 1054893 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062938 WHERE b.weenie_Class_Id = 1054893 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062939 WHERE b.weenie_Class_Id = 1054893 AND s.value = 6784;

-- Mana Tonic of Critical Chance: WCID 1054894 -> 1062940-1062949 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062940 WHERE b.weenie_Class_Id = 1054894 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062941 WHERE b.weenie_Class_Id = 1054894 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062942 WHERE b.weenie_Class_Id = 1054894 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062943 WHERE b.weenie_Class_Id = 1054894 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062944 WHERE b.weenie_Class_Id = 1054894 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062945 WHERE b.weenie_Class_Id = 1054894 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062946 WHERE b.weenie_Class_Id = 1054894 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062947 WHERE b.weenie_Class_Id = 1054894 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062948 WHERE b.weenie_Class_Id = 1054894 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062949 WHERE b.weenie_Class_Id = 1054894 AND s.value = 6724;

-- Mana Tonic of Critical Damage: WCID 1054895 -> 1062950-1062959 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062950 WHERE b.weenie_Class_Id = 1054895 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062951 WHERE b.weenie_Class_Id = 1054895 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062952 WHERE b.weenie_Class_Id = 1054895 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062953 WHERE b.weenie_Class_Id = 1054895 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062954 WHERE b.weenie_Class_Id = 1054895 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062955 WHERE b.weenie_Class_Id = 1054895 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062956 WHERE b.weenie_Class_Id = 1054895 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062957 WHERE b.weenie_Class_Id = 1054895 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062958 WHERE b.weenie_Class_Id = 1054895 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062959 WHERE b.weenie_Class_Id = 1054895 AND s.value = 6734;

-- Mana Tonic of Slashing Protection: WCID 1054896 -> 1062960-1062969 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062960 WHERE b.weenie_Class_Id = 1054896 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062961 WHERE b.weenie_Class_Id = 1054896 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062962 WHERE b.weenie_Class_Id = 1054896 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062963 WHERE b.weenie_Class_Id = 1054896 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062964 WHERE b.weenie_Class_Id = 1054896 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062965 WHERE b.weenie_Class_Id = 1054896 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062966 WHERE b.weenie_Class_Id = 1054896 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062967 WHERE b.weenie_Class_Id = 1054896 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062968 WHERE b.weenie_Class_Id = 1054896 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062969 WHERE b.weenie_Class_Id = 1054896 AND s.value = 6612;

-- Mana Tonic of Piercing Protection: WCID 1054897 -> 1062970-1062979 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062970 WHERE b.weenie_Class_Id = 1054897 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062971 WHERE b.weenie_Class_Id = 1054897 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062972 WHERE b.weenie_Class_Id = 1054897 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062973 WHERE b.weenie_Class_Id = 1054897 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062974 WHERE b.weenie_Class_Id = 1054897 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062975 WHERE b.weenie_Class_Id = 1054897 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062976 WHERE b.weenie_Class_Id = 1054897 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062977 WHERE b.weenie_Class_Id = 1054897 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062978 WHERE b.weenie_Class_Id = 1054897 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062979 WHERE b.weenie_Class_Id = 1054897 AND s.value = 6622;

-- Mana Tonic of Bludgeoning Protection: WCID 1054898 -> 1062980-1062989 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062980 WHERE b.weenie_Class_Id = 1054898 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062981 WHERE b.weenie_Class_Id = 1054898 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062982 WHERE b.weenie_Class_Id = 1054898 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062983 WHERE b.weenie_Class_Id = 1054898 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062984 WHERE b.weenie_Class_Id = 1054898 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062985 WHERE b.weenie_Class_Id = 1054898 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062986 WHERE b.weenie_Class_Id = 1054898 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062987 WHERE b.weenie_Class_Id = 1054898 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062988 WHERE b.weenie_Class_Id = 1054898 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062989 WHERE b.weenie_Class_Id = 1054898 AND s.value = 6632;

-- Mana Tonic of Acid Protection: WCID 1054899 -> 1062990-1062999 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062990 WHERE b.weenie_Class_Id = 1054899 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062991 WHERE b.weenie_Class_Id = 1054899 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062992 WHERE b.weenie_Class_Id = 1054899 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062993 WHERE b.weenie_Class_Id = 1054899 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062994 WHERE b.weenie_Class_Id = 1054899 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062995 WHERE b.weenie_Class_Id = 1054899 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062996 WHERE b.weenie_Class_Id = 1054899 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062997 WHERE b.weenie_Class_Id = 1054899 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062998 WHERE b.weenie_Class_Id = 1054899 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1062999 WHERE b.weenie_Class_Id = 1054899 AND s.value = 6642;

-- Mana Tonic of Fire Protection: WCID 1054900 -> 1063000-1063009 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063000 WHERE b.weenie_Class_Id = 1054900 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063001 WHERE b.weenie_Class_Id = 1054900 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063002 WHERE b.weenie_Class_Id = 1054900 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063003 WHERE b.weenie_Class_Id = 1054900 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063004 WHERE b.weenie_Class_Id = 1054900 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063005 WHERE b.weenie_Class_Id = 1054900 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063006 WHERE b.weenie_Class_Id = 1054900 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063007 WHERE b.weenie_Class_Id = 1054900 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063008 WHERE b.weenie_Class_Id = 1054900 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063009 WHERE b.weenie_Class_Id = 1054900 AND s.value = 6652;

-- Mana Tonic of Cold Protection: WCID 1054901 -> 1063010-1063019 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063010 WHERE b.weenie_Class_Id = 1054901 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063011 WHERE b.weenie_Class_Id = 1054901 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063012 WHERE b.weenie_Class_Id = 1054901 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063013 WHERE b.weenie_Class_Id = 1054901 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063014 WHERE b.weenie_Class_Id = 1054901 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063015 WHERE b.weenie_Class_Id = 1054901 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063016 WHERE b.weenie_Class_Id = 1054901 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063017 WHERE b.weenie_Class_Id = 1054901 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063018 WHERE b.weenie_Class_Id = 1054901 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063019 WHERE b.weenie_Class_Id = 1054901 AND s.value = 6662;

-- Mana Tonic of Lightning Protection: WCID 1054902 -> 1063020-1063029 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063020 WHERE b.weenie_Class_Id = 1054902 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063021 WHERE b.weenie_Class_Id = 1054902 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063022 WHERE b.weenie_Class_Id = 1054902 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063023 WHERE b.weenie_Class_Id = 1054902 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063024 WHERE b.weenie_Class_Id = 1054902 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063025 WHERE b.weenie_Class_Id = 1054902 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063026 WHERE b.weenie_Class_Id = 1054902 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063027 WHERE b.weenie_Class_Id = 1054902 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063028 WHERE b.weenie_Class_Id = 1054902 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063029 WHERE b.weenie_Class_Id = 1054902 AND s.value = 6672;

-- Health Tincture of Armor: WCID 1054903 -> 1063030-1063039 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063030 WHERE b.weenie_Class_Id = 1054903 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063031 WHERE b.weenie_Class_Id = 1054903 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063032 WHERE b.weenie_Class_Id = 1054903 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063033 WHERE b.weenie_Class_Id = 1054903 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063034 WHERE b.weenie_Class_Id = 1054903 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063035 WHERE b.weenie_Class_Id = 1054903 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063036 WHERE b.weenie_Class_Id = 1054903 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063037 WHERE b.weenie_Class_Id = 1054903 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063038 WHERE b.weenie_Class_Id = 1054903 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063039 WHERE b.weenie_Class_Id = 1054903 AND s.value = 6592;

-- Health Tincture of Warding: WCID 1054904 -> 1063040-1063049 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063040 WHERE b.weenie_Class_Id = 1054904 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063041 WHERE b.weenie_Class_Id = 1054904 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063042 WHERE b.weenie_Class_Id = 1054904 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063043 WHERE b.weenie_Class_Id = 1054904 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063044 WHERE b.weenie_Class_Id = 1054904 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063045 WHERE b.weenie_Class_Id = 1054904 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063046 WHERE b.weenie_Class_Id = 1054904 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063047 WHERE b.weenie_Class_Id = 1054904 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063048 WHERE b.weenie_Class_Id = 1054904 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063049 WHERE b.weenie_Class_Id = 1054904 AND s.value = 6602;

-- Health Tincture of Regeneration: WCID 1054905 -> 1063050-1063059 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063050 WHERE b.weenie_Class_Id = 1054905 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063051 WHERE b.weenie_Class_Id = 1054905 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063052 WHERE b.weenie_Class_Id = 1054905 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063053 WHERE b.weenie_Class_Id = 1054905 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063054 WHERE b.weenie_Class_Id = 1054905 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063055 WHERE b.weenie_Class_Id = 1054905 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063056 WHERE b.weenie_Class_Id = 1054905 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063057 WHERE b.weenie_Class_Id = 1054905 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063058 WHERE b.weenie_Class_Id = 1054905 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063059 WHERE b.weenie_Class_Id = 1054905 AND s.value = 6694;

-- Health Tincture of Rejuvenation: WCID 1054906 -> 1063060-1063069 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063060 WHERE b.weenie_Class_Id = 1054906 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063061 WHERE b.weenie_Class_Id = 1054906 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063062 WHERE b.weenie_Class_Id = 1054906 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063063 WHERE b.weenie_Class_Id = 1054906 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063064 WHERE b.weenie_Class_Id = 1054906 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063065 WHERE b.weenie_Class_Id = 1054906 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063066 WHERE b.weenie_Class_Id = 1054906 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063067 WHERE b.weenie_Class_Id = 1054906 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063068 WHERE b.weenie_Class_Id = 1054906 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063069 WHERE b.weenie_Class_Id = 1054906 AND s.value = 6704;

-- Health Tincture of Clarity: WCID 1054907 -> 1063070-1063079 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063070 WHERE b.weenie_Class_Id = 1054907 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063071 WHERE b.weenie_Class_Id = 1054907 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063072 WHERE b.weenie_Class_Id = 1054907 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063073 WHERE b.weenie_Class_Id = 1054907 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063074 WHERE b.weenie_Class_Id = 1054907 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063075 WHERE b.weenie_Class_Id = 1054907 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063076 WHERE b.weenie_Class_Id = 1054907 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063077 WHERE b.weenie_Class_Id = 1054907 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063078 WHERE b.weenie_Class_Id = 1054907 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063079 WHERE b.weenie_Class_Id = 1054907 AND s.value = 6714;

-- Health Tincture of Blood Drinker: WCID 1054908 -> 1063080-1063089 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063080 WHERE b.weenie_Class_Id = 1054908 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063081 WHERE b.weenie_Class_Id = 1054908 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063082 WHERE b.weenie_Class_Id = 1054908 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063083 WHERE b.weenie_Class_Id = 1054908 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063084 WHERE b.weenie_Class_Id = 1054908 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063085 WHERE b.weenie_Class_Id = 1054908 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063086 WHERE b.weenie_Class_Id = 1054908 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063087 WHERE b.weenie_Class_Id = 1054908 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063088 WHERE b.weenie_Class_Id = 1054908 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063089 WHERE b.weenie_Class_Id = 1054908 AND s.value = 6744;

-- Health Tincture of Spirit Drinker: WCID 1054909 -> 1063090-1063099 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063090 WHERE b.weenie_Class_Id = 1054909 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063091 WHERE b.weenie_Class_Id = 1054909 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063092 WHERE b.weenie_Class_Id = 1054909 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063093 WHERE b.weenie_Class_Id = 1054909 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063094 WHERE b.weenie_Class_Id = 1054909 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063095 WHERE b.weenie_Class_Id = 1054909 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063096 WHERE b.weenie_Class_Id = 1054909 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063097 WHERE b.weenie_Class_Id = 1054909 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063098 WHERE b.weenie_Class_Id = 1054909 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063099 WHERE b.weenie_Class_Id = 1054909 AND s.value = 6774;

-- Health Tincture of Heart Seeker: WCID 1054910 -> 1063100-1063109 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063100 WHERE b.weenie_Class_Id = 1054910 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063101 WHERE b.weenie_Class_Id = 1054910 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063102 WHERE b.weenie_Class_Id = 1054910 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063103 WHERE b.weenie_Class_Id = 1054910 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063104 WHERE b.weenie_Class_Id = 1054910 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063105 WHERE b.weenie_Class_Id = 1054910 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063106 WHERE b.weenie_Class_Id = 1054910 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063107 WHERE b.weenie_Class_Id = 1054910 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063108 WHERE b.weenie_Class_Id = 1054910 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063109 WHERE b.weenie_Class_Id = 1054910 AND s.value = 6754;

-- Health Tincture of Swift Killer: WCID 1054911 -> 1063110-1063119 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063110 WHERE b.weenie_Class_Id = 1054911 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063111 WHERE b.weenie_Class_Id = 1054911 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063112 WHERE b.weenie_Class_Id = 1054911 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063113 WHERE b.weenie_Class_Id = 1054911 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063114 WHERE b.weenie_Class_Id = 1054911 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063115 WHERE b.weenie_Class_Id = 1054911 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063116 WHERE b.weenie_Class_Id = 1054911 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063117 WHERE b.weenie_Class_Id = 1054911 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063118 WHERE b.weenie_Class_Id = 1054911 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063119 WHERE b.weenie_Class_Id = 1054911 AND s.value = 6764;

-- Health Tincture of Defender: WCID 1054912 -> 1063120-1063129 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063120 WHERE b.weenie_Class_Id = 1054912 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063121 WHERE b.weenie_Class_Id = 1054912 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063122 WHERE b.weenie_Class_Id = 1054912 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063123 WHERE b.weenie_Class_Id = 1054912 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063124 WHERE b.weenie_Class_Id = 1054912 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063125 WHERE b.weenie_Class_Id = 1054912 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063126 WHERE b.weenie_Class_Id = 1054912 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063127 WHERE b.weenie_Class_Id = 1054912 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063128 WHERE b.weenie_Class_Id = 1054912 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063129 WHERE b.weenie_Class_Id = 1054912 AND s.value = 6784;

-- Health Tincture of Critical Chance: WCID 1054913 -> 1063130-1063139 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063130 WHERE b.weenie_Class_Id = 1054913 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063131 WHERE b.weenie_Class_Id = 1054913 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063132 WHERE b.weenie_Class_Id = 1054913 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063133 WHERE b.weenie_Class_Id = 1054913 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063134 WHERE b.weenie_Class_Id = 1054913 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063135 WHERE b.weenie_Class_Id = 1054913 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063136 WHERE b.weenie_Class_Id = 1054913 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063137 WHERE b.weenie_Class_Id = 1054913 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063138 WHERE b.weenie_Class_Id = 1054913 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063139 WHERE b.weenie_Class_Id = 1054913 AND s.value = 6724;

-- Health Tincture of Critical Damage: WCID 1054914 -> 1063140-1063149 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063140 WHERE b.weenie_Class_Id = 1054914 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063141 WHERE b.weenie_Class_Id = 1054914 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063142 WHERE b.weenie_Class_Id = 1054914 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063143 WHERE b.weenie_Class_Id = 1054914 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063144 WHERE b.weenie_Class_Id = 1054914 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063145 WHERE b.weenie_Class_Id = 1054914 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063146 WHERE b.weenie_Class_Id = 1054914 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063147 WHERE b.weenie_Class_Id = 1054914 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063148 WHERE b.weenie_Class_Id = 1054914 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063149 WHERE b.weenie_Class_Id = 1054914 AND s.value = 6734;

-- Health Tincture of Slashing Protection: WCID 1054915 -> 1063150-1063159 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063150 WHERE b.weenie_Class_Id = 1054915 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063151 WHERE b.weenie_Class_Id = 1054915 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063152 WHERE b.weenie_Class_Id = 1054915 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063153 WHERE b.weenie_Class_Id = 1054915 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063154 WHERE b.weenie_Class_Id = 1054915 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063155 WHERE b.weenie_Class_Id = 1054915 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063156 WHERE b.weenie_Class_Id = 1054915 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063157 WHERE b.weenie_Class_Id = 1054915 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063158 WHERE b.weenie_Class_Id = 1054915 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063159 WHERE b.weenie_Class_Id = 1054915 AND s.value = 6612;

-- Health Tincture of Piercing Protection: WCID 1054916 -> 1063160-1063169 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063160 WHERE b.weenie_Class_Id = 1054916 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063161 WHERE b.weenie_Class_Id = 1054916 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063162 WHERE b.weenie_Class_Id = 1054916 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063163 WHERE b.weenie_Class_Id = 1054916 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063164 WHERE b.weenie_Class_Id = 1054916 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063165 WHERE b.weenie_Class_Id = 1054916 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063166 WHERE b.weenie_Class_Id = 1054916 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063167 WHERE b.weenie_Class_Id = 1054916 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063168 WHERE b.weenie_Class_Id = 1054916 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063169 WHERE b.weenie_Class_Id = 1054916 AND s.value = 6622;

-- Health Tincture of Bludgeoning Protection: WCID 1054917 -> 1063170-1063179 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063170 WHERE b.weenie_Class_Id = 1054917 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063171 WHERE b.weenie_Class_Id = 1054917 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063172 WHERE b.weenie_Class_Id = 1054917 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063173 WHERE b.weenie_Class_Id = 1054917 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063174 WHERE b.weenie_Class_Id = 1054917 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063175 WHERE b.weenie_Class_Id = 1054917 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063176 WHERE b.weenie_Class_Id = 1054917 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063177 WHERE b.weenie_Class_Id = 1054917 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063178 WHERE b.weenie_Class_Id = 1054917 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063179 WHERE b.weenie_Class_Id = 1054917 AND s.value = 6632;

-- Health Tincture of Acid Protection: WCID 1054918 -> 1063180-1063189 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063180 WHERE b.weenie_Class_Id = 1054918 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063181 WHERE b.weenie_Class_Id = 1054918 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063182 WHERE b.weenie_Class_Id = 1054918 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063183 WHERE b.weenie_Class_Id = 1054918 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063184 WHERE b.weenie_Class_Id = 1054918 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063185 WHERE b.weenie_Class_Id = 1054918 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063186 WHERE b.weenie_Class_Id = 1054918 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063187 WHERE b.weenie_Class_Id = 1054918 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063188 WHERE b.weenie_Class_Id = 1054918 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063189 WHERE b.weenie_Class_Id = 1054918 AND s.value = 6642;

-- Health Tincture of Fire Protection: WCID 1054919 -> 1063190-1063199 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063190 WHERE b.weenie_Class_Id = 1054919 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063191 WHERE b.weenie_Class_Id = 1054919 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063192 WHERE b.weenie_Class_Id = 1054919 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063193 WHERE b.weenie_Class_Id = 1054919 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063194 WHERE b.weenie_Class_Id = 1054919 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063195 WHERE b.weenie_Class_Id = 1054919 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063196 WHERE b.weenie_Class_Id = 1054919 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063197 WHERE b.weenie_Class_Id = 1054919 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063198 WHERE b.weenie_Class_Id = 1054919 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063199 WHERE b.weenie_Class_Id = 1054919 AND s.value = 6652;

-- Health Tincture of Cold Protection: WCID 1054920 -> 1063200-1063209 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063200 WHERE b.weenie_Class_Id = 1054920 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063201 WHERE b.weenie_Class_Id = 1054920 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063202 WHERE b.weenie_Class_Id = 1054920 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063203 WHERE b.weenie_Class_Id = 1054920 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063204 WHERE b.weenie_Class_Id = 1054920 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063205 WHERE b.weenie_Class_Id = 1054920 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063206 WHERE b.weenie_Class_Id = 1054920 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063207 WHERE b.weenie_Class_Id = 1054920 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063208 WHERE b.weenie_Class_Id = 1054920 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063209 WHERE b.weenie_Class_Id = 1054920 AND s.value = 6662;

-- Health Tincture of Lightning Protection: WCID 1054921 -> 1063210-1063219 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063210 WHERE b.weenie_Class_Id = 1054921 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063211 WHERE b.weenie_Class_Id = 1054921 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063212 WHERE b.weenie_Class_Id = 1054921 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063213 WHERE b.weenie_Class_Id = 1054921 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063214 WHERE b.weenie_Class_Id = 1054921 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063215 WHERE b.weenie_Class_Id = 1054921 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063216 WHERE b.weenie_Class_Id = 1054921 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063217 WHERE b.weenie_Class_Id = 1054921 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063218 WHERE b.weenie_Class_Id = 1054921 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063219 WHERE b.weenie_Class_Id = 1054921 AND s.value = 6672;

-- Stamina Tincture of Armor: WCID 1054922 -> 1063220-1063229 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063220 WHERE b.weenie_Class_Id = 1054922 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063221 WHERE b.weenie_Class_Id = 1054922 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063222 WHERE b.weenie_Class_Id = 1054922 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063223 WHERE b.weenie_Class_Id = 1054922 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063224 WHERE b.weenie_Class_Id = 1054922 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063225 WHERE b.weenie_Class_Id = 1054922 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063226 WHERE b.weenie_Class_Id = 1054922 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063227 WHERE b.weenie_Class_Id = 1054922 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063228 WHERE b.weenie_Class_Id = 1054922 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063229 WHERE b.weenie_Class_Id = 1054922 AND s.value = 6592;

-- Stamina Tincture of Warding: WCID 1054923 -> 1063230-1063239 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063230 WHERE b.weenie_Class_Id = 1054923 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063231 WHERE b.weenie_Class_Id = 1054923 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063232 WHERE b.weenie_Class_Id = 1054923 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063233 WHERE b.weenie_Class_Id = 1054923 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063234 WHERE b.weenie_Class_Id = 1054923 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063235 WHERE b.weenie_Class_Id = 1054923 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063236 WHERE b.weenie_Class_Id = 1054923 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063237 WHERE b.weenie_Class_Id = 1054923 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063238 WHERE b.weenie_Class_Id = 1054923 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063239 WHERE b.weenie_Class_Id = 1054923 AND s.value = 6602;

-- Stamina Tincture of Regeneration: WCID 1054924 -> 1063240-1063249 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063240 WHERE b.weenie_Class_Id = 1054924 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063241 WHERE b.weenie_Class_Id = 1054924 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063242 WHERE b.weenie_Class_Id = 1054924 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063243 WHERE b.weenie_Class_Id = 1054924 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063244 WHERE b.weenie_Class_Id = 1054924 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063245 WHERE b.weenie_Class_Id = 1054924 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063246 WHERE b.weenie_Class_Id = 1054924 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063247 WHERE b.weenie_Class_Id = 1054924 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063248 WHERE b.weenie_Class_Id = 1054924 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063249 WHERE b.weenie_Class_Id = 1054924 AND s.value = 6694;

-- Stamina Tincture of Rejuvenation: WCID 1054925 -> 1063250-1063259 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063250 WHERE b.weenie_Class_Id = 1054925 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063251 WHERE b.weenie_Class_Id = 1054925 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063252 WHERE b.weenie_Class_Id = 1054925 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063253 WHERE b.weenie_Class_Id = 1054925 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063254 WHERE b.weenie_Class_Id = 1054925 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063255 WHERE b.weenie_Class_Id = 1054925 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063256 WHERE b.weenie_Class_Id = 1054925 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063257 WHERE b.weenie_Class_Id = 1054925 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063258 WHERE b.weenie_Class_Id = 1054925 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063259 WHERE b.weenie_Class_Id = 1054925 AND s.value = 6704;

-- Stamina Tincture of Clarity: WCID 1054926 -> 1063260-1063269 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063260 WHERE b.weenie_Class_Id = 1054926 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063261 WHERE b.weenie_Class_Id = 1054926 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063262 WHERE b.weenie_Class_Id = 1054926 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063263 WHERE b.weenie_Class_Id = 1054926 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063264 WHERE b.weenie_Class_Id = 1054926 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063265 WHERE b.weenie_Class_Id = 1054926 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063266 WHERE b.weenie_Class_Id = 1054926 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063267 WHERE b.weenie_Class_Id = 1054926 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063268 WHERE b.weenie_Class_Id = 1054926 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063269 WHERE b.weenie_Class_Id = 1054926 AND s.value = 6714;

-- Stamina Tincture of Blood Drinker: WCID 1054927 -> 1063270-1063279 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063270 WHERE b.weenie_Class_Id = 1054927 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063271 WHERE b.weenie_Class_Id = 1054927 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063272 WHERE b.weenie_Class_Id = 1054927 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063273 WHERE b.weenie_Class_Id = 1054927 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063274 WHERE b.weenie_Class_Id = 1054927 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063275 WHERE b.weenie_Class_Id = 1054927 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063276 WHERE b.weenie_Class_Id = 1054927 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063277 WHERE b.weenie_Class_Id = 1054927 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063278 WHERE b.weenie_Class_Id = 1054927 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063279 WHERE b.weenie_Class_Id = 1054927 AND s.value = 6744;

-- Stamina Tincture of Spirit Drinker: WCID 1054928 -> 1063280-1063289 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063280 WHERE b.weenie_Class_Id = 1054928 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063281 WHERE b.weenie_Class_Id = 1054928 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063282 WHERE b.weenie_Class_Id = 1054928 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063283 WHERE b.weenie_Class_Id = 1054928 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063284 WHERE b.weenie_Class_Id = 1054928 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063285 WHERE b.weenie_Class_Id = 1054928 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063286 WHERE b.weenie_Class_Id = 1054928 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063287 WHERE b.weenie_Class_Id = 1054928 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063288 WHERE b.weenie_Class_Id = 1054928 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063289 WHERE b.weenie_Class_Id = 1054928 AND s.value = 6774;

-- Stamina Tincture of Heart Seeker: WCID 1054929 -> 1063290-1063299 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063290 WHERE b.weenie_Class_Id = 1054929 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063291 WHERE b.weenie_Class_Id = 1054929 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063292 WHERE b.weenie_Class_Id = 1054929 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063293 WHERE b.weenie_Class_Id = 1054929 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063294 WHERE b.weenie_Class_Id = 1054929 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063295 WHERE b.weenie_Class_Id = 1054929 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063296 WHERE b.weenie_Class_Id = 1054929 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063297 WHERE b.weenie_Class_Id = 1054929 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063298 WHERE b.weenie_Class_Id = 1054929 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063299 WHERE b.weenie_Class_Id = 1054929 AND s.value = 6754;

-- Stamina Tincture of Swift Killer: WCID 1054930 -> 1063300-1063309 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063300 WHERE b.weenie_Class_Id = 1054930 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063301 WHERE b.weenie_Class_Id = 1054930 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063302 WHERE b.weenie_Class_Id = 1054930 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063303 WHERE b.weenie_Class_Id = 1054930 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063304 WHERE b.weenie_Class_Id = 1054930 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063305 WHERE b.weenie_Class_Id = 1054930 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063306 WHERE b.weenie_Class_Id = 1054930 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063307 WHERE b.weenie_Class_Id = 1054930 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063308 WHERE b.weenie_Class_Id = 1054930 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063309 WHERE b.weenie_Class_Id = 1054930 AND s.value = 6764;

-- Stamina Tincture of Defender: WCID 1054931 -> 1063310-1063319 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063310 WHERE b.weenie_Class_Id = 1054931 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063311 WHERE b.weenie_Class_Id = 1054931 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063312 WHERE b.weenie_Class_Id = 1054931 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063313 WHERE b.weenie_Class_Id = 1054931 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063314 WHERE b.weenie_Class_Id = 1054931 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063315 WHERE b.weenie_Class_Id = 1054931 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063316 WHERE b.weenie_Class_Id = 1054931 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063317 WHERE b.weenie_Class_Id = 1054931 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063318 WHERE b.weenie_Class_Id = 1054931 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063319 WHERE b.weenie_Class_Id = 1054931 AND s.value = 6784;

-- Stamina Tincture of Critical Chance: WCID 1054932 -> 1063320-1063329 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063320 WHERE b.weenie_Class_Id = 1054932 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063321 WHERE b.weenie_Class_Id = 1054932 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063322 WHERE b.weenie_Class_Id = 1054932 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063323 WHERE b.weenie_Class_Id = 1054932 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063324 WHERE b.weenie_Class_Id = 1054932 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063325 WHERE b.weenie_Class_Id = 1054932 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063326 WHERE b.weenie_Class_Id = 1054932 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063327 WHERE b.weenie_Class_Id = 1054932 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063328 WHERE b.weenie_Class_Id = 1054932 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063329 WHERE b.weenie_Class_Id = 1054932 AND s.value = 6724;

-- Stamina Tincture of Critical Damage: WCID 1054933 -> 1063330-1063339 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063330 WHERE b.weenie_Class_Id = 1054933 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063331 WHERE b.weenie_Class_Id = 1054933 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063332 WHERE b.weenie_Class_Id = 1054933 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063333 WHERE b.weenie_Class_Id = 1054933 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063334 WHERE b.weenie_Class_Id = 1054933 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063335 WHERE b.weenie_Class_Id = 1054933 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063336 WHERE b.weenie_Class_Id = 1054933 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063337 WHERE b.weenie_Class_Id = 1054933 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063338 WHERE b.weenie_Class_Id = 1054933 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063339 WHERE b.weenie_Class_Id = 1054933 AND s.value = 6734;

-- Stamina Tincture of Slashing Protection: WCID 1054934 -> 1063340-1063349 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063340 WHERE b.weenie_Class_Id = 1054934 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063341 WHERE b.weenie_Class_Id = 1054934 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063342 WHERE b.weenie_Class_Id = 1054934 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063343 WHERE b.weenie_Class_Id = 1054934 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063344 WHERE b.weenie_Class_Id = 1054934 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063345 WHERE b.weenie_Class_Id = 1054934 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063346 WHERE b.weenie_Class_Id = 1054934 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063347 WHERE b.weenie_Class_Id = 1054934 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063348 WHERE b.weenie_Class_Id = 1054934 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063349 WHERE b.weenie_Class_Id = 1054934 AND s.value = 6612;

-- Stamina Tincture of Piercing Protection: WCID 1054935 -> 1063350-1063359 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063350 WHERE b.weenie_Class_Id = 1054935 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063351 WHERE b.weenie_Class_Id = 1054935 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063352 WHERE b.weenie_Class_Id = 1054935 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063353 WHERE b.weenie_Class_Id = 1054935 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063354 WHERE b.weenie_Class_Id = 1054935 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063355 WHERE b.weenie_Class_Id = 1054935 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063356 WHERE b.weenie_Class_Id = 1054935 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063357 WHERE b.weenie_Class_Id = 1054935 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063358 WHERE b.weenie_Class_Id = 1054935 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063359 WHERE b.weenie_Class_Id = 1054935 AND s.value = 6622;

-- Stamina Tincture of Bludgeoning Protection: WCID 1054936 -> 1063360-1063369 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063360 WHERE b.weenie_Class_Id = 1054936 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063361 WHERE b.weenie_Class_Id = 1054936 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063362 WHERE b.weenie_Class_Id = 1054936 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063363 WHERE b.weenie_Class_Id = 1054936 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063364 WHERE b.weenie_Class_Id = 1054936 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063365 WHERE b.weenie_Class_Id = 1054936 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063366 WHERE b.weenie_Class_Id = 1054936 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063367 WHERE b.weenie_Class_Id = 1054936 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063368 WHERE b.weenie_Class_Id = 1054936 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063369 WHERE b.weenie_Class_Id = 1054936 AND s.value = 6632;

-- Stamina Tincture of Acid Protection: WCID 1054937 -> 1063370-1063379 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063370 WHERE b.weenie_Class_Id = 1054937 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063371 WHERE b.weenie_Class_Id = 1054937 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063372 WHERE b.weenie_Class_Id = 1054937 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063373 WHERE b.weenie_Class_Id = 1054937 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063374 WHERE b.weenie_Class_Id = 1054937 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063375 WHERE b.weenie_Class_Id = 1054937 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063376 WHERE b.weenie_Class_Id = 1054937 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063377 WHERE b.weenie_Class_Id = 1054937 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063378 WHERE b.weenie_Class_Id = 1054937 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063379 WHERE b.weenie_Class_Id = 1054937 AND s.value = 6642;

-- Stamina Tincture of Fire Protection: WCID 1054938 -> 1063380-1063389 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063380 WHERE b.weenie_Class_Id = 1054938 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063381 WHERE b.weenie_Class_Id = 1054938 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063382 WHERE b.weenie_Class_Id = 1054938 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063383 WHERE b.weenie_Class_Id = 1054938 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063384 WHERE b.weenie_Class_Id = 1054938 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063385 WHERE b.weenie_Class_Id = 1054938 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063386 WHERE b.weenie_Class_Id = 1054938 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063387 WHERE b.weenie_Class_Id = 1054938 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063388 WHERE b.weenie_Class_Id = 1054938 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063389 WHERE b.weenie_Class_Id = 1054938 AND s.value = 6652;

-- Stamina Tincture of Cold Protection: WCID 1054939 -> 1063390-1063399 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063390 WHERE b.weenie_Class_Id = 1054939 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063391 WHERE b.weenie_Class_Id = 1054939 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063392 WHERE b.weenie_Class_Id = 1054939 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063393 WHERE b.weenie_Class_Id = 1054939 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063394 WHERE b.weenie_Class_Id = 1054939 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063395 WHERE b.weenie_Class_Id = 1054939 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063396 WHERE b.weenie_Class_Id = 1054939 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063397 WHERE b.weenie_Class_Id = 1054939 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063398 WHERE b.weenie_Class_Id = 1054939 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063399 WHERE b.weenie_Class_Id = 1054939 AND s.value = 6662;

-- Stamina Tincture of Lightning Protection: WCID 1054940 -> 1063400-1063409 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063400 WHERE b.weenie_Class_Id = 1054940 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063401 WHERE b.weenie_Class_Id = 1054940 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063402 WHERE b.weenie_Class_Id = 1054940 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063403 WHERE b.weenie_Class_Id = 1054940 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063404 WHERE b.weenie_Class_Id = 1054940 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063405 WHERE b.weenie_Class_Id = 1054940 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063406 WHERE b.weenie_Class_Id = 1054940 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063407 WHERE b.weenie_Class_Id = 1054940 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063408 WHERE b.weenie_Class_Id = 1054940 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063409 WHERE b.weenie_Class_Id = 1054940 AND s.value = 6672;

-- Mana Tincture of Armor: WCID 1054941 -> 1063410-1063419 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063410 WHERE b.weenie_Class_Id = 1054941 AND s.value = 6583;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063411 WHERE b.weenie_Class_Id = 1054941 AND s.value = 6584;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063412 WHERE b.weenie_Class_Id = 1054941 AND s.value = 6585;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063413 WHERE b.weenie_Class_Id = 1054941 AND s.value = 6586;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063414 WHERE b.weenie_Class_Id = 1054941 AND s.value = 6587;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063415 WHERE b.weenie_Class_Id = 1054941 AND s.value = 6588;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063416 WHERE b.weenie_Class_Id = 1054941 AND s.value = 6589;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063417 WHERE b.weenie_Class_Id = 1054941 AND s.value = 6590;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063418 WHERE b.weenie_Class_Id = 1054941 AND s.value = 6591;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063419 WHERE b.weenie_Class_Id = 1054941 AND s.value = 6592;

-- Mana Tincture of Warding: WCID 1054942 -> 1063420-1063429 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063420 WHERE b.weenie_Class_Id = 1054942 AND s.value = 6593;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063421 WHERE b.weenie_Class_Id = 1054942 AND s.value = 6594;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063422 WHERE b.weenie_Class_Id = 1054942 AND s.value = 6595;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063423 WHERE b.weenie_Class_Id = 1054942 AND s.value = 6596;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063424 WHERE b.weenie_Class_Id = 1054942 AND s.value = 6597;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063425 WHERE b.weenie_Class_Id = 1054942 AND s.value = 6598;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063426 WHERE b.weenie_Class_Id = 1054942 AND s.value = 6599;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063427 WHERE b.weenie_Class_Id = 1054942 AND s.value = 6600;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063428 WHERE b.weenie_Class_Id = 1054942 AND s.value = 6601;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063429 WHERE b.weenie_Class_Id = 1054942 AND s.value = 6602;

-- Mana Tincture of Regeneration: WCID 1054943 -> 1063430-1063439 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063430 WHERE b.weenie_Class_Id = 1054943 AND s.value = 6685;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063431 WHERE b.weenie_Class_Id = 1054943 AND s.value = 6686;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063432 WHERE b.weenie_Class_Id = 1054943 AND s.value = 6687;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063433 WHERE b.weenie_Class_Id = 1054943 AND s.value = 6688;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063434 WHERE b.weenie_Class_Id = 1054943 AND s.value = 6689;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063435 WHERE b.weenie_Class_Id = 1054943 AND s.value = 6690;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063436 WHERE b.weenie_Class_Id = 1054943 AND s.value = 6691;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063437 WHERE b.weenie_Class_Id = 1054943 AND s.value = 6692;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063438 WHERE b.weenie_Class_Id = 1054943 AND s.value = 6693;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063439 WHERE b.weenie_Class_Id = 1054943 AND s.value = 6694;

-- Mana Tincture of Rejuvenation: WCID 1054944 -> 1063440-1063449 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063440 WHERE b.weenie_Class_Id = 1054944 AND s.value = 6695;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063441 WHERE b.weenie_Class_Id = 1054944 AND s.value = 6696;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063442 WHERE b.weenie_Class_Id = 1054944 AND s.value = 6697;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063443 WHERE b.weenie_Class_Id = 1054944 AND s.value = 6698;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063444 WHERE b.weenie_Class_Id = 1054944 AND s.value = 6699;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063445 WHERE b.weenie_Class_Id = 1054944 AND s.value = 6700;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063446 WHERE b.weenie_Class_Id = 1054944 AND s.value = 6701;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063447 WHERE b.weenie_Class_Id = 1054944 AND s.value = 6702;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063448 WHERE b.weenie_Class_Id = 1054944 AND s.value = 6703;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063449 WHERE b.weenie_Class_Id = 1054944 AND s.value = 6704;

-- Mana Tincture of Clarity: WCID 1054945 -> 1063450-1063459 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063450 WHERE b.weenie_Class_Id = 1054945 AND s.value = 6705;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063451 WHERE b.weenie_Class_Id = 1054945 AND s.value = 6706;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063452 WHERE b.weenie_Class_Id = 1054945 AND s.value = 6707;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063453 WHERE b.weenie_Class_Id = 1054945 AND s.value = 6708;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063454 WHERE b.weenie_Class_Id = 1054945 AND s.value = 6709;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063455 WHERE b.weenie_Class_Id = 1054945 AND s.value = 6710;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063456 WHERE b.weenie_Class_Id = 1054945 AND s.value = 6711;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063457 WHERE b.weenie_Class_Id = 1054945 AND s.value = 6712;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063458 WHERE b.weenie_Class_Id = 1054945 AND s.value = 6713;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063459 WHERE b.weenie_Class_Id = 1054945 AND s.value = 6714;

-- Mana Tincture of Blood Drinker: WCID 1054946 -> 1063460-1063469 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063460 WHERE b.weenie_Class_Id = 1054946 AND s.value = 6735;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063461 WHERE b.weenie_Class_Id = 1054946 AND s.value = 6736;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063462 WHERE b.weenie_Class_Id = 1054946 AND s.value = 6737;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063463 WHERE b.weenie_Class_Id = 1054946 AND s.value = 6738;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063464 WHERE b.weenie_Class_Id = 1054946 AND s.value = 6739;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063465 WHERE b.weenie_Class_Id = 1054946 AND s.value = 6740;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063466 WHERE b.weenie_Class_Id = 1054946 AND s.value = 6741;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063467 WHERE b.weenie_Class_Id = 1054946 AND s.value = 6742;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063468 WHERE b.weenie_Class_Id = 1054946 AND s.value = 6743;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063469 WHERE b.weenie_Class_Id = 1054946 AND s.value = 6744;

-- Mana Tincture of Spirit Drinker: WCID 1054947 -> 1063470-1063479 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063470 WHERE b.weenie_Class_Id = 1054947 AND s.value = 6765;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063471 WHERE b.weenie_Class_Id = 1054947 AND s.value = 6766;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063472 WHERE b.weenie_Class_Id = 1054947 AND s.value = 6767;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063473 WHERE b.weenie_Class_Id = 1054947 AND s.value = 6768;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063474 WHERE b.weenie_Class_Id = 1054947 AND s.value = 6769;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063475 WHERE b.weenie_Class_Id = 1054947 AND s.value = 6770;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063476 WHERE b.weenie_Class_Id = 1054947 AND s.value = 6771;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063477 WHERE b.weenie_Class_Id = 1054947 AND s.value = 6772;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063478 WHERE b.weenie_Class_Id = 1054947 AND s.value = 6773;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063479 WHERE b.weenie_Class_Id = 1054947 AND s.value = 6774;

-- Mana Tincture of Heart Seeker: WCID 1054948 -> 1063480-1063489 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063480 WHERE b.weenie_Class_Id = 1054948 AND s.value = 6745;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063481 WHERE b.weenie_Class_Id = 1054948 AND s.value = 6746;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063482 WHERE b.weenie_Class_Id = 1054948 AND s.value = 6747;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063483 WHERE b.weenie_Class_Id = 1054948 AND s.value = 6748;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063484 WHERE b.weenie_Class_Id = 1054948 AND s.value = 6749;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063485 WHERE b.weenie_Class_Id = 1054948 AND s.value = 6750;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063486 WHERE b.weenie_Class_Id = 1054948 AND s.value = 6751;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063487 WHERE b.weenie_Class_Id = 1054948 AND s.value = 6752;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063488 WHERE b.weenie_Class_Id = 1054948 AND s.value = 6753;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063489 WHERE b.weenie_Class_Id = 1054948 AND s.value = 6754;

-- Mana Tincture of Swift Killer: WCID 1054949 -> 1063490-1063499 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063490 WHERE b.weenie_Class_Id = 1054949 AND s.value = 6755;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063491 WHERE b.weenie_Class_Id = 1054949 AND s.value = 6756;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063492 WHERE b.weenie_Class_Id = 1054949 AND s.value = 6757;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063493 WHERE b.weenie_Class_Id = 1054949 AND s.value = 6758;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063494 WHERE b.weenie_Class_Id = 1054949 AND s.value = 6759;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063495 WHERE b.weenie_Class_Id = 1054949 AND s.value = 6760;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063496 WHERE b.weenie_Class_Id = 1054949 AND s.value = 6761;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063497 WHERE b.weenie_Class_Id = 1054949 AND s.value = 6762;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063498 WHERE b.weenie_Class_Id = 1054949 AND s.value = 6763;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063499 WHERE b.weenie_Class_Id = 1054949 AND s.value = 6764;

-- Mana Tincture of Defender: WCID 1054950 -> 1063500-1063509 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063500 WHERE b.weenie_Class_Id = 1054950 AND s.value = 6775;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063501 WHERE b.weenie_Class_Id = 1054950 AND s.value = 6776;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063502 WHERE b.weenie_Class_Id = 1054950 AND s.value = 6777;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063503 WHERE b.weenie_Class_Id = 1054950 AND s.value = 6778;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063504 WHERE b.weenie_Class_Id = 1054950 AND s.value = 6779;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063505 WHERE b.weenie_Class_Id = 1054950 AND s.value = 6780;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063506 WHERE b.weenie_Class_Id = 1054950 AND s.value = 6781;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063507 WHERE b.weenie_Class_Id = 1054950 AND s.value = 6782;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063508 WHERE b.weenie_Class_Id = 1054950 AND s.value = 6783;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063509 WHERE b.weenie_Class_Id = 1054950 AND s.value = 6784;

-- Mana Tincture of Critical Chance: WCID 1054951 -> 1063510-1063519 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063510 WHERE b.weenie_Class_Id = 1054951 AND s.value = 6715;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063511 WHERE b.weenie_Class_Id = 1054951 AND s.value = 6716;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063512 WHERE b.weenie_Class_Id = 1054951 AND s.value = 6717;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063513 WHERE b.weenie_Class_Id = 1054951 AND s.value = 6718;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063514 WHERE b.weenie_Class_Id = 1054951 AND s.value = 6719;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063515 WHERE b.weenie_Class_Id = 1054951 AND s.value = 6720;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063516 WHERE b.weenie_Class_Id = 1054951 AND s.value = 6721;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063517 WHERE b.weenie_Class_Id = 1054951 AND s.value = 6722;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063518 WHERE b.weenie_Class_Id = 1054951 AND s.value = 6723;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063519 WHERE b.weenie_Class_Id = 1054951 AND s.value = 6724;

-- Mana Tincture of Critical Damage: WCID 1054952 -> 1063520-1063529 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063520 WHERE b.weenie_Class_Id = 1054952 AND s.value = 6725;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063521 WHERE b.weenie_Class_Id = 1054952 AND s.value = 6726;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063522 WHERE b.weenie_Class_Id = 1054952 AND s.value = 6727;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063523 WHERE b.weenie_Class_Id = 1054952 AND s.value = 6728;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063524 WHERE b.weenie_Class_Id = 1054952 AND s.value = 6729;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063525 WHERE b.weenie_Class_Id = 1054952 AND s.value = 6730;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063526 WHERE b.weenie_Class_Id = 1054952 AND s.value = 6731;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063527 WHERE b.weenie_Class_Id = 1054952 AND s.value = 6732;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063528 WHERE b.weenie_Class_Id = 1054952 AND s.value = 6733;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063529 WHERE b.weenie_Class_Id = 1054952 AND s.value = 6734;

-- Mana Tincture of Slashing Protection: WCID 1054953 -> 1063530-1063539 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063530 WHERE b.weenie_Class_Id = 1054953 AND s.value = 6603;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063531 WHERE b.weenie_Class_Id = 1054953 AND s.value = 6604;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063532 WHERE b.weenie_Class_Id = 1054953 AND s.value = 6605;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063533 WHERE b.weenie_Class_Id = 1054953 AND s.value = 6606;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063534 WHERE b.weenie_Class_Id = 1054953 AND s.value = 6607;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063535 WHERE b.weenie_Class_Id = 1054953 AND s.value = 6608;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063536 WHERE b.weenie_Class_Id = 1054953 AND s.value = 6609;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063537 WHERE b.weenie_Class_Id = 1054953 AND s.value = 6610;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063538 WHERE b.weenie_Class_Id = 1054953 AND s.value = 6611;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063539 WHERE b.weenie_Class_Id = 1054953 AND s.value = 6612;

-- Mana Tincture of Piercing Protection: WCID 1054954 -> 1063540-1063549 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063540 WHERE b.weenie_Class_Id = 1054954 AND s.value = 6613;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063541 WHERE b.weenie_Class_Id = 1054954 AND s.value = 6614;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063542 WHERE b.weenie_Class_Id = 1054954 AND s.value = 6615;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063543 WHERE b.weenie_Class_Id = 1054954 AND s.value = 6616;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063544 WHERE b.weenie_Class_Id = 1054954 AND s.value = 6617;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063545 WHERE b.weenie_Class_Id = 1054954 AND s.value = 6618;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063546 WHERE b.weenie_Class_Id = 1054954 AND s.value = 6619;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063547 WHERE b.weenie_Class_Id = 1054954 AND s.value = 6620;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063548 WHERE b.weenie_Class_Id = 1054954 AND s.value = 6621;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063549 WHERE b.weenie_Class_Id = 1054954 AND s.value = 6622;

-- Mana Tincture of Bludgeoning Protection: WCID 1054955 -> 1063550-1063559 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063550 WHERE b.weenie_Class_Id = 1054955 AND s.value = 6623;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063551 WHERE b.weenie_Class_Id = 1054955 AND s.value = 6624;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063552 WHERE b.weenie_Class_Id = 1054955 AND s.value = 6625;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063553 WHERE b.weenie_Class_Id = 1054955 AND s.value = 6626;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063554 WHERE b.weenie_Class_Id = 1054955 AND s.value = 6627;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063555 WHERE b.weenie_Class_Id = 1054955 AND s.value = 6628;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063556 WHERE b.weenie_Class_Id = 1054955 AND s.value = 6629;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063557 WHERE b.weenie_Class_Id = 1054955 AND s.value = 6630;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063558 WHERE b.weenie_Class_Id = 1054955 AND s.value = 6631;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063559 WHERE b.weenie_Class_Id = 1054955 AND s.value = 6632;

-- Mana Tincture of Acid Protection: WCID 1054956 -> 1063560-1063569 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063560 WHERE b.weenie_Class_Id = 1054956 AND s.value = 6633;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063561 WHERE b.weenie_Class_Id = 1054956 AND s.value = 6634;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063562 WHERE b.weenie_Class_Id = 1054956 AND s.value = 6635;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063563 WHERE b.weenie_Class_Id = 1054956 AND s.value = 6636;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063564 WHERE b.weenie_Class_Id = 1054956 AND s.value = 6637;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063565 WHERE b.weenie_Class_Id = 1054956 AND s.value = 6638;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063566 WHERE b.weenie_Class_Id = 1054956 AND s.value = 6639;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063567 WHERE b.weenie_Class_Id = 1054956 AND s.value = 6640;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063568 WHERE b.weenie_Class_Id = 1054956 AND s.value = 6641;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063569 WHERE b.weenie_Class_Id = 1054956 AND s.value = 6642;

-- Mana Tincture of Fire Protection: WCID 1054957 -> 1063570-1063579 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063570 WHERE b.weenie_Class_Id = 1054957 AND s.value = 6643;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063571 WHERE b.weenie_Class_Id = 1054957 AND s.value = 6644;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063572 WHERE b.weenie_Class_Id = 1054957 AND s.value = 6645;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063573 WHERE b.weenie_Class_Id = 1054957 AND s.value = 6646;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063574 WHERE b.weenie_Class_Id = 1054957 AND s.value = 6647;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063575 WHERE b.weenie_Class_Id = 1054957 AND s.value = 6648;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063576 WHERE b.weenie_Class_Id = 1054957 AND s.value = 6649;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063577 WHERE b.weenie_Class_Id = 1054957 AND s.value = 6650;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063578 WHERE b.weenie_Class_Id = 1054957 AND s.value = 6651;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063579 WHERE b.weenie_Class_Id = 1054957 AND s.value = 6652;

-- Mana Tincture of Cold Protection: WCID 1054958 -> 1063580-1063589 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063580 WHERE b.weenie_Class_Id = 1054958 AND s.value = 6653;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063581 WHERE b.weenie_Class_Id = 1054958 AND s.value = 6654;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063582 WHERE b.weenie_Class_Id = 1054958 AND s.value = 6655;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063583 WHERE b.weenie_Class_Id = 1054958 AND s.value = 6656;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063584 WHERE b.weenie_Class_Id = 1054958 AND s.value = 6657;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063585 WHERE b.weenie_Class_Id = 1054958 AND s.value = 6658;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063586 WHERE b.weenie_Class_Id = 1054958 AND s.value = 6659;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063587 WHERE b.weenie_Class_Id = 1054958 AND s.value = 6660;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063588 WHERE b.weenie_Class_Id = 1054958 AND s.value = 6661;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063589 WHERE b.weenie_Class_Id = 1054958 AND s.value = 6662;

-- Mana Tincture of Lightning Protection: WCID 1054959 -> 1063590-1063599 (tiers 1-10)
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063590 WHERE b.weenie_Class_Id = 1054959 AND s.value = 6663;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063591 WHERE b.weenie_Class_Id = 1054959 AND s.value = 6664;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063592 WHERE b.weenie_Class_Id = 1054959 AND s.value = 6665;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063593 WHERE b.weenie_Class_Id = 1054959 AND s.value = 6666;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063594 WHERE b.weenie_Class_Id = 1054959 AND s.value = 6667;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063595 WHERE b.weenie_Class_Id = 1054959 AND s.value = 6668;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063596 WHERE b.weenie_Class_Id = 1054959 AND s.value = 6669;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063597 WHERE b.weenie_Class_Id = 1054959 AND s.value = 6670;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063598 WHERE b.weenie_Class_Id = 1054959 AND s.value = 6671;
UPDATE biota b JOIN biota_properties_d_i_d s ON s.object_Id = b.id AND s.`type` = 28 SET b.weenie_Class_Id = 1063599 WHERE b.weenie_Class_Id = 1054959 AND s.value = 6672;

-- Gristly Steak of Sudden Health: WCID 1054960 -> 1063600-1063609 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063600 WHERE b.weenie_Class_Id = 1054960 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063601 WHERE b.weenie_Class_Id = 1054960 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063602 WHERE b.weenie_Class_Id = 1054960 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063603 WHERE b.weenie_Class_Id = 1054960 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063604 WHERE b.weenie_Class_Id = 1054960 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063605 WHERE b.weenie_Class_Id = 1054960 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063606 WHERE b.weenie_Class_Id = 1054960 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063607 WHERE b.weenie_Class_Id = 1054960 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063608 WHERE b.weenie_Class_Id = 1054960 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063609 WHERE b.weenie_Class_Id = 1054960 AND s.value = 400;

-- Gristly Steak of Sudden Stamina: WCID 1054961 -> 1063610-1063619 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063610 WHERE b.weenie_Class_Id = 1054961 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063611 WHERE b.weenie_Class_Id = 1054961 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063612 WHERE b.weenie_Class_Id = 1054961 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063613 WHERE b.weenie_Class_Id = 1054961 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063614 WHERE b.weenie_Class_Id = 1054961 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063615 WHERE b.weenie_Class_Id = 1054961 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063616 WHERE b.weenie_Class_Id = 1054961 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063617 WHERE b.weenie_Class_Id = 1054961 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063618 WHERE b.weenie_Class_Id = 1054961 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063619 WHERE b.weenie_Class_Id = 1054961 AND s.value = 600;

-- Gristly Steak of Sudden Mana: WCID 1054962 -> 1063620-1063629 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063620 WHERE b.weenie_Class_Id = 1054962 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063621 WHERE b.weenie_Class_Id = 1054962 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063622 WHERE b.weenie_Class_Id = 1054962 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063623 WHERE b.weenie_Class_Id = 1054962 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063624 WHERE b.weenie_Class_Id = 1054962 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063625 WHERE b.weenie_Class_Id = 1054962 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063626 WHERE b.weenie_Class_Id = 1054962 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063627 WHERE b.weenie_Class_Id = 1054962 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063628 WHERE b.weenie_Class_Id = 1054962 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063629 WHERE b.weenie_Class_Id = 1054962 AND s.value = 600;

-- Gristly Pepper Steak of Sudden Health: WCID 1054963 -> 1063630-1063639 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063630 WHERE b.weenie_Class_Id = 1054963 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063631 WHERE b.weenie_Class_Id = 1054963 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063632 WHERE b.weenie_Class_Id = 1054963 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063633 WHERE b.weenie_Class_Id = 1054963 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063634 WHERE b.weenie_Class_Id = 1054963 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063635 WHERE b.weenie_Class_Id = 1054963 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063636 WHERE b.weenie_Class_Id = 1054963 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063637 WHERE b.weenie_Class_Id = 1054963 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063638 WHERE b.weenie_Class_Id = 1054963 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063639 WHERE b.weenie_Class_Id = 1054963 AND s.value = 400;

-- Gristly Pepper Steak of Sudden Stamina: WCID 1054964 -> 1063640-1063649 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063640 WHERE b.weenie_Class_Id = 1054964 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063641 WHERE b.weenie_Class_Id = 1054964 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063642 WHERE b.weenie_Class_Id = 1054964 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063643 WHERE b.weenie_Class_Id = 1054964 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063644 WHERE b.weenie_Class_Id = 1054964 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063645 WHERE b.weenie_Class_Id = 1054964 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063646 WHERE b.weenie_Class_Id = 1054964 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063647 WHERE b.weenie_Class_Id = 1054964 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063648 WHERE b.weenie_Class_Id = 1054964 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063649 WHERE b.weenie_Class_Id = 1054964 AND s.value = 600;

-- Gristly Pepper Steak of Sudden Mana: WCID 1054965 -> 1063650-1063659 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063650 WHERE b.weenie_Class_Id = 1054965 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063651 WHERE b.weenie_Class_Id = 1054965 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063652 WHERE b.weenie_Class_Id = 1054965 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063653 WHERE b.weenie_Class_Id = 1054965 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063654 WHERE b.weenie_Class_Id = 1054965 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063655 WHERE b.weenie_Class_Id = 1054965 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063656 WHERE b.weenie_Class_Id = 1054965 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063657 WHERE b.weenie_Class_Id = 1054965 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063658 WHERE b.weenie_Class_Id = 1054965 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063659 WHERE b.weenie_Class_Id = 1054965 AND s.value = 600;

-- Gristly Brined Steak of Sudden Health: WCID 1054966 -> 1063660-1063669 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063660 WHERE b.weenie_Class_Id = 1054966 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063661 WHERE b.weenie_Class_Id = 1054966 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063662 WHERE b.weenie_Class_Id = 1054966 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063663 WHERE b.weenie_Class_Id = 1054966 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063664 WHERE b.weenie_Class_Id = 1054966 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063665 WHERE b.weenie_Class_Id = 1054966 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063666 WHERE b.weenie_Class_Id = 1054966 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063667 WHERE b.weenie_Class_Id = 1054966 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063668 WHERE b.weenie_Class_Id = 1054966 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063669 WHERE b.weenie_Class_Id = 1054966 AND s.value = 400;

-- Gristly Brined Steak of Sudden Stamina: WCID 1054967 -> 1063670-1063679 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063670 WHERE b.weenie_Class_Id = 1054967 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063671 WHERE b.weenie_Class_Id = 1054967 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063672 WHERE b.weenie_Class_Id = 1054967 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063673 WHERE b.weenie_Class_Id = 1054967 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063674 WHERE b.weenie_Class_Id = 1054967 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063675 WHERE b.weenie_Class_Id = 1054967 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063676 WHERE b.weenie_Class_Id = 1054967 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063677 WHERE b.weenie_Class_Id = 1054967 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063678 WHERE b.weenie_Class_Id = 1054967 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063679 WHERE b.weenie_Class_Id = 1054967 AND s.value = 600;

-- Gristly Brined Steak of Sudden Mana: WCID 1054968 -> 1063680-1063689 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063680 WHERE b.weenie_Class_Id = 1054968 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063681 WHERE b.weenie_Class_Id = 1054968 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063682 WHERE b.weenie_Class_Id = 1054968 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063683 WHERE b.weenie_Class_Id = 1054968 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063684 WHERE b.weenie_Class_Id = 1054968 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063685 WHERE b.weenie_Class_Id = 1054968 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063686 WHERE b.weenie_Class_Id = 1054968 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063687 WHERE b.weenie_Class_Id = 1054968 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063688 WHERE b.weenie_Class_Id = 1054968 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063689 WHERE b.weenie_Class_Id = 1054968 AND s.value = 600;

-- Steak of Sudden Health: WCID 1054969 -> 1063690-1063699 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063690 WHERE b.weenie_Class_Id = 1054969 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063691 WHERE b.weenie_Class_Id = 1054969 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063692 WHERE b.weenie_Class_Id = 1054969 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063693 WHERE b.weenie_Class_Id = 1054969 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063694 WHERE b.weenie_Class_Id = 1054969 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063695 WHERE b.weenie_Class_Id = 1054969 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063696 WHERE b.weenie_Class_Id = 1054969 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063697 WHERE b.weenie_Class_Id = 1054969 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063698 WHERE b.weenie_Class_Id = 1054969 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063699 WHERE b.weenie_Class_Id = 1054969 AND s.value = 400;

-- Steak of Sudden Stamina: WCID 1054970 -> 1063700-1063709 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063700 WHERE b.weenie_Class_Id = 1054970 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063701 WHERE b.weenie_Class_Id = 1054970 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063702 WHERE b.weenie_Class_Id = 1054970 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063703 WHERE b.weenie_Class_Id = 1054970 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063704 WHERE b.weenie_Class_Id = 1054970 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063705 WHERE b.weenie_Class_Id = 1054970 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063706 WHERE b.weenie_Class_Id = 1054970 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063707 WHERE b.weenie_Class_Id = 1054970 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063708 WHERE b.weenie_Class_Id = 1054970 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063709 WHERE b.weenie_Class_Id = 1054970 AND s.value = 600;

-- Steak of Sudden Mana: WCID 1054971 -> 1063710-1063719 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063710 WHERE b.weenie_Class_Id = 1054971 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063711 WHERE b.weenie_Class_Id = 1054971 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063712 WHERE b.weenie_Class_Id = 1054971 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063713 WHERE b.weenie_Class_Id = 1054971 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063714 WHERE b.weenie_Class_Id = 1054971 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063715 WHERE b.weenie_Class_Id = 1054971 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063716 WHERE b.weenie_Class_Id = 1054971 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063717 WHERE b.weenie_Class_Id = 1054971 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063718 WHERE b.weenie_Class_Id = 1054971 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063719 WHERE b.weenie_Class_Id = 1054971 AND s.value = 600;

-- Pepper Steak of Sudden Health: WCID 1054972 -> 1063720-1063729 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063720 WHERE b.weenie_Class_Id = 1054972 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063721 WHERE b.weenie_Class_Id = 1054972 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063722 WHERE b.weenie_Class_Id = 1054972 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063723 WHERE b.weenie_Class_Id = 1054972 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063724 WHERE b.weenie_Class_Id = 1054972 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063725 WHERE b.weenie_Class_Id = 1054972 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063726 WHERE b.weenie_Class_Id = 1054972 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063727 WHERE b.weenie_Class_Id = 1054972 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063728 WHERE b.weenie_Class_Id = 1054972 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063729 WHERE b.weenie_Class_Id = 1054972 AND s.value = 400;

-- Pepper Steak of Sudden Stamina: WCID 1054973 -> 1063730-1063739 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063730 WHERE b.weenie_Class_Id = 1054973 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063731 WHERE b.weenie_Class_Id = 1054973 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063732 WHERE b.weenie_Class_Id = 1054973 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063733 WHERE b.weenie_Class_Id = 1054973 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063734 WHERE b.weenie_Class_Id = 1054973 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063735 WHERE b.weenie_Class_Id = 1054973 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063736 WHERE b.weenie_Class_Id = 1054973 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063737 WHERE b.weenie_Class_Id = 1054973 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063738 WHERE b.weenie_Class_Id = 1054973 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063739 WHERE b.weenie_Class_Id = 1054973 AND s.value = 600;

-- Pepper Steak of Sudden Mana: WCID 1054974 -> 1063740-1063749 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063740 WHERE b.weenie_Class_Id = 1054974 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063741 WHERE b.weenie_Class_Id = 1054974 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063742 WHERE b.weenie_Class_Id = 1054974 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063743 WHERE b.weenie_Class_Id = 1054974 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063744 WHERE b.weenie_Class_Id = 1054974 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063745 WHERE b.weenie_Class_Id = 1054974 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063746 WHERE b.weenie_Class_Id = 1054974 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063747 WHERE b.weenie_Class_Id = 1054974 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063748 WHERE b.weenie_Class_Id = 1054974 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063749 WHERE b.weenie_Class_Id = 1054974 AND s.value = 600;

-- Brined Steak of Sudden Health: WCID 1054975 -> 1063750-1063759 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063750 WHERE b.weenie_Class_Id = 1054975 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063751 WHERE b.weenie_Class_Id = 1054975 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063752 WHERE b.weenie_Class_Id = 1054975 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063753 WHERE b.weenie_Class_Id = 1054975 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063754 WHERE b.weenie_Class_Id = 1054975 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063755 WHERE b.weenie_Class_Id = 1054975 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063756 WHERE b.weenie_Class_Id = 1054975 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063757 WHERE b.weenie_Class_Id = 1054975 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063758 WHERE b.weenie_Class_Id = 1054975 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063759 WHERE b.weenie_Class_Id = 1054975 AND s.value = 400;

-- Brined Steak of Sudden Stamina: WCID 1054976 -> 1063760-1063769 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063760 WHERE b.weenie_Class_Id = 1054976 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063761 WHERE b.weenie_Class_Id = 1054976 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063762 WHERE b.weenie_Class_Id = 1054976 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063763 WHERE b.weenie_Class_Id = 1054976 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063764 WHERE b.weenie_Class_Id = 1054976 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063765 WHERE b.weenie_Class_Id = 1054976 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063766 WHERE b.weenie_Class_Id = 1054976 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063767 WHERE b.weenie_Class_Id = 1054976 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063768 WHERE b.weenie_Class_Id = 1054976 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063769 WHERE b.weenie_Class_Id = 1054976 AND s.value = 600;

-- Brined Steak of Sudden Mana: WCID 1054977 -> 1063770-1063779 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063770 WHERE b.weenie_Class_Id = 1054977 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063771 WHERE b.weenie_Class_Id = 1054977 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063772 WHERE b.weenie_Class_Id = 1054977 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063773 WHERE b.weenie_Class_Id = 1054977 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063774 WHERE b.weenie_Class_Id = 1054977 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063775 WHERE b.weenie_Class_Id = 1054977 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063776 WHERE b.weenie_Class_Id = 1054977 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063777 WHERE b.weenie_Class_Id = 1054977 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063778 WHERE b.weenie_Class_Id = 1054977 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063779 WHERE b.weenie_Class_Id = 1054977 AND s.value = 600;

-- Tender Steak of Sudden Health: WCID 1054978 -> 1063780-1063789 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063780 WHERE b.weenie_Class_Id = 1054978 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063781 WHERE b.weenie_Class_Id = 1054978 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063782 WHERE b.weenie_Class_Id = 1054978 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063783 WHERE b.weenie_Class_Id = 1054978 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063784 WHERE b.weenie_Class_Id = 1054978 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063785 WHERE b.weenie_Class_Id = 1054978 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063786 WHERE b.weenie_Class_Id = 1054978 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063787 WHERE b.weenie_Class_Id = 1054978 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063788 WHERE b.weenie_Class_Id = 1054978 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063789 WHERE b.weenie_Class_Id = 1054978 AND s.value = 400;

-- Tender Steak of Sudden Stamina: WCID 1054979 -> 1063790-1063799 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063790 WHERE b.weenie_Class_Id = 1054979 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063791 WHERE b.weenie_Class_Id = 1054979 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063792 WHERE b.weenie_Class_Id = 1054979 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063793 WHERE b.weenie_Class_Id = 1054979 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063794 WHERE b.weenie_Class_Id = 1054979 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063795 WHERE b.weenie_Class_Id = 1054979 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063796 WHERE b.weenie_Class_Id = 1054979 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063797 WHERE b.weenie_Class_Id = 1054979 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063798 WHERE b.weenie_Class_Id = 1054979 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063799 WHERE b.weenie_Class_Id = 1054979 AND s.value = 600;

-- Tender Steak of Sudden Mana: WCID 1054980 -> 1063800-1063809 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063800 WHERE b.weenie_Class_Id = 1054980 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063801 WHERE b.weenie_Class_Id = 1054980 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063802 WHERE b.weenie_Class_Id = 1054980 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063803 WHERE b.weenie_Class_Id = 1054980 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063804 WHERE b.weenie_Class_Id = 1054980 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063805 WHERE b.weenie_Class_Id = 1054980 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063806 WHERE b.weenie_Class_Id = 1054980 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063807 WHERE b.weenie_Class_Id = 1054980 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063808 WHERE b.weenie_Class_Id = 1054980 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063809 WHERE b.weenie_Class_Id = 1054980 AND s.value = 600;

-- Tender Pepper Steak of Sudden Health: WCID 1054981 -> 1063810-1063819 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063810 WHERE b.weenie_Class_Id = 1054981 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063811 WHERE b.weenie_Class_Id = 1054981 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063812 WHERE b.weenie_Class_Id = 1054981 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063813 WHERE b.weenie_Class_Id = 1054981 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063814 WHERE b.weenie_Class_Id = 1054981 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063815 WHERE b.weenie_Class_Id = 1054981 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063816 WHERE b.weenie_Class_Id = 1054981 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063817 WHERE b.weenie_Class_Id = 1054981 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063818 WHERE b.weenie_Class_Id = 1054981 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063819 WHERE b.weenie_Class_Id = 1054981 AND s.value = 400;

-- Tender Pepper Steak of Sudden Stamina: WCID 1054982 -> 1063820-1063829 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063820 WHERE b.weenie_Class_Id = 1054982 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063821 WHERE b.weenie_Class_Id = 1054982 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063822 WHERE b.weenie_Class_Id = 1054982 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063823 WHERE b.weenie_Class_Id = 1054982 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063824 WHERE b.weenie_Class_Id = 1054982 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063825 WHERE b.weenie_Class_Id = 1054982 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063826 WHERE b.weenie_Class_Id = 1054982 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063827 WHERE b.weenie_Class_Id = 1054982 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063828 WHERE b.weenie_Class_Id = 1054982 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063829 WHERE b.weenie_Class_Id = 1054982 AND s.value = 600;

-- Tender Pepper Steak of Sudden Mana: WCID 1054983 -> 1063830-1063839 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063830 WHERE b.weenie_Class_Id = 1054983 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063831 WHERE b.weenie_Class_Id = 1054983 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063832 WHERE b.weenie_Class_Id = 1054983 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063833 WHERE b.weenie_Class_Id = 1054983 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063834 WHERE b.weenie_Class_Id = 1054983 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063835 WHERE b.weenie_Class_Id = 1054983 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063836 WHERE b.weenie_Class_Id = 1054983 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063837 WHERE b.weenie_Class_Id = 1054983 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063838 WHERE b.weenie_Class_Id = 1054983 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063839 WHERE b.weenie_Class_Id = 1054983 AND s.value = 600;

-- Tender Brined Steak of Sudden Health: WCID 1054984 -> 1063840-1063849 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063840 WHERE b.weenie_Class_Id = 1054984 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063841 WHERE b.weenie_Class_Id = 1054984 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063842 WHERE b.weenie_Class_Id = 1054984 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063843 WHERE b.weenie_Class_Id = 1054984 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063844 WHERE b.weenie_Class_Id = 1054984 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063845 WHERE b.weenie_Class_Id = 1054984 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063846 WHERE b.weenie_Class_Id = 1054984 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063847 WHERE b.weenie_Class_Id = 1054984 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063848 WHERE b.weenie_Class_Id = 1054984 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063849 WHERE b.weenie_Class_Id = 1054984 AND s.value = 400;

-- Tender Brined Steak of Sudden Stamina: WCID 1054985 -> 1063850-1063859 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063850 WHERE b.weenie_Class_Id = 1054985 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063851 WHERE b.weenie_Class_Id = 1054985 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063852 WHERE b.weenie_Class_Id = 1054985 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063853 WHERE b.weenie_Class_Id = 1054985 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063854 WHERE b.weenie_Class_Id = 1054985 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063855 WHERE b.weenie_Class_Id = 1054985 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063856 WHERE b.weenie_Class_Id = 1054985 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063857 WHERE b.weenie_Class_Id = 1054985 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063858 WHERE b.weenie_Class_Id = 1054985 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063859 WHERE b.weenie_Class_Id = 1054985 AND s.value = 600;

-- Tender Brined Steak of Sudden Mana: WCID 1054986 -> 1063860-1063869 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063860 WHERE b.weenie_Class_Id = 1054986 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063861 WHERE b.weenie_Class_Id = 1054986 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063862 WHERE b.weenie_Class_Id = 1054986 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063863 WHERE b.weenie_Class_Id = 1054986 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063864 WHERE b.weenie_Class_Id = 1054986 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063865 WHERE b.weenie_Class_Id = 1054986 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063866 WHERE b.weenie_Class_Id = 1054986 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063867 WHERE b.weenie_Class_Id = 1054986 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063868 WHERE b.weenie_Class_Id = 1054986 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063869 WHERE b.weenie_Class_Id = 1054986 AND s.value = 600;

-- Choice Steak of Sudden Health: WCID 1054987 -> 1063870-1063879 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063870 WHERE b.weenie_Class_Id = 1054987 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063871 WHERE b.weenie_Class_Id = 1054987 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063872 WHERE b.weenie_Class_Id = 1054987 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063873 WHERE b.weenie_Class_Id = 1054987 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063874 WHERE b.weenie_Class_Id = 1054987 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063875 WHERE b.weenie_Class_Id = 1054987 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063876 WHERE b.weenie_Class_Id = 1054987 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063877 WHERE b.weenie_Class_Id = 1054987 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063878 WHERE b.weenie_Class_Id = 1054987 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063879 WHERE b.weenie_Class_Id = 1054987 AND s.value = 400;

-- Choice Steak of Sudden Stamina: WCID 1054988 -> 1063880-1063889 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063880 WHERE b.weenie_Class_Id = 1054988 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063881 WHERE b.weenie_Class_Id = 1054988 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063882 WHERE b.weenie_Class_Id = 1054988 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063883 WHERE b.weenie_Class_Id = 1054988 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063884 WHERE b.weenie_Class_Id = 1054988 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063885 WHERE b.weenie_Class_Id = 1054988 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063886 WHERE b.weenie_Class_Id = 1054988 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063887 WHERE b.weenie_Class_Id = 1054988 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063888 WHERE b.weenie_Class_Id = 1054988 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063889 WHERE b.weenie_Class_Id = 1054988 AND s.value = 600;

-- Choice Steak of Sudden Mana: WCID 1054989 -> 1063890-1063899 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063890 WHERE b.weenie_Class_Id = 1054989 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063891 WHERE b.weenie_Class_Id = 1054989 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063892 WHERE b.weenie_Class_Id = 1054989 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063893 WHERE b.weenie_Class_Id = 1054989 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063894 WHERE b.weenie_Class_Id = 1054989 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063895 WHERE b.weenie_Class_Id = 1054989 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063896 WHERE b.weenie_Class_Id = 1054989 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063897 WHERE b.weenie_Class_Id = 1054989 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063898 WHERE b.weenie_Class_Id = 1054989 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063899 WHERE b.weenie_Class_Id = 1054989 AND s.value = 600;

-- Choice Pepper Steak of Sudden Health: WCID 1054990 -> 1063900-1063909 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063900 WHERE b.weenie_Class_Id = 1054990 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063901 WHERE b.weenie_Class_Id = 1054990 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063902 WHERE b.weenie_Class_Id = 1054990 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063903 WHERE b.weenie_Class_Id = 1054990 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063904 WHERE b.weenie_Class_Id = 1054990 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063905 WHERE b.weenie_Class_Id = 1054990 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063906 WHERE b.weenie_Class_Id = 1054990 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063907 WHERE b.weenie_Class_Id = 1054990 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063908 WHERE b.weenie_Class_Id = 1054990 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063909 WHERE b.weenie_Class_Id = 1054990 AND s.value = 400;

-- Choice Pepper Steak of Sudden Stamina: WCID 1054991 -> 1063910-1063919 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063910 WHERE b.weenie_Class_Id = 1054991 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063911 WHERE b.weenie_Class_Id = 1054991 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063912 WHERE b.weenie_Class_Id = 1054991 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063913 WHERE b.weenie_Class_Id = 1054991 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063914 WHERE b.weenie_Class_Id = 1054991 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063915 WHERE b.weenie_Class_Id = 1054991 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063916 WHERE b.weenie_Class_Id = 1054991 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063917 WHERE b.weenie_Class_Id = 1054991 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063918 WHERE b.weenie_Class_Id = 1054991 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063919 WHERE b.weenie_Class_Id = 1054991 AND s.value = 600;

-- Choice Pepper Steak of Sudden Mana: WCID 1054992 -> 1063920-1063929 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063920 WHERE b.weenie_Class_Id = 1054992 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063921 WHERE b.weenie_Class_Id = 1054992 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063922 WHERE b.weenie_Class_Id = 1054992 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063923 WHERE b.weenie_Class_Id = 1054992 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063924 WHERE b.weenie_Class_Id = 1054992 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063925 WHERE b.weenie_Class_Id = 1054992 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063926 WHERE b.weenie_Class_Id = 1054992 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063927 WHERE b.weenie_Class_Id = 1054992 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063928 WHERE b.weenie_Class_Id = 1054992 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063929 WHERE b.weenie_Class_Id = 1054992 AND s.value = 600;

-- Choice Brined Steak of Sudden Health: WCID 1054993 -> 1063930-1063939 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063930 WHERE b.weenie_Class_Id = 1054993 AND s.value = 40;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063931 WHERE b.weenie_Class_Id = 1054993 AND s.value = 80;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063932 WHERE b.weenie_Class_Id = 1054993 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063933 WHERE b.weenie_Class_Id = 1054993 AND s.value = 160;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063934 WHERE b.weenie_Class_Id = 1054993 AND s.value = 200;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063935 WHERE b.weenie_Class_Id = 1054993 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063936 WHERE b.weenie_Class_Id = 1054993 AND s.value = 280;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063937 WHERE b.weenie_Class_Id = 1054993 AND s.value = 320;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063938 WHERE b.weenie_Class_Id = 1054993 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063939 WHERE b.weenie_Class_Id = 1054993 AND s.value = 400;

-- Choice Brined Steak of Sudden Stamina: WCID 1054994 -> 1063940-1063949 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063940 WHERE b.weenie_Class_Id = 1054994 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063941 WHERE b.weenie_Class_Id = 1054994 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063942 WHERE b.weenie_Class_Id = 1054994 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063943 WHERE b.weenie_Class_Id = 1054994 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063944 WHERE b.weenie_Class_Id = 1054994 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063945 WHERE b.weenie_Class_Id = 1054994 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063946 WHERE b.weenie_Class_Id = 1054994 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063947 WHERE b.weenie_Class_Id = 1054994 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063948 WHERE b.weenie_Class_Id = 1054994 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063949 WHERE b.weenie_Class_Id = 1054994 AND s.value = 600;

-- Choice Brined Steak of Sudden Mana: WCID 1054995 -> 1063950-1063959 (tiers 1-10)
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063950 WHERE b.weenie_Class_Id = 1054995 AND s.value = 60;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063951 WHERE b.weenie_Class_Id = 1054995 AND s.value = 120;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063952 WHERE b.weenie_Class_Id = 1054995 AND s.value = 180;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063953 WHERE b.weenie_Class_Id = 1054995 AND s.value = 240;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063954 WHERE b.weenie_Class_Id = 1054995 AND s.value = 300;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063955 WHERE b.weenie_Class_Id = 1054995 AND s.value = 360;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063956 WHERE b.weenie_Class_Id = 1054995 AND s.value = 420;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063957 WHERE b.weenie_Class_Id = 1054995 AND s.value = 480;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063958 WHERE b.weenie_Class_Id = 1054995 AND s.value = 540;
UPDATE biota b JOIN biota_properties_int s ON s.object_Id = b.id AND s.`type` = 90 SET b.weenie_Class_Id = 1063959 WHERE b.weenie_Class_Id = 1054995 AND s.value = 600;

COMMIT;

-- Post-migration verification (should return 0 rows): any biota still on an old, retired WCID
-- SELECT weenie_Class_Id, COUNT(*) FROM biota
-- WHERE weenie_Class_Id BETWEEN 1054600 AND 1054995 GROUP BY weenie_Class_Id;
