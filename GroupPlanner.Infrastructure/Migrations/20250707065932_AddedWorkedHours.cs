using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedWorkedHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkedHours",
                schema: "dbo",
                table: "Subtasks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkedHours",
                schema: "dbo",
                table: "Subtasks");
        }
    }
}
