using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MoveUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exercicios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    GrupoMuscular = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercicios", x => x.Id);
                    table.CheckConstraint("CK_Exercicio_Grupo", "length(trim(GrupoMuscular)) BETWEEN 1 AND 80");
                    table.CheckConstraint("CK_Exercicio_Nome", "length(trim(Nome)) BETWEEN 1 AND 120");
                });

            migrationBuilder.CreateTable(
                name: "Treinos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DiaSemana = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Treinos", x => x.Id);
                    table.CheckConstraint("CK_Treino_Dia", "DiaSemana BETWEEN 1 AND 7");
                    table.CheckConstraint("CK_Treino_Nome", "length(trim(Nome)) BETWEEN 1 AND 120");
                });

            migrationBuilder.CreateTable(
                name: "TreinoExercicios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TreinoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExercicioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    Series = table.Column<int>(type: "INTEGER", nullable: false),
                    Repeticoes = table.Column<int>(type: "INTEGER", nullable: false),
                    CargaInicial = table.Column<double>(type: "REAL", nullable: false),
                    TempoDescanso = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreinoExercicios", x => x.Id);
                    table.CheckConstraint("CK_Item_Carga", "CargaInicial >= 0");
                    table.CheckConstraint("CK_Item_Descanso", "TempoDescanso >= 0");
                    table.CheckConstraint("CK_Item_Ordem", "Ordem >= 0");
                    table.CheckConstraint("CK_Item_Repeticoes", "Repeticoes > 0");
                    table.CheckConstraint("CK_Item_Series", "Series > 0");
                    table.ForeignKey(
                        name: "FK_TreinoExercicios_Exercicios_ExercicioId",
                        column: x => x.ExercicioId,
                        principalTable: "Exercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreinoExercicios_Treinos_TreinoId",
                        column: x => x.TreinoId,
                        principalTable: "Treinos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Exercicios",
                columns: new[] { "Id", "CreatedAt", "Descricao", "GrupoMuscular", "Nome", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-4000-8000-000000000001"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Peito", "Supino reto", null },
                    { new Guid("00000000-0000-4000-8000-000000000002"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Peito", "Supino inclinado", null },
                    { new Guid("00000000-0000-4000-8000-000000000003"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Peito", "Crucifixo", null },
                    { new Guid("00000000-0000-4000-8000-000000000004"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Costas", "Puxada frontal", null },
                    { new Guid("00000000-0000-4000-8000-000000000005"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Costas", "Remada baixa", null },
                    { new Guid("00000000-0000-4000-8000-000000000006"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Pernas", "Agachamento", null },
                    { new Guid("00000000-0000-4000-8000-000000000007"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Pernas", "Leg press", null },
                    { new Guid("00000000-0000-4000-8000-000000000008"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Pernas", "Cadeira extensora", null },
                    { new Guid("00000000-0000-4000-8000-000000000009"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Bíceps", "Rosca direta", null },
                    { new Guid("00000000-0000-4000-8000-000000000010"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Tríceps", "Tríceps pulley", null },
                    { new Guid("00000000-0000-4000-8000-000000000011"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ombros", "Desenvolvimento", null },
                    { new Guid("00000000-0000-4000-8000-000000000012"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Abdômen", "Abdominal", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreinoExercicios_ExercicioId",
                table: "TreinoExercicios",
                column: "ExercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_TreinoExercicios_TreinoId_ExercicioId",
                table: "TreinoExercicios",
                columns: new[] { "TreinoId", "ExercicioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreinoExercicios_TreinoId_Ordem",
                table: "TreinoExercicios",
                columns: new[] { "TreinoId", "Ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Treinos_DiaSemana",
                table: "Treinos",
                column: "DiaSemana");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TreinoExercicios");

            migrationBuilder.DropTable(
                name: "Exercicios");

            migrationBuilder.DropTable(
                name: "Treinos");
        }
    }
}
