using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsStarterTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStarterTemplate",
                table: "TrainingPrograms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPrograms_IsStarterTemplate",
                table: "TrainingPrograms",
                column: "IsStarterTemplate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingPrograms_IsStarterTemplate",
                table: "TrainingPrograms");

            migrationBuilder.DropColumn(
                name: "IsStarterTemplate",
                table: "TrainingPrograms");
        }
    }
}
