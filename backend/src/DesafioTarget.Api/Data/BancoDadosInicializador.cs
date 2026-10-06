using System.Text.Json;
using DesafioTarget.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DesafioTarget.Api.Data;

public static class BancoDadosInicializador
{
    public static async Task InicializarAsync(IServiceProvider servicos, CancellationToken cancellationToken = default)
    {
        await using var escopo = servicos.CreateAsyncScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<DesafioTargetDbContext>();
        if (contexto.Database.IsRelational())
            await contexto.Database.MigrateAsync(cancellationToken);

        if (await contexto.Produtos.AnyAsync(cancellationToken)) return;

        var caminhoEstoque = Path.Combine(AppContext.BaseDirectory, "Data", "estoque.json");
        var json = await File.ReadAllTextAsync(caminhoEstoque, cancellationToken);
        var dados = JsonSerializer.Deserialize<EstoqueSeed>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (dados?.Estoque is null || dados.Estoque.Count == 0)
            throw new InvalidDataException("O arquivo de estoque inicial não contém produtos.");

        contexto.Produtos.AddRange(dados.Estoque.Select(produto => new ProdutoEstoqueEntity
        {
            CodigoProduto = produto.CodigoProduto,
            DescricaoProduto = produto.DescricaoProduto,
            Estoque = produto.Estoque
        }));
        await contexto.SaveChangesAsync(cancellationToken);
    }
}
