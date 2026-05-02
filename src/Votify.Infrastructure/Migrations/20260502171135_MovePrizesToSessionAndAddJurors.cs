using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MovePrizesToSessionAndAddJurors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prizes_Categories_CategoryId",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "TargetVoter",
                table: "Prizes");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Prizes",
                newName: "VotingSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Prizes_CategoryId",
                table: "Prizes",
                newName: "IX_Prizes_VotingSessionId");

            migrationBuilder.AddColumn<List<string>>(
                name: "JurorEmails",
                table: "VotingSessions",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Prizes_VotingSessions_VotingSessionId",
                table: "Prizes",
                column: "VotingSessionId",
                principalTable: "VotingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prizes_VotingSessions_VotingSessionId",
                table: "Prizes");

            migrationBuilder.DropColumn(
                name: "JurorEmails",
                table: "VotingSessions");

            migrationBuilder.RenameColumn(
                name: "VotingSessionId",
                table: "Prizes",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Prizes_VotingSessionId",
                table: "Prizes",
                newName: "IX_Prizes_CategoryId");

            migrationBuilder.AddColumn<int>(
                name: "TargetVoter",
                table: "Prizes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Prizes_Categories_CategoryId",
                table: "Prizes",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
