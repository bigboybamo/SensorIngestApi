using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SensorIngestApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    device_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.device_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "devices");
        }
    }
}
