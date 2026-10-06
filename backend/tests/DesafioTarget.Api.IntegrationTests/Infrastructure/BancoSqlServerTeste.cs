using DesafioTarget.Api.Data;
using DesafioTarget.Api.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DesafioTarget.Api.IntegrationTests.Infrastructure;

public sealed class BancoSqlServerTeste : IAsyncDisposable
{
    public const string VariavelConexao = "DESAFIO_TARGET_SQLSERVER_TEST_CONNECTION";
    private const string PrefixoBanco = "DesafioTarget_IntegrationTests_";
    private readonly string _nomeBanco;
    private bool _bancoCriado;

    private BancoSqlServerTeste(string conexao)
    {
        _nomeBanco = $"{PrefixoBanco}{Guid.NewGuid():N}";
        var configuracao = new SqlConnectionStringBuilder(conexao)
        {
            InitialCatalog = _nomeBanco
        };
        ConnectionString = configuracao.ConnectionString;
    }

    public string ConnectionString { get; }

    public static async Task<BancoSqlServerTeste> CriarAsync()
    {
        var conexao = Environment.GetEnvironmentVariable(VariavelConexao);
        if (string.IsNullOrWhiteSpace(conexao))
            throw new InvalidOperationException($"Configure {VariavelConexao} para executar os testes com SQL Server.");

        var banco = new BancoSqlServerTeste(conexao);
        try
        {
            await banco.CriarBancoAsync();
            await using var contexto = banco.CriarContexto();
            await contexto.Database.MigrateAsync();
            contexto.Produtos.Add(new ProdutoEstoqueEntity
            {
                CodigoProduto = 101,
                DescricaoProduto = "Caneta Azul",
                Estoque = 10
            });
            await contexto.SaveChangesAsync();
            return banco;
        }
        catch (Exception erroInicializacao)
        {
            try
            {
                await banco.DisposeAsync();
            }
            catch (Exception erroLimpeza)
            {
                throw new AggregateException("Falha ao inicializar e remover o banco exclusivo dos testes.",
                    erroInicializacao, erroLimpeza);
            }

            throw;
        }
    }

    public DesafioTargetDbContext CriarContexto(params IInterceptor[] interceptadores)
    {
        var opcoes = new DbContextOptionsBuilder<DesafioTargetDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(interceptadores)
            .Options;
        return new DesafioTargetDbContext(opcoes);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_bancoCriado)
            return;

        ValidarBancoParaExclusao();
        await using var conexaoMaster = CriarConexaoMaster();
        await conexaoMaster.OpenAsync();
        await using var comandoExiste = conexaoMaster.CreateCommand();
        comandoExiste.CommandText = "SELECT DB_ID(@nomeBanco);";
        comandoExiste.Parameters.AddWithValue("@nomeBanco", _nomeBanco);
        var identificadorBanco = await comandoExiste.ExecuteScalarAsync();
        if (identificadorBanco is null or DBNull)
        {
            _bancoCriado = false;
            return;
        }

        await using var contexto = CriarContexto();
        SqlConnection.ClearPool((SqlConnection)contexto.Database.GetDbConnection());
        await contexto.Database.EnsureDeletedAsync();
        _bancoCriado = false;
    }

    private async Task CriarBancoAsync()
    {
        await using var conexao = CriarConexaoMaster();
        await conexao.OpenAsync();
        await using var comando = conexao.CreateCommand();
        comando.CommandText = $"CREATE DATABASE [{_nomeBanco}];";
        await comando.ExecuteNonQueryAsync();
        _bancoCriado = true;
    }

    private SqlConnection CriarConexaoMaster()
    {
        var configuracao = new SqlConnectionStringBuilder(ConnectionString)
        {
            InitialCatalog = "master"
        };
        return new SqlConnection(configuracao.ConnectionString);
    }

    private void ValidarBancoParaExclusao()
    {
        var configuracao = new SqlConnectionStringBuilder(ConnectionString);
        if (!_nomeBanco.StartsWith(PrefixoBanco, StringComparison.Ordinal)
            || _nomeBanco.Length != PrefixoBanco.Length + 32
            || !Guid.TryParseExact(_nomeBanco[PrefixoBanco.Length..], "N", out _)
            || !string.Equals(configuracao.InitialCatalog, _nomeBanco, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A limpeza permite remover somente o banco exclusivo criado pelos testes.");
        }
    }
}
