using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyballTeam.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteAndDeactivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teams_ClubId_Season_Name",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_TeamMemberships_ClubId_TeamId_UserId",
                table: "TeamMemberships");

            migrationBuilder.DropIndex(
                name: "IX_ClubMemberships_ClubId_UserId",
                table: "ClubMemberships");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Teams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Teams",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                table: "Teams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Teams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "TeamMemberships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TeamMemberships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                table: "TeamMemberships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "TeamMemberships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Clubs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Clubs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                table: "Clubs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "Clubs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "ClubMemberships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "ClubMemberships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ClubMemberships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                table: "ClubMemberships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "ClubMemberships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ClubId_Season_Name",
                table: "Teams",
                columns: new[] { "ClubId", "Season", "Name" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatedById",
                table: "Teams",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DeletedById",
                table: "Teams",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_UpdatedById",
                table: "Teams",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_ClubId_TeamId_UserId",
                table: "TeamMemberships",
                columns: new[] { "ClubId", "TeamId", "UserId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_CreatedById",
                table: "TeamMemberships",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_DeletedById",
                table: "TeamMemberships",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_UpdatedById",
                table: "TeamMemberships",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_CreatedById",
                table: "Clubs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_DeletedById",
                table: "Clubs",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_UpdatedById",
                table: "Clubs",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_ClubId_UserId",
                table: "ClubMemberships",
                columns: new[] { "ClubId", "UserId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_CreatedById",
                table: "ClubMemberships",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_DeletedById",
                table: "ClubMemberships",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_UpdatedById",
                table: "ClubMemberships",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubMemberships_AspNetUsers_CreatedById",
                table: "ClubMemberships",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubMemberships_AspNetUsers_DeletedById",
                table: "ClubMemberships",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubMemberships_AspNetUsers_UpdatedById",
                table: "ClubMemberships",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_AspNetUsers_CreatedById",
                table: "Clubs",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_AspNetUsers_DeletedById",
                table: "Clubs",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_AspNetUsers_UpdatedById",
                table: "Clubs",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMemberships_AspNetUsers_CreatedById",
                table: "TeamMemberships",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMemberships_AspNetUsers_DeletedById",
                table: "TeamMemberships",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMemberships_AspNetUsers_UpdatedById",
                table: "TeamMemberships",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_AspNetUsers_CreatedById",
                table: "Teams",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_AspNetUsers_DeletedById",
                table: "Teams",
                column: "DeletedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_AspNetUsers_UpdatedById",
                table: "Teams",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubMemberships_AspNetUsers_CreatedById",
                table: "ClubMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_ClubMemberships_AspNetUsers_DeletedById",
                table: "ClubMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_ClubMemberships_AspNetUsers_UpdatedById",
                table: "ClubMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_AspNetUsers_CreatedById",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_AspNetUsers_DeletedById",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_AspNetUsers_UpdatedById",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMemberships_AspNetUsers_CreatedById",
                table: "TeamMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMemberships_AspNetUsers_DeletedById",
                table: "TeamMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMemberships_AspNetUsers_UpdatedById",
                table: "TeamMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_AspNetUsers_CreatedById",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_AspNetUsers_DeletedById",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_AspNetUsers_UpdatedById",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_ClubId_Season_Name",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CreatedById",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_DeletedById",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_UpdatedById",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_TeamMemberships_ClubId_TeamId_UserId",
                table: "TeamMemberships");

            migrationBuilder.DropIndex(
                name: "IX_TeamMemberships_CreatedById",
                table: "TeamMemberships");

            migrationBuilder.DropIndex(
                name: "IX_TeamMemberships_DeletedById",
                table: "TeamMemberships");

            migrationBuilder.DropIndex(
                name: "IX_TeamMemberships_UpdatedById",
                table: "TeamMemberships");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_CreatedById",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_DeletedById",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_UpdatedById",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_ClubMemberships_ClubId_UserId",
                table: "ClubMemberships");

            migrationBuilder.DropIndex(
                name: "IX_ClubMemberships_CreatedById",
                table: "ClubMemberships");

            migrationBuilder.DropIndex(
                name: "IX_ClubMemberships_DeletedById",
                table: "ClubMemberships");

            migrationBuilder.DropIndex(
                name: "IX_ClubMemberships_UpdatedById",
                table: "ClubMemberships");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "TeamMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TeamMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "TeamMemberships");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "TeamMemberships");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ClubMemberships");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "ClubMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ClubMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "ClubMemberships");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "ClubMemberships");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ClubId_Season_Name",
                table: "Teams",
                columns: new[] { "ClubId", "Season", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberships_ClubId_TeamId_UserId",
                table: "TeamMemberships",
                columns: new[] { "ClubId", "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_ClubId_UserId",
                table: "ClubMemberships",
                columns: new[] { "ClubId", "UserId" },
                unique: true);
        }
    }
}
