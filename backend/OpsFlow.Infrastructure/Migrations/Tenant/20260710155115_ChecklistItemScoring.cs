using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsFlow.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class ChecklistItemScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailCorrectiveActionText",
                table: "ChecklistTemplateItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailScoreThreshold",
                table: "ChecklistTemplateItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhotoRequired",
                table: "ChecklistTemplateItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ScoringType",
                table: "ChecklistTemplateItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "ChecklistTemplateItems",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 1.0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailCorrectiveActionText",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "FailScoreThreshold",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "PhotoRequired",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "ScoringType",
                table: "ChecklistTemplateItems");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "ChecklistTemplateItems");
        }
    }
}
