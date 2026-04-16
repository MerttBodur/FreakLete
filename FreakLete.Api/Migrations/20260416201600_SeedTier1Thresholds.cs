using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedTier1Thresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            UpdateTier(migrationBuilder, "benchpress",           "StrengthRatio", "[0.5,1.0,1.25,1.5,1.75]",   "[0.35,0.7,0.9,1.1,1.35]");
            UpdateTier(migrationBuilder, "backsquat",            "StrengthRatio", "[0.75,1.25,1.5,2.0,2.5]",   "[0.5,0.9,1.1,1.5,1.9]");
            UpdateTier(migrationBuilder, "conventionaldeadlift", "StrengthRatio", "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.0,1.4,1.8,2.2]");
            UpdateTier(migrationBuilder, "sumodeadlift",         "StrengthRatio", "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.0,1.4,1.8,2.2]");
            UpdateTier(migrationBuilder, "overheadpress",        "StrengthRatio", "[0.35,0.55,0.75,0.95,1.15]","[0.2,0.4,0.55,0.7,0.85]");
            UpdateTier(migrationBuilder, "powerclean",           "StrengthRatio", "[0.6,0.9,1.2,1.5,1.8]",     "[0.4,0.65,0.85,1.05,1.3]");
            UpdateTier(migrationBuilder, "powersnatch",          "StrengthRatio", "[0.4,0.7,0.9,1.15,1.4]",    "[0.3,0.5,0.65,0.8,1.0]");
            UpdateTier(migrationBuilder, "frontsquat",           "StrengthRatio", "[0.6,1.0,1.25,1.6,2.0]",    "[0.4,0.7,0.9,1.2,1.55]");
            UpdateTier(migrationBuilder, "romaniandeadlift",     "StrengthRatio", "[0.8,1.2,1.6,2.0,2.4]",     "[0.55,0.8,1.1,1.4,1.75]");
            UpdateTier(migrationBuilder, "barbellrow",           "StrengthRatio", "[0.5,0.9,1.15,1.4,1.7]",    "[0.35,0.6,0.8,1.0,1.2]");
            UpdateTier(migrationBuilder, "pullup",               "StrengthRatio", "[1.0,1.2,1.4,1.6,1.9]",     "[1.0,1.1,1.25,1.45,1.7]");
            UpdateTier(migrationBuilder, "trapbardeadlift",      "StrengthRatio", "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.0,1.4,1.8,2.2]");
            UpdateTier(migrationBuilder, "hipthrust",            "StrengthRatio", "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.1,1.5,1.9,2.3]");
            UpdateTier(migrationBuilder, "pushpress",            "StrengthRatio", "[0.5,0.75,0.95,1.2,1.5]",   "[0.3,0.5,0.7,0.9,1.1]");

            UpdateTier(migrationBuilder, "verticaljump",         "AthleticAbsolute", "[30,45,55,65,75]",         "[20,32,42,52,60]");
            UpdateTier(migrationBuilder, "standingbroadjump",    "AthleticAbsolute", "[180,220,250,280,310]",     "[150,190,220,245,275]");
            UpdateTier(migrationBuilder, "rsi",                  "AthleticAbsolute", "[1.5,2.0,2.5,3.0,3.5]",    "[1.2,1.6,2.0,2.5,3.0]");

            UpdateTier(migrationBuilder, "fortyyarddash",        "AthleticInverse", "[5.8,5.3,4.9,4.6,4.4]",    "[6.6,6.0,5.5,5.1,4.8]");
            UpdateTier(migrationBuilder, "tenmetersprint",       "AthleticInverse", "[2.2,2.0,1.85,1.75,1.65]", "[2.5,2.25,2.05,1.9,1.8]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ExerciseDefinitions"
                SET "TierType" = '',
                    "TierThresholdsMale" = '',
                    "TierThresholdsFemale" = ''
                WHERE "TierType" IN ('StrengthRatio','AthleticAbsolute','AthleticInverse');
                """);
        }

        private static void UpdateTier(
            MigrationBuilder b, string catalogId, string tierType, string male, string female)
        {
            b.Sql($"""
                UPDATE "ExerciseDefinitions"
                SET "TierType" = '{tierType}',
                    "TierThresholdsMale" = '{male}',
                    "TierThresholdsFemale" = '{female}'
                WHERE "CatalogId" = '{catalogId}';
                """);
        }
    }
}
