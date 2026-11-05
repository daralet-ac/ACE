/*
 * Items changing hands, for the Daralet Admin plugin's Items tab: given, traded, dropped on the ground, picked up off the
 * ground or a player's corpse by someone else, sold to and bought back from vendors, salvaged, and copied or restored by
 * an admin (@itemcopy, @itemrestore). Loot, crafting and moves within a character's own packs aren't logged, and selling,
 * buying and salvaging only for items worth item_history_min_value or more. Market sales are in
 * player_market_transactions already.
 *
 * snapshot is the whole item, kept for one that's gone (sold, salvaged, dropped) so @itemrestore can make it again. The
 * server deletes sold, bought and salvaged rows, and every snapshot, after item_history_destroyed_days (30), and the rest
 * after item_history_days (0: never).
 */

CREATE TABLE IF NOT EXISTS `item_transfer_log` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `kind` VARCHAR(20) NOT NULL,
  `item_id` INT UNSIGNED NOT NULL,
  `item_wcid` INT UNSIGNED NOT NULL,
  `item_name` VARCHAR(100) NOT NULL,
  `amount` INT NOT NULL,
  `from_character_id` INT UNSIGNED NULL,
  `from_name` VARCHAR(50) NULL,
  `from_account_id` INT UNSIGNED NULL,
  `to_character_id` INT UNSIGNED NULL,
  `to_name` VARCHAR(50) NULL,
  `to_account_id` INT UNSIGNED NULL,
  `details` VARCHAR(200) NULL,
  `snapshot` MEDIUMTEXT NULL,
  `created_at_utc` DATETIME(6) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_item_transfer_log_item` (`item_id`, `id`),
  KEY `idx_item_transfer_log_from` (`from_character_id`, `id`),
  KEY `idx_item_transfer_log_to` (`to_character_id`, `id`),
  KEY `idx_item_transfer_log_created` (`created_at_utc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
