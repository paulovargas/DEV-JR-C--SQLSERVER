using Xunit;

namespace DesafioTarget.Api.IntegrationTests.Infrastructure;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(BancoSqlServerTeste.VariavelConexao)))
            Skip = $"Configure {BancoSqlServerTeste.VariavelConexao} para executar os testes com SQL Server.";
    }
}
