using Xunit;

namespace DesafioTarget.Api.IntegrationTests.Infrastructure;

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(BancoSqlServerTeste.VariavelConexao)))
            Skip = $"Configure {BancoSqlServerTeste.VariavelConexao} para executar os testes com SQL Server.";
    }
}
