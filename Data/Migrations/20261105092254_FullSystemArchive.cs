using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace HR.Data.Migrations
{
    /// <inheritdoc />
    public partial class FullSystemArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "ArchiveFolders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "ArchiveFiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "ArchiveFiles",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentDate",
                table: "ArchiveFiles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "ArchiveFiles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "ArchiveFiles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Party",
                table: "ArchiveFiles",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "ArchiveFiles",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ItemType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    ItemName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Details = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AppRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Permissions = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRoles", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Role = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUsers_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArchiveShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ItemType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: true),
                    FolderId = table.Column<int>(type: "int", nullable: true),
                    SharedWithUser = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    SharedWithDepartmentId = table.Column<int>(type: "int", nullable: true),
                    Permission = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    SharedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    SharedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveShares_ArchiveFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "ArchiveFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArchiveShares_ArchiveFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "ArchiveFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArchiveShares_Departments_SharedWithDepartmentId",
                        column: x => x.SharedWithDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    Group = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveFolders_IsDeleted",
                table: "ArchiveFolders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveFolders_IsFavorite",
                table: "ArchiveFolders",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveFiles_IsDeleted",
                table: "ArchiveFiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveFiles_IsFavorite",
                table: "ArchiveFiles",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Action",
                table: "ActivityLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CreatedAt",
                table: "ActivityLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserName",
                table: "ActivityLogs",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_AppRoles_Code",
                table: "AppRoles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_DepartmentId",
                table: "AppUsers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveShares_FileId",
                table: "ArchiveShares",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveShares_FolderId",
                table: "ArchiveShares",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveShares_ItemType_FileId_FolderId",
                table: "ArchiveShares",
                columns: new[] { "ItemType", "FileId", "FolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveShares_SharedWithDepartmentId",
                table: "ArchiveShares",
                column: "SharedWithDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "AppRoles");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "ArchiveShares");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_ArchiveFolders_IsDeleted",
                table: "ArchiveFolders");

            migrationBuilder.DropIndex(
                name: "IX_ArchiveFolders_IsFavorite",
                table: "ArchiveFolders");

            migrationBuilder.DropIndex(
                name: "IX_ArchiveFiles_IsDeleted",
                table: "ArchiveFiles");

            migrationBuilder.DropIndex(
                name: "IX_ArchiveFiles_IsFavorite",
                table: "ArchiveFiles");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "ArchiveFolders");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "ArchiveFiles");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "ArchiveFiles");

            migrationBuilder.DropColumn(
                name: "DocumentDate",
                table: "ArchiveFiles");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "ArchiveFiles");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "ArchiveFiles");

            migrationBuilder.DropColumn(
                name: "Party",
                table: "ArchiveFiles");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "ArchiveFiles");
        }
    }
}
