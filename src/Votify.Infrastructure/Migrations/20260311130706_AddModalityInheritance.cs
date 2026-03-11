using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Votify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModalityInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Modality",
                table: "Events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Events",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "Modality",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
