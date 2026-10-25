using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Migrations
{
    /// <inheritdoc />
    public partial class ReworkCachedSongCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "CachedSongs",
                newName: "CachedSongs_legacy");

            migrationBuilder.CreateTable(
                name: "CachedSongs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArtistName = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedArtistName = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    FileId = table.Column<string>(type: "TEXT", nullable: false),
                    FileUniqueId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedSongs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedSongs_NormalizedArtistName_NormalizedName",
                table: "CachedSongs",
                columns: new[] { "NormalizedArtistName", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_CachedSongs_SourceUrl",
                table: "CachedSongs",
                column: "SourceUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedSongs");

            migrationBuilder.Sql("DROP TABLE IF EXISTS \"CachedSongs_legacy\";");

            migrationBuilder.CreateTable(
                name: "CachedSongs",
                columns: table => new
                {
                    SongId = table.Column<string>(type: "TEXT", nullable: false),
                    Store = table.Column<string>(type: "TEXT", nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: false),
                    FileUniqueId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ArtistName = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "gamdl")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedSongs", x => new { x.SongId, x.Store });
                });
        }
    }
}
