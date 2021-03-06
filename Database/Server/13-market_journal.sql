DROP TABLE IF EXISTS `market_journal`;

CREATE TABLE `market_journal` (
	`transactionID` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	`transactionDate` BIGINT(20) NULL DEFAULT NULL,
	`entryTypeID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`ownerID1` INT(10) UNSIGNED NULL DEFAULT '0',
	`ownerID2` INT(10) UNSIGNED NULL DEFAULT '0',
	`referenceID` VARCHAR(255) NULL DEFAULT NULL,
	`amount` DOUBLE NOT NULL DEFAULT '0',
	`balance` DOUBLE NOT NULL DEFAULT '0',
	`description` VARCHAR(43) NULL DEFAULT NULL,
	`accountKey` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY (`transactionID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;