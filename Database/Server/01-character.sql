DROP TABLE IF EXISTS `chrInformation`;

/**
 * Character information
 */
CREATE TABLE `chrInformation` (
	`characterID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`accountID` INT(10) UNSIGNED NULL DEFAULT NULL,
	`activeCloneID` INT(10) UNSIGNED NULL DEFAULT NULL,
	`timeLastJump` BIGINT(20) NOT NULL DEFAULT '0',
	`title` VARCHAR(85) NOT NULL DEFAULT '',
	`description` TEXT NOT NULL,
	`bounty` DOUBLE NOT NULL DEFAULT '0',
	`balance` DOUBLE NOT NULL DEFAULT '0',
	`securityRating` DOUBLE NOT NULL DEFAULT '0',
	`petitionMessage` VARCHAR(85) NOT NULL DEFAULT '',
	`logonMinutes` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`corporationID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`titleMask` BIGINT(20) UNSIGNED NOT NULL DEFAULT '0',
	`corpRole` BIGINT(20) UNSIGNED NOT NULL DEFAULT '0',
	`rolesAtAll` BIGINT(20) UNSIGNED NOT NULL DEFAULT '0',
	`rolesAtBase` BIGINT(20) UNSIGNED NOT NULL DEFAULT '0',
	`rolesAtHQ` BIGINT(20) UNSIGNED NOT NULL DEFAULT '0',
	`rolesAtOther` BIGINT(20) UNSIGNED NOT NULL DEFAULT '0',
	`corporationDateTime` BIGINT(20) UNSIGNED NOT NULL DEFAULT '0',
	`startDateTime` BIGINT(20) UNSIGNED NOT NULL DEFAULT '0',
	`createDateTime` BIGINT(20) UNSIGNED NOT NULL DEFAULT '0',
	`ancestryID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`careerID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`schoolID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`careerSpecialityID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`gender` TINYINT(4) NOT NULL DEFAULT '0',
	`accessoryID` INT(10) UNSIGNED NULL DEFAULT NULL,
	`beardID` INT(10) UNSIGNED NULL DEFAULT NULL,
	`costumeID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`decoID` INT(10) UNSIGNED NULL DEFAULT NULL,
	`eyebrowsID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`eyesID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`hairID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`lipstickID` INT(10) UNSIGNED NULL DEFAULT NULL,
	`makeupID` INT(10) UNSIGNED NULL DEFAULT NULL,
	`skinID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`backgroundID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`lightID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`headRotation1` DOUBLE NOT NULL DEFAULT '0',
	`headRotation2` DOUBLE NOT NULL DEFAULT '0',
	`headRotation3` DOUBLE NOT NULL DEFAULT '0',
	`eyeRotation1` DOUBLE NOT NULL DEFAULT '0',
	`eyeRotation2` DOUBLE NOT NULL DEFAULT '0',
	`eyeRotation3` DOUBLE NOT NULL DEFAULT '0',
	`camPos1` DOUBLE NOT NULL DEFAULT '0',
	`camPos2` DOUBLE NOT NULL DEFAULT '0',
	`camPos3` DOUBLE NOT NULL DEFAULT '0',
	`morph1e` DOUBLE NULL DEFAULT NULL,
	`morph1n` DOUBLE NULL DEFAULT NULL,
	`morph1s` DOUBLE NULL DEFAULT NULL,
	`morph1w` DOUBLE NULL DEFAULT NULL,
	`morph2e` DOUBLE NULL DEFAULT NULL,
	`morph2n` DOUBLE NULL DEFAULT NULL,
	`morph2s` DOUBLE NULL DEFAULT NULL,
	`morph2w` DOUBLE NULL DEFAULT NULL,
	`morph3e` DOUBLE NULL DEFAULT NULL,
	`morph3n` DOUBLE NULL DEFAULT NULL,
	`morph3s` DOUBLE NULL DEFAULT NULL,
	`morph3w` DOUBLE NULL DEFAULT NULL,
	`morph4e` DOUBLE NULL DEFAULT NULL,
	`morph4n` DOUBLE NULL DEFAULT NULL,
	`morph4s` DOUBLE NULL DEFAULT NULL,
	`morph4w` DOUBLE NULL DEFAULT NULL,
	`stationID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`solarSystemID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`constellationID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`regionID` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`online` TINYINT(1) NOT NULL DEFAULT '0',
	`nextRespecTime` BIGINT(20) NOT NULL DEFAULT '0',
	`freeRespecs` INT(11) NOT NULL DEFAULT '2',
	`lastOnline` BIGINT(20) NOT NULL DEFAULT '0',
  PRIMARY KEY  (`characterID`),
  KEY `FK_CHARACTER__ACCOUNTS` (`accountID`),
  KEY `FK_CHARACTER__CHRACCESSORIES` (`accessoryID`),
  KEY `FK_CHARACTER__CHRANCESTRIES` (`ancestryID`),
  KEY `FK_CHARACTER__CHRBEARDS` (`beardID`),
  KEY `FK_CHARACTER__CHRCAREERS` (`careerID`),
  KEY `FK_CHARACTER__CHRCAREERSPECIALITIES` (`careerSpecialityID`),
  KEY `FK_CHARACTER__CHRCOSTUMES` (`costumeID`),
  KEY `FK_CHARACTER__CHRDECOS` (`decoID`),
  KEY `FK_CHARACTER__CHREYEBROWS` (`eyebrowsID`),
  KEY `FK_CHARACTER__CHREYES` (`eyesID`),
  KEY `FK_CHARACTER__CHRHAIRS` (`hairID`),
  KEY `FK_CHARACTER__CHRLIPSTICKS` (`lipstickID`),
  KEY `FK_CHARACTER__CHRMAKEUPS` (`makeupID`),
  KEY `FK_CHARACTER__CHRSCHOOLS` (`schoolID`),
  KEY `FK_CHARACTER__CHRSKINS` (`skinID`),
  KEY `FK_CHARACTER__CHRBACKGROUNDS` (`backgroundID`),
  KEY `FK_CHARACTER__CHRLIGHTS` (`lightID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

/*
 * Insert agents information into the chrInformation table
 */
INSERT INTO chrInformation
 SELECT
  characterID,accountID,NULL AS activeCloneID, 0 AS timeLastJump,title,description,bounty,balance,securityRating,petitionMessage,logonMinutes,
  corporationID, 0 AS titleMask, 0 AS corpRole,0 AS rolesAtAll, 0 AS rolesAtBase, 0 AS rolesAtHQ,0 AS rolesAtOther,
  corporationDateTime,startDateTime,createDateTime,
  ancestryID,careerID,schoolID,careerSpecialityID,gender,
  accessoryID,beardID,costumeID,decoID,eyebrowsID,eyesID,hairID,lipstickID,makeupID,skinID,backgroundID,lightID,
  headRotation1,headRotation2,headRotation3,
  eyeRotation1,eyeRotation2,eyeRotation3,
  camPos1,camPos2,camPos3,
  morph1e,morph1n,morph1s,morph1w,
  morph2e,morph2n,morph2s,morph2w,
  morph3e,morph3n,morph3s,morph3w,
  morph4e,morph4n,morph4s,morph4w,
  stationID,solarSystemID,constellationID,regionID,
  0 AS online, 0 AS nextRespecTime, 2 AS freeRespecs, 0 AS lastOnline
 FROM chrStatic;

DROP TABLE IF EXISTS `chrSkillQueue`;

/**
 * Create table for skill queue
 */
CREATE TABLE `chrSkillQueue` (
  `orderIndex` int(10) unsigned NOT NULL,
  `characterID` int(10) unsigned NOT NULL,
  `skillItemID` int(10) unsigned NOT NULL,
  `level` int(10) unsigned NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
