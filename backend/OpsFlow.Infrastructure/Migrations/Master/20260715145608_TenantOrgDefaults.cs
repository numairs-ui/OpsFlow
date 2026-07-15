using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsFlow.Migrations.Master
{
    /// <inheritdoc />
    public partial class TenantOrgDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DefaultDepositDeadlineLocalTime",
                table: "Tenants",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultDoughNeedTargetsJson",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultOverdueGraceMinutes",
                table: "Tenants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultTillABase",
                table: "Tenants",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultTillBBase",
                table: "Tenants",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultTimezoneId",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocaleCode",
                table: "Tenants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DefaultDepositDeadlineLocalTime",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DefaultDoughNeedTargetsJson",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DefaultOverdueGraceMinutes",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DefaultTillABase",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DefaultTillBBase",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DefaultTimezoneId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LocaleCode",
                table: "Tenants");
        }
    }
}
