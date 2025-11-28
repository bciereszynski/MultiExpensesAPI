using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiExpensesAPI.Migrations
{
    /// <inheritdoc />
    public partial class Make_inivtation_multiple_use : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupInvitations_Users_UsedByUserId",
                table: "GroupInvitations");

            migrationBuilder.DropIndex(
                name: "IX_GroupInvitations_UsedByUserId",
                table: "GroupInvitations");

            migrationBuilder.DropColumn(
                name: "IsUsed",
                table: "GroupInvitations");

            migrationBuilder.DropColumn(
                name: "UsedByUserId",
                table: "GroupInvitations");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "GroupInvitations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "GroupInvitations");

            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "GroupInvitations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UsedByUserId",
                table: "GroupInvitations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupInvitations_UsedByUserId",
                table: "GroupInvitations",
                column: "UsedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupInvitations_Users_UsedByUserId",
                table: "GroupInvitations",
                column: "UsedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
