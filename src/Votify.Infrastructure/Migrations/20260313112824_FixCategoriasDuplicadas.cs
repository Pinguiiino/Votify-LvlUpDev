using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCategoriasDuplicadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Events_EventId1",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_EventId1",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "EventId1",
                table: "Categories");

            migrationBuilder.AlterColumn<List<double>>(
                name: "Criterios",
                table: "Categories",
                type: "double precision[]",
                nullable: true,
                oldClrType: typeof(List<double>),
                oldType: "double precision[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<double>>(
                name: "Criterios",
                table: "Categories",
                type: "double precision[]",
                nullable: false,
                oldClrType: typeof(List<double>),
                oldType: "double precision[]",
                oldNullable: true);

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
    }
}
