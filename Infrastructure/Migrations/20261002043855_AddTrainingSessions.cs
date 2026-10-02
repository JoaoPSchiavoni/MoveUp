using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoveUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessoesTreino",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TreinoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TreinoOrigemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NomeTreino = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Inicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Fim = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesTreino", x => x.Id);
                    table.CheckConstraint("CK_Sessao_Fim", "(Status = 'emAndamento' AND Fim IS NULL) OR (Status <> 'emAndamento' AND Fim IS NOT NULL AND Fim >= Inicio)");
                    table.CheckConstraint("CK_Sessao_Nome", "length(trim(NomeTreino)) BETWEEN 1 AND 120");
                    table.CheckConstraint("CK_Sessao_Observacao", "Observacao IS NULL OR length(Observacao) <= 2000");
                    table.CheckConstraint("CK_Sessao_Status", "Status IN ('emAndamento', 'concluida', 'cancelada')");
                    table.ForeignKey(
                        name: "FK_SessoesTreino_Treinos_TreinoId",
                        column: x => x.TreinoId,
                        principalTable: "Treinos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SessaoExercicios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessaoTreinoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    GrupoMuscular = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    TempoDescanso = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessaoExercicios", x => x.Id);
                    table.CheckConstraint("CK_SessaoExercicio_Descanso", "TempoDescanso >= 0");
                    table.CheckConstraint("CK_SessaoExercicio_Ordem", "Ordem >= 0");
                    table.ForeignKey(
                        name: "FK_SessaoExercicios_SessoesTreino_SessaoTreinoId",
                        column: x => x.SessaoTreinoId,
                        principalTable: "SessoesTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeriesRealizadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessaoExercicioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    RepeticoesPlanejadas = table.Column<int>(type: "INTEGER", nullable: false),
                    CargaPlanejada = table.Column<double>(type: "REAL", nullable: false),
                    Repeticoes = table.Column<int>(type: "INTEGER", nullable: true),
                    Carga = table.Column<double>(type: "REAL", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ConcluidaEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesRealizadas", x => x.Id);
                    table.CheckConstraint("CK_Serie_Planejamento", "Ordem >= 0 AND RepeticoesPlanejadas > 0 AND CargaPlanejada >= 0");
                    table.CheckConstraint("CK_Serie_Resultado", "(Status = 'concluida' AND Repeticoes IS NOT NULL AND Repeticoes > 0 AND Carga IS NOT NULL AND Carga >= 0 AND ConcluidaEm IS NOT NULL) OR (Status <> 'concluida' AND Repeticoes IS NULL AND Carga IS NULL AND ConcluidaEm IS NULL)");
                    table.CheckConstraint("CK_Serie_Status", "Status IN ('pendente', 'concluida', 'pulada')");
                    table.ForeignKey(
                        name: "FK_SeriesRealizadas_SessaoExercicios_SessaoExercicioId",
                        column: x => x.SessaoExercicioId,
                        principalTable: "SessaoExercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeriesRealizadas_SessaoExercicioId_Ordem",
                table: "SeriesRealizadas",
                columns: new[] { "SessaoExercicioId", "Ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessaoExercicios_SessaoTreinoId_Ordem",
                table: "SessaoExercicios",
                columns: new[] { "SessaoTreinoId", "Ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessoesTreino_Inicio_Id",
                table: "SessoesTreino",
                columns: new[] { "Inicio", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SessoesTreino_Status",
                table: "SessoesTreino",
                column: "Status",
                unique: true,
                filter: "Status = 'emAndamento'");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesTreino_TreinoId",
                table: "SessoesTreino",
                column: "TreinoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeriesRealizadas");

            migrationBuilder.DropTable(
                name: "SessaoExercicios");

            migrationBuilder.DropTable(
                name: "SessoesTreino");
        }
    }
}
