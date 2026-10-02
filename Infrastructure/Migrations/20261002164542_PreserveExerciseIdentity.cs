using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoveUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreserveExerciseIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExercicioOrigemId",
                table: "SessaoExercicios",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExercicioOrigemId",
                table: "SessaoExercicios");
        }
    }
}
