using DesafioTarget.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DesafioTarget.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _banco = $"api-http-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(servicos =>
        {
            servicos.RemoveAll<DbContextOptions<DesafioTargetDbContext>>();
            servicos.AddDbContext<DesafioTargetDbContext>(opcoes => opcoes.UseInMemoryDatabase(_banco));
        });
    }
}
