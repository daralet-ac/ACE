/*
 * Account quests: quests whose names start with ACCOUNT_ are now kept once per account (account_quest_registry) and
 * shared by every character on it, instead of being copied into each character's own quest registry.
 *
 * This creates the table and moves the existing ACCOUNT_ stamps into it. For each account and quest it keeps the
 * highest completion count and the latest completion time among the account's characters that aren't deleted (the
 * old code ignored deleted characters too). The moved rows are copied to character_properties_quest_registry_account_backup
 * first and then deleted from character_properties_quest_registry, where the server no longer reads them.
 *
 * Run it with the server stopped. It is safe to run more than once: a second run finds nothing left to move.
 * Once you are happy with the result, the backup table can be dropped:
 *   DROP TABLE `character_properties_quest_registry_account_backup`;
 */

CREATE TABLE IF NOT EXISTS `account_quest_registry` (
  `account_id` int unsigned NOT NULL COMMENT 'Id of the account this quest belongs to',
  `quest_name` varchar(255) NOT NULL COMMENT 'Unique Name of Quest',
  `last_time_completed` int unsigned NOT NULL DEFAULT '0' COMMENT 'Timestamp of last successful completion',
  `num_times_completed` int NOT NULL DEFAULT '0' COMMENT 'Number of successful completions',
  PRIMARY KEY (`account_id`, `quest_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Quests shared by every character on an account';

-- same columns, no foreign key, so the copies outlast their characters
CREATE TABLE IF NOT EXISTS `character_properties_quest_registry_account_backup` LIKE `character_properties_quest_registry`;

START TRANSACTION;

INSERT IGNORE INTO `character_properties_quest_registry_account_backup`
SELECT * FROM `character_properties_quest_registry` WHERE `quest_Name` LIKE 'ACCOUNT\_%';

INSERT INTO `account_quest_registry` (`account_id`, `quest_name`, `last_time_completed`, `num_times_completed`)
SELECT c.`account_Id`, q.`quest_Name`, MAX(q.`last_Time_Completed`), MAX(q.`num_Times_Completed`)
FROM `character_properties_quest_registry` q
JOIN `character` c ON c.`id` = q.`character_Id`
WHERE q.`quest_Name` LIKE 'ACCOUNT\_%' AND c.`is_Deleted` = 0
GROUP BY c.`account_Id`, q.`quest_Name`
ON DUPLICATE KEY UPDATE
  `last_time_completed` = GREATEST(`account_quest_registry`.`last_time_completed`, VALUES(`last_time_completed`)),
  `num_times_completed` = GREATEST(`account_quest_registry`.`num_times_completed`, VALUES(`num_times_completed`));

DELETE FROM `character_properties_quest_registry` WHERE `quest_Name` LIKE 'ACCOUNT\_%';

COMMIT;
