using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveTopNFromEventToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TopNProjectsAllowed",
                table: "Events");

            migrationBuilder.AddColumn<int>(
                name: "TopNProjectsAllowed",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 3);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TopNProjectsAllowed",
                table: "Categories");

            migrationBuilder.AddColumn<int>(
                name: "TopNProjectsAllowed",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
