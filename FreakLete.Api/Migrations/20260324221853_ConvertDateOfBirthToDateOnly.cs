using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertDateOfBirthToDateOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Explicit USING cast preserves existing values by stripping the time component.
            // PostgreSQL: timestamptz → date is a safe narrowing cast.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Users"
                ALTER COLUMN "DateOfBirth" TYPE date
                USING "DateOfBirth"::date;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Users"
                ALTER COLUMN "DateOfBirth" TYPE timestamp with time zone
                USING "DateOfBirth"::timestamp with time zone;
                """);
        }
    }
}
