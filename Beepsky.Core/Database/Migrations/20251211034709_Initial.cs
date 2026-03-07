using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beepsky.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordGuilds",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordGuilds", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordUsers",
                columns: table => new
                {
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    CanUseCommands = table.Column<bool>(type: "boolean", nullable: false),
                    Mention = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordUsers", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "DiscordGuildUserStatistics",
                columns: table => new
                {
                    DiscordGuildUserStatisticId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    MessagesSent = table.Column<long>(type: "bigint", nullable: false),
                    BeepskyCommandsUsed = table.Column<long>(type: "bigint", nullable: false),
                    BeepskyChattedWith = table.Column<long>(type: "bigint", nullable: false),
                    MostChattyDay = table.Column<DateOnly>(type: "date", nullable: false),
                    MessagesSentOnMostChattyDay = table.Column<long>(type: "bigint", nullable: false),
                    Today = table.Column<DateOnly>(type: "date", nullable: false),
                    MessagesSentToday = table.Column<long>(type: "bigint", nullable: false),
                    TimesJoinedVoice = table.Column<long>(type: "bigint", nullable: false),
                    TimesKickedFromVoice = table.Column<long>(type: "bigint", nullable: false),
                    MessagesDeletedFrom = table.Column<long>(type: "bigint", nullable: false),
                    MessagesDeletedBy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordGuildUserStatistics", x => x.DiscordGuildUserStatisticId);
                    table.ForeignKey(
                        name: "FK_DiscordGuildUserStatistics_DiscordGuilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "DiscordGuilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscordGuildUserStatistics_DiscordUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuildUserStatistics_GuildId",
                table: "DiscordGuildUserStatistics",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuildUserStatistics_UserId",
                table: "DiscordGuildUserStatistics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuildUserStatistics_Year",
                table: "DiscordGuildUserStatistics",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordGuildUserStatistics");

            migrationBuilder.DropTable(
                name: "DiscordGuilds");

            migrationBuilder.DropTable(
                name: "DiscordUsers");
        }
    }
}
