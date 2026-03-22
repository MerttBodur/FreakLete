using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseDefinitions",
                columns: table => new
                {
                    CatalogId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    TurkishName = table.Column<string>(type: "text", nullable: false),
                    EnglishName = table.Column<string>(type: "text", nullable: false),
                    SourceSection = table.Column<string>(type: "text", nullable: false),
                    Force = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false),
                    Mechanic = table.Column<string>(type: "text", nullable: false),
                    Equipment = table.Column<string>(type: "text", nullable: false),
                    PrimaryMusclesText = table.Column<string>(type: "text", nullable: false),
                    SecondaryMusclesText = table.Column<string>(type: "text", nullable: false),
                    InstructionsText = table.Column<string>(type: "text", nullable: false),
                    TrackingMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PrimaryLabel = table.Column<string>(type: "text", nullable: false),
                    PrimaryUnit = table.Column<string>(type: "text", nullable: false),
                    SecondaryLabel = table.Column<string>(type: "text", nullable: false),
                    SecondaryUnit = table.Column<string>(type: "text", nullable: false),
                    SupportsGroundContactTime = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsConcentricTime = table.Column<bool>(type: "boolean", nullable: false),
                    MovementPattern = table.Column<string>(type: "text", nullable: false),
                    AthleticQuality = table.Column<string>(type: "text", nullable: false),
                    SportRelevance = table.Column<string>(type: "text", nullable: false),
                    NervousSystemLoad = table.Column<string>(type: "text", nullable: false),
                    GctProfile = table.Column<string>(type: "text", nullable: false),
                    LoadPrescription = table.Column<string>(type: "text", nullable: false),
                    CommonMistakes = table.Column<string>(type: "text", nullable: false),
                    Progression = table.Column<string>(type: "text", nullable: false),
                    Regression = table.Column<string>(type: "text", nullable: false),
                    RecommendedRank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseDefinitions", x => x.CatalogId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WeightKg = table.Column<double>(type: "double precision", nullable: true),
                    BodyFatPercentage = table.Column<double>(type: "double precision", nullable: true),
                    SportName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GymExperienceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AthleticPerformanceEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MovementName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MovementCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SecondaryValue = table.Column<double>(type: "double precision", nullable: true),
                    SecondaryUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GroundContactTimeMs = table.Column<double>(type: "double precision", nullable: true),
                    ConcentricTimeSeconds = table.Column<double>(type: "double precision", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleticPerformanceEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AthleticPerformanceEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovementGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MovementName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MovementCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GoalMetricLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetValue = table.Column<double>(type: "double precision", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovementGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovementGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ExerciseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExerciseCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TrackingMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    RIR = table.Column<int>(type: "integer", nullable: true),
                    Metric1Value = table.Column<double>(type: "double precision", nullable: true),
                    Metric1Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Metric2Value = table.Column<double>(type: "double precision", nullable: true),
                    Metric2Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GroundContactTimeMs = table.Column<double>(type: "double precision", nullable: true),
                    ConcentricTimeSeconds = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    WorkoutName = table.Column<string>(type: "text", nullable: false),
                    WorkoutDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workouts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkoutId = table.Column<int>(type: "integer", nullable: false),
                    ExerciseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExerciseCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TrackingMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Sets = table.Column<int>(type: "integer", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    RIR = table.Column<int>(type: "integer", nullable: true),
                    RestSeconds = table.Column<int>(type: "integer", nullable: true),
                    GroundContactTimeMs = table.Column<double>(type: "double precision", nullable: true),
                    ConcentricTimeSeconds = table.Column<double>(type: "double precision", nullable: true),
                    Metric1Value = table.Column<double>(type: "double precision", nullable: true),
                    Metric1Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Metric2Value = table.Column<double>(type: "double precision", nullable: true),
                    Metric2Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseEntries_Workouts_WorkoutId",
                        column: x => x.WorkoutId,
                        principalTable: "Workouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AthleticPerformanceEntries_UserId",
                table: "AthleticPerformanceEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseDefinitions_Category",
                table: "ExerciseDefinitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseDefinitions_Name",
                table: "ExerciseDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseEntries_WorkoutId",
                table: "ExerciseEntries",
                column: "WorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_MovementGoals_UserId",
                table: "MovementGoals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrEntries_UserId",
                table: "PrEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_UserId",
                table: "Workouts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AthleticPerformanceEntries");

            migrationBuilder.DropTable(
                name: "ExerciseDefinitions");

            migrationBuilder.DropTable(
                name: "ExerciseEntries");

            migrationBuilder.DropTable(
                name: "MovementGoals");

            migrationBuilder.DropTable(
                name: "PrEntries");

            migrationBuilder.DropTable(
                name: "Workouts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
