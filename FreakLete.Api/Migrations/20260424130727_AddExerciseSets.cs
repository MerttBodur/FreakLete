using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExerciseEntryId = table.Column<int>(type: "integer", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseSets_ExerciseEntries_ExerciseEntryId",
                        column: x => x.ExerciseEntryId,
                        principalTable: "ExerciseEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "ExerciseSets" ("ExerciseEntryId", "SetNumber", "Reps", "Weight")
                SELECT e."Id", gs, e."Reps", e."Metric1Value"
                FROM "ExerciseEntries" e
                CROSS JOIN LATERAL generate_series(1, GREATEST(e."Sets", 1)) AS gs
                WHERE e."TrackingMode" = 'Strength';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSets_ExerciseEntryId",
                table: "ExerciseSets",
                column: "ExerciseEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseSets");
        }
    }
}
