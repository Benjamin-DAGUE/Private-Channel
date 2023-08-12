using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateChannel.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class _0102 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RemainingUnlockAttempts",
                table: "Notes",
                type: "int",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingUnlockAttempts",
                table: "Notes");
        }
    }
}
