BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013044927_TrainerExpertiseAndAvailability'
)
BEGIN
    ALTER TABLE [Assignments] ADD [RequiredTrainingType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013044927_TrainerExpertiseAndAvailability'
)
BEGIN
    CREATE TABLE [StaffTimeOff] (
        [Id] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [Start] bigint NOT NULL,
        [End] bigint NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_StaffTimeOff] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StaffTimeOff_Users_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013044927_TrainerExpertiseAndAvailability'
)
BEGIN
    CREATE TABLE [StaffWorkingHours] (
        [Id] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartMinute] int NOT NULL,
        [EndMinute] int NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_StaffWorkingHours] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StaffWorkingHours_Users_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013044927_TrainerExpertiseAndAvailability'
)
BEGIN
    CREATE TABLE [TrainerExpertise] (
        [Id] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [TrainingType] int NOT NULL,
        [Level] int NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_TrainerExpertise] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrainerExpertise_Users_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013044927_TrainerExpertiseAndAvailability'
)
BEGIN
    CREATE INDEX [IX_StaffTimeOff_StaffId_Start_End] ON [StaffTimeOff] ([StaffId], [Start], [End]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013044927_TrainerExpertiseAndAvailability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StaffWorkingHours_StaffId_DayOfWeek_StartMinute] ON [StaffWorkingHours] ([StaffId], [DayOfWeek], [StartMinute]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013044927_TrainerExpertiseAndAvailability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TrainerExpertise_StaffId_TrainingType] ON [TrainerExpertise] ([StaffId], [TrainingType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013044927_TrainerExpertiseAndAvailability'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013044927_TrainerExpertiseAndAvailability', N'10.0.12');
END;

COMMIT;
GO
