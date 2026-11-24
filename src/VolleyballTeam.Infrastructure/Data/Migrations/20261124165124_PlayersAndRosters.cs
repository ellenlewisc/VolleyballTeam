using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyballTeam.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayersAndRosters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GradYear = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    DeletedById = table.Column<int>(type: "int", nullable: true),
                    ClubId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.UniqueConstraint("AK_Players_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Players_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Players_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Players_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Players_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Guardians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UnsubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnsubscribeToken = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    DeletedById = table.Column<int>(type: "int", nullable: true),
                    ClubId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guardians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guardians_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Guardians_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Guardians_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Guardians_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Guardians_Players_ClubId_PlayerId",
                        columns: x => new { x.ClubId, x.PlayerId },
                        principalTable: "Players",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RosterEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamSeasonId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    JerseyNumber = table.Column<int>(type: "int", nullable: true),
                    PrimaryPosition = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    SecondaryPosition = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    JoinedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    LeftOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    DeletedById = table.Column<int>(type: "int", nullable: true),
                    ClubId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RosterEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RosterEntries_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RosterEntries_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RosterEntries_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RosterEntries_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterEntries_Players_ClubId_PlayerId",
                        columns: x => new { x.ClubId, x.PlayerId },
                        principalTable: "Players",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterEntries_TeamSeasons_ClubId_TeamSeasonId",
                        columns: x => new { x.ClubId, x.TeamSeasonId },
                        principalTable: "TeamSeasons",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_ClubId_PlayerId_Email",
                table: "Guardians",
                columns: new[] { "ClubId", "PlayerId", "Email" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_CreatedById",
                table: "Guardians",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_DeletedById",
                table: "Guardians",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_UnsubscribeToken",
                table: "Guardians",
                column: "UnsubscribeToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_UpdatedById",
                table: "Guardians",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Players_ClubId_LastName_FirstName",
                table: "Players",
                columns: new[] { "ClubId", "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_Players_CreatedById",
                table: "Players",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Players_DeletedById",
                table: "Players",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Players_UpdatedById",
                table: "Players",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RosterEntries_ClubId_PlayerId",
                table: "RosterEntries",
                columns: new[] { "ClubId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_RosterEntries_ClubId_TeamSeasonId_JerseyNumber",
                table: "RosterEntries",
                columns: new[] { "ClubId", "TeamSeasonId", "JerseyNumber" },
                unique: true,
                filter: "[LeftOn] IS NULL AND [DeletedAt] IS NULL AND [JerseyNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RosterEntries_ClubId_TeamSeasonId_PlayerId",
                table: "RosterEntries",
                columns: new[] { "ClubId", "TeamSeasonId", "PlayerId" },
                unique: true,
                filter: "[LeftOn] IS NULL AND [DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RosterEntries_CreatedById",
                table: "RosterEntries",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RosterEntries_DeletedById",
                table: "RosterEntries",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RosterEntries_UpdatedById",
                table: "RosterEntries",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Guardians");

            migrationBuilder.DropTable(
                name: "RosterEntries");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
