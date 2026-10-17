BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013062223_PublicReferenceData'
)
BEGIN
    CREATE TABLE [PublicReferenceRecords] (
        [Id] uniqueidentifier NOT NULL,
        [ExternalKey] nvarchar(300) NOT NULL,
        [Kind] nvarchar(40) NOT NULL,
        [DisplayName] nvarchar(4000) NOT NULL,
        [SourceUrl] nvarchar(4000) NOT NULL,
        [CollectedOn] date NOT NULL,
        [SourceAsOf] date NULL,
        [SourceMetadataJson] nvarchar(4000) NOT NULL,
        [PayloadJson] nvarchar(4000) NOT NULL,
        [ContentSha256] nvarchar(64) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_PublicReferenceRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013062223_PublicReferenceData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PublicReferenceRecords_ExternalKey] ON [PublicReferenceRecords] ([ExternalKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013062223_PublicReferenceData'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013062223_PublicReferenceData', N'10.0.12');
END;

COMMIT;
GO
