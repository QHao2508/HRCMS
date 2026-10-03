using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Horse_BackEnd.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExpiresAt = table.Column<long>(type: "bigint", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    Consumed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SentAt = table.Column<long>(type: "bigint", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Stock = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MinimumStock = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Archived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Read = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DistanceMetres = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Intensity = table.Column<int>(type: "int", nullable: false),
                    Surface = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FrequencyPerWeek = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Archived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    NationalIdProtected = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    FailedLogins = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<long>(type: "bigint", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Replenishments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Replenishments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Replenishments_Inventory_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Inventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Inventory_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Inventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CleaningStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stalls_Stables_StableId",
                        column: x => x.StableId,
                        principalTable: "Stables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Sire = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Dam = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Breed = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MeasurementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeclaredHealth = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    HealthNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BoardingStart = table.Column<DateOnly>(type: "date", nullable: false),
                    BoardingEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    PreferredHeadTrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreferredGroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreferredVeterinarianId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registrations_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    StorageName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "Registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Horses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Sire = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Dam = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Breed = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BoardingStart = table.Column<DateOnly>(type: "date", nullable: false),
                    BoardingEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    HealthStatus = table.Column<int>(type: "int", nullable: false),
                    Archived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Horses_Registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "Registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Horses_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreatmentPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<long>(type: "bigint", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ApprovedPortionKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    ActualPortionKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareTasks_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FollowUps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Clearance = table.Column<bool>(type: "bit", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowUps_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAt = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    RoutedTo = table.Column<int>(type: "int", nullable: false),
                    Resolved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Measurements_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupersedesRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VeterinarianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExaminationAt = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Symptoms = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    HealthStatus = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Occupancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndedAt = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Occupancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Occupancies_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Occupancies_Stalls_StallId",
                        column: x => x.StallId,
                        principalTable: "Stalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plans_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Plans_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Plans_Users_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreventiveCare",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ReminderSent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventiveCare", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreventiveCare_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncidentPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    StorageName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentPhotos_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Injuries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InjuryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    BodyLocation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Cause = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Injuries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Injuries_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Injuries_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Restrictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainingLock = table.Column<bool>(type: "bit", nullable: false),
                    BlockAllTraining = table.Column<bool>(type: "bit", nullable: false),
                    MaxIntensity = table.Column<int>(type: "int", nullable: true),
                    MaxDistanceMetres = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    NoSprint = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<long>(type: "bigint", nullable: false),
                    ValidUntil = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Cleared = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restrictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Restrictions_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Restrictions_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Treatments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InjuryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FollowUpDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Medication = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Treatments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Treatments_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Treatments_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledAt = table.Column<long>(type: "bigint", nullable: false),
                    TrainingType = table.Column<int>(type: "int", nullable: false),
                    DistanceMetres = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Intensity = table.Column<int>(type: "int", nullable: false),
                    Surface = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Target = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Users_RiderId",
                        column: x => x.RiderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AdjustFutureSessions = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evaluations_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DistanceMetres = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    TimeSeconds = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SpeedMetresPerSecond = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    HeartRate = table.Column<int>(type: "int", nullable: true),
                    Intensity = table.Column<int>(type: "int", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AbnormalObservation = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Results_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRevisions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingRevisions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_HorseId_Role_Active",
                table: "Assignments",
                columns: new[] { "HorseId", "Role", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_RegistrationId",
                table: "Attachments",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_CareTasks_HorseId",
                table: "CareTasks",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_UserId_Purpose",
                table: "Challenges",
                columns: new[] { "UserId", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_SessionId",
                table: "Evaluations",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_HorseId",
                table: "FollowUps",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_OwnerId",
                table: "Horses",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_RegistrationId",
                table: "Horses",
                column: "RegistrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentPhotos_IncidentId",
                table: "IncidentPhotos",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_HorseId",
                table: "Incidents",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Injuries_HorseId",
                table: "Injuries",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Injuries_MedicalRecordId",
                table: "Injuries",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_HorseId",
                table: "Measurements",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_HorseId",
                table: "MedicalRecords",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_Read",
                table: "Notifications",
                columns: new[] { "RecipientId", "Read" });

            migrationBuilder.CreateIndex(
                name: "IX_Occupancies_HorseId",
                table: "Occupancies",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Occupancies_StallId",
                table: "Occupancies",
                column: "StallId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_HorseId",
                table: "Plans",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_TemplateId",
                table: "Plans",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_TrainerId",
                table: "Plans",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveCare_HorseId",
                table: "PreventiveCare",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_OwnerId",
                table: "Registrations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Replenishments_ItemId",
                table: "Replenishments",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Restrictions_HorseId_Cleared",
                table: "Restrictions",
                columns: new[] { "HorseId", "Cleared" });

            migrationBuilder.CreateIndex(
                name: "IX_Restrictions_MedicalRecordId",
                table: "Restrictions",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_SessionId",
                table: "Results",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_HorseId",
                table: "Sessions",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_PlanId",
                table: "Sessions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_RiderId",
                table: "Sessions",
                column: "RiderId");

            migrationBuilder.CreateIndex(
                name: "IX_Stalls_StableId",
                table: "Stalls",
                column: "StableId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ItemId",
                table: "StockMovements",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRevisions_PlanId",
                table: "TrainingRevisions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRevisions_SessionId",
                table: "TrainingRevisions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Treatments_HorseId",
                table: "Treatments",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Treatments_MedicalRecordId",
                table: "Treatments",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "Audit");

            migrationBuilder.DropTable(
                name: "CareTasks");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "EmailMessages");

            migrationBuilder.DropTable(
                name: "Evaluations");

            migrationBuilder.DropTable(
                name: "FollowUps");

            migrationBuilder.DropTable(
                name: "IncidentPhotos");

            migrationBuilder.DropTable(
                name: "Injuries");

            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Occupancies");

            migrationBuilder.DropTable(
                name: "PreventiveCare");

            migrationBuilder.DropTable(
                name: "Replenishments");

            migrationBuilder.DropTable(
                name: "Restrictions");

            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "TrainingRevisions");

            migrationBuilder.DropTable(
                name: "Treatments");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "Stalls");

            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "MedicalRecords");

            migrationBuilder.DropTable(
                name: "Stables");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "Horses");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "Registrations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
