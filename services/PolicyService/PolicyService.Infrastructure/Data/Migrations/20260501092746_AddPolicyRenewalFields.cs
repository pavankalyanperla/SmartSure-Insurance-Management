using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolicyService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyRenewalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRenewed",
                table: "Policies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RenewalCount",
                table: "Policies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RenewedFromPolicyId",
                table: "Policies",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRenewed",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "RenewalCount",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "RenewedFromPolicyId",
                table: "Policies");
        }
    }
}
