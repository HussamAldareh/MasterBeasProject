using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBeasProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class addmoreforprice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FinalPrice",
                table: "Bookings",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPriceApproved",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalPrice",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsPriceApproved",
                table: "Bookings");
        }
    }
}
