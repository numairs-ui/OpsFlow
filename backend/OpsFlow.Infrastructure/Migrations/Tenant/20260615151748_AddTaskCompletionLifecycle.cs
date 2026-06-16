using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsFlow.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddTaskCompletionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskInstances_StoreId",
                table: "TaskInstances");

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "TaskInstances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "TaskInstances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUserId",
                table: "TaskInstances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeferReason",
                table: "TaskInstances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeferredByUserId",
                table: "TaskInstances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeferredTo",
                table: "TaskInstances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VerifiedAt",
                table: "TaskInstances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedByUserId",
                table: "TaskInstances",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    TaskInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedByUserId = table.Column<string>(type: "text", nullable: true),
                    CompletedByVolunteerName = table.Column<string>(type: "text", nullable: true),
                    FieldValues = table.Column<string>(type: "jsonb", nullable: false),
                    CorrectiveActions = table.Column<string>(type: "jsonb", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCompletions_TaskInstances_TaskInstanceId",
                        column: x => x.TaskInstanceId,
                        principalTable: "TaskInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_StoreId_Status_DueAt",
                table: "TaskInstances",
                columns: new[] { "StoreId", "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskCompletions_TaskInstanceId",
                table: "TaskCompletions",
                column: "TaskInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskCompletions");

            migrationBuilder.DropIndex(
                name: "IX_TaskInstances_StoreId_Status_DueAt",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "DeferReason",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "DeferredByUserId",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "DeferredTo",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "TaskInstances");

            migrationBuilder.DropColumn(
                name: "VerifiedByUserId",
                table: "TaskInstances");

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_StoreId",
                table: "TaskInstances",
                column: "StoreId");
        }
    }
}
