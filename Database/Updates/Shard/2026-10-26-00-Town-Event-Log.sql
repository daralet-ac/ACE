/*
 * The town event log: one row for every town attack (TownEvent) once it ends, and one for every player who took part
 * in it, with their scores, ranks and rewards. See ACE.Server.TownEvents.TownEventLog.
 *
 * town_event
 *   uid                  the attack's own id, so a reward claimed from the Captain later can find its row
 *   started_by           unrest (it started on its own) or admin (@townevent start)
 *   outcome              held, fell (the last wave wasn't cleared in time), looted (the Supply Chest), stopped
 *                        (@townevent stop) or cancelled (no creatures or roads were found)
 *   supply_level_start   the town's guard supply level when the attack began, and _end when it ended
 *   waves_reached        the wave the attack ended on, of wave_count
 *   participants         players who earned credit; rewarded is those rewarded at the end
 *   warlord_pyreals      what each defender got when the Warlord fell (0 if it didn't), and warlord_paid how many got it
 *
 * town_event_participant
 *   rank_*               the player's place in each score among those who earned credit (1 is top), or null for none
 *   reward               paid (at the end), claim (waiting at the Captain), claimed, expired (not claimed in time),
 *                        or none (not rewarded)
 *   xp                   the XP paid or claimed, before the player's own XP bonuses
 */

CREATE TABLE IF NOT EXISTS `town_event` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `uid` CHAR(32) NOT NULL,
  `town` VARCHAR(40) NOT NULL,
  `tier` TINYINT UNSIGNED NOT NULL,
  `started_by` VARCHAR(10) NOT NULL,
  `outcome` VARCHAR(10) NOT NULL,
  `family` VARCHAR(60) NULL,
  `minor_family` VARCHAR(60) NULL,
  `roads` VARCHAR(100) NULL,
  `chest_raid` TINYINT(1) NOT NULL,
  `chest_looted_percent` TINYINT UNSIGNED NOT NULL,
  `supply_level_start` TINYINT UNSIGNED NOT NULL,
  `supply_level_end` TINYINT UNSIGNED NOT NULL,
  `waves_reached` TINYINT UNSIGNED NOT NULL,
  `wave_count` TINYINT UNSIGNED NOT NULL,
  `attackers_spawned` SMALLINT UNSIGNED NOT NULL,
  `participants` SMALLINT UNSIGNED NOT NULL,
  `rewarded` SMALLINT UNSIGNED NOT NULL,
  `warlord_pyreals` INT UNSIGNED NOT NULL,
  `warlord_paid` SMALLINT UNSIGNED NOT NULL,
  `started_at_utc` DATETIME(6) NOT NULL,
  `ended_at_utc` DATETIME(6) NOT NULL,
  `duration_seconds` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `idx_town_event_uid` (`uid`),
  KEY `idx_town_event_town` (`town`, `ended_at_utc`),
  KEY `idx_town_event_ended` (`ended_at_utc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `town_event_participant` (
  `event_id` INT UNSIGNED NOT NULL,
  `character_id` INT UNSIGNED NOT NULL,
  `character_name` VARCHAR(50) NOT NULL,
  `level` SMALLINT UNSIGNED NOT NULL,
  `member_at_end` TINYINT(1) NOT NULL,
  `damage` INT UNSIGNED NOT NULL,
  `healing` INT UNSIGNED NOT NULL,
  `damage_taken` INT UNSIGNED NOT NULL,
  `killing_blows` SMALLINT UNSIGNED NOT NULL,
  `rank_damage` SMALLINT UNSIGNED NULL,
  `rank_healing` SMALLINT UNSIGNED NULL,
  `rank_damage_taken` SMALLINT UNSIGNED NULL,
  `rank_killing_blows` SMALLINT UNSIGNED NULL,
  `categories_topped` TINYINT UNSIGNED NOT NULL,
  `over_level_kills` SMALLINT UNSIGNED NOT NULL,
  `prompts_refused` SMALLINT UNSIGNED NOT NULL,
  `kills_after_refusal` SMALLINT UNSIGNED NOT NULL,
  `credit_lost` TINYINT(1) NOT NULL,
  `reward` VARCHAR(10) NOT NULL,
  `reward_mod` DOUBLE NOT NULL,
  `xp` BIGINT UNSIGNED NOT NULL,
  `warlord_pyreals` INT UNSIGNED NOT NULL,
  `claimed_at_utc` DATETIME(6) NULL,
  PRIMARY KEY (`event_id`, `character_id`),
  KEY `idx_town_event_participant_character` (`character_id`),
  CONSTRAINT `fk_town_event_participant_event` FOREIGN KEY (`event_id`) REFERENCES `town_event` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
