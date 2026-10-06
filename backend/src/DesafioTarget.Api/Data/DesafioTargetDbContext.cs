using DesafioTarget.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DesafioTarget.Api.Data;

public sealed class DesafioTargetDbContext(DbContextOptions<DesafioTargetDbContext> options) : DbContext(options)
{
    public DbSet<ProdutoEstoqueEntity> Produtos => Set<ProdutoEstoqueEntity>();
    public DbSet<MovimentacaoEstoqueEntity> Movimentacoes => Set<MovimentacaoEstoqueEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProdutoEstoqueEntity>(entidade =>
        {
            entidade.ToTable("Produtos");
            entidade.HasKey(produto => produto.CodigoProduto);
            entidade.Property(produto => produto.CodigoProduto).ValueGeneratedNever();
            entidade.Property(produto => produto.DescricaoProduto).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<MovimentacaoEstoqueEntity>(entidade =>
        {
            entidade.ToTable("MovimentacoesEstoque");
            entidade.HasKey(movimentacao => movimentacao.Id);
            entidade.Property(movimentacao => movimentacao.Id).UseIdentityColumn();
            entidade.Property(movimentacao => movimentacao.Tipo).HasConversion<string>().HasMaxLength(10).IsRequired();
            entidade.Property(movimentacao => movimentacao.DescricaoProduto).HasMaxLength(150).IsRequired();
            entidade.Property(movimentacao => movimentacao.Descricao).HasMaxLength(500).IsRequired();
            entidade.HasOne<ProdutoEstoqueEntity>().WithMany().HasForeignKey(movimentacao => movimentacao.CodigoProduto).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class ProdutoEstoqueEntity
{
    public int CodigoProduto { get; set; }
    public string DescricaoProduto { get; set; } = string.Empty;
    public int Estoque { get; set; }
}

public sealed class MovimentacaoEstoqueEntity
{
    public long Id { get; set; }
    public int CodigoProduto { get; set; }
    public string DescricaoProduto { get; set; } = string.Empty;
    public TipoMovimentacao Tipo { get; set; }
    public int Quantidade { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int EstoqueAnterior { get; set; }
    public int EstoqueFinal { get; set; }
    public DateTimeOffset RealizadaEm { get; set; }
}
