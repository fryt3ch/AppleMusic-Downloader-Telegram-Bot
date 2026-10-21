using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Migrations
{
    /// <inheritdoc />
    public partial class AddCachedSongProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "CachedSongs",
                type: "TEXT",
                nullable: false,
                defaultValue: "gamdl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Provider",
                table: "CachedSongs");
        }
    }
}
