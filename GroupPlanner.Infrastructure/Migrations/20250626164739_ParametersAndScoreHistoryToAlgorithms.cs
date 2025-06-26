using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ParametersAndScoreHistoryToAlgorithms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParametersJson",
                schema: "dbo",
                table: "AlgorithmResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoreHistoryJson",
                schema: "dbo",
                table: "AlgorithmResults",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParametersJson",
                schema: "dbo",
                table: "AlgorithmResults");

            migrationBuilder.DropColumn(
                name: "ScoreHistoryJson",
                schema: "dbo",
                table: "AlgorithmResults");
        }
    }
}
