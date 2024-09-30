using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailViewer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFieldsFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClickUpApiKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ClickUpListId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ClickUpUserId",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClickUpApiKey",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClickUpListId",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClickUpUserId",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
