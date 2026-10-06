using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DesafioTarget.Api.Models.Comissoes;
using DesafioTarget.Api.Models.Juros;
using DesafioTarget.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DesafioTarget.Api.Tests;

public sealed class LimitesMonetariosEndpointTests : IClassFixture<ApiFactory>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _cliente;
    private readonly DateOnly _hoje = new(2026, 10, 6);

    public LimitesMonetariosEndpointTests(ApiFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(servicos =>
        {
            servicos.RemoveAll<TimeProvider>();
            servicos.AddSingleton<TimeProvider>(new RelogioFixo());
        }));
        _cliente = _factory.CreateClient();
    }

    public static IEnumerable<object[]> ValoresAcimaDoLimite()
    {
        yield return [LimitesMonetarios.ValorMaximo + 0.01m];
        yield return [decimal.MaxValue];
    }

    public static IEnumerable<object[]> ValoresJurosInvalidos()
    {
        yield return [0m];
        yield return [-1m];
        yield return [LimitesMonetarios.ValorMaximo + 0.01m];
        yield return [decimal.MaxValue];
    }

    [Fact]
    public async Task Comissoes_DeveAceitarVendaNoLimiteEManterCentavosDaComissao()
    {
        using var resposta = await _cliente.PostAsJsonAsync("/api/comissoes/calcular", new
        {
            vendas = new[] { new Venda("Maria", LimitesMonetarios.ValorMaximo) }
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<CalculoComissaoResponse>();
        Assert.NotNull(resultado);
        var vendedor = Assert.Single(resultado.Vendedores);
        Assert.Equal(LimitesMonetarios.ValorMaximo, vendedor.ValorTotalVendas);
        Assert.Equal(39614081257132168796771975.15m, vendedor.ComissaoTotal);
        Assert.Equal(vendedor.ComissaoTotal, resultado.ComissaoTotalGeral);
    }

    [Theory]
    [MemberData(nameof(ValoresAcimaDoLimite))]
    public async Task Comissoes_DeveTraduzirValorAcimaDoLimiteParaHttp400(decimal valor)
    {
        using var resposta = await _cliente.PostAsJsonAsync("/api/comissoes/calcular", new
        {
            vendas = new[] { new Venda("Maria", valor) }
        });

        await VerificarValidacaoAsync(resposta, "vendas[0].valor");
    }

    [Theory]
    [InlineData("Maria")]
    [InlineData("Joao")]
    public async Task Comissoes_DeveRejeitarSomaAcimaDoLimiteGlobal(string segundoVendedor)
    {
        using var resposta = await _cliente.PostAsJsonAsync("/api/comissoes/calcular", new
        {
            vendas = new[]
            {
                new Venda("Maria", LimitesMonetarios.ValorMaximo - 0.01m),
                new Venda(segundoVendedor, 0.02m)
            }
        });

        await VerificarValidacaoAsync(resposta, "vendas");
    }

    [Fact]
    public async Task Comissoes_DeveResponderHttp400QuandoASomaExcederiaDecimal()
    {
        using var resposta = await _cliente.PostAsJsonAsync("/api/comissoes/calcular", new
        {
            vendas = Enumerable.Range(0, 101)
                .Select(indice => new Venda($"Vendedor {indice}", LimitesMonetarios.ValorMaximo))
                .ToArray()
        });

        await VerificarValidacaoAsync(resposta, "vendas");
    }

    [Fact]
    public async Task Comissoes_DeveAceitarSomaExatamenteNoLimite()
    {
        using var resposta = await _cliente.PostAsJsonAsync("/api/comissoes/calcular", new
        {
            vendas = new[]
            {
                new Venda("Maria", LimitesMonetarios.ValorMaximo / 2m),
                new Venda("Maria", LimitesMonetarios.ValorMaximo / 2m)
            }
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<CalculoComissaoResponse>();
        Assert.NotNull(resultado);
        Assert.Equal(LimitesMonetarios.ValorMaximo, Assert.Single(resultado.Vendedores).ValorTotalVendas);
    }

    [Theory]
    [MemberData(nameof(ValoresJurosInvalidos))]
    public async Task Juros_DeveRetornarValidacaoParaValoresInvalidos(decimal valor)
    {
        using var resposta = await _cliente.PostAsJsonAsync(
            "/api/juros/calcular",
            new CalculoJurosRequest(valor, _hoje));

        await VerificarValidacaoAsync(resposta, "valor");
    }

    [Fact]
    public async Task Juros_DeveRejeitarDataDeVencimentoAusente()
    {
        using var resposta = await _cliente.PostAsJsonAsync("/api/juros/calcular", new { valor = 100m });

        await VerificarValidacaoAsync(resposta, "dataVencimento");
    }

    [Fact]
    public async Task Juros_DeveAceitarValorMaximoSemAtraso()
    {
        using var resposta = await _cliente.PostAsJsonAsync(
            "/api/juros/calcular",
            new CalculoJurosRequest(LimitesMonetarios.ValorMaximo, _hoje));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<CalculoJurosResponse>();
        Assert.NotNull(resultado);
        Assert.Equal(_hoje, resultado.DataCalculo);
        Assert.Equal(0, resultado.DiasAtraso);
        Assert.Equal(0m, resultado.ValorJuros);
        Assert.Equal(LimitesMonetarios.ValorMaximo, resultado.ValorAtualizado);
    }

    [Fact]
    public async Task Juros_DeveRejeitarValorAtualizadoAcimaDoLimite()
    {
        using var resposta = await _cliente.PostAsJsonAsync(
            "/api/juros/calcular",
            new CalculoJurosRequest(LimitesMonetarios.ValorMaximo, _hoje.AddDays(-1)));

        await VerificarValidacaoAsync(resposta, "valor");
    }

    [Fact]
    public async Task Juros_DeveTraduzirOverflowFisicoParaHttp400()
    {
        using var resposta = await _cliente.PostAsJsonAsync(
            "/api/juros/calcular",
            new CalculoJurosRequest(LimitesMonetarios.ValorMaximo, DateOnly.MinValue.AddDays(1)));

        await VerificarValidacaoAsync(resposta, "valor");
    }

    [Fact]
    public async Task Juros_DevePreservarCalculoNormalComRelogioInjetado()
    {
        using var resposta = await _cliente.PostAsJsonAsync(
            "/api/juros/calcular",
            new CalculoJurosRequest(100m, new DateOnly(2026, 10, 1)));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<CalculoJurosResponse>();
        Assert.NotNull(resultado);
        Assert.Equal(_hoje, resultado.DataCalculo);
        Assert.Equal(5, resultado.DiasAtraso);
        Assert.Equal(12.50m, resultado.ValorJuros);
        Assert.Equal(112.50m, resultado.ValorAtualizado);
    }

    [Fact]
    public async Task Juros_DeveAceitarValorAtualizadoExatamenteNoLimite()
    {
        using var resposta = await _cliente.PostAsJsonAsync(
            "/api/juros/calcular",
            new CalculoJurosRequest(LimitesMonetarios.ValorMaximo / 2m, _hoje.AddDays(-40)));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<CalculoJurosResponse>();
        Assert.NotNull(resultado);
        Assert.Equal(40, resultado.DiasAtraso);
        Assert.Equal(LimitesMonetarios.ValorMaximo, resultado.ValorAtualizado);
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _factory.Dispose();
    }

    private static async Task VerificarValidacaoAsync(HttpResponseMessage resposta, string campo)
    {
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        using var problema = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(400, problema.RootElement.GetProperty("status").GetInt32());
        var mensagens = problema.RootElement.GetProperty("errors").GetProperty(campo);
        Assert.NotEmpty(mensagens.EnumerateArray());
        Assert.False(string.IsNullOrWhiteSpace(mensagens[0].GetString()));
    }

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
