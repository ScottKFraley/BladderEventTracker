﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace trackerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTrackingLogRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "TrackingLog",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TrackingLog_UserId",
                table: "TrackingLog",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrackingLog_Users_UserId",
                table: "TrackingLog",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackingLog_Users_UserId",
                table: "TrackingLog");

            migrationBuilder.DropIndex(
                name: "IX_TrackingLog_UserId",
                table: "TrackingLog");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TrackingLog");
        }
    }
}
