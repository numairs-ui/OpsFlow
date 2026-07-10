using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsFlow.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class MultiStoreRecurringAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Create the join table first, so we can backfill from the old scalar column before dropping it.
            migrationBuilder.CreateTable(
                name: "RecurringAssignmentStores",
                columns: table => new
                {
                    RecurringAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringAssignmentStores", x => new { x.RecurringAssignmentId, x.StoreId });
                    table.ForeignKey(
                        name: "FK_RecurringAssignmentStores_RecurringAssignments_RecurringAss~",
                        column: x => x.RecurringAssignmentId,
                        principalTable: "RecurringAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringAssignmentStores_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringAssignmentStores_StoreId",
                table: "RecurringAssignmentStores",
                column: "StoreId");

            // 2) Backfill one join row per existing assignment's current StoreId (bajco-dev has live data).
            migrationBuilder.Sql(
                @"INSERT INTO ""RecurringAssignmentStores"" (""RecurringAssignmentId"", ""StoreId"")
                  SELECT ""Id"", ""StoreId"" FROM ""RecurringAssignments"";");

            // 3) Only now drop the old scalar column + its FK/index — backfill has preserved the data.
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringAssignments_Stores_StoreId",
                table: "RecurringAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RecurringAssignments_StoreId",
                table: "RecurringAssignments");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "RecurringAssignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add the scalar column, collapse each assignment's targets back to one store (picking
            // any target), then drop the join table. Multi-store assignments lose their extra targets.
            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "RecurringAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                @"UPDATE ""RecurringAssignments"" ra
                  SET ""StoreId"" = (
                      SELECT ras.""StoreId"" FROM ""RecurringAssignmentStores"" ras
                      WHERE ras.""RecurringAssignmentId"" = ra.""Id"" LIMIT 1)
                  WHERE EXISTS (
                      SELECT 1 FROM ""RecurringAssignmentStores"" ras
                      WHERE ras.""RecurringAssignmentId"" = ra.""Id"");");

            migrationBuilder.DropTable(
                name: "RecurringAssignmentStores");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringAssignments_StoreId",
                table: "RecurringAssignments",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringAssignments_Stores_StoreId",
                table: "RecurringAssignments",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
