/*
 * The town event log's combat log: the swings and damage between the raiders and the players, guards and Captain, both
 * ways, for balancing the guards against the attackers. See ACE.Server.TownEvents.TownCombatStats.
 *
 * town_event
 *   combat   JSON, by "attacker>target" side (raider>guard, captain>raider, ...): swings, how many were evaded, blocked
 *            and parried, the landed swings' damage before and after mitigation, and all the damage dealt (spells
 *            included). Null for an attack with no fighting.
 */

ALTER TABLE `town_event`
  ADD COLUMN `combat` TEXT NULL AFTER `other_kills`;
