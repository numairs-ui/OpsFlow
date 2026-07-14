using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsFlow.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class SafeReleaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ChecklistId",
                table: "TaskInstances",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AdHocTaskTemplateId",
                table: "TaskInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceTaskInstanceId",
                table: "TaskInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CompositeScorePercent",
                table: "TaskCompletions",
                type: "numeric(5,1)",
                precision: 5,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemScores",
                table: "TaskCompletions",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DepositDeadlineLocalTime",
                table: "StoreSettings",
                type: "time without time zone",
                nullable: true);

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

            migrationBuilder.CreateTable(
                name: "MissedDepositFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FlaggedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissedDepositFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissedDepositFlags_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_AdHocTaskTemplateId",
                table: "TaskInstances",
                column: "AdHocTaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_SourceTaskInstanceId",
                table: "TaskInstances",
                column: "SourceTaskInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_MissedDepositFlags_StoreId_BusinessDate",
                table: "MissedDepositFlags",
                columns: new[] { "StoreId", "BusinessDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_TaskInstances_SourceTaskInstanceId",
                table: "TaskInstances",
                column: "SourceTaskInstanceId",
                principalTable: "TaskInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskInstances_TaskTemplates_AdHocTaskTemplateId",
                table: "TaskInstances",
                column: "AdHocTaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_TaskInstances_SourceTaskInstanceId",
                table: "TaskInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskInstances_TaskTemplates_AdHocTaskTemplateId",
                table: "TaskInstances");

            migrationBuilder.DropTable(
                name: "MissedDepositFlags");

            migrationBuilder.DropIndex(
                name: "IX_TaskInstances_AdHocTaskTemplateId",
                table: "TaskInstances");

            migrationBuilder.DropIndex(
                name: "IX_TaskInstances_SourceTaskInstanceId",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "AdHocTaskTemplateId",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "SourceTaskInstanceId",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "CompositeScorePercent",
                table: "TaskCompletions");

            migrationBuilder.DropColumn(
                name: "ItemScores",
                table: "TaskCompletions");

            migrationBuilder.DropColumn(
                name: "DepositDeadlineLocalTime",
                table: "StoreSettings");

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

            migrationBuilder.AlterColumn<Guid>(
                name: "ChecklistId",
                table: "TaskInstances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
