using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Momentix.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeAiEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiConfidence",
                table: "ChallengeSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiEvaluatedAt",
                table: "ChallengeSubmissions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiFeedback",
                table: "ChallengeSubmissions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "AiIsSatisfied",
                table: "ChallengeSubmissions",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiModel",
                table: "ChallengeSubmissions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiConfidence",
                table: "ChallengeSubmissions");

            migrationBuilder.DropColumn(
                name: "AiEvaluatedAt",
                table: "ChallengeSubmissions");

            migrationBuilder.DropColumn(
                name: "AiFeedback",
                table: "ChallengeSubmissions");

            migrationBuilder.DropColumn(
                name: "AiIsSatisfied",
                table: "ChallengeSubmissions");

            migrationBuilder.DropColumn(
                name: "AiModel",
                table: "ChallengeSubmissions");
        }
    }
}
