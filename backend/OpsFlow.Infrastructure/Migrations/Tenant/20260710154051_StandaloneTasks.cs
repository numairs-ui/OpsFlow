using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsFlow.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class StandaloneTasks : Migration
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

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_AdHocTaskTemplateId",
                table: "TaskInstances",
                column: "AdHocTaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskInstances_SourceTaskInstanceId",
                table: "TaskInstances",
                column: "SourceTaskInstanceId");

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
