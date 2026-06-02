using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBeasProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEngineerAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EngineerAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EngineerProfileId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EngineerAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EngineerAvailabilities_EngineerProfiles_EngineerProfileId",
                        column: x => x.EngineerProfileId,
                        principalTable: "EngineerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EngineerAvailabilities_EngineerProfileId",
                table: "EngineerAvailabilities",
                column: "EngineerProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EngineerAvailabilities");
        }
    }
}
