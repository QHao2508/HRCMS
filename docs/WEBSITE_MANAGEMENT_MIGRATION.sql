-- Run using a connection to the HRCMS database on the intended server.
IF DB_NAME() <> N'HRCMS'
    THROW 50001, 'Select the HRCMS database before applying this script.', 1;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009025334_WebsiteManagement'
)
BEGIN
    CREATE TABLE [WebsiteSettings] (
        [Id] uniqueidentifier NOT NULL,
        [ContentJson] nvarchar(max) NOT NULL,
        [LogoName] nvarchar(4000) NULL,
        [LogoType] nvarchar(4000) NULL,
        [HeroName] nvarchar(4000) NULL,
        [HeroType] nvarchar(4000) NULL,
        [BackgroundName] nvarchar(4000) NULL,
        [BackgroundType] nvarchar(4000) NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_WebsiteSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009025334_WebsiteManagement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009025334_WebsiteManagement', N'10.0.12');
END;

COMMIT;
GO
