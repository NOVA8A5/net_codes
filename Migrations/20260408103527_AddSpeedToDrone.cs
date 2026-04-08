using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project1.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeedToDrone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Speed",
                table: "Drones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Speed",
                table: "DroneDatas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Speed",
                table: "Drones");

            migrationBuilder.DropColumn(
                name: "Speed",
                table: "DroneDatas");
        }
    }
}
