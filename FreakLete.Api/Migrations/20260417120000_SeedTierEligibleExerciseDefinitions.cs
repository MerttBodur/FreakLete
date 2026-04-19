using FreakLete.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedTierEligibleExerciseDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            TierEligibleDefinitionsSeed.ApplyViaMigration(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var r in TierEligibleDefinitionsSeed.Rows)
                migrationBuilder.Sql($"""DELETE FROM "ExerciseDefinitions" WHERE "CatalogId" = '{r.CatalogId}';""");
        }
    }
}
