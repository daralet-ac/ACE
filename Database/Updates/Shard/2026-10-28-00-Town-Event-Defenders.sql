/*
 * The town event log's split of the fight between the players and the guards, for balancing the guards against the
 * attackers. See ACE.Server.TownEvents.TownEvent.DescribeDefenders.
 *
 * town_event
 *   warlord_killed_by    player, guard, captain or other, or null if the Warlord didn't fall
 *   *_damage             damage dealt to the attackers by the players (members of the Town Defense at the town's
 *                        level, and their pets), the guards and the Captain
 *   *_kills              attackers killed by each; a kill that doesn't count (the attacker rises again) isn't here,
 *                        and other_kills are by anything else, or with no damager known
 */

ALTER TABLE `town_event`
  ADD COLUMN `warlord_killed_by` VARCHAR(10) NULL AFTER `warlord_paid`,
  ADD COLUMN `player_damage` INT UNSIGNED NOT NULL DEFAULT 0 AFTER `warlord_killed_by`,
  ADD COLUMN `guard_damage` INT UNSIGNED NOT NULL DEFAULT 0 AFTER `player_damage`,
  ADD COLUMN `captain_damage` INT UNSIGNED NOT NULL DEFAULT 0 AFTER `guard_damage`,
  ADD COLUMN `player_kills` SMALLINT UNSIGNED NOT NULL DEFAULT 0 AFTER `captain_damage`,
  ADD COLUMN `guard_kills` SMALLINT UNSIGNED NOT NULL DEFAULT 0 AFTER `player_kills`,
  ADD COLUMN `captain_kills` SMALLINT UNSIGNED NOT NULL DEFAULT 0 AFTER `guard_kills`,
  ADD COLUMN `other_kills` SMALLINT UNSIGNED NOT NULL DEFAULT 0 AFTER `captain_kills`;
