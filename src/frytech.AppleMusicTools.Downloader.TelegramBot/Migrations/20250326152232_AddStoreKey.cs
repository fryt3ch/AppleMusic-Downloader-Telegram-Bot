using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CachedSongs",
                table: "CachedSongs");

            migrationBuilder.AddColumn<string>(
                name: "Store",
                table: "CachedSongs",
                type: "TEXT",
                nullable: false,
                defaultValue: "us");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CachedSongs",
                table: "CachedSongs",
                columns: new[] { "SongId", "Store" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CachedSongs",
                table: "CachedSongs");

            migrationBuilder.DropColumn(
                name: "Store",
                table: "CachedSongs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CachedSongs",
                table: "CachedSongs",
                column: "SongId");
        }
    }
}
