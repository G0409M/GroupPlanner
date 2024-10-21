using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EstimatedTimeAndProgressStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgressStatus",
                schema: "dbo",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "EstimatedTime",
                schema: "dbo",
                table: "Subtasks",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "ProgressStatus",
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
                name: "ProgressStatus",
                schema: "dbo",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EstimatedTime",
                schema: "dbo",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "ProgressStatus",
                schema: "dbo",
                table: "Subtasks");
        }
    }
}
