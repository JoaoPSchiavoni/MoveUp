using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoveUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsistency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DataPresenca",
                table: "SessoesTreino",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FusoPresenca",
                table: "SessoesTreino",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcompanhamentoConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Fuso = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Inicio = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcompanhamentoConfig", x => x.Id);
                    table.CheckConstraint("CK_Acompanhamento_Unico", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "MetaRevisoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Inicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Dias = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaRevisoes", x => x.Id);
                    table.CheckConstraint("CK_Meta_Dias", "Dias BETWEEN 1 AND 7");
                });

            migrationBuilder.CreateTable(
                name: "RotinaRevisoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Inicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Dias = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotinaRevisoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessoesTreino_Status_DataPresenca",
                table: "SessoesTreino",
                columns: new[] { "Status", "DataPresenca" });

            migrationBuilder.CreateIndex(
                name: "IX_MetaRevisoes_Inicio",
                table: "MetaRevisoes",
                column: "Inicio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RotinaRevisoes_Inicio",
                table: "RotinaRevisoes",
                column: "Inicio",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcompanhamentoConfig");

            migrationBuilder.DropTable(
                name: "MetaRevisoes");

            migrationBuilder.DropTable(
                name: "RotinaRevisoes");

            migrationBuilder.DropIndex(
                name: "IX_SessoesTreino_Status_DataPresenca",
                table: "SessoesTreino");

            migrationBuilder.DropColumn(
                name: "DataPresenca",
                table: "SessoesTreino");

            migrationBuilder.DropColumn(
                name: "FusoPresenca",
                table: "SessoesTreino");
        }
    }
}
