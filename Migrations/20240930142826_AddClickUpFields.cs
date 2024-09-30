using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailViewer.Migrations
{
    /// <inheritdoc />
    public partial class AddClickUpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClickUpWorkspaceId",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedClickUpApiKey",
                table: "Users",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClickUpWorkspaceId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EncryptedClickUpApiKey",
                table: "Users");
        }
    }
}
