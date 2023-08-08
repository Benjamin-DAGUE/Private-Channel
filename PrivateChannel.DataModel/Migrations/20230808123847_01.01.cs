using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateChannel.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class _0101 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CipherText = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    AuthTag = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    IV = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Salt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ExpirationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notes");
        }
    }
}
