using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beepsky.Database.Migrations
{
    /// <inheritdoc />
    public partial class FileRemovedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FileRemoved",
                table: "AudioDownloads",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileRemoved",
                table: "AudioDownloads");
        }
    }
}
