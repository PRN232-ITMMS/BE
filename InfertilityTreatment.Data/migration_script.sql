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
GO

CREATE TABLE [TreatmentServices] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [BasePrice] decimal(12,2) NULL,
    [EstimatedDuration] int NULL,
    [Requirements] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TreatmentServices] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [Gender] tinyint NULL,
    [Role] tinyint NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TreatmentPackages] (
    [Id] int NOT NULL IDENTITY,
    [ServiceId] int NOT NULL,
    [PackageName] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Price] decimal(12,2) NOT NULL,
    [IncludedServices] nvarchar(max) NULL,
    [DurationWeeks] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TreatmentPackages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TreatmentPackages_TreatmentServices_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [TreatmentServices] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Address] nvarchar(500) NULL,
    [EmergencyContactName] nvarchar(200) NULL,
    [EmergencyContactPhone] nvarchar(20) NULL,
    [MedicalHistory] nvarchar(max) NULL,
    [MaritalStatus] nvarchar(50) NULL,
    [Occupation] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Customers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Doctors] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [LicenseNumber] nvarchar(100) NOT NULL,
    [Specialization] nvarchar(200) NULL,
    [YearsOfExperience] int NOT NULL,
    [Education] nvarchar(500) NULL,
    [Biography] nvarchar(max) NULL,
    [ConsultationFee] decimal(10,2) NULL,
    [IsAvailable] bit NOT NULL,
    [SuccessRate] decimal(5,2) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Doctors_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Token] nvarchar(500) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [TreatmentCycles] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NOT NULL,
    [DoctorId] int NOT NULL,
    [PackageId] int NOT NULL,
    [CycleNumber] int NOT NULL,
    [Status] tinyint NOT NULL,
    [StartDate] datetime2 NULL,
    [ExpectedEndDate] datetime2 NULL,
    [ActualEndDate] datetime2 NULL,
    [TotalCost] decimal(12,2) NULL,
    [Notes] nvarchar(max) NULL,
    [IsSuccessful] bit NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TreatmentCycles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TreatmentCycles_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TreatmentCycles_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TreatmentCycles_TreatmentPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [TreatmentPackages] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_Customers_UserId] ON [Customers] ([UserId]);
GO

CREATE INDEX [IX_Doctors_IsAvailable] ON [Doctors] ([IsAvailable]);
GO

CREATE UNIQUE INDEX [IX_Doctors_LicenseNumber] ON [Doctors] ([LicenseNumber]);
GO

CREATE INDEX [IX_Doctors_Specialization] ON [Doctors] ([Specialization]);
GO

CREATE UNIQUE INDEX [IX_Doctors_UserId] ON [Doctors] ([UserId]);
GO

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
GO

CREATE INDEX [IX_TreatmentCycles_CustomerId] ON [TreatmentCycles] ([CustomerId]);
GO

CREATE INDEX [IX_TreatmentCycles_DoctorId] ON [TreatmentCycles] ([DoctorId]);
GO

CREATE INDEX [IX_TreatmentCycles_PackageId] ON [TreatmentCycles] ([PackageId]);
GO

CREATE INDEX [IX_TreatmentCycles_StartDate] ON [TreatmentCycles] ([StartDate]);
GO

CREATE INDEX [IX_TreatmentCycles_Status] ON [TreatmentCycles] ([Status]);
GO

CREATE INDEX [IX_TreatmentPackages_ServiceId] ON [TreatmentPackages] ([ServiceId]);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO

CREATE INDEX [IX_Users_Role] ON [Users] ([Role]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250607113314_InitialCreate', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [IsRead] bit NOT NULL,
    [RelatedEntityType] nvarchar(100) NULL,
    [RelatedEntityId] int NULL,
    [ScheduledAt] datetime2 NULL,
    [SentAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_notifications_UserId] ON [notifications] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250616163935_AddNotification', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

EXEC sp_rename N'[notifications]', N'Notifications';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250616171431_RenameNotificationsTable', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Appointments] (
    [Id] int NOT NULL IDENTITY,
    [CycleId] int NOT NULL,
    [DoctorId] int NOT NULL,
    [AppointmentType] tinyint NOT NULL,
    [ScheduledDateTime] datetime2 NOT NULL,
    [Status] tinyint NOT NULL,
    [Duration] int NOT NULL,
    [Notes] nvarchar(max) NULL,
    [Results] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Appointments_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Appointments_TreatmentCycles_CycleId] FOREIGN KEY ([CycleId]) REFERENCES [TreatmentCycles] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Appointments_CycleId] ON [Appointments] ([CycleId]);
GO

CREATE INDEX [IX_Appointments_DoctorId] ON [Appointments] ([DoctorId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250617101038_AddAppointment', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Appointments] DROP CONSTRAINT [FK_Appointments_Doctors_DoctorId];
GO

ALTER TABLE [Appointments] DROP CONSTRAINT [FK_Appointments_TreatmentCycles_CycleId];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'UpdatedAt');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var0 + '];');
UPDATE [Appointments] SET [UpdatedAt] = '0001-01-01T00:00:00.0000000' WHERE [UpdatedAt] IS NULL;
ALTER TABLE [Appointments] ALTER COLUMN [UpdatedAt] datetime2 NOT NULL;
ALTER TABLE [Appointments] ADD DEFAULT '0001-01-01T00:00:00.0000000' FOR [UpdatedAt];
GO

CREATE TABLE [DoctorSchedules] (
    [Id] int NOT NULL IDENTITY,
    [DoctorId] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_DoctorSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DoctorSchedules_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_DoctorSchedules_DoctorId] ON [DoctorSchedules] ([DoctorId]);
GO

ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_TreatmentCycles_CycleId] FOREIGN KEY ([CycleId]) REFERENCES [TreatmentCycles] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250618142306_AddDoctorSchedules', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

EXEC sp_rename N'[Appointments].[Duration]', N'DoctorScheduleId', N'COLUMN';
GO

CREATE INDEX [IX_Appointments_DoctorScheduleId] ON [Appointments] ([DoctorScheduleId]);
GO

ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_DoctorSchedules_DoctorScheduleId] FOREIGN KEY ([DoctorScheduleId]) REFERENCES [DoctorSchedules] ([Id]) ON DELETE CASCADE;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250618143728_ModifyAppointments', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [TestResults] (
    [Id] int NOT NULL IDENTITY,
    [CycleId] int NOT NULL,
    [TestType] nvarchar(100) NOT NULL,
    [TestDate] datetime2 NOT NULL,
    [Results] nvarchar(max) NULL,
    [ReferenceRange] nvarchar(100) NULL,
    [Status] nvarchar(50) NOT NULL,
    [DoctorNotes] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TestResults] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TestResults_TreatmentCycles_CycleId] FOREIGN KEY ([CycleId]) REFERENCES [TreatmentCycles] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_TestResults_CycleId] ON [TestResults] ([CycleId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250619082902_AddTestResults', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TestResults]') AND [c].[name] = N'TestType');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [TestResults] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [TestResults] ALTER COLUMN [TestType] tinyint NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250619095006_ModifyFieldTestResults', N'8.0.16');
GO

COMMIT;
GO

