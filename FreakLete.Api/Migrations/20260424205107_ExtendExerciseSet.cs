using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExtendExerciseSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ConcentricTimeSeconds",
                table: "ExerciseSets",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RIR",
                table: "ExerciseSets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestSeconds",
                table: "ExerciseSets",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
    UPDATE ""ExerciseSets"" s
    SET ""RIR"" = e.""RIR"",
        ""RestSeconds"" = e.""RestSeconds"",
        ""ConcentricTimeSeconds"" = e.""ConcentricTimeSeconds""
    FROM ""ExerciseEntries"" e
    WHERE s.""ExerciseEntryId"" = e.""Id""
      AND e.""TrackingMode"" = 'Strength';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcentricTimeSeconds",
                table: "ExerciseSets");

            migrationBuilder.DropColumn(
                name: "RIR",
                table: "ExerciseSets");

            migrationBuilder.DropColumn(
                name: "RestSeconds",
                table: "ExerciseSets");
        }
    }
}
