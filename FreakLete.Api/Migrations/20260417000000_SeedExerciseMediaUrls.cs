using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedExerciseMediaUrls : Migration
    {
        private const string BaseUrl = "https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            string[] catalogIds =
            [
                "benchpress",
                "backsquat",
                "conventionaldeadlift",
                "sumodeadlift",
                "overheadpress",
                "powerclean",
                "powersnatch",
                "frontsquat",
                "romaniandeadlift",
                "barbellrow",
                "pullup",
                "trapbardeadlift",
                "hipthrust",
                "pushpress",
                "verticaljump",
                "standingbroadjump",
                "rsi",
                "fortyyarddash",
                "tenmetersprint",
            ];

            foreach (var id in catalogIds)
                migrationBuilder.Sql($"""
                    UPDATE "ExerciseDefinitions"
                    SET "MediaUrl" = '{BaseUrl}/{id}.mp4'
                    WHERE "CatalogId" = '{id}';
                    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ExerciseDefinitions"
                SET "MediaUrl" = NULL
                WHERE "MediaUrl" LIKE 'https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/%';
                """);
        }
    }
}
