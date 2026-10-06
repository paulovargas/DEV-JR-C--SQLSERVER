using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DesafioTarget.Api.Data;
using DesafioTarget.Api.Models;
using DesafioTarget.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DesafioTarget.Api.Tests;

public sealed class ErrosEndpointTests
{
    private const string DadoSensivel = "segredo-nao-deve-aparecer-na-resposta";
    private const string DetalheErroInterno = "Não foi possível concluir a operação. Tente novamente mais tarde.";

    [Theory]
    [InlineData("/api/produtos/999", "Produto de código 999 não encontrado.")]
    [InlineData("/api/movimentacoes/999", "Movimentação 999 não encontrada.")]
    public async Task Consultar_DevePadronizarRecursosNaoEncontrados(string caminho, string detalhe)
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();

        using var resposta = await cliente.GetAsync(caminho);

        using var problema = await VerificarProblemaAsync(resposta, HttpStatusCode.NotFound, caminho);
        Assert.Equal(detalhe, problema.RootElement.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData(999, "entrada", 1, HttpStatusCode.NotFound, "Produto de código 999 não encontrado.")]
    [InlineData(101, "saida", 151, HttpStatusCode.Conflict, "Estoque insuficiente. Saldo atual: 150.")]
    [InlineData(101, "entrada", int.MaxValue, HttpStatusCode.BadRequest, "A quantidade informada excede o limite suportado para o estoque.")]
    public async Task Movimentar_DevePadronizarErrosDeNegocioSemAlterarSaldoOuHistorico(
        int codigoProduto, string tipo, int quantidade, HttpStatusCode status, string detalhe)
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();
        var produtosAnteriores = await cliente.GetFromJsonAsync<ProdutoEstoque[]>("/api/produtos");

        using var resposta = await cliente.PostAsJsonAsync("/api/movimentacoes", new
        {
            codigoProduto,
            tipo,
            quantidade,
            descricao = "Teste do contrato de erros"
        });

        using var problema = await VerificarProblemaAsync(resposta, status, "/api/movimentacoes");
        Assert.Equal(detalhe, problema.RootElement.GetProperty("detail").GetString());
        Assert.Equal(produtosAnteriores, await cliente.GetFromJsonAsync<ProdutoEstoque[]>("/api/produtos"));
        Assert.Empty((await cliente.GetFromJsonAsync<MovimentacaoEstoque[]>("/api/movimentacoes"))!);
    }

    [Theory]
    [InlineData("/api/comissoes/calcular", "{\"vendas\":[]}", "vendas")]
    [InlineData("/api/juros/calcular", "{\"valor\":0,\"dataVencimento\":\"2026-10-06\"}", "valor")]
    [InlineData("/api/movimentacoes", "{\"codigoProduto\":101,\"tipo\":\"entrada\",\"quantidade\":0,\"descricao\":\"Teste\"}", "quantidade")]
    public async Task Validar_DeveIncluirCamposInvalidosNoMesmoContratoDeProblema(
        string caminho, string json, string campo)
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();
        using var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

        using var resposta = await cliente.PostAsync(caminho, conteudo);

        using var problema = await VerificarProblemaAsync(resposta, HttpStatusCode.BadRequest, caminho);
        var mensagens = problema.RootElement.GetProperty("errors").GetProperty(campo);
        Assert.NotEmpty(mensagens.EnumerateArray());
        Assert.False(string.IsNullOrWhiteSpace(mensagens[0].GetString()));
    }

    [Fact]
    public async Task Calcular_DevePreservarErrosDeCalculoInvalidoExceptionNoContratoHttp()
    {
        using var factory = new ApiFactory();
        using var factoryConfigurada = factory.WithWebHostBuilder(builder => builder.ConfigureServices(servicos =>
        {
            servicos.RemoveAll<ICalculadoraComissao>();
            servicos.AddSingleton<ICalculadoraComissao>(new CalculadoraComFalhaDeValidacao());
        }));
        using var cliente = factoryConfigurada.CreateClient();

        using var resposta = await cliente.PostAsJsonAsync("/api/comissoes/calcular", new
        {
            vendas = new[] { new Venda("Maria", 100m) }
        });

        using var problema = await VerificarProblemaAsync(resposta, HttpStatusCode.BadRequest, "/api/comissoes/calcular");
        var mensagens = problema.RootElement.GetProperty("errors").GetProperty("vendas[0].valor");
        Assert.Equal("O valor informado não permite realizar o cálculo.", mensagens[0].GetString());
    }

    [Theory]
    [InlineData("Development", "json")]
    [InlineData("Development", "enum")]
    [InlineData("Development", "numero")]
    [InlineData("Development", "ausente")]
    [InlineData("Development", "nulo")]
    [InlineData("Production", "json")]
    [InlineData("Production", "enum")]
    [InlineData("Production", "numero")]
    [InlineData("Production", "ausente")]
    [InlineData("Production", "nulo")]
    public async Task Desserializar_DeveRetornarProblemaSeguroParaCorposInvalidos(string ambiente, string caso)
    {
        using var factory = new ApiFactory();
        using var factoryConfigurada = factory.WithWebHostBuilder(builder => builder.UseEnvironment(ambiente));
        using var cliente = factoryConfigurada.CreateClient();
        var json = caso switch
        {
            "json" => $"{{\"descricao\":\"{DadoSensivel}\",\"quantidade\":",
            "enum" => $"{{\"codigoProduto\":101,\"tipo\":\"{DadoSensivel}\",\"quantidade\":1,\"descricao\":\"Teste\"}}",
            "numero" => $"{{\"codigoProduto\":\"{DadoSensivel}\",\"tipo\":\"entrada\",\"quantidade\":1,\"descricao\":\"Teste\"}}",
            "nulo" => "null",
            _ => string.Empty
        };
        using var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

        using var resposta = await cliente.PostAsync("/api/movimentacoes", conteudo);

        using var problema = await VerificarProblemaAsync(resposta, HttpStatusCode.BadRequest, "/api/movimentacoes");
        VerificarAusenciaDeDetalhesInternos(problema.RootElement.GetRawText());
        Assert.Equal(150, (await cliente.GetFromJsonAsync<ProdutoEstoque>("/api/produtos/101"))!.Estoque);
        Assert.Empty((await cliente.GetFromJsonAsync<MovimentacaoEstoque[]>("/api/movimentacoes"))!);
    }

    [Theory]
    [InlineData("Development", "GET", "/api/recurso-inexistente", HttpStatusCode.NotFound)]
    [InlineData("Development", "DELETE", "/api/produtos", HttpStatusCode.MethodNotAllowed)]
    [InlineData("Production", "GET", "/api/recurso-inexistente", HttpStatusCode.NotFound)]
    [InlineData("Production", "DELETE", "/api/produtos", HttpStatusCode.MethodNotAllowed)]
    public async Task Rotear_DevePadronizarRespostasSemCorpoSemExporQueryString(
        string ambiente, string metodo, string caminho, HttpStatusCode status)
    {
        using var factory = new ApiFactory();
        using var factoryConfigurada = factory.WithWebHostBuilder(builder => builder.UseEnvironment(ambiente));
        using var cliente = factoryConfigurada.CreateClient();
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), $"{caminho}?token={DadoSensivel}");

        using var resposta = await cliente.SendAsync(requisicao);

        using var problema = await VerificarProblemaAsync(resposta, status, caminho);
        VerificarAusenciaDeDetalhesInternos(problema.RootElement.GetRawText());
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Persistir_DeveOcultarFalhaRegistrarTraceIdEPreservarSaldoEHistorico(string ambiente)
    {
        var interceptador = new FalhaAoSalvarInterceptor();
        var banco = $"falha-persistencia-{Guid.NewGuid()}";
        using var logs = new ProvedorLogs();
        using var factory = new ApiFactory();
        using var factoryConfigurada = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(ambiente);
            builder.ConfigureLogging(opcoes => opcoes.AddProvider(logs));
            builder.ConfigureServices(servicos =>
            {
                servicos.RemoveAll<DbContextOptions<DesafioTargetDbContext>>();
                servicos.AddDbContext<DesafioTargetDbContext>(opcoes =>
                    opcoes.UseInMemoryDatabase(banco).AddInterceptors(interceptador));
            });
        });
        using var cliente = factoryConfigurada.CreateClient();
        var produtoAnterior = await cliente.GetFromJsonAsync<ProdutoEstoque>("/api/produtos/101");
        Assert.NotNull(produtoAnterior);
        interceptador.Ativo = true;

        using var resposta = await cliente.PostAsJsonAsync("/api/movimentacoes", new
        {
            codigoProduto = 101,
            tipo = "entrada",
            quantidade = 5,
            descricao = DadoSensivel
        });

        using var problema = await VerificarProblemaAsync(resposta, HttpStatusCode.InternalServerError, "/api/movimentacoes");
        Assert.Equal("Erro interno do servidor.", problema.RootElement.GetProperty("title").GetString());
        Assert.Equal(DetalheErroInterno, problema.RootElement.GetProperty("detail").GetString());
        VerificarAusenciaDeDetalhesInternos(problema.RootElement.GetRawText());
        var traceId = problema.RootElement.GetProperty("traceId").GetString();
        Assert.Contains(logs.Registros, registro =>
            registro.Nivel == LogLevel.Error &&
            ReferenceEquals(registro.Excecao, interceptador.Erro) &&
            registro.Propriedades.Any(propriedade =>
                string.Equals(propriedade.Key, "TraceId", StringComparison.OrdinalIgnoreCase) &&
                Equals(propriedade.Value?.ToString(), traceId)));
        Assert.Equal(produtoAnterior, await cliente.GetFromJsonAsync<ProdutoEstoque>("/api/produtos/101"));
        Assert.Empty((await cliente.GetFromJsonAsync<MovimentacaoEstoque[]>("/api/movimentacoes"))!);
    }

    private static async Task<JsonDocument> VerificarProblemaAsync(
        HttpResponseMessage resposta, HttpStatusCode status, string caminho)
    {
        Assert.Equal(status, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var raiz = problema.RootElement;
        Assert.Equal((int)status, raiz.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(raiz.GetProperty("type").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(raiz.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(raiz.GetProperty("detail").GetString()));
        Assert.Equal(caminho, raiz.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(raiz.GetProperty("traceId").GetString()));
        return problema;
    }

    private static void VerificarAusenciaDeDetalhesInternos(string json)
    {
        Assert.DoesNotContain(DadoSensivel, json);
        Assert.DoesNotContain("Exception", json);
        Assert.DoesNotContain("stackTrace", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.", json);
        Assert.DoesNotContain("JsonException", json);
        Assert.DoesNotContain("INSERT INTO", json);
        Assert.DoesNotContain("Password=", json);
    }

    private sealed class CalculadoraComFalhaDeValidacao : ICalculadoraComissao
    {
        public CalculoComissaoResponse Calcular(IEnumerable<Venda> vendas) =>
            throw new CalculoInvalidoException(new Dictionary<string, string[]>
            {
                ["vendas[0].valor"] = ["O valor informado não permite realizar o cálculo."]
            });
    }

    private sealed class FalhaAoSalvarInterceptor : SaveChangesInterceptor
    {
        public bool Ativo { get; set; }

        public DbUpdateException Erro { get; } = new(
            $"INSERT INTO MovimentacoesEstoque; Password={DadoSensivel}",
            new InvalidOperationException("Falha interna de persistência."));

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Ativo)
                throw Erro;

            return ValueTask.FromResult(result);
        }
    }

    private sealed record RegistroLog(
        LogLevel Nivel, Exception? Excecao, IReadOnlyList<KeyValuePair<string, object?>> Propriedades);

    private sealed class ProvedorLogs : ILoggerProvider
    {
        public ConcurrentQueue<RegistroLog> Registros { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturadorLogs(Registros);

        public void Dispose() { }

        private sealed class CapturadorLogs(ConcurrentQueue<RegistroLog> registros) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var propriedades = state is IEnumerable<KeyValuePair<string, object?>> valores
                    ? valores.ToArray()
                    : Array.Empty<KeyValuePair<string, object?>>();
                registros.Enqueue(new RegistroLog(logLevel, exception, propriedades));
            }
        }
    }
}
