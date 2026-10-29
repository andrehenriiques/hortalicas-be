using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hortalicas.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Estufas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ComprimentoM = table.Column<double>(type: "REAL", nullable: false),
                    LarguraM = table.Column<double>(type: "REAL", nullable: false),
                    DiasMaxSemIrrigacao = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estufas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hortalicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CicloDias = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hortalicas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Cpf = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Canteiros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EstufaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Canteiros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Canteiros_Estufas_EstufaId",
                        column: x => x.EstufaId,
                        principalTable: "Estufas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Irrigacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EstufaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    DuracaoMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Irrigacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Irrigacoes_Estufas_EstufaId",
                        column: x => x.EstufaId,
                        principalTable: "Estufas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Plantios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CanteiroId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HortalicaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DataPlantio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DataPrevistaColheita = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DataEncerramento = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    MotivoEncerramento = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plantios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plantios_Canteiros_CanteiroId",
                        column: x => x.CanteiroId,
                        principalTable: "Canteiros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Plantios_Hortalicas_HortalicaId",
                        column: x => x.HortalicaId,
                        principalTable: "Hortalicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Colheitas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlantioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Data = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PesoAproveitadoG = table.Column<int>(type: "INTEGER", nullable: false),
                    PesoDescartadoG = table.Column<int>(type: "INTEGER", nullable: false),
                    MotivoDescarte = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    EncerrouCiclo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Lote = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colheitas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Colheitas_Plantios_PlantioId",
                        column: x => x.PlantioId,
                        principalTable: "Plantios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Canteiros_EstufaId_Numero",
                table: "Canteiros",
                columns: new[] { "EstufaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colheitas_Data",
                table: "Colheitas",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_Colheitas_Lote",
                table: "Colheitas",
                column: "Lote",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colheitas_PlantioId",
                table: "Colheitas",
                column: "PlantioId");

            migrationBuilder.CreateIndex(
                name: "IX_Estufas_Nome",
                table: "Estufas",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hortalicas_Nome",
                table: "Hortalicas",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Irrigacoes_EstufaId_Data",
                table: "Irrigacoes",
                columns: new[] { "EstufaId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_Plantios_CanteiroId_Situacao",
                table: "Plantios",
                columns: new[] { "CanteiroId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Plantios_HortalicaId",
                table: "Plantios",
                column: "HortalicaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Cpf",
                table: "Usuarios",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Colheitas");

            migrationBuilder.DropTable(
                name: "Irrigacoes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Plantios");

            migrationBuilder.DropTable(
                name: "Canteiros");

            migrationBuilder.DropTable(
                name: "Hortalicas");

            migrationBuilder.DropTable(
                name: "Estufas");
        }
    }
}
