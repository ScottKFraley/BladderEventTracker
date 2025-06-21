using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace trackerApi.Migrations
{
    /// <inheritdoc />
    public partial class ConvertEventDateToDateTimeOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrackingLog_EventDate",
                table: "TrackingLog");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TrackingLog_EventDate",
                table: "TrackingLog",
                column: "EventDate",
                unique: true);
        }
    }
}
