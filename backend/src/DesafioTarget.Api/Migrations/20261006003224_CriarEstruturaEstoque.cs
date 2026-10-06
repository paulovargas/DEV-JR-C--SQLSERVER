using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesafioTarget.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriarEstruturaEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    CodigoProduto = table.Column<int>(type: "int", nullable: false),
                    DescricaoProduto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Estoque = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.CodigoProduto);
                });

            migrationBuilder.CreateTable(
                name: "MovimentacoesEstoque",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoProduto = table.Column<int>(type: "int", nullable: false),
                    DescricaoProduto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EstoqueAnterior = table.Column<int>(type: "int", nullable: false),
                    EstoqueFinal = table.Column<int>(type: "int", nullable: false),
                    RealizadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentacoesEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentacoesEstoque_Produtos_CodigoProduto",
                        column: x => x.CodigoProduto,
                        principalTable: "Produtos",
                        principalColumn: "CodigoProduto",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesEstoque_CodigoProduto",
                table: "MovimentacoesEstoque",
                column: "CodigoProduto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimentacoesEstoque");

            migrationBuilder.DropTable(
                name: "Produtos");
        }
    }
}
