using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beepsky.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AudioDownloadTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AudioDownloads",
                columns: table => new
                {
                    DownloadId = table.Column<Guid>(type: "uuid", nullable: false),
                    DownloadUrl = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAccessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlayCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioDownloads", x => x.DownloadId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioDownloads");
        }
    }
}
