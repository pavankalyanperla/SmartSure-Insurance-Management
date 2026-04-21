using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolicyService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyTypeAdminFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoRenewal",
                table: "PolicyTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ClaimLimit",
                table: "PolicyTypes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CoverageDetails",
                table: "PolicyTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DurationMonths",
                table: "PolicyTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnrolledCount",
                table: "PolicyTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Exclusions",
                table: "PolicyTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GracePeriodDays",
                table: "PolicyTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxAge",
                table: "PolicyTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinAge",
                table: "PolicyTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RiskCategory",
                table: "PolicyTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WaitingPeriod",
                table: "PolicyTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "PolicyTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AutoRenewal", "ClaimLimit", "CoverageDetails", "DurationMonths", "EnrolledCount", "Exclusions", "GracePeriodDays", "MaxAge", "MinAge", "RiskCategory", "WaitingPeriod" },
                values: new object[] { false, 0m, "", 12, 0, "", 30, 65, 18, "Standard", "" });

            migrationBuilder.UpdateData(
                table: "PolicyTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AutoRenewal", "ClaimLimit", "CoverageDetails", "DurationMonths", "EnrolledCount", "Exclusions", "GracePeriodDays", "MaxAge", "MinAge", "RiskCategory", "WaitingPeriod" },
                values: new object[] { false, 0m, "", 12, 0, "", 30, 65, 18, "Standard", "" });

            migrationBuilder.UpdateData(
                table: "PolicyTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AutoRenewal", "ClaimLimit", "CoverageDetails", "DurationMonths", "EnrolledCount", "Exclusions", "GracePeriodDays", "MaxAge", "MinAge", "RiskCategory", "WaitingPeriod" },
                values: new object[] { false, 0m, "", 12, 0, "", 30, 65, 18, "Standard", "" });

            migrationBuilder.UpdateData(
                table: "PolicyTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AutoRenewal", "ClaimLimit", "CoverageDetails", "DurationMonths", "EnrolledCount", "Exclusions", "GracePeriodDays", "MaxAge", "MinAge", "RiskCategory", "WaitingPeriod" },
                values: new object[] { false, 0m, "", 12, 0, "", 30, 65, 18, "Standard", "" });

            migrationBuilder.UpdateData(
                table: "PolicyTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AutoRenewal", "ClaimLimit", "CoverageDetails", "DurationMonths", "EnrolledCount", "Exclusions", "GracePeriodDays", "MaxAge", "MinAge", "RiskCategory", "WaitingPeriod" },
                values: new object[] { false, 0m, "", 12, 0, "", 30, 65, 18, "Standard", "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoRenewal",
                table: "PolicyTypes");

            migrationBuilder.DropColumn(
                name: "ClaimLimit",
                table: "PolicyTypes");

            migrationBuilder.DropColumn(
                name: "CoverageDetails",
                table: "PolicyTypes");

            migrationBuilder.DropColumn(
                name: "DurationMonths",
                table: "PolicyTypes");

            migrationBuilder.DropColumn(
                name: "EnrolledCount",
                table: "PolicyTypes");

            migrationBuilder.DropColumn(
                name: "Exclusions",
                table: "PolicyTypes");

            migrationBuilder.DropColumn(
                name: "GracePeriodDays",
                table: "PolicyTypes");

            migrationBuilder.DropColumn(
                name: "MaxAge",
                table: "PolicyTypes");

            migrationBuilder.DropColumn(
                name: "MinAge",
                table: "PolicyTypes");

            migrationBuilder.DropColumn(
                name: "RiskCategory",
                table: "PolicyTypes");

            migrationBuilder.DropColumn(
                name: "WaitingPeriod",
                table: "PolicyTypes");
        }
    }
}
