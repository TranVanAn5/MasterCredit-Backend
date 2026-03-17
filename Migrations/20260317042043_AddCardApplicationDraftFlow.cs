using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterCredit.Migrations
{
    /// <inheritdoc />
    public partial class AddCardApplicationDraftFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Income",
                table: "CardApplications");

            migrationBuilder.AddColumn<decimal>(
                name: "GrossAnnualIncome",
                table: "CardApplications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IdCardPath",
                table: "CardApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SalarySlipPath",
                table: "CardApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrossAnnualIncome",
                table: "CardApplications");

            migrationBuilder.DropColumn(
                name: "IdCardPath",
                table: "CardApplications");

            migrationBuilder.DropColumn(
                name: "SalarySlipPath",
                table: "CardApplications");

            migrationBuilder.AddColumn<string>(
                name: "Income",
                table: "CardApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
