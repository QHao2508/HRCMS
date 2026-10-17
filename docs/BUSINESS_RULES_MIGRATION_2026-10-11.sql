BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [StockMovements] ADD [ReplenishmentRequestId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Sessions] ADD [DurationMinutes] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Sessions] ADD [ScheduledEndAt] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Replenishments] ADD [ReceivedAt] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Replenishments] ADD [ReceivedBy] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Replenishments] ADD [ReceivedQuantity] decimal(18,3) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Replenishments] ADD [ReviewNotes] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Replenishments] ADD [ReviewedAt] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Replenishments] ADD [ReviewedBy] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Incidents] ADD [Resolution] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Incidents] ADD [ResolvedAt] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Incidents] ADD [ResolvedBy] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Horses] ADD [ArchiveCategory] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Horses] ADD [ArchiveReason] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Horses] ADD [ArchivedAt] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [Horses] ADD [ArchivedBy] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    CREATE INDEX [IX_StockMovements_ReplenishmentRequestId] ON [StockMovements] ([ReplenishmentRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    ALTER TABLE [StockMovements] ADD CONSTRAINT [FK_StockMovements_Replenishments_ReplenishmentRequestId] FOREIGN KEY ([ReplenishmentRequestId]) REFERENCES [Replenishments] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011120226_BusinessRuleLifecycle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011120226_BusinessRuleLifecycle', N'10.0.12');
END;

COMMIT;
GO
