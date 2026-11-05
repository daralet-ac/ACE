/*
 * Items changing hands between characters, for the Daralet Admin plugin's Items tab: given, traded, dropped on the ground,
 * and picked up off the ground or a player's corpse by someone else. Loot, vendors, crafting and moves within a
 * character's own packs aren't logged, so this stays small. Market sales are in player_market_transactions already.
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
  `created_at_utc` DATETIME(6) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_item_transfer_log_item` (`item_id`, `id`),
  KEY `idx_item_transfer_log_from` (`from_character_id`, `id`),
  KEY `idx_item_transfer_log_to` (`to_character_id`, `id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
