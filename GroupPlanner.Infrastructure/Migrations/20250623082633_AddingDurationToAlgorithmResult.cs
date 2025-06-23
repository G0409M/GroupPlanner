using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingDurationToAlgorithmResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                schema: "dbo",
                table: "AlgorithmResults",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                schema: "dbo",
                table: "AlgorithmResults");
        }
    }
}
