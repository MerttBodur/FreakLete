using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedTierEligibleExerciseDefinitions : Migration
    {
        private sealed record SeedRow(
            string CatalogId,
            string Name,
            string Category,
            string Mechanic,
            string TrackingMode,
            string TierType,
            string Male,
            string Female);

        private static readonly SeedRow[] Rows =
        [
            new("benchpress",           "Bench Press",            "Push",                "compound", "Strength",         "StrengthRatio",     "[0.5,1.0,1.25,1.5,1.75]",   "[0.35,0.7,0.9,1.1,1.35]"),
            new("backsquat",            "Back Squat",             "Squat Variation",     "compound", "Strength",         "StrengthRatio",     "[0.75,1.25,1.5,2.0,2.5]",   "[0.5,0.9,1.1,1.5,1.9]"),
            new("conventionaldeadlift", "Conventional Deadlift",  "Deadlift Variation",  "compound", "Strength",         "StrengthRatio",     "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.0,1.4,1.8,2.2]"),
            new("sumodeadlift",         "Sumo Deadlift",          "Deadlift Variation",  "compound", "Strength",         "StrengthRatio",     "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.0,1.4,1.8,2.2]"),
            new("overheadpress",        "Overhead Press",         "Push",                "compound", "Strength",         "StrengthRatio",     "[0.35,0.55,0.75,0.95,1.15]","[0.2,0.4,0.55,0.7,0.85]"),
            new("powerclean",           "Power Clean",            "Olympic Lifts",       "compound", "Strength",         "StrengthRatio",     "[0.6,0.9,1.2,1.5,1.8]",     "[0.4,0.65,0.85,1.05,1.3]"),
            new("powersnatch",          "Power Snatch",           "Olympic Lifts",       "compound", "Strength",         "StrengthRatio",     "[0.4,0.7,0.9,1.15,1.4]",    "[0.3,0.5,0.65,0.8,1.0]"),
            new("frontsquat",           "Front Squat",            "Squat Variation",     "compound", "Strength",         "StrengthRatio",     "[0.6,1.0,1.25,1.6,2.0]",    "[0.4,0.7,0.9,1.2,1.55]"),
            new("romaniandeadlift",     "Romanian Deadlift",      "Deadlift Variation",  "compound", "Strength",         "StrengthRatio",     "[0.8,1.2,1.6,2.0,2.4]",     "[0.55,0.8,1.1,1.4,1.75]"),
            new("barbellrow",           "Barbell Row",            "Pull",                "compound", "Strength",         "StrengthRatio",     "[0.5,0.9,1.15,1.4,1.7]",    "[0.35,0.6,0.8,1.0,1.2]"),
            new("pullup",               "Pull-Up",                "Pull",                "compound", "Strength",         "StrengthRatio",     "[1.0,1.2,1.4,1.6,1.9]",     "[1.0,1.1,1.25,1.45,1.7]"),
            new("trapbardeadlift",      "Trap Bar Deadlift",      "Deadlift Variation",  "compound", "Strength",         "StrengthRatio",     "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.0,1.4,1.8,2.2]"),
            new("hipthrust",            "Barbell Hip Thrust",     "Deadlift Variation",  "compound", "Strength",         "StrengthRatio",     "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.1,1.5,1.9,2.3]"),
            new("pushpress",            "Push Press",             "Push",                "compound", "Strength",         "StrengthRatio",     "[0.5,0.75,0.95,1.2,1.5]",   "[0.3,0.5,0.7,0.9,1.1]"),

            new("verticaljump",         "Vertical Jump",          "Jumps",               "",         "AthleticHeight",   "AthleticAbsolute",  "[30,45,55,65,75]",          "[20,32,42,52,60]"),
            new("standingbroadjump",    "Standing Broad Jump",    "Jumps",               "",         "AthleticDistance", "AthleticAbsolute",  "[180,220,250,280,310]",     "[150,190,220,245,275]"),
            new("rsi",                  "RSI",                    "Plyometrics",         "",         "AthleticIndex",    "AthleticAbsolute",  "[1.5,2.0,2.5,3.0,3.5]",     "[1.2,1.6,2.0,2.5,3.0]"),

            new("fortyyarddash",        "40 Yard Dash",           "Sprints",             "",         "AthleticTime",     "AthleticInverse",   "[5.8,5.3,4.9,4.6,4.4]",     "[6.6,6.0,5.5,5.1,4.8]"),
            new("tenmetersprint",       "10 Meter Sprint",        "Sprints",             "",         "AthleticTime",     "AthleticInverse",   "[2.2,2.0,1.85,1.75,1.65]",  "[2.5,2.25,2.05,1.9,1.8]"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var r in Rows)
            {
                migrationBuilder.Sql($"""
                    INSERT INTO "ExerciseDefinitions" (
                        "CatalogId", "Name", "Category", "DisplayName", "TurkishName", "EnglishName",
                        "SourceSection", "Force", "Level", "Mechanic", "Equipment",
                        "PrimaryMusclesText", "SecondaryMusclesText", "InstructionsText",
                        "TrackingMode", "PrimaryLabel", "PrimaryUnit", "SecondaryLabel", "SecondaryUnit",
                        "SupportsGroundContactTime", "SupportsConcentricTime",
                        "MovementPattern", "AthleticQuality", "SportRelevance", "NervousSystemLoad",
                        "GctProfile", "LoadPrescription", "CommonMistakes", "Progression", "Regression",
                        "RecommendedRank",
                        "TierType", "TierThresholdsMale", "TierThresholdsFemale"
                    )
                    VALUES (
                        '{r.CatalogId}', '{r.Name}', '{r.Category}', '{r.Name}', '', '{r.Name}',
                        '', '', '', '{r.Mechanic}', '',
                        '', '', '',
                        '{r.TrackingMode}', '', '', '', '',
                        false, false,
                        '', '', '', '',
                        '', '', '', '', '',
                        0,
                        '{r.TierType}', '{r.Male}', '{r.Female}'
                    )
                    ON CONFLICT ("CatalogId") DO UPDATE SET
                        "Mechanic" = EXCLUDED."Mechanic",
                        "TierType" = EXCLUDED."TierType",
                        "TierThresholdsMale" = EXCLUDED."TierThresholdsMale",
                        "TierThresholdsFemale" = EXCLUDED."TierThresholdsFemale";
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var r in Rows)
            {
                migrationBuilder.Sql($"""
                    DELETE FROM "ExerciseDefinitions" WHERE "CatalogId" = '{r.CatalogId}';
                    """);
            }
        }
    }
}
