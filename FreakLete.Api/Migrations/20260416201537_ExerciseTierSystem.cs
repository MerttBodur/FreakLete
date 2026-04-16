using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExerciseTierSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TierParentId",
                table: "ExerciseDefinitions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TierScale",
                table: "ExerciseDefinitions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TierThresholdsFemale",
                table: "ExerciseDefinitions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TierThresholdsMale",
                table: "ExerciseDefinitions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TierType",
                table: "ExerciseDefinitions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserExerciseTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CatalogId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExerciseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TierLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RawValue = table.Column<double>(type: "double precision", nullable: false),
                    BasisValue = table.Column<double>(type: "double precision", nullable: true),
                    Ratio = table.Column<double>(type: "double precision", nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExerciseTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExerciseTiers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserExerciseTiers_UserId_CatalogId",
                table: "UserExerciseTiers",
                columns: new[] { "UserId", "CatalogId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserExerciseTiers");

            migrationBuilder.DropColumn(
                name: "TierParentId",
                table: "ExerciseDefinitions");

            migrationBuilder.DropColumn(
                name: "TierScale",
                table: "ExerciseDefinitions");

            migrationBuilder.DropColumn(
                name: "TierThresholdsFemale",
                table: "ExerciseDefinitions");

            migrationBuilder.DropColumn(
                name: "TierThresholdsMale",
                table: "ExerciseDefinitions");

            migrationBuilder.DropColumn(
                name: "TierType",
                table: "ExerciseDefinitions");
        }
    }
}
