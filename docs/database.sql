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

