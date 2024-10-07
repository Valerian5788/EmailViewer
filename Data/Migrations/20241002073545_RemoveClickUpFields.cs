using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailViewer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClickUpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedClickUpApiKey",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedClickUpApiKey",
                table: "Users",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
