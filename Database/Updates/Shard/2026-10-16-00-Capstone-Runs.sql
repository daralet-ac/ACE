/*
 * The capstone leaderboard (the website's Leaderboards page, /api/leaderboards/capstone): one row for every capstone
 * dungeon a fellowship has completed, with how long it took and who was in the fellowship.
 *
 * The clock starts when the fellowship's instance of the dungeon is created (or when it is given a numbered copy), and
 * stops when the dungeon's completion quest is stamped. See ACE.Server.Managers.CapstoneRunTracker.
 *
 *   dungeon            the dungeon's key on the website: its completion quest without "Completed" (GlendenWoodDungeon)
 *   fellowship_size    how many were in the fellowship when it finished
 *   difficulty_tier    the Dungeon: Difficulty mod in force: 0 for none, 1-20 for I-XX
 *   roster_key         the fellowship's character ids, smallest first, joined with commas: the same key is the same group,
 *                      so the board can keep the best time of each group
 */

CREATE TABLE IF NOT EXISTS `capstone_run` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `dungeon` VARCHAR(40) NOT NULL,
  `fellowship_size` TINYINT UNSIGNED NOT NULL,
  `difficulty_tier` TINYINT UNSIGNED NOT NULL,
  `completion_seconds` INT UNSIGNED NOT NULL,
  `roster_key` VARCHAR(120) NOT NULL,
  `completed_at_utc` DATETIME(6) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_capstone_run_board` (`dungeon`, `difficulty_tier`, `fellowship_size`, `completion_seconds`),
  KEY `idx_capstone_run_roster` (`roster_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `capstone_run_member` (
  `run_id` INT UNSIGNED NOT NULL,
  `character_id` INT UNSIGNED NOT NULL,
  `character_name` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`run_id`, `character_id`),
  KEY `idx_capstone_run_member_character` (`character_id`),
  CONSTRAINT `fk_capstone_run_member_run` FOREIGN KEY (`run_id`) REFERENCES `capstone_run` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
