-- Required for filtered indexes when this script is executed by sqlcmd.
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Audit] (
        [Id] uniqueidentifier NOT NULL,
        [ActorId] uniqueidentifier NOT NULL,
        [Action] int NOT NULL,
        [ReferenceId] uniqueidentifier NOT NULL,
        [Detail] nvarchar(4000) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Audit] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Challenges] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Purpose] int NOT NULL,
        [CodeHash] nvarchar(4000) NOT NULL,
        [ExpiresAt] bigint NOT NULL,
        [Attempts] int NOT NULL,
        [Consumed] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Challenges] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [EmailMessages] (
        [Id] uniqueidentifier NOT NULL,
        [Recipient] nvarchar(4000) NOT NULL,
        [Subject] nvarchar(4000) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [SentAt] bigint NULL,
        [Attempts] int NOT NULL,
        [NextAttemptAt] bigint NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_EmailMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Inventory] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(4000) NOT NULL,
        [Category] nvarchar(4000) NOT NULL,
        [Unit] nvarchar(4000) NOT NULL,
        [Stock] decimal(18,3) NOT NULL,
        [MinimumStock] decimal(18,3) NOT NULL,
        [Archived] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Inventory] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [RecipientId] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        [Message] nvarchar(4000) NOT NULL,
        [ReferenceId] uniqueidentifier NULL,
        [Read] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Stables] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(4000) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Stables] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Templates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(4000) NOT NULL,
        [Goal] nvarchar(4000) NOT NULL,
        [Phase] nvarchar(4000) NOT NULL,
        [DistanceMetres] decimal(18,3) NOT NULL,
        [Intensity] int NOT NULL,
        [Surface] nvarchar(4000) NOT NULL,
        [FrequencyPerWeek] int NOT NULL,
        [Notes] nvarchar(4000) NOT NULL,
        [Archived] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Templates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [UserName] nvarchar(80) NOT NULL,
        [FirstName] nvarchar(4000) NOT NULL,
        [LastName] nvarchar(4000) NOT NULL,
        [Phone] nvarchar(4000) NOT NULL,
        [Address] nvarchar(4000) NOT NULL,
        [PasswordHash] nvarchar(4000) NOT NULL,
        [NationalIdProtected] nvarchar(4000) NULL,
        [Role] int NOT NULL,
        [EmailVerified] bit NOT NULL,
        [Active] bit NOT NULL,
        [FailedLogins] int NOT NULL,
        [LockedUntil] bigint NULL,
        [SecurityStamp] nvarchar(4000) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Replenishments] (
        [Id] uniqueidentifier NOT NULL,
        [ItemId] uniqueidentifier NOT NULL,
        [RequestedBy] uniqueidentifier NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(4000) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Replenishments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Replenishments_Inventory_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Inventory] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [StockMovements] (
        [Id] uniqueidentifier NOT NULL,
        [ItemId] uniqueidentifier NOT NULL,
        [ActorId] uniqueidentifier NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Reason] nvarchar(4000) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_StockMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockMovements_Inventory_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Inventory] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Stalls] (
        [Id] uniqueidentifier NOT NULL,
        [StableId] uniqueidentifier NOT NULL,
        [Name] nvarchar(4000) NOT NULL,
        [CleaningStatus] int NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Stalls] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Stalls_Stables_StableId] FOREIGN KEY ([StableId]) REFERENCES [Stables] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Registrations] (
        [Id] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Name] nvarchar(4000) NOT NULL,
        [Sire] nvarchar(4000) NOT NULL,
        [Dam] nvarchar(4000) NOT NULL,
        [DateOfBirth] date NOT NULL,
        [Gender] int NOT NULL,
        [Breed] nvarchar(4000) NOT NULL,
        [RegistrationNumber] nvarchar(4000) NULL,
        [HeightCm] decimal(18,3) NOT NULL,
        [WeightKg] decimal(18,3) NOT NULL,
        [MeasurementDate] date NOT NULL,
        [DeclaredHealth] nvarchar(4000) NOT NULL,
        [HealthNotes] nvarchar(4000) NOT NULL,
        [BoardingStart] date NOT NULL,
        [BoardingEnd] date NULL,
        [PreferredHeadTrainerId] uniqueidentifier NULL,
        [PreferredGroomId] uniqueidentifier NULL,
        [PreferredVeterinarianId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [ReviewReason] nvarchar(4000) NULL,
        [ReviewedBy] uniqueidentifier NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Registrations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Registrations_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Attachments] (
        [Id] uniqueidentifier NOT NULL,
        [RegistrationId] uniqueidentifier NOT NULL,
        [UploadedBy] uniqueidentifier NOT NULL,
        [FileName] nvarchar(4000) NOT NULL,
        [StorageName] nvarchar(4000) NOT NULL,
        [ContentType] nvarchar(4000) NOT NULL,
        [Length] bigint NOT NULL,
        [Type] int NOT NULL,
        [CertificateNumber] nvarchar(4000) NULL,
        [IssueDate] date NULL,
        [ExpiryDate] date NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Attachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Attachments_Registrations_RegistrationId] FOREIGN KEY ([RegistrationId]) REFERENCES [Registrations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Horses] (
        [Id] uniqueidentifier NOT NULL,
        [RegistrationId] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Name] nvarchar(4000) NOT NULL,
        [Sire] nvarchar(4000) NOT NULL,
        [Dam] nvarchar(4000) NOT NULL,
        [DateOfBirth] date NOT NULL,
        [Gender] int NOT NULL,
        [Breed] nvarchar(4000) NOT NULL,
        [RegistrationNumber] nvarchar(4000) NULL,
        [BoardingStart] date NOT NULL,
        [BoardingEnd] date NULL,
        [HealthStatus] int NOT NULL,
        [Archived] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Horses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Horses_Registrations_RegistrationId] FOREIGN KEY ([RegistrationId]) REFERENCES [Registrations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Horses_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Assignments] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [StaffId] uniqueidentifier NOT NULL,
        [Role] int NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NULL,
        [Notes] nvarchar(4000) NOT NULL,
        [Active] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Assignments_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [CareTasks] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [GroomId] uniqueidentifier NOT NULL,
        [TreatmentPlanId] uniqueidentifier NULL,
        [Type] int NOT NULL,
        [ScheduledAt] bigint NOT NULL,
        [Instructions] nvarchar(4000) NOT NULL,
        [ApprovedPortionKg] decimal(18,3) NULL,
        [ActualPortionKg] decimal(18,3) NULL,
        [Status] int NOT NULL,
        [CompletedAt] bigint NULL,
        [Notes] nvarchar(4000) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_CareTasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CareTasks_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [FollowUps] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [PreviousRecordId] uniqueidentifier NOT NULL,
        [CurrentRecordId] uniqueidentifier NOT NULL,
        [Clearance] bit NOT NULL,
        [Outcome] nvarchar(4000) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_FollowUps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FollowUps_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Incidents] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [ReporterId] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NULL,
        [OccurredAt] bigint NOT NULL,
        [Type] int NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [Severity] int NOT NULL,
        [RoutedTo] int NOT NULL,
        [Resolved] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Incidents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Incidents_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Measurements] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [Date] date NOT NULL,
        [HeightCm] decimal(18,3) NOT NULL,
        [WeightKg] decimal(18,3) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Measurements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Measurements_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [MedicalRecords] (
        [Id] uniqueidentifier NOT NULL,
        [SupersedesRecordId] uniqueidentifier NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [VeterinarianId] uniqueidentifier NOT NULL,
        [ExaminationAt] bigint NOT NULL,
        [Reason] nvarchar(4000) NOT NULL,
        [Symptoms] nvarchar(4000) NOT NULL,
        [Findings] nvarchar(4000) NOT NULL,
        [Diagnosis] nvarchar(4000) NOT NULL,
        [HealthStatus] int NOT NULL,
        [Notes] nvarchar(4000) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_MedicalRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MedicalRecords_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Occupancies] (
        [Id] uniqueidentifier NOT NULL,
        [StallId] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [EndedAt] bigint NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Occupancies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Occupancies_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Occupancies_Stalls_StallId] FOREIGN KEY ([StallId]) REFERENCES [Stalls] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Plans] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [TrainerId] uniqueidentifier NOT NULL,
        [TemplateId] uniqueidentifier NOT NULL,
        [Goal] nvarchar(4000) NOT NULL,
        [Phase] nvarchar(4000) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Notes] nvarchar(4000) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Plans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Plans_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Plans_Templates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [Templates] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Plans_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [PreventiveCare] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        [DueDate] date NOT NULL,
        [CompletedDate] date NULL,
        [Notes] nvarchar(4000) NOT NULL,
        [ReminderSent] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_PreventiveCare] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PreventiveCare_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [IncidentPhotos] (
        [Id] uniqueidentifier NOT NULL,
        [IncidentId] uniqueidentifier NOT NULL,
        [UploadedBy] uniqueidentifier NOT NULL,
        [FileName] nvarchar(4000) NOT NULL,
        [StorageName] nvarchar(4000) NOT NULL,
        [ContentType] nvarchar(4000) NOT NULL,
        [Length] bigint NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_IncidentPhotos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IncidentPhotos_Incidents_IncidentId] FOREIGN KEY ([IncidentId]) REFERENCES [Incidents] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Injuries] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [MedicalRecordId] uniqueidentifier NOT NULL,
        [InjuryDate] date NOT NULL,
        [Type] int NOT NULL,
        [BodyLocation] nvarchar(4000) NOT NULL,
        [Severity] int NOT NULL,
        [Cause] nvarchar(4000) NOT NULL,
        [Status] int NOT NULL,
        [ReviewDate] date NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Injuries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Injuries_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Injuries_MedicalRecords_MedicalRecordId] FOREIGN KEY ([MedicalRecordId]) REFERENCES [MedicalRecords] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Restrictions] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [MedicalRecordId] uniqueidentifier NOT NULL,
        [TrainingLock] bit NOT NULL,
        [BlockAllTraining] bit NOT NULL,
        [MaxIntensity] int NULL,
        [MaxDistanceMetres] decimal(18,3) NULL,
        [NoSprint] bit NOT NULL,
        [ValidFrom] bigint NOT NULL,
        [ValidUntil] bigint NULL,
        [Reason] nvarchar(4000) NOT NULL,
        [Cleared] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Restrictions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Restrictions_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Restrictions_MedicalRecords_MedicalRecordId] FOREIGN KEY ([MedicalRecordId]) REFERENCES [MedicalRecords] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Treatments] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [MedicalRecordId] uniqueidentifier NOT NULL,
        [InjuryId] uniqueidentifier NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [FollowUpDate] date NOT NULL,
        [Objective] nvarchar(4000) NOT NULL,
        [Instructions] nvarchar(4000) NOT NULL,
        [Medication] nvarchar(4000) NOT NULL,
        [Frequency] nvarchar(4000) NOT NULL,
        [Completed] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Treatments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Treatments_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Treatments_MedicalRecords_MedicalRecordId] FOREIGN KEY ([MedicalRecordId]) REFERENCES [MedicalRecords] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Sessions] (
        [Id] uniqueidentifier NOT NULL,
        [HorseId] uniqueidentifier NOT NULL,
        [PlanId] uniqueidentifier NOT NULL,
        [RiderId] uniqueidentifier NULL,
        [ScheduledAt] bigint NOT NULL,
        [TrainingType] int NOT NULL,
        [DistanceMetres] decimal(18,3) NOT NULL,
        [Intensity] int NOT NULL,
        [Surface] nvarchar(4000) NOT NULL,
        [Target] nvarchar(4000) NOT NULL,
        [Notes] nvarchar(4000) NOT NULL,
        [Status] int NOT NULL,
        [StartedAt] bigint NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sessions_Horses_HorseId] FOREIGN KEY ([HorseId]) REFERENCES [Horses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sessions_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Plans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sessions_Users_RiderId] FOREIGN KEY ([RiderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Evaluations] (
        [Id] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [TrainerId] uniqueidentifier NOT NULL,
        [Comment] nvarchar(4000) NOT NULL,
        [AdjustFutureSessions] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Evaluations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Evaluations_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Results] (
        [Id] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [DistanceMetres] decimal(18,3) NOT NULL,
        [TimeSeconds] decimal(18,3) NOT NULL,
        [SpeedMetresPerSecond] decimal(18,3) NOT NULL,
        [HeartRate] int NULL,
        [Intensity] int NOT NULL,
        [Feedback] nvarchar(4000) NOT NULL,
        [AbnormalObservation] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Results] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Results_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE TABLE [TrainingRevisions] (
        [Id] uniqueidentifier NOT NULL,
        [PlanId] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NULL,
        [ActorId] uniqueidentifier NOT NULL,
        [Snapshot] nvarchar(max) NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_TrainingRevisions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrainingRevisions_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Plans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TrainingRevisions_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Assignments_HorseId_Role_Active] ON [Assignments] ([HorseId], [Role], [Active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Attachments_RegistrationId] ON [Attachments] ([RegistrationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_CareTasks_HorseId] ON [CareTasks] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Challenges_UserId_Purpose] ON [Challenges] ([UserId], [Purpose]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Evaluations_SessionId] ON [Evaluations] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_FollowUps_HorseId] ON [FollowUps] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Horses_OwnerId] ON [Horses] ([OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Horses_RegistrationId] ON [Horses] ([RegistrationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_IncidentPhotos_IncidentId] ON [IncidentPhotos] ([IncidentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Incidents_HorseId] ON [Incidents] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Injuries_HorseId] ON [Injuries] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Injuries_MedicalRecordId] ON [Injuries] ([MedicalRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Measurements_HorseId] ON [Measurements] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_MedicalRecords_HorseId] ON [MedicalRecords] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientId_Read] ON [Notifications] ([RecipientId], [Read]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Occupancies_HorseId] ON [Occupancies] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Occupancies_StallId] ON [Occupancies] ([StallId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Plans_HorseId] ON [Plans] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Plans_TemplateId] ON [Plans] ([TemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Plans_TrainerId] ON [Plans] ([TrainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_PreventiveCare_HorseId] ON [PreventiveCare] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Registrations_OwnerId] ON [Registrations] ([OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Replenishments_ItemId] ON [Replenishments] ([ItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Restrictions_HorseId_Cleared] ON [Restrictions] ([HorseId], [Cleared]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Restrictions_MedicalRecordId] ON [Restrictions] ([MedicalRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Results_SessionId] ON [Results] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Sessions_HorseId] ON [Sessions] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Sessions_PlanId] ON [Sessions] ([PlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Sessions_RiderId] ON [Sessions] ([RiderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Stalls_StableId] ON [Stalls] ([StableId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_StockMovements_ItemId] ON [StockMovements] ([ItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_TrainingRevisions_PlanId] ON [TrainingRevisions] ([PlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_TrainingRevisions_SessionId] ON [TrainingRevisions] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Treatments_HorseId] ON [Treatments] ([HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Treatments_MedicalRecordId] ON [Treatments] ([MedicalRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003125915_InitialSqlServer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003125915_InitialSqlServer', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004130807_WorkerDeliveryReliability'
)
BEGIN
    ALTER TABLE [EmailMessages] ADD [ChallengeId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004130807_WorkerDeliveryReliability'
)
BEGIN
    ALTER TABLE [EmailMessages] ADD [DiscardedAt] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004130807_WorkerDeliveryReliability'
)
BEGIN
    ALTER TABLE [EmailMessages] ADD [ExpiresAt] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004130807_WorkerDeliveryReliability'
)
BEGIN
    CREATE INDEX [IX_Notifications_ReferenceId_Type] ON [Notifications] ([ReferenceId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004130807_WorkerDeliveryReliability'
)
BEGIN
    CREATE INDEX [IX_EmailMessages_ChallengeId] ON [EmailMessages] ([ChallengeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004130807_WorkerDeliveryReliability'
)
BEGIN
    CREATE INDEX [IX_EmailMessages_SentAt_DiscardedAt_NextAttemptAt] ON [EmailMessages] ([SentAt], [DiscardedAt], [NextAttemptAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004130807_WorkerDeliveryReliability'
)
BEGIN
    ALTER TABLE [EmailMessages] ADD CONSTRAINT [FK_EmailMessages_Challenges_ChallengeId] FOREIGN KEY ([ChallengeId]) REFERENCES [Challenges] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004130807_WorkerDeliveryReliability'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004130807_WorkerDeliveryReliability', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'WeightKg');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [WeightKg] decimal(18,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'Sire');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [Sire] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'Name');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [Name] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'MeasurementDate');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [MeasurementDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'HeightCm');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [HeightCm] decimal(18,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'HealthNotes');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [HealthNotes] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'Gender');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [Gender] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'DeclaredHealth');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [DeclaredHealth] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'DateOfBirth');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [DateOfBirth] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'Dam');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [Dam] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'Breed');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [Breed] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'BoardingStart');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [Registrations] ALTER COLUMN [BoardingStart] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005060507_PartialRegistrationDraft'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005060507_PartialRegistrationDraft', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_TrainingRevisions_PlanId] ON [TrainingRevisions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_StockMovements_ItemId] ON [StockMovements];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_Sessions_HorseId] ON [Sessions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_Sessions_PlanId] ON [Sessions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_Sessions_RiderId] ON [Sessions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_Registrations_OwnerId] ON [Registrations];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_Plans_HorseId] ON [Plans];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_Measurements_HorseId] ON [Measurements];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_Horses_OwnerId] ON [Horses];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_CareTasks_HorseId] ON [CareTasks];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_TrainingRevisions_PlanId_CreatedAt] ON [TrainingRevisions] ([PlanId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_StockMovements_ItemId_CreatedAt] ON [StockMovements] ([ItemId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Sessions_HorseId_Status_ScheduledAt] ON [Sessions] ([HorseId], [Status], [ScheduledAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Sessions_PlanId_ScheduledAt] ON [Sessions] ([PlanId], [ScheduledAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Sessions_RiderId_Status_ScheduledAt] ON [Sessions] ([RiderId], [Status], [ScheduledAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Registrations_OwnerId_Status_CreatedAt] ON [Registrations] ([OwnerId], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Plans_HorseId_Status] ON [Plans] ([HorseId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientId_Read_CreatedAt] ON [Notifications] ([RecipientId], [Read], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Measurements_HorseId_Date] ON [Measurements] ([HorseId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Horses_OwnerId_Archived] ON [Horses] ([OwnerId], [Archived]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_CareTasks_GroomId_ScheduledAt] ON [CareTasks] ([GroomId], [ScheduledAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_CareTasks_HorseId_ScheduledAt] ON [CareTasks] ([HorseId], [ScheduledAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Audit_ReferenceId_CreatedAt] ON [Audit] ([ReferenceId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Assignments_StaffId_Active_HorseId] ON [Assignments] ([StaffId], [Active], [HorseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007115208_QueryPerformanceIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007115208_QueryPerformanceIndexes', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008031610_RealtimeNotificationOutbox'
)
BEGIN
    CREATE TABLE [RealtimeOutboxMessages] (
        [Id] uniqueidentifier NOT NULL,
        [RecipientId] uniqueidentifier NOT NULL,
        [NotificationId] uniqueidentifier NOT NULL,
        [Attempts] int NOT NULL,
        [NextAttemptAt] bigint NULL,
        [LockedUntil] bigint NULL,
        [LeaseOwner] uniqueidentifier NULL,
        [SentAt] bigint NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_RealtimeOutboxMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008031610_RealtimeNotificationOutbox'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RealtimeOutboxMessages_NotificationId] ON [RealtimeOutboxMessages] ([NotificationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008031610_RealtimeNotificationOutbox'
)
BEGIN
    CREATE INDEX [IX_RealtimeOutboxMessages_SentAt_NextAttemptAt_LockedUntil] ON [RealtimeOutboxMessages] ([SentAt], [NextAttemptAt], [LockedUntil]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008031610_RealtimeNotificationOutbox'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008031610_RealtimeNotificationOutbox', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008033133_RealtimeNotificationReadSignals'
)
BEGIN
    DROP INDEX [IX_RealtimeOutboxMessages_NotificationId] ON [RealtimeOutboxMessages];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008033133_RealtimeNotificationReadSignals'
)
BEGIN
    ALTER TABLE [RealtimeOutboxMessages] ADD [EventType] nvarchar(4000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008033133_RealtimeNotificationReadSignals'
)
BEGIN
    ALTER TABLE [RealtimeOutboxMessages] ADD [NotificationVersion] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008033133_RealtimeNotificationReadSignals'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RealtimeOutboxMessages_NotificationId_NotificationVersion] ON [RealtimeOutboxMessages] ([NotificationId], [NotificationVersion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008033133_RealtimeNotificationReadSignals'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008033133_RealtimeNotificationReadSignals', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008033516_RealtimeResourceSignals'
)
BEGIN
    EXEC sp_rename N'[RealtimeOutboxMessages].[NotificationVersion]', N'SourceVersion', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008033516_RealtimeResourceSignals'
)
BEGIN
    EXEC sp_rename N'[RealtimeOutboxMessages].[NotificationId]', N'SourceId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008033516_RealtimeResourceSignals'
)
BEGIN
    EXEC sp_rename N'[RealtimeOutboxMessages].[IX_RealtimeOutboxMessages_NotificationId_NotificationVersion]', N'IX_RealtimeOutboxMessages_SourceId_SourceVersion', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008033516_RealtimeResourceSignals'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008033516_RealtimeResourceSignals', N'10.0.12');
END;

COMMIT;
GO

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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013105448_MedicalCaseHistory'
)
BEGIN
    DROP INDEX [IX_MedicalRecords_HorseId] ON [MedicalRecords];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013105448_MedicalCaseHistory'
)
BEGIN
    ALTER TABLE [MedicalRecords] ADD [CaseRootId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013105448_MedicalCaseHistory'
)
BEGIN
    ALTER TABLE [MedicalRecords] ADD [InitialSnapshotJson] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013105448_MedicalCaseHistory'
)
BEGIN
    ALTER TABLE [FollowUps] ADD [CurrentSnapshotJson] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013105448_MedicalCaseHistory'
)
BEGIN
    ALTER TABLE [FollowUps] ADD [PreviousSnapshotJson] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013105448_MedicalCaseHistory'
)
BEGIN
    CREATE INDEX [IX_MedicalRecords_HorseId_CaseRootId] ON [MedicalRecords] ([HorseId], [CaseRootId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013105448_MedicalCaseHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013105448_MedicalCaseHistory', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    ALTER TABLE [Templates] ADD [CurriculumJson] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    ALTER TABLE [Templates] ADD [IsSystemDefault] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    ALTER TABLE [Templates] ADD [MinimumExpertiseLevel] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    ALTER TABLE [Templates] ADD [RequiredTrainingTypesJson] nvarchar(4000) NOT NULL DEFAULT N'[]';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    ALTER TABLE [Templates] ADD [SourceUrl] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    ALTER TABLE [Templates] ADD [SystemKey] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    ALTER TABLE [Sessions] ADD [TemplateExerciseKey] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    ALTER TABLE [Sessions] ADD [UseCustomExercise] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    ALTER TABLE [Plans] ADD [TemplatePlanOptionKey] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    ALTER TABLE [Plans] ADD [UseCustomPlan] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    CREATE TABLE [TrainerTemplateAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [TemplateId] uniqueidentifier NOT NULL,
        [TrainerId] uniqueidentifier NOT NULL,
        [AssignedBy] uniqueidentifier NOT NULL,
        [Active] bit NOT NULL,
        [CreatedAt] bigint NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_TrainerTemplateAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrainerTemplateAssignments_Templates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [Templates] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TrainerTemplateAssignments_Users_AssignedBy] FOREIGN KEY ([AssignedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TrainerTemplateAssignments_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Archived', N'CreatedAt', N'CurriculumJson', N'DistanceMetres', N'FrequencyPerWeek', N'Goal', N'Intensity', N'IsSystemDefault', N'MinimumExpertiseLevel', N'Name', N'Notes', N'Phase', N'RequiredTrainingTypesJson', N'SourceUrl', N'Surface', N'SystemKey', N'Version') AND [object_id] = OBJECT_ID(N'[Templates]'))
        SET IDENTITY_INSERT [Templates] ON;
    EXEC(N'INSERT INTO [Templates] ([Id], [Archived], [CreatedAt], [CurriculumJson], [DistanceMetres], [FrequencyPerWeek], [Goal], [Intensity], [IsSystemDefault], [MinimumExpertiseLevel], [Name], [Notes], [Phase], [RequiredTrainingTypesJson], [SourceUrl], [Surface], [SystemKey], [Version])
    VALUES (''9b590cbd-cc33-4f4d-8f03-000000000001'', CAST(0 AS bit), CAST(639274464000000000 AS bigint), N''{"planOptions":[{"key":"standard","label":"X\u00E2y d\u1EF1ng th\u1EC3 l\u1EF1c n\u1EC1n t\u1EA3ng","goal":"X\u00E2y th\u1EC3 l\u1EF1c v\u00E0 l\u00E0m quen nh\u1ECBp v\u1EADn \u0111\u1ED9ng t\u1EEBng b\u01B0\u1EDBc.","phase":"Base"}],"sessionOptions":[{"key":"walk-easy","label":"\u0110i b\u1ED9 nh\u1EB9","trainingType":"Walk","distanceMetres":400,"intensity":"Light","surface":"C\u00E1t","durationMinutes":15,"target":"Gi\u1EEF b\u01B0\u1EDBc \u0111i \u0111\u1EC1u v\u00E0 ph\u1EA3n h\u1ED3i \u1ED5n \u0111\u1ECBnh v\u1EDBi ng\u01B0\u1EDDi c\u01B0\u1EE1i."},{"key":"trot-base","label":"N\u01B0\u1EDBc ki\u1EC7u n\u1EC1n t\u1EA3ng","trainingType":"Trot","distanceMetres":800,"intensity":"Light","surface":"C\u00E1t","durationMinutes":20,"target":"Duy tr\u00EC nh\u1ECBp n\u01B0\u1EDBc ki\u1EC7u nh\u1EB9 v\u00E0 theo d\u00F5i h\u1ED3i ph\u1EE5c."}]}'', 800.0, 3, N''Xây thể lực và làm quen nhịp vận động từng bước.'', 0, CAST(1 AS bit), 1, N''Xây dựng thể lực nền tảng'', N''Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.'', N''Base'', N''["Walk","Trot"]'', N''https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/'', N''Cát'', N''foundation'', CAST(0 AS bigint)),
    (''9b590cbd-cc33-4f4d-8f03-000000000002'', CAST(0 AS bit), CAST(639274464000000000 AS bigint), N''{"planOptions":[{"key":"standard","label":"Duy tr\u00EC nh\u1ECBp ch\u1EA1y \u1ED5n \u0111\u1ECBnh","goal":"Ph\u00E1t tri\u1EC3n s\u1EE9c b\u1EC1n v\u00E0 kh\u1EA3 n\u0103ng gi\u1EEF nh\u1ECBp canter.","phase":"Build"}],"sessionOptions":[{"key":"canter-light","label":"Canter nh\u1EB9","trainingType":"Canter","distanceMetres":800,"intensity":"Light","surface":"C\u00E1t","durationMinutes":20,"target":"Duy tr\u00EC nh\u1ECBp ch\u1EA1y \u1ED5n \u0111\u1ECBnh v\u00E0 ki\u1EC3m so\u00E1t h\u01B0\u1EDBng v\u1EADn \u0111\u1ED9ng."},{"key":"canter-steady","label":"Canter \u0111\u1EC1u","trainingType":"Canter","distanceMetres":1200,"intensity":"Moderate","surface":"C\u00E1t","durationMinutes":25,"target":"Gi\u1EEF nh\u1ECBp canter \u0111\u1EC1u, kh\u1EDFi \u0111\u1ED9ng v\u00E0 th\u1EA3 l\u1ECFng \u0111\u1EA7y \u0111\u1EE7."}]}'', 1200.0, 3, N''Phát triển sức bền và khả năng giữ nhịp canter.'', 0, CAST(1 AS bit), 2, N''Duy trì nhịp chạy ổn định'', N''Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.'', N''Build'', N''["Canter"]'', N''https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/'', N''Cát'', N''steady-canter'', CAST(0 AS bigint)),
    (''9b590cbd-cc33-4f4d-8f03-000000000003'', CAST(0 AS bit), CAST(639274464000000000 AS bigint), N''{"planOptions":[{"key":"standard","label":"Ph\u00E1t tri\u1EC3n t\u1ED1c \u0111\u1ED9 theo t\u1EEBng \u0111o\u1EA1n","goal":"R\u00E8n kh\u1EA3 n\u0103ng t\u0103ng t\u1ED1c c\u00F3 ki\u1EC3m so\u00E1t xen k\u1EBD h\u1ED3i ph\u1EE5c.","phase":"Peak"}],"sessionOptions":[{"key":"gallop-interval","label":"\u0110o\u1EA1n gallop ki\u1EC3m so\u00E1t","trainingType":"Gallop","distanceMetres":600,"intensity":"Moderate","surface":"C\u00E1t","durationMinutes":20,"target":"V\u1EADn \u0111\u1ED9ng theo t\u1EEBng \u0111o\u1EA1n, xen k\u1EBD h\u1ED3i ph\u1EE5c v\u00E0 quan s\u00E1t ph\u1EA3n \u1EE9ng."},{"key":"sprint-short","label":"\u0110o\u1EA1n t\u0103ng t\u1ED1c ng\u1EAFn","trainingType":"Sprint","distanceMetres":300,"intensity":"Heavy","surface":"C\u00E1t","durationMinutes":15,"target":"T\u0103ng t\u1ED1c ng\u1EAFn theo h\u01B0\u1EDBng d\u1EABn chuy\u00EAn m\u00F4n, kh\u00F4ng \u00E1p d\u1EE5ng khi c\u00F3 h\u1EA1n ch\u1EBF s\u1EE9c kh\u1ECFe."}]}'', 600.0, 3, N''Rèn khả năng tăng tốc có kiểm soát xen kẽ hồi phục.'', 0, CAST(1 AS bit), 3, N''Phát triển tốc độ theo từng đoạn'', N''Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.'', N''Peak'', N''["Gallop","Sprint"]'', N''https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/'', N''Cát'', N''speed-intervals'', CAST(0 AS bigint)),
    (''9b590cbd-cc33-4f4d-8f03-000000000004'', CAST(0 AS bit), CAST(639274464000000000 AS bigint), N''{"planOptions":[{"key":"standard","label":"L\u00E0m quen c\u1ED5ng xu\u1EA5t ph\u00E1t","goal":"X\u00E2y s\u1EF1 b\u00ECnh t\u0129nh v\u00E0 ph\u1EA3n h\u1ED3i khi v\u00E0o, ra c\u1ED5ng xu\u1EA5t ph\u00E1t.","phase":"Technique"}],"sessionOptions":[{"key":"gate-familiar","label":"L\u00E0m quen c\u1ED5ng","trainingType":"StartingGate","distanceMetres":100,"intensity":"Light","surface":"C\u00E1t","durationMinutes":15,"target":"L\u00E0m quen khu v\u1EF1c c\u1ED5ng v\u00E0 gi\u1EEF ph\u1EA3n h\u1ED3i b\u00ECnh t\u0129nh."},{"key":"gate-exit","label":"Ph\u1EA3n h\u1ED3i ra c\u1ED5ng","trainingType":"StartingGate","distanceMetres":200,"intensity":"Light","surface":"C\u00E1t","durationMinutes":20,"target":"Th\u1EF1c h\u00E0nh v\u00E0o v\u00E0 ra c\u1ED5ng theo t\u00EDn hi\u1EC7u c\u1EE7a ng\u01B0\u1EDDi h\u01B0\u1EDBng d\u1EABn."}]}'', 100.0, 3, N''Xây sự bình tĩnh và phản hồi khi vào, ra cổng xuất phát.'', 0, CAST(1 AS bit), 2, N''Làm quen cổng xuất phát'', N''Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.'', N''Technique'', N''["StartingGate"]'', N''https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/'', N''Cát'', N''starting-gate'', CAST(0 AS bigint)),
    (''9b590cbd-cc33-4f4d-8f03-000000000005'', CAST(0 AS bit), CAST(639274464000000000 AS bigint), N''{"planOptions":[{"key":"standard","label":"V\u1EADn \u0111\u1ED9ng nh\u1EB9 gi\u1EEFa c\u00E1c bu\u1ED5i n\u1EB7ng","goal":"C\u00E2n b\u1EB1ng kh\u1ED1i l\u01B0\u1EE3ng hu\u1EA5n luy\u1EC7n b\u1EB1ng v\u1EADn \u0111\u1ED9ng nh\u1EB9.","phase":"Recovery"}],"sessionOptions":[{"key":"recovery-easy","label":"V\u1EADn \u0111\u1ED9ng nh\u1EB9","trainingType":"Recovery","distanceMetres":300,"intensity":"Light","surface":"C\u00E1t","durationMinutes":15,"target":"Theo d\u00F5i v\u1EADn \u0111\u1ED9ng nh\u1EB9 theo \u0111\u00E1nh gi\u00E1 c\u1EE7a Trainer v\u00E0 b\u00E1c s\u0129."},{"key":"walk-recovery","label":"\u0110i b\u1ED9 th\u1EA3 l\u1ECFng","trainingType":"Walk","distanceMetres":500,"intensity":"Light","surface":"C\u00E1t","durationMinutes":20,"target":"\u0110i b\u1ED9 \u0111\u1EC1u \u0111\u1EC3 th\u1EA3 l\u1ECFng, ghi nh\u1EADn ph\u1EA3n \u1EE9ng sau bu\u1ED5i t\u1EADp."}]}'', 300.0, 3, N''Cân bằng khối lượng huấn luyện bằng vận động nhẹ.'', 0, CAST(1 AS bit), 1, N''Vận động nhẹ giữa các buổi nặng'', N''Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.'', N''Recovery'', N''["Walk","Recovery"]'', N''https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/'', N''Cát'', N''light-recovery'', CAST(0 AS bigint))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Archived', N'CreatedAt', N'CurriculumJson', N'DistanceMetres', N'FrequencyPerWeek', N'Goal', N'Intensity', N'IsSystemDefault', N'MinimumExpertiseLevel', N'Name', N'Notes', N'Phase', N'RequiredTrainingTypesJson', N'SourceUrl', N'Surface', N'SystemKey', N'Version') AND [object_id] = OBJECT_ID(N'[Templates]'))
        SET IDENTITY_INSERT [Templates] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Templates_SystemKey] ON [Templates] ([SystemKey]) WHERE [SystemKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    CREATE INDEX [IX_TrainerTemplateAssignments_AssignedBy] ON [TrainerTemplateAssignments] ([AssignedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TrainerTemplateAssignments_TemplateId_TrainerId] ON [TrainerTemplateAssignments] ([TemplateId], [TrainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    CREATE INDEX [IX_TrainerTemplateAssignments_TrainerId] ON [TrainerTemplateAssignments] ([TrainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013143607_DefaultTrainingCurricula'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013143607_DefaultTrainingCurricula', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    ALTER TABLE [Notifications] ADD [MessageCode] nvarchar(4000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''HorseIsArchived'' WHERE [MessageCode] = N'''' AND [Message] = N''Horse is archived.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''AHorseRegistrationRequiresReview'' WHERE [MessageCode] = N'''' AND [Message] = N''A horse registration requires review.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''YourHorseRegistrationRequiresRevision'' WHERE [MessageCode] = N'''' AND [Message] = N''Your horse registration requires revision.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''YourHorseRegistrationWasApproved'' WHERE [MessageCode] = N'''' AND [Message] = N''Your horse registration was approved.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''YouHaveBeenAssignedToAHorse'' WHERE [MessageCode] = N'''' AND [Message] = N''You have been assigned to a horse.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''OfficialHorseStaffAssignmentChanged'' WHERE [MessageCode] = N'''' AND [Message] = N''Official horse staff assignment changed.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ATrainingSessionWasAssignedToYou'' WHERE [MessageCode] = N'''' AND [Message] = N''A training session was assigned to you.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ATrainingAssignmentWasRemoved'' WHERE [MessageCode] = N'''' AND [Message] = N''A training assignment was removed.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ARiderSubmittedASessionResult'' WHERE [MessageCode] = N'''' AND [Message] = N''A rider submitted a session result.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ATrainingObservationRequiresReview'' WHERE [MessageCode] = N'''' AND [Message] = N''A training observation requires review.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ANewIncidentRequiresReview'' WHERE [MessageCode] = N'''' AND [Message] = N''A new incident requires review.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ASessionWasSkipped'' WHERE [MessageCode] = N'''' AND [Message] = N''A session was skipped.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''HorseHealthStatusChangedReviewCurrentTrainingLimits'' WHERE [MessageCode] = N'''' AND [Message] = N''Horse health status changed. Review current training limits.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''TrainingRestrictionsChangedReviewPlannedSessions'' WHERE [MessageCode] = N'''' AND [Message] = N''Training restrictions changed. Review planned sessions.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''AMedicalRestrictionChangedDuringYourSessionStopIncompatible'' WHERE [MessageCode] = N'''' AND [Message] = N''A medical restriction changed during your session. Stop incompatible activity and contact the Veterinarian.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''MedicalClearanceIssuedReviewTheTrainingPlanBeforeResuming'' WHERE [MessageCode] = N'''' AND [Message] = N''Medical clearance issued. Review the training plan before resuming.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''MedicalFollowUpCompletedReviewCurrentRestrictions'' WHERE [MessageCode] = N'''' AND [Message] = N''Medical follow-up completed. Review current restrictions.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''PreventiveCareIsDue'' WHERE [MessageCode] = N'''' AND [Message] = N''Preventive care is due.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ATrainingSessionIsOverdue'' WHERE [MessageCode] = N'''' AND [Message] = N''A training session is overdue.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''MedicalFollowUpIsDue'' WHERE [MessageCode] = N'''' AND [Message] = N''Medical follow-up is due.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ACareTaskWasAssignedToYou'' WHERE [MessageCode] = N'''' AND [Message] = N''A care task was assigned to you.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ACareTaskRequiresReview'' WHERE [MessageCode] = N'''' AND [Message] = N''A care task requires review.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''AnInventoryItemReachedItsMinimumStock'' WHERE [MessageCode] = N'''' AND [Message] = N''An inventory item reached its minimum stock.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''AReplenishmentRequestNeedsReview'' WHERE [MessageCode] = N'''' AND [Message] = N''A replenishment request needs review.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''YourReplenishmentRequestWasReviewed'' WHERE [MessageCode] = N'''' AND [Message] = N''Your replenishment request was reviewed.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''TreatmentTaskCompletedReviewPlan'' WHERE [MessageCode] = N'''' AND [Message] = N''A groom completed a treatment care task. Review the treatment plan status.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ThePlanWasArchivedAndThisSessionWasCancelled'' WHERE [MessageCode] = N'''' AND [Message] = N''The plan was archived and this session was cancelled.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''StaffDisabledWithOpenWork'' WHERE [MessageCode] = N'''' AND [Message] = N''A staff account was disabled with open work requiring reassignment.'';')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''AHorseRegistrationRequiresReview'' WHERE [MessageCode] = N'''' AND [Type] = 0;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''YourHorseRegistrationRequiresRevision'' WHERE [MessageCode] = N'''' AND [Type] = 1;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''YourHorseRegistrationWasApproved'' WHERE [MessageCode] = N'''' AND [Type] = 2;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''OfficialHorseStaffAssignmentChanged'' WHERE [MessageCode] = N'''' AND [Type] = 3;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ATrainingSessionWasAssignedToYou'' WHERE [MessageCode] = N'''' AND [Type] = 4;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ATrainingAssignmentWasRemoved'' WHERE [MessageCode] = N'''' AND [Type] = 5;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ARiderSubmittedASessionResult'' WHERE [MessageCode] = N'''' AND [Type] = 6;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ANewIncidentRequiresReview'' WHERE [MessageCode] = N'''' AND [Type] = 7;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ASessionWasSkipped'' WHERE [MessageCode] = N'''' AND [Type] = 8;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''HorseHealthStatusChangedReviewCurrentTrainingLimits'' WHERE [MessageCode] = N'''' AND [Type] = 9;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''TrainingRestrictionsChangedReviewPlannedSessions'' WHERE [MessageCode] = N'''' AND [Type] = 10;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''MedicalFollowUpCompletedReviewCurrentRestrictions'' WHERE [MessageCode] = N'''' AND [Type] = 11;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''PreventiveCareIsDue'' WHERE [MessageCode] = N'''' AND [Type] = 12;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ATrainingSessionIsOverdue'' WHERE [MessageCode] = N'''' AND [Type] = 13;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''MedicalFollowUpIsDue'' WHERE [MessageCode] = N'''' AND [Type] = 14;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ACareTaskWasAssignedToYou'' WHERE [MessageCode] = N'''' AND [Type] = 15;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''ACareTaskRequiresReview'' WHERE [MessageCode] = N'''' AND [Type] = 16;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''AnInventoryItemReachedItsMinimumStock'' WHERE [MessageCode] = N'''' AND [Type] = 17;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''AReplenishmentRequestNeedsReview'' WHERE [MessageCode] = N'''' AND [Type] = 18;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''YourReplenishmentRequestWasReviewed'' WHERE [MessageCode] = N'''' AND [Type] = 19;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    EXEC(N'UPDATE [Notifications] SET [MessageCode] = N''TreatmentTaskCompletedReviewPlan'' WHERE [MessageCode] = N'''' AND [Type] = 20;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026020000_NotificationMessageCodes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026020000_NotificationMessageCodes', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026070938_LocalizeDefaultTrainingCurricula'
)
BEGIN
    EXEC(N'UPDATE [Templates] SET [CurriculumJson] = N''{"planOptions":[{"key":"standard","label":"Duy tr\u00EC nh\u1ECBp ch\u1EA1y \u1ED5n \u0111\u1ECBnh","goal":"Ph\u00E1t tri\u1EC3n s\u1EE9c b\u1EC1n v\u00E0 kh\u1EA3 n\u0103ng gi\u1EEF nh\u1ECBp ch\u1EA1y n\u01B0\u1EDBc \u0111\u1EA1i ch\u1EADm.","phase":"Build"}],"sessionOptions":[{"key":"canter-light","label":"Ch\u1EA1y n\u01B0\u1EDBc \u0111\u1EA1i ch\u1EADm nh\u1EB9","trainingType":"Canter","distanceMetres":800,"intensity":"Light","surface":"C\u00E1t","durationMinutes":20,"target":"Duy tr\u00EC nh\u1ECBp ch\u1EA1y \u1ED5n \u0111\u1ECBnh v\u00E0 ki\u1EC3m so\u00E1t h\u01B0\u1EDBng v\u1EADn \u0111\u1ED9ng."},{"key":"canter-steady","label":"Ch\u1EA1y n\u01B0\u1EDBc \u0111\u1EA1i ch\u1EADm \u0111\u1EC1u","trainingType":"Canter","distanceMetres":1200,"intensity":"Moderate","surface":"C\u00E1t","durationMinutes":25,"target":"Gi\u1EEF nh\u1ECBp ch\u1EA1y n\u01B0\u1EDBc \u0111\u1EA1i ch\u1EADm \u0111\u1EC1u, kh\u1EDFi \u0111\u1ED9ng v\u00E0 th\u1EA3 l\u1ECFng \u0111\u1EA7y \u0111\u1EE7."}]}'', [Goal] = N''Phát triển sức bền và khả năng giữ nhịp chạy nước đại chậm.''
    WHERE [Id] = ''9b590cbd-cc33-4f4d-8f03-000000000002'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026070938_LocalizeDefaultTrainingCurricula'
)
BEGIN
    EXEC(N'UPDATE [Templates] SET [CurriculumJson] = N''{"planOptions":[{"key":"standard","label":"Ph\u00E1t tri\u1EC3n t\u1ED1c \u0111\u1ED9 theo t\u1EEBng \u0111o\u1EA1n","goal":"R\u00E8n kh\u1EA3 n\u0103ng t\u0103ng t\u1ED1c c\u00F3 ki\u1EC3m so\u00E1t xen k\u1EBD h\u1ED3i ph\u1EE5c.","phase":"Peak"}],"sessionOptions":[{"key":"gallop-interval","label":"\u0110o\u1EA1n ch\u1EA1y n\u01B0\u1EDBc \u0111\u1EA1i c\u00F3 ki\u1EC3m so\u00E1t","trainingType":"Gallop","distanceMetres":600,"intensity":"Moderate","surface":"C\u00E1t","durationMinutes":20,"target":"V\u1EADn \u0111\u1ED9ng theo t\u1EEBng \u0111o\u1EA1n, xen k\u1EBD h\u1ED3i ph\u1EE5c v\u00E0 quan s\u00E1t ph\u1EA3n \u1EE9ng."},{"key":"sprint-short","label":"\u0110o\u1EA1n t\u0103ng t\u1ED1c ng\u1EAFn","trainingType":"Sprint","distanceMetres":300,"intensity":"Heavy","surface":"C\u00E1t","durationMinutes":15,"target":"T\u0103ng t\u1ED1c ng\u1EAFn theo h\u01B0\u1EDBng d\u1EABn chuy\u00EAn m\u00F4n, kh\u00F4ng \u00E1p d\u1EE5ng khi c\u00F3 h\u1EA1n ch\u1EBF s\u1EE9c kh\u1ECFe."}]}''
    WHERE [Id] = ''9b590cbd-cc33-4f4d-8f03-000000000003'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026070938_LocalizeDefaultTrainingCurricula'
)
BEGIN
    EXEC(N'UPDATE [Templates] SET [CurriculumJson] = N''{"planOptions":[{"key":"standard","label":"V\u1EADn \u0111\u1ED9ng nh\u1EB9 gi\u1EEFa c\u00E1c bu\u1ED5i n\u1EB7ng","goal":"C\u00E2n b\u1EB1ng kh\u1ED1i l\u01B0\u1EE3ng hu\u1EA5n luy\u1EC7n b\u1EB1ng v\u1EADn \u0111\u1ED9ng nh\u1EB9.","phase":"Recovery"}],"sessionOptions":[{"key":"recovery-easy","label":"V\u1EADn \u0111\u1ED9ng nh\u1EB9","trainingType":"Recovery","distanceMetres":300,"intensity":"Light","surface":"C\u00E1t","durationMinutes":15,"target":"Theo d\u00F5i v\u1EADn \u0111\u1ED9ng nh\u1EB9 theo \u0111\u00E1nh gi\u00E1 c\u1EE7a hu\u1EA5n luy\u1EC7n vi\u00EAn v\u00E0 b\u00E1c s\u0129."},{"key":"walk-recovery","label":"\u0110i b\u1ED9 th\u1EA3 l\u1ECFng","trainingType":"Walk","distanceMetres":500,"intensity":"Light","surface":"C\u00E1t","durationMinutes":20,"target":"\u0110i b\u1ED9 \u0111\u1EC1u \u0111\u1EC3 th\u1EA3 l\u1ECFng, ghi nh\u1EADn ph\u1EA3n \u1EE9ng sau bu\u1ED5i t\u1EADp."}]}''
    WHERE [Id] = ''9b590cbd-cc33-4f4d-8f03-000000000005'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026070938_LocalizeDefaultTrainingCurricula'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026070938_LocalizeDefaultTrainingCurricula', N'10.0.12');
END;

COMMIT;
GO
