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

CREATE TABLE [TreatmentPhases] (
    [Id] int NOT NULL IDENTITY,
    [CycleId] int NOT NULL,
    [PhaseName] nvarchar(200) NOT NULL,
    [PhaseOrder] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [Cost] decimal(12,2) NOT NULL,
    [Instructions] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TreatmentPhases] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TreatmentPhases_TreatmentCycles_CycleId] FOREIGN KEY ([CycleId]) REFERENCES [TreatmentCycles] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_TreatmentPhases_CycleId] ON [TreatmentPhases] ([CycleId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250619110253_AddTreatmentCycles', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_TreatmentPhases_CycleId] ON [TreatmentPhases];
GO

CREATE TABLE [TestResults] (
    [Id] int NOT NULL IDENTITY,
    [CycleId] int NOT NULL,
    [TestType] tinyint NOT NULL,
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

CREATE UNIQUE INDEX [IX_TreatmentPhases_CycleId_PhaseOrder] ON [TreatmentPhases] ([CycleId], [PhaseOrder]);
GO

CREATE INDEX [IX_TestResults_CycleId] ON [TestResults] ([CycleId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250620134544_AddTestResults', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [TestResults] ADD [AppointmentId] int NOT NULL DEFAULT 0;
GO

CREATE INDEX [IX_TestResults_AppointmentId] ON [TestResults] ([AppointmentId]);
GO

ALTER TABLE [TestResults] ADD CONSTRAINT [FK_TestResults_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE CASCADE;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250620141302_ModifyFieldTestResults', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [TestResults] DROP CONSTRAINT [FK_TestResults_Appointments_AppointmentId];
GO

ALTER TABLE [TestResults] ADD CONSTRAINT [FK_TestResults_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250620141845_ModifyFieldAppointments', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_TreatmentPhases_CycleId_PhaseOrder] ON [TreatmentPhases];
GO

CREATE TABLE [Medications] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [ActiveIngredient] nvarchar(200) NOT NULL,
    [Manufacturer] nvarchar(200) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [StorageInstructions] nvarchar(500) NOT NULL,
    [SideEffects] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Medications] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_Medications_Name] ON [Medications] ([Name]);
GO

CREATE INDEX [IX_Medications_ActiveIngredient] ON [Medications] ([ActiveIngredient]);
GO

CREATE TABLE [Prescriptions] (
    [Id] int NOT NULL IDENTITY,
    [MedicationId] int NOT NULL,
    [PhaseId] int NOT NULL,
    [Dosage] nvarchar(100) NOT NULL,
    [Frequency] nvarchar(100) NOT NULL,
    [Duration] int NOT NULL,
    [Instructions] nvarchar(500) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Prescriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Prescriptions_Medications_MedicationId] FOREIGN KEY ([MedicationId]) REFERENCES [Medications] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Prescriptions_TreatmentPhases_PhaseId] FOREIGN KEY ([PhaseId]) REFERENCES [TreatmentPhases] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Prescriptions_MedicationId] ON [Prescriptions] ([MedicationId]);
GO

CREATE INDEX [IX_Prescriptions_PhaseId] ON [Prescriptions] ([PhaseId]);
GO

CREATE INDEX [IX_Prescriptions_StartDate_EndDate] ON [Prescriptions] ([StartDate], [EndDate]);
GO

CREATE INDEX [IX_Prescriptions_StartDate] ON [Prescriptions] ([StartDate]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250623122502_AddMedicationAndPrescription', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_Medications_Name] ON [Medications];
DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Medications]') AND [c].[name] = N'Name');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Medications] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Medications] ALTER COLUMN [Name] nvarchar(200) NOT NULL;
CREATE INDEX [IX_Medications_Name] ON [Medications] ([Name]);
GO

DROP INDEX [IX_Medications_ActiveIngredient] ON [Medications];
DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Medications]') AND [c].[name] = N'ActiveIngredient');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Medications] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Medications] ALTER COLUMN [ActiveIngredient] nvarchar(200) NOT NULL;
CREATE INDEX [IX_Medications_ActiveIngredient] ON [Medications] ([ActiveIngredient]);
GO

CREATE TABLE [Reviews] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NOT NULL,
    [DoctorId] int NULL,
    [CycleId] int NULL,
    [Rating] int NOT NULL,
    [ReviewType] nvarchar(50) NOT NULL,
    [Comment] nvarchar(max) NOT NULL,
    [IsApproved] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reviews_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
    CONSTRAINT [FK_Reviews_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]),
    CONSTRAINT [FK_Reviews_TreatmentCycles_CycleId] FOREIGN KEY ([CycleId]) REFERENCES [TreatmentCycles] ([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_Reviews_CustomerId] ON [Reviews] ([CustomerId]);
GO

CREATE INDEX [IX_Reviews_CycleId] ON [Reviews] ([CycleId]);
GO

CREATE INDEX [IX_Reviews_DoctorId] ON [Reviews] ([DoctorId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250624114954_AddReviews', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Reviews]') AND [c].[name] = N'Rating');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Reviews] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Reviews] ALTER COLUMN [Rating] int NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250627034653_newReview', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO


                -- User performance indexes
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Users_Role' AND object_id = OBJECT_ID('Users'))
                    CREATE INDEX IX_Users_Role ON Users(Role);
                    
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Users_Email' AND object_id = OBJECT_ID('Users'))
                    CREATE INDEX IX_Users_Email ON Users(Email);

                -- Treatment cycle indexes for analytics
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_TreatmentCycles_Status_StartDate' AND object_id = OBJECT_ID('TreatmentCycles'))
                    CREATE INDEX IX_TreatmentCycles_Status_StartDate ON TreatmentCycles(Status, StartDate);
                    
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_TreatmentCycles_CustomerId_Status' AND object_id = OBJECT_ID('TreatmentCycles'))
                    CREATE INDEX IX_TreatmentCycles_CustomerId_Status ON TreatmentCycles(CustomerId, Status);
                    
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_TreatmentCycles_DoctorId_Status' AND object_id = OBJECT_ID('TreatmentCycles'))
                    CREATE INDEX IX_TreatmentCycles_DoctorId_Status ON TreatmentCycles(DoctorId, Status);

                -- Appointment indexes for booking
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Appointments_DoctorId_ScheduledDateTime' AND object_id = OBJECT_ID('Appointments'))
                    CREATE INDEX IX_Appointments_DoctorId_ScheduledDateTime ON Appointments(DoctorId, ScheduledDateTime);
                    
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Appointments_Status_ScheduledDateTime' AND object_id = OBJECT_ID('Appointments'))
                    CREATE INDEX IX_Appointments_Status_ScheduledDateTime ON Appointments(Status, ScheduledDateTime);

                -- Test result indexes for analytics
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_TestResults_CycleId_TestDate' AND object_id = OBJECT_ID('TestResults'))
                    CREATE INDEX IX_TestResults_CycleId_TestDate ON TestResults(CycleId, TestDate);
                    
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_TestResults_TestType_Status' AND object_id = OBJECT_ID('TestResults'))
                    CREATE INDEX IX_TestResults_TestType_Status ON TestResults(TestType, Status);

                -- Review indexes for doctor statistics
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Reviews_DoctorId_IsApproved' AND object_id = OBJECT_ID('Reviews'))
                    CREATE INDEX IX_Reviews_DoctorId_IsApproved ON Reviews(DoctorId, IsApproved);
                    
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Reviews_Rating_CreatedAt' AND object_id = OBJECT_ID('Reviews'))
                    CREATE INDEX IX_Reviews_Rating_CreatedAt ON Reviews(Rating, CreatedAt);

                -- Notification indexes
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Notifications_UserId_IsRead' AND object_id = OBJECT_ID('Notifications'))
                    CREATE INDEX IX_Notifications_UserId_IsRead ON Notifications(UserId, IsRead);
                    
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Notifications_Type_ScheduledAt' AND object_id = OBJECT_ID('Notifications'))
                    CREATE INDEX IX_Notifications_Type_ScheduledAt ON Notifications(Type, ScheduledAt);
            
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250701013431_AddPerformanceIndexes', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO


                CREATE VIEW vw_TreatmentSuccessRates AS
                SELECT 
                    ts.Id as ServiceId,
                    ts.Name as TreatmentType,
                    COUNT(tc.Id) as TotalCycles,
                    SUM(CASE WHEN tc.Status = 3 THEN 1 ELSE 0 END) as SuccessfulCycles,
                    CASE 
                        WHEN COUNT(tc.Id) = 0 THEN 0
                        ELSE CAST(SUM(CASE WHEN tc.Status = 3 THEN 1 ELSE 0 END) * 100.0 / COUNT(tc.Id) AS DECIMAL(5,2))
                    END as SuccessRate
                FROM TreatmentServices ts
                LEFT JOIN TreatmentPackages tp ON ts.Id = tp.ServiceId  
                LEFT JOIN TreatmentCycles tc ON tp.Id = tc.PackageId
                GROUP BY ts.Id, ts.Name
                HAVING COUNT(tc.Id) > 0
            
GO


                CREATE VIEW vw_RevenueAnalytics AS
                SELECT 
                    ts.Id as ServiceId,
                    ts.Name as ServiceName,
                    YEAR(tc.CreatedAt) as Year,
                    MONTH(tc.CreatedAt) as Month,
                    COUNT(tc.Id) as TotalCycles,
                    SUM(tp.Price) as TotalRevenue,
                    CASE 
                        WHEN COUNT(tc.Id) = 0 THEN 0
                        ELSE AVG(tp.Price)
                    END as AvgRevenuePerCycle
                FROM TreatmentServices ts
                INNER JOIN TreatmentPackages tp ON ts.Id = tp.ServiceId
                INNER JOIN TreatmentCycles tc ON tp.Id = tc.PackageId  
                WHERE tc.CreatedAt IS NOT NULL
                GROUP BY ts.Id, ts.Name, YEAR(tc.CreatedAt), MONTH(tc.CreatedAt)
            
GO


                CREATE VIEW vw_DoctorPerformance AS
                SELECT 
                    d.Id as DoctorId,
                    u.FullName as DoctorName,
                    COUNT(DISTINCT tc.CustomerId) as TotalPatients,
                    COUNT(tc.Id) as TotalCycles,
                    SUM(CASE WHEN tc.Status = 3 THEN 1 ELSE 0 END) as SuccessfulCycles,
                    CASE 
                        WHEN COUNT(tc.Id) = 0 THEN 0
                        ELSE CAST(SUM(CASE WHEN tc.Status = 3 THEN 1 ELSE 0 END) * 100.0 / COUNT(tc.Id) AS DECIMAL(5,2))
                    END as SuccessRate,
                    ISNULL(AVG(CAST(r.Rating AS FLOAT)), 0) as AvgRating,
                    COUNT(r.Id) as TotalReviews
                FROM Doctors d
                INNER JOIN Users u ON d.UserId = u.Id
                LEFT JOIN TreatmentCycles tc ON d.Id = tc.DoctorId
                LEFT JOIN Reviews r ON d.Id = r.DoctorId AND r.IsApproved = 1
                GROUP BY d.Id, u.FullName
            
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250704040006_CreatePerformanceViews', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Payments] (
    [Id] int NOT NULL IDENTITY,
    [PaymentId] nvarchar(50) NOT NULL,
    [CustomerId] int NOT NULL,
    [TreatmentCycleId] int NULL,
    [AppointmentId] int NULL,
    [Amount] decimal(12,2) NOT NULL,
    [PaymentMethod] nvarchar(50) NOT NULL,
    [Status] int NOT NULL,
    [TransactionId] nvarchar(100) NULL,
    [Description] nvarchar(500) NULL,
    [ProcessedAt] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_Payments_PaymentId] UNIQUE ([PaymentId]),
    CONSTRAINT [FK_Payments_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Payments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Payments_TreatmentCycles_TreatmentCycleId] FOREIGN KEY ([TreatmentCycleId]) REFERENCES [TreatmentCycles] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [Invoices] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceNumber] nvarchar(50) NOT NULL,
    [CustomerId] int NOT NULL,
    [AppointmentId] int NULL,
    [TreatmentCycleId] int NULL,
    [PaymentId] nvarchar(50) NULL,
    [SubTotal] decimal(12,2) NOT NULL,
    [TaxAmount] decimal(12,2) NOT NULL,
    [DiscountAmount] decimal(12,2) NOT NULL,
    [TotalAmount] decimal(12,2) NOT NULL,
    [Status] int NOT NULL,
    [IssueDate] datetime2 NOT NULL,
    [DueDate] datetime2 NULL,
    [PaidDate] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [Terms] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Invoices_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Invoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Invoices_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([PaymentId]) ON DELETE SET NULL,
    CONSTRAINT [FK_Invoices_TreatmentCycles_TreatmentCycleId] FOREIGN KEY ([TreatmentCycleId]) REFERENCES [TreatmentCycles] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [InvoiceItems] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(12,2) NOT NULL,
    [TotalPrice] decimal(12,2) NOT NULL,
    [TreatmentServiceId] int NULL,
    [MedicationId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_InvoiceItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoiceItems_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InvoiceItems_Medications_MedicationId] FOREIGN KEY ([MedicationId]) REFERENCES [Medications] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_InvoiceItems_TreatmentServices_TreatmentServiceId] FOREIGN KEY ([TreatmentServiceId]) REFERENCES [TreatmentServices] ([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_InvoiceItems_InvoiceId] ON [InvoiceItems] ([InvoiceId]);
GO

CREATE INDEX [IX_InvoiceItems_MedicationId] ON [InvoiceItems] ([MedicationId]);
GO

CREATE INDEX [IX_InvoiceItems_TreatmentServiceId] ON [InvoiceItems] ([TreatmentServiceId]);
GO

CREATE INDEX [IX_Invoices_AppointmentId] ON [Invoices] ([AppointmentId]);
GO

CREATE INDEX [IX_Invoices_CustomerId] ON [Invoices] ([CustomerId]);
GO

CREATE INDEX [IX_Invoices_DueDate] ON [Invoices] ([DueDate]);
GO

CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]);
GO

CREATE INDEX [IX_Invoices_IssueDate] ON [Invoices] ([IssueDate]);
GO

CREATE INDEX [IX_Invoices_PaymentId] ON [Invoices] ([PaymentId]);
GO

CREATE INDEX [IX_Invoices_Status] ON [Invoices] ([Status]);
GO

CREATE INDEX [IX_Invoices_TreatmentCycleId] ON [Invoices] ([TreatmentCycleId]);
GO

CREATE INDEX [IX_Payments_AppointmentId] ON [Payments] ([AppointmentId]);
GO

CREATE INDEX [IX_Payments_CreatedAt] ON [Payments] ([CreatedAt]);
GO

CREATE INDEX [IX_Payments_CustomerId] ON [Payments] ([CustomerId]);
GO

CREATE UNIQUE INDEX [IX_Payments_PaymentId] ON [Payments] ([PaymentId]);
GO

CREATE INDEX [IX_Payments_Status] ON [Payments] ([Status]);
GO

CREATE INDEX [IX_Payments_TransactionId] ON [Payments] ([TransactionId]);
GO

CREATE INDEX [IX_Payments_TreatmentCycleId] ON [Payments] ([TreatmentCycleId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250704055238_AddPaymentInvoiceSystem', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO


                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_CustomerId' AND object_id = OBJECT_ID('Payments'))
                    CREATE INDEX IX_Payments_CustomerId ON Payments(CustomerId)
            
GO


                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_Status' AND object_id = OBJECT_ID('Payments'))
                    CREATE INDEX IX_Payments_Status ON Payments(Status)
            
GO


                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TreatmentCycles_Status_StartDate' AND object_id = OBJECT_ID('TreatmentCycles'))
                    CREATE INDEX IX_TreatmentCycles_Status_StartDate ON TreatmentCycles(Status, StartDate)
            
GO


                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Appointments_DoctorId_ScheduledDateTime' AND object_id = OBJECT_ID('Appointments'))
                    CREATE INDEX IX_Appointments_DoctorId_ScheduledDateTime ON Appointments(DoctorId, ScheduledDateTime)
            
GO


                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_CustomerId' AND object_id = OBJECT_ID('Invoices'))
                    CREATE INDEX IX_Invoices_CustomerId ON Invoices(CustomerId)
            
GO


                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_Status' AND object_id = OBJECT_ID('Invoices'))
                    CREATE INDEX IX_Invoices_Status ON Invoices(Status)
            
GO


                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_IssueDate' AND object_id = OBJECT_ID('Invoices'))
                    CREATE INDEX IX_Invoices_IssueDate ON Invoices(IssueDate)
            
GO


                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InvoiceItems_InvoiceId' AND object_id = OBJECT_ID('InvoiceItems'))
                    CREATE INDEX IX_InvoiceItems_InvoiceId ON InvoiceItems(InvoiceId)
            
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250704060534_AddPaymentSystemIndexes', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Payments] ADD [PaymentGatewayResponse] nvarchar(max) NULL;
GO

CREATE TABLE [PaymentLogs] (
    [Id] int NOT NULL IDENTITY,
    [PaymentId] int NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [RequestData] nvarchar(max) NULL,
    [ResponseData] nvarchar(max) NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_PaymentLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PaymentLogs_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_PaymentLogs_PaymentId] ON [PaymentLogs] ([PaymentId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250707020906_AddPayments', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Payments] DROP CONSTRAINT [FK_Payments_Appointments_AppointmentId];
GO

ALTER TABLE [Payments] DROP CONSTRAINT [FK_Payments_TreatmentCycles_TreatmentCycleId];
GO

ALTER TABLE [Payments] ADD [TreatmentPackageId] int NOT NULL DEFAULT 0;
GO

CREATE INDEX [IX_Payments_TreatmentPackageId] ON [Payments] ([TreatmentPackageId]);
GO

ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]);
GO

ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_TreatmentCycles_TreatmentCycleId] FOREIGN KEY ([TreatmentCycleId]) REFERENCES [TreatmentCycles] ([Id]);
GO

ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_TreatmentPackages_TreatmentPackageId] FOREIGN KEY ([TreatmentPackageId]) REFERENCES [TreatmentPackages] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250707035713_ModifyPayments', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TreatmentPhases]') AND [c].[name] = N'Status');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [TreatmentPhases] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [TreatmentPhases] ALTER COLUMN [Status] nvarchar(50) NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250707173655_ChangeStatusCyclePharse', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Reviews]') AND [c].[name] = N'ReviewType');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Reviews] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Reviews] ALTER COLUMN [ReviewType] nvarchar(max) NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250708082855_UpdateReviewTypeToEnum', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Reviews]') AND [c].[name] = N'ReviewType');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Reviews] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Reviews] DROP COLUMN [ReviewType];
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250708084828_RemoveReviewTypeToEnum', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [TreatmentPhases] ADD [ActualStartDate] datetime2 NULL;
GO

ALTER TABLE [TreatmentPhases] ADD [ScheduledStartDate] datetime2 NULL;
GO

ALTER TABLE [TreatmentCycles] ADD [ActualStartDate] datetime2 NULL;
GO

ALTER TABLE [TreatmentCycles] ADD [DoctorNotes] nvarchar(1000) NULL;
GO

ALTER TABLE [TreatmentCycles] ADD [EstimatedCompletionDate] datetime2 NULL;
GO

ALTER TABLE [TreatmentCycles] ADD [SpecialInstructions] nvarchar(max) NULL;
GO

ALTER TABLE [TreatmentCycles] ADD [TreatmentPlan] nvarchar(2000) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250708124103_AddCycleInitializationFields', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP TABLE [InvoiceItems];
GO

DROP TABLE [PaymentLogs];
GO

DROP TABLE [Invoices];
GO

DROP TABLE [Payments];
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'CycleId');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [Appointments] ALTER COLUMN [CycleId] int NULL;
GO

ALTER TABLE [Appointments] ADD [CustomerId] int NOT NULL DEFAULT 0;
GO

CREATE INDEX [IX_Appointments_CustomerId] ON [Appointments] ([CustomerId]);
GO

ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250711112200_AddCustomerIdToAppointments', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Appointments] DROP CONSTRAINT [FK_Appointments_Customers_CustomerId];
GO

ALTER TABLE [DoctorSchedules] DROP CONSTRAINT [FK_DoctorSchedules_Doctors_DoctorId];
GO

DROP INDEX [IX_DoctorSchedules_DoctorId] ON [DoctorSchedules];
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DoctorSchedules]') AND [c].[name] = N'DoctorId');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [DoctorSchedules] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [DoctorSchedules] DROP COLUMN [DoctorId];
GO

DROP INDEX [IX_Appointments_CycleId] ON [Appointments];
DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'CycleId');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var9 + '];');
UPDATE [Appointments] SET [CycleId] = 0 WHERE [CycleId] IS NULL;
ALTER TABLE [Appointments] ALTER COLUMN [CycleId] int NOT NULL;
ALTER TABLE [Appointments] ADD DEFAULT 0 FOR [CycleId];
CREATE INDEX [IX_Appointments_CycleId] ON [Appointments] ([CycleId]);
GO

ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250711113509_FixFieldDoctorSchedules', N'8.0.16');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'CycleId');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [Appointments] ALTER COLUMN [CycleId] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250711121406_FixFieldAppointments', N'8.0.16');
GO

COMMIT;
GO

