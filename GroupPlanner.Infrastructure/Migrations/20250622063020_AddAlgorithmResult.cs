using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlgorithmResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlgorithmResults",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Algorithm = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResultValue = table.Column<double>(type: "float", nullable: false),
                    ResultData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlgorithmResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlgorithmResults_User_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "dbo",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlgorithmResults_CreatedById",
                schema: "dbo",
                table: "AlgorithmResults",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlgorithmResults",
                schema: "dbo");
        }
    }
}
