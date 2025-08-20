using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace trackerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_TrackingLog_LeakAmount",
                table: "TrackingLog",
                sql: "LeakAmount >= 0 AND LeakAmount <= 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TrackingLog_PainLevel",
                table: "TrackingLog",
                sql: "PainLevel >= 0 AND PainLevel <= 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TrackingLog_Urgency",
                table: "TrackingLog",
                sql: "Urgency >= 0 AND Urgency <= 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TrackingLog_LeakAmount",
                table: "TrackingLog");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TrackingLog_PainLevel",
                table: "TrackingLog");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TrackingLog_Urgency",
                table: "TrackingLog");
        }
    }
}
