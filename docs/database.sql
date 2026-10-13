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

CREATE TABLE [Stables] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(4000) NOT NULL,
    [CreatedAt] bigint NOT NULL,
    [Version] bigint NOT NULL,
    CONSTRAINT [PK_Stables] PRIMARY KEY ([Id])
);

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

CREATE INDEX [IX_Assignments_HorseId_Role_Active] ON [Assignments] ([HorseId], [Role], [Active]);

CREATE INDEX [IX_Attachments_RegistrationId] ON [Attachments] ([RegistrationId]);

CREATE INDEX [IX_CareTasks_HorseId] ON [CareTasks] ([HorseId]);

CREATE INDEX [IX_Challenges_UserId_Purpose] ON [Challenges] ([UserId], [Purpose]);

CREATE UNIQUE INDEX [IX_Evaluations_SessionId] ON [Evaluations] ([SessionId]);

CREATE INDEX [IX_FollowUps_HorseId] ON [FollowUps] ([HorseId]);

CREATE INDEX [IX_Horses_OwnerId] ON [Horses] ([OwnerId]);

CREATE UNIQUE INDEX [IX_Horses_RegistrationId] ON [Horses] ([RegistrationId]);

CREATE INDEX [IX_IncidentPhotos_IncidentId] ON [IncidentPhotos] ([IncidentId]);

CREATE INDEX [IX_Incidents_HorseId] ON [Incidents] ([HorseId]);

CREATE INDEX [IX_Injuries_HorseId] ON [Injuries] ([HorseId]);

CREATE INDEX [IX_Injuries_MedicalRecordId] ON [Injuries] ([MedicalRecordId]);

CREATE INDEX [IX_Measurements_HorseId] ON [Measurements] ([HorseId]);

CREATE INDEX [IX_MedicalRecords_HorseId] ON [MedicalRecords] ([HorseId]);

CREATE INDEX [IX_Notifications_RecipientId_Read] ON [Notifications] ([RecipientId], [Read]);

CREATE INDEX [IX_Occupancies_HorseId] ON [Occupancies] ([HorseId]);

CREATE INDEX [IX_Occupancies_StallId] ON [Occupancies] ([StallId]);

CREATE INDEX [IX_Plans_HorseId] ON [Plans] ([HorseId]);

CREATE INDEX [IX_Plans_TemplateId] ON [Plans] ([TemplateId]);

CREATE INDEX [IX_Plans_TrainerId] ON [Plans] ([TrainerId]);

CREATE INDEX [IX_PreventiveCare_HorseId] ON [PreventiveCare] ([HorseId]);

CREATE INDEX [IX_Registrations_OwnerId] ON [Registrations] ([OwnerId]);

CREATE INDEX [IX_Replenishments_ItemId] ON [Replenishments] ([ItemId]);

CREATE INDEX [IX_Restrictions_HorseId_Cleared] ON [Restrictions] ([HorseId], [Cleared]);

CREATE INDEX [IX_Restrictions_MedicalRecordId] ON [Restrictions] ([MedicalRecordId]);

CREATE UNIQUE INDEX [IX_Results_SessionId] ON [Results] ([SessionId]);

CREATE INDEX [IX_Sessions_HorseId] ON [Sessions] ([HorseId]);

CREATE INDEX [IX_Sessions_PlanId] ON [Sessions] ([PlanId]);

CREATE INDEX [IX_Sessions_RiderId] ON [Sessions] ([RiderId]);

CREATE INDEX [IX_Stalls_StableId] ON [Stalls] ([StableId]);

CREATE INDEX [IX_StockMovements_ItemId] ON [StockMovements] ([ItemId]);

CREATE INDEX [IX_TrainingRevisions_PlanId] ON [TrainingRevisions] ([PlanId]);

CREATE INDEX [IX_TrainingRevisions_SessionId] ON [TrainingRevisions] ([SessionId]);

CREATE INDEX [IX_Treatments_HorseId] ON [Treatments] ([HorseId]);

CREATE INDEX [IX_Treatments_MedicalRecordId] ON [Treatments] ([MedicalRecordId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261003125915_InitialSqlServer', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [EmailMessages] ADD [ChallengeId] uniqueidentifier NULL;

ALTER TABLE [EmailMessages] ADD [DiscardedAt] bigint NULL;

ALTER TABLE [EmailMessages] ADD [ExpiresAt] bigint NULL;

CREATE INDEX [IX_Notifications_ReferenceId_Type] ON [Notifications] ([ReferenceId], [Type]);

CREATE INDEX [IX_EmailMessages_ChallengeId] ON [EmailMessages] ([ChallengeId]);

CREATE INDEX [IX_EmailMessages_SentAt_DiscardedAt_NextAttemptAt] ON [EmailMessages] ([SentAt], [DiscardedAt], [NextAttemptAt]);

ALTER TABLE [EmailMessages] ADD CONSTRAINT [FK_EmailMessages_Challenges_ChallengeId] FOREIGN KEY ([ChallengeId]) REFERENCES [Challenges] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261004130807_WorkerDeliveryReliability', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'WeightKg');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Registrations] ALTER COLUMN [WeightKg] decimal(18,3) NULL;

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'Sire');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [Sire] nvarchar(4000) NULL;

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'Name');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [Name] nvarchar(4000) NULL;

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'MeasurementDate');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [MeasurementDate] date NULL;

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'HeightCm');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [HeightCm] decimal(18,3) NULL;

DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'HealthNotes');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [HealthNotes] nvarchar(4000) NULL;

DECLARE @var6 nvarchar(max);
SELECT @var6 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'Gender');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var6 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [Gender] int NULL;

DECLARE @var7 nvarchar(max);
SELECT @var7 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'DeclaredHealth');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var7 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [DeclaredHealth] nvarchar(4000) NULL;

DECLARE @var8 nvarchar(max);
SELECT @var8 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'DateOfBirth');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var8 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [DateOfBirth] date NULL;

DECLARE @var9 nvarchar(max);
SELECT @var9 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'Dam');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var9 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [Dam] nvarchar(4000) NULL;

DECLARE @var10 nvarchar(max);
SELECT @var10 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'Breed');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var10 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [Breed] nvarchar(4000) NULL;

DECLARE @var11 nvarchar(max);
SELECT @var11 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Registrations]') AND [c].[name] = N'BoardingStart');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Registrations] DROP CONSTRAINT ' + @var11 + ';');
ALTER TABLE [Registrations] ALTER COLUMN [BoardingStart] date NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261005060507_PartialRegistrationDraft', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_TrainingRevisions_PlanId] ON [TrainingRevisions];

DROP INDEX [IX_StockMovements_ItemId] ON [StockMovements];

DROP INDEX [IX_Sessions_HorseId] ON [Sessions];

DROP INDEX [IX_Sessions_PlanId] ON [Sessions];

DROP INDEX [IX_Sessions_RiderId] ON [Sessions];

DROP INDEX [IX_Registrations_OwnerId] ON [Registrations];

DROP INDEX [IX_Plans_HorseId] ON [Plans];

DROP INDEX [IX_Measurements_HorseId] ON [Measurements];

DROP INDEX [IX_Horses_OwnerId] ON [Horses];

DROP INDEX [IX_CareTasks_HorseId] ON [CareTasks];

CREATE INDEX [IX_TrainingRevisions_PlanId_CreatedAt] ON [TrainingRevisions] ([PlanId], [CreatedAt]);

CREATE INDEX [IX_StockMovements_ItemId_CreatedAt] ON [StockMovements] ([ItemId], [CreatedAt]);

CREATE INDEX [IX_Sessions_HorseId_Status_ScheduledAt] ON [Sessions] ([HorseId], [Status], [ScheduledAt]);

CREATE INDEX [IX_Sessions_PlanId_ScheduledAt] ON [Sessions] ([PlanId], [ScheduledAt]);

CREATE INDEX [IX_Sessions_RiderId_Status_ScheduledAt] ON [Sessions] ([RiderId], [Status], [ScheduledAt]);

CREATE INDEX [IX_Registrations_OwnerId_Status_CreatedAt] ON [Registrations] ([OwnerId], [Status], [CreatedAt]);

CREATE INDEX [IX_Plans_HorseId_Status] ON [Plans] ([HorseId], [Status]);

CREATE INDEX [IX_Notifications_RecipientId_Read_CreatedAt] ON [Notifications] ([RecipientId], [Read], [CreatedAt]);

CREATE INDEX [IX_Measurements_HorseId_Date] ON [Measurements] ([HorseId], [Date]);

CREATE INDEX [IX_Horses_OwnerId_Archived] ON [Horses] ([OwnerId], [Archived]);

CREATE INDEX [IX_CareTasks_GroomId_ScheduledAt] ON [CareTasks] ([GroomId], [ScheduledAt]);

CREATE INDEX [IX_CareTasks_HorseId_ScheduledAt] ON [CareTasks] ([HorseId], [ScheduledAt]);

CREATE INDEX [IX_Audit_ReferenceId_CreatedAt] ON [Audit] ([ReferenceId], [CreatedAt]);

CREATE INDEX [IX_Assignments_StaffId_Active_HorseId] ON [Assignments] ([StaffId], [Active], [HorseId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261007115208_QueryPerformanceIndexes', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
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

CREATE UNIQUE INDEX [IX_RealtimeOutboxMessages_NotificationId] ON [RealtimeOutboxMessages] ([NotificationId]);

CREATE INDEX [IX_RealtimeOutboxMessages_SentAt_NextAttemptAt_LockedUntil] ON [RealtimeOutboxMessages] ([SentAt], [NextAttemptAt], [LockedUntil]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261008031610_RealtimeNotificationOutbox', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_RealtimeOutboxMessages_NotificationId] ON [RealtimeOutboxMessages];

ALTER TABLE [RealtimeOutboxMessages] ADD [EventType] nvarchar(4000) NOT NULL DEFAULT N'';

ALTER TABLE [RealtimeOutboxMessages] ADD [NotificationVersion] bigint NOT NULL DEFAULT CAST(0 AS bigint);

CREATE UNIQUE INDEX [IX_RealtimeOutboxMessages_NotificationId_NotificationVersion] ON [RealtimeOutboxMessages] ([NotificationId], [NotificationVersion]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261008033133_RealtimeNotificationReadSignals', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
EXEC sp_rename N'[RealtimeOutboxMessages].[NotificationVersion]', N'SourceVersion', 'COLUMN';

EXEC sp_rename N'[RealtimeOutboxMessages].[NotificationId]', N'SourceId', 'COLUMN';

EXEC sp_rename N'[RealtimeOutboxMessages].[IX_RealtimeOutboxMessages_NotificationId_NotificationVersion]', N'IX_RealtimeOutboxMessages_SourceId_SourceVersion', 'INDEX';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261008033516_RealtimeResourceSignals', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261009025334_WebsiteManagement', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [StockMovements] ADD [ReplenishmentRequestId] uniqueidentifier NULL;

ALTER TABLE [Sessions] ADD [DurationMinutes] int NULL;

ALTER TABLE [Sessions] ADD [ScheduledEndAt] bigint NULL;

ALTER TABLE [Replenishments] ADD [ReceivedAt] bigint NULL;

ALTER TABLE [Replenishments] ADD [ReceivedBy] uniqueidentifier NULL;

ALTER TABLE [Replenishments] ADD [ReceivedQuantity] decimal(18,3) NOT NULL DEFAULT 0.0;

ALTER TABLE [Replenishments] ADD [ReviewNotes] nvarchar(4000) NULL;

ALTER TABLE [Replenishments] ADD [ReviewedAt] bigint NULL;

ALTER TABLE [Replenishments] ADD [ReviewedBy] uniqueidentifier NULL;

ALTER TABLE [Incidents] ADD [Resolution] nvarchar(4000) NULL;

ALTER TABLE [Incidents] ADD [ResolvedAt] bigint NULL;

ALTER TABLE [Incidents] ADD [ResolvedBy] uniqueidentifier NULL;

ALTER TABLE [Horses] ADD [ArchiveCategory] int NULL;

ALTER TABLE [Horses] ADD [ArchiveReason] nvarchar(4000) NULL;

ALTER TABLE [Horses] ADD [ArchivedAt] bigint NULL;

ALTER TABLE [Horses] ADD [ArchivedBy] uniqueidentifier NULL;

CREATE INDEX [IX_StockMovements_ReplenishmentRequestId] ON [StockMovements] ([ReplenishmentRequestId]);

ALTER TABLE [StockMovements] ADD CONSTRAINT [FK_StockMovements_Replenishments_ReplenishmentRequestId] FOREIGN KEY ([ReplenishmentRequestId]) REFERENCES [Replenishments] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261011120226_BusinessRuleLifecycle', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Assignments] ADD [RequiredTrainingType] int NULL;

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

CREATE INDEX [IX_StaffTimeOff_StaffId_Start_End] ON [StaffTimeOff] ([StaffId], [Start], [End]);

CREATE UNIQUE INDEX [IX_StaffWorkingHours_StaffId_DayOfWeek_StartMinute] ON [StaffWorkingHours] ([StaffId], [DayOfWeek], [StartMinute]);

CREATE UNIQUE INDEX [IX_TrainerExpertise_StaffId_TrainingType] ON [TrainerExpertise] ([StaffId], [TrainingType]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261013044927_TrainerExpertiseAndAvailability', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
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

CREATE UNIQUE INDEX [IX_PublicReferenceRecords_ExternalKey] ON [PublicReferenceRecords] ([ExternalKey]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261013062223_PublicReferenceData', N'10.0.12');

COMMIT;
GO
