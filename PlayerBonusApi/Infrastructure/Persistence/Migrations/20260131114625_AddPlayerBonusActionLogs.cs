using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlayerBonusApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerBonusActionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_bonus_action_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerBonusId = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    OperatorUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OperatorUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_bonus_action_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_bonus_action_logs_player_bonuses_PlayerBonusId",
                        column: x => x.PlayerBonusId,
                        principalTable: "player_bonuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_bonus_action_logs_ActionType",
                table: "player_bonus_action_logs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_player_bonus_action_logs_PlayerBonusId",
                table: "player_bonus_action_logs",
                column: "PlayerBonusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_bonus_action_logs");
        }
    }
}
