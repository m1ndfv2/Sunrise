-- DBeaver-ready migration script for AddClansMvp (MySQL 8+)
--
-- How to run in DBeaver:
-- 1) Open SQL Editor on your Sunrise MySQL connection.
-- 2) Select the target database in Navigator (or uncomment USE below).
-- 3) Execute the "UP" block (Alt+X / Ctrl+Enter depending on keymap).
--
-- IMPORTANT: this script changes schema only and does not delete existing users.
--
-- USE `your_sunrise_database`;

/* ============================================================
   UP: apply clans MVP schema
   ============================================================ */
START TRANSACTION;

CREATE TABLE `clan` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(32) NOT NULL,
  `AvatarUrl` VARCHAR(2048) NULL,
  `CreatedAt` DATETIME(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_clan_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `clan_member` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `ClanId` INT NOT NULL,
  `UserId` INT NOT NULL,
  `Role` INT NOT NULL,
  `JoinedAt` DATETIME(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_clan_member_ClanId_UserId` (`ClanId`, `UserId`),
  UNIQUE KEY `IX_clan_member_UserId` (`UserId`),
  CONSTRAINT `FK_clan_member_clan_ClanId`
    FOREIGN KEY (`ClanId`) REFERENCES `clan` (`Id`)
    ON DELETE CASCADE,
  CONSTRAINT `FK_clan_member_user_UserId`
    FOREIGN KEY (`UserId`) REFERENCES `user` (`Id`)
    ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

ALTER TABLE `user`
  ADD COLUMN `ClanId` INT NULL;

CREATE INDEX `IX_user_ClanId` ON `user` (`ClanId`);

ALTER TABLE `user`
  ADD CONSTRAINT `FK_user_clan_ClanId`
    FOREIGN KEY (`ClanId`) REFERENCES `clan` (`Id`)
    ON DELETE SET NULL;

COMMIT;


/* ============================================================
   DOWN: rollback clans MVP schema (run only if you need rollback)
   ============================================================ */
-- START TRANSACTION;
--
-- ALTER TABLE `user` DROP FOREIGN KEY `FK_user_clan_ClanId`;
-- DROP TABLE `clan_member`;
-- DROP TABLE `clan`;
-- DROP INDEX `IX_user_ClanId` ON `user`;
-- ALTER TABLE `user` DROP COLUMN `ClanId`;
--
-- COMMIT;
