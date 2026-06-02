using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBeasProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class addCityForEn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "EngineerProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "EngineerProfiles");
        }
    }
}
