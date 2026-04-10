using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVoteToTopPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpertVote_RawScore",
                table: "Votes");

            migrationBuilder.DropColumn(
                name: "RawScore",
                table: "Votes");

            migrationBuilder.AddColumn<int>(
                name: "TopPosition",
                table: "Votes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TopPosition",
                table: "Votes");

            migrationBuilder.AddColumn<double>(
                name: "ExpertVote_RawScore",
                table: "Votes",
                type: "double precision",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RawScore",
                table: "Votes",
                type: "double precision",
                precision: 5,
                scale: 2,
                nullable: true);
        }
    }
}
