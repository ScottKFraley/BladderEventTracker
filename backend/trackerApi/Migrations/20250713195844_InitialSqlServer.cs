using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace trackerApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackingLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Accident = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ChangePadOrUnderware = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LeakAmount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Urgency = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AwokeFromSleep = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PainLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingLog_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingLog_UserId",
                table: "TrackingLog",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackingLog");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
