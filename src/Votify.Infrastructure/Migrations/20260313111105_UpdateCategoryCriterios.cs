using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoryCriterios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeightCriterionA",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "WeightCriterionB",
                table: "Categories");

            migrationBuilder.AddColumn<List<double>>(
                name: "Criterios",
                table: "Categories",
                type: "double precision[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "EventId1",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_EventId1",
                table: "Categories",
                column: "EventId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Events_EventId1",
                table: "Categories",
                column: "EventId1",
                principalTable: "Events",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Events_EventId1",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_EventId1",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Criterios",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "EventId1",
                table: "Categories");

            migrationBuilder.AddColumn<double>(
                name: "WeightCriterionA",
                table: "Categories",
                type: "double precision",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WeightCriterionB",
                table: "Categories",
                type: "double precision",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
