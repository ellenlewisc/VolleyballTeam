using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolleyballTeam.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeasonsAndTeamSeasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teams_ClubId_Season_Name",
                table: "Teams");

            migrationBuilder.AlterColumn<string>(
                name: "Season",
                table: "Teams",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                    table.UniqueConstraint("AK_Seasons_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Seasons_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Seasons_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Seasons_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Seasons_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamSeasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    SeasonId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AgeGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RolledOverFromId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_TeamSeasons", x => x.Id);
                    table.UniqueConstraint("AK_TeamSeasons_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_TeamSeasons_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamSeasons_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamSeasons_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamSeasons_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamSeasons_Seasons_ClubId_SeasonId",
                        columns: x => new { x.ClubId, x.SeasonId },
                        principalTable: "Seasons",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamSeasons_TeamSeasons_ClubId_RolledOverFromId",
                        columns: x => new { x.ClubId, x.RolledOverFromId },
                        principalTable: "TeamSeasons",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamSeasons_Teams_ClubId_TeamId",
                        columns: x => new { x.ClubId, x.TeamId },
                        principalTable: "Teams",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamSeasonStaff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamSeasonId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_TeamSeasonStaff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamSeasonStaff_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamSeasonStaff_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamSeasonStaff_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamSeasonStaff_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamSeasonStaff_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamSeasonStaff_TeamSeasons_ClubId_TeamSeasonId",
                        columns: x => new { x.ClubId, x.TeamSeasonId },
                        principalTable: "TeamSeasons",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ClubId_Name",
                table: "Teams",
                columns: new[] { "ClubId", "Name" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_ClubId_Name",
                table: "Seasons",
                columns: new[] { "ClubId", "Name" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_CreatedById",
                table: "Seasons",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_DeletedById",
                table: "Seasons",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_UpdatedById",
                table: "Seasons",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasons_ClubId_RolledOverFromId",
                table: "TeamSeasons",
                columns: new[] { "ClubId", "RolledOverFromId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasons_ClubId_SeasonId_DisplayName",
                table: "TeamSeasons",
                columns: new[] { "ClubId", "SeasonId", "DisplayName" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasons_ClubId_SeasonId_TeamId",
                table: "TeamSeasons",
                columns: new[] { "ClubId", "SeasonId", "TeamId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasons_ClubId_TeamId",
                table: "TeamSeasons",
                columns: new[] { "ClubId", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasons_CreatedById",
                table: "TeamSeasons",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasons_DeletedById",
                table: "TeamSeasons",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasons_UpdatedById",
                table: "TeamSeasons",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStaff_ClubId_TeamSeasonId_UserId",
                table: "TeamSeasonStaff",
                columns: new[] { "ClubId", "TeamSeasonId", "UserId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStaff_CreatedById",
                table: "TeamSeasonStaff",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStaff_DeletedById",
                table: "TeamSeasonStaff",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStaff_UpdatedById",
                table: "TeamSeasonStaff",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStaff_UserId",
                table: "TeamSeasonStaff",
                column: "UserId");

            // Phase 0 kept one Teams row per season; the newest season per club becomes Active and older ones Archived.
            migrationBuilder.Sql("""
                INSERT INTO [Seasons] ([ClubId], [Name], [Status], [CreatedAt], [UpdatedAt])
                SELECT s.[ClubId], s.[Season],
                       CASE WHEN ROW_NUMBER() OVER (PARTITION BY s.[ClubId] ORDER BY s.[Season] DESC) = 1 THEN N'Active' ELSE N'Archived' END,
                       SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM (SELECT DISTINCT [ClubId], [Season] FROM [Teams] WHERE [Season] IS NOT NULL AND [DeletedAt] IS NULL) AS s;

                INSERT INTO [TeamSeasons] ([ClubId], [TeamId], [SeasonId], [DisplayName], [AgeGroup], [Level], [CreatedAt], [UpdatedAt])
                SELECT t.[ClubId], t.[Id], s.[Id], t.[Name], t.[AgeGroup], t.[Level], SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM [Teams] AS t
                JOIN [Seasons] AS s ON s.[ClubId] = t.[ClubId] AND s.[Name] = t.[Season]
                WHERE t.[DeletedAt] IS NULL;

                INSERT INTO [TeamSeasonStaff] ([ClubId], [TeamSeasonId], [UserId], [Role], [CreatedAt], [UpdatedAt])
                SELECT m.[ClubId], ts.[Id], m.[UserId], m.[Role], SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM [TeamMemberships] AS m
                JOIN [TeamSeasons] AS ts ON ts.[ClubId] = m.[ClubId] AND ts.[TeamId] = m.[TeamId]
                WHERE m.[DeletedAt] IS NULL AND m.[Role] <> N'Parent';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamSeasonStaff");

            migrationBuilder.DropTable(
                name: "TeamSeasons");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropIndex(
                name: "IX_Teams_ClubId_Name",
                table: "Teams");

            migrationBuilder.AlterColumn<string>(
                name: "Season",
                table: "Teams",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ClubId_Season_Name",
                table: "Teams",
                columns: new[] { "ClubId", "Season", "Name" },
                unique: true,
                filter: "[DeletedAt] IS NULL");
        }
    }
}
