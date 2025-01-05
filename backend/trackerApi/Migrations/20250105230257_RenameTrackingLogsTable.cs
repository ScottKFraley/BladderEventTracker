using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace trackerApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameTrackingLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TrackingLogs",
                table: "TrackingLogs");

            migrationBuilder.RenameTable(
                name: "TrackingLogs",
                newName: "TrackingLog");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrackingLog",
                table: "TrackingLog",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrackingLog",
                table: "TrackingLog");

            migrationBuilder.RenameTable(
                name: "TrackingLog",
                newName: "TrackingLogs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrackingLogs",
                table: "TrackingLogs",
                column: "Id");
        }
    }
}
