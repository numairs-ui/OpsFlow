using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsFlow.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddMultiRegionScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RegionId",
                table: "RefreshTokens",
                newName: "RegionIdsCsv");

            migrationBuilder.AddColumn<string>(
                name: "RegionIdsCsv",
                table: "UserProfiles",
                type: "text",
                nullable: true);

            // AssignedToUserId was added to the model in Wave 16 without its own migration, so it
            // already exists on some databases. Add it idempotently so this migration applies to both
            // drifted databases and fresh deploys.
            migrationBuilder.Sql(
                "ALTER TABLE \"RecurringAssignments\" ADD COLUMN IF NOT EXISTS \"AssignedToUserId\" text;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegionIdsCsv",
                table: "UserProfiles");

            migrationBuilder.Sql(
                "ALTER TABLE \"RecurringAssignments\" DROP COLUMN IF EXISTS \"AssignedToUserId\";");

            migrationBuilder.RenameColumn(
                name: "RegionIdsCsv",
                table: "RefreshTokens",
                newName: "RegionId");
        }
    }
}
