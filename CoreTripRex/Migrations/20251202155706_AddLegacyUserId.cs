using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreTripRex.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LegacyUserId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegacyUserId",
                table: "AspNetUsers");
        }
    }
}
