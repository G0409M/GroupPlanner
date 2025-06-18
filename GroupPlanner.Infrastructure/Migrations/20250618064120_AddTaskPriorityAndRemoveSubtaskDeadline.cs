using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskPriorityAndRemoveSubtaskDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deadline",
                schema: "dbo",
                table: "Subtasks");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                schema: "dbo",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "dbo",
                table: "Tasks");

            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                schema: "dbo",
                table: "Subtasks",
                type: "datetime2",
                nullable: true);
        }
    }
}
