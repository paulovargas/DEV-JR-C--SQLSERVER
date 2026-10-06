using DesafioTarget.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DesafioTarget.Api.IntegrationTests.Infrastructure;

public sealed class SqlServerApiFactory(BancoSqlServerTeste banco, params IInterceptor[] interceptadores)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:SqlServer", banco.ConnectionString);
        builder.ConfigureAppConfiguration((_, configuracao) =>
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = banco.ConnectionString
            }));
        builder.ConfigureLogging(opcoes => opcoes.ClearProviders());
        builder.ConfigureServices(servicos =>
        {
            servicos.RemoveAll<DbContextOptions<DesafioTargetDbContext>>();
            servicos.AddDbContext<DesafioTargetDbContext>(opcoes => opcoes
                .UseSqlServer(banco.ConnectionString)
                .AddInterceptors(interceptadores));
        });
    }
}
