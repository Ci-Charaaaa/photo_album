using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoAlbum.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        /// 应用补丁，创建数据库表和索引，并插入初始数据
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "person",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "photo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Remark = table.Column<string>(type: "TEXT", nullable: true),
                    SourceName = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    TakenTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImportTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OtherInfo = table.Column<string>(type: "TEXT", nullable: true),
                    Hash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "photo_set",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<long>(type: "INTEGER", nullable: true),
                    Remark = table.Column<string>(type: "TEXT", nullable: true),
                    CoverPhotoId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_set", x => x.Id);
                    table.ForeignKey(
                        name: "FK_photo_set_photo_CoverPhotoId",
                        column: x => x.CoverPhotoId,
                        principalTable: "photo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_photo_set_photo_set_ParentId",
                        column: x => x.ParentId,
                        principalTable: "photo_set",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "photo_album",
                columns: table => new
                {
                    AlbumId = table.Column<long>(type: "INTEGER", nullable: false),
                    PhotoId = table.Column<long>(type: "INTEGER", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_album", x => new { x.PhotoId, x.AlbumId });
                    table.ForeignKey(
                        name: "FK_photo_album_photo_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "photo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_photo_album_photo_set_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "photo_set",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "photo_set",
                columns: new[] { "Id", "CoverPhotoId", "Name", "ParentId", "Remark" },
                values: new object[] { 1L, null, "未分类", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_photo_Hash",
                table: "photo",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_photo_album_AlbumId",
                table: "photo_album",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_photo_set_CoverPhotoId",
                table: "photo_set",
                column: "CoverPhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_photo_set_ParentId",
                table: "photo_set",
                column: "ParentId");
        }

        /// <inheritdoc />
        /// 撤销补丁，删除数据库表和索引
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person");

            migrationBuilder.DropTable(
                name: "photo_album");

            migrationBuilder.DropTable(
                name: "photo_set");

            migrationBuilder.DropTable(
                name: "photo");
        }
    }
}
