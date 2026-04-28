using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightedVote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeightedVotes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    VotingSessionId = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightedVotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeightedCriterionScores",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WeightedVoteId = table.Column<string>(type: "text", nullable: false),
                    CriterionId = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightedCriterionScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightedCriterionScores_WeightedVotes_WeightedVoteId",
                        column: x => x.WeightedVoteId,
                        principalTable: "WeightedVotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightedCriterionScores_WeightedVoteId",
                table: "WeightedCriterionScores",
                column: "WeightedVoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeightedCriterionScores");

            migrationBuilder.DropTable(
                name: "WeightedVotes");
        }
    }
}
