using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TailorCV.CVGenerator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertEnumsToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "cvgenerator",
                table: "generated_cvs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "pdf_status",
                schema: "cvgenerator",
                table: "generated_cvs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "generation_type",
                schema: "cvgenerator",
                table: "generated_cvs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "cvgenerator",
                table: "generated_cvs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "pdf_status",
                schema: "cvgenerator",
                table: "generated_cvs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "generation_type",
                schema: "cvgenerator",
                table: "generated_cvs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
