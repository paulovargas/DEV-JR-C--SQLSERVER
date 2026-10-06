# Desafio técnico — Target Sistemas

API REST em ASP.NET Core que resolve os três exercícios propostos: comissão de vendedores, movimentação de estoque e juros por atraso.

## Tecnologias

- .NET 8 e ASP.NET Core Minimal APIs
- Entity Framework Core e SQL Server para persistência do estoque
- `System.Text.Json` para leitura do arquivo de vendas e da carga inicial de produtos
- xUnit para testes automatizados

O estoque é persistido no SQL Server. Na primeira execução, a API aplica as migrations e inclui no banco os produtos de `Data/estoque.json`.

## Como executar

É necessário ter o SDK do .NET 8 e o SQL Server em execução. O container local está configurado em `C:\projetos\sqlserver-desafio-target` e pode ser iniciado com `docker compose up -d` nessa pasta.

Dentro da pasta `backend`, configure a conexão com a senha definida no arquivo `.env` do container e inicie a API:

```powershell
$env:ConnectionStrings__SqlServer = "Server=localhost,1433;Database=DesafioTarget;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True"
dotnet restore
dotnet run --project src/DesafioTarget.Api
```

A API ficará disponível em `http://localhost:5080`. O arquivo [`DesafioTarget.Api.http`](src/DesafioTarget.Api/DesafioTarget.Api.http) contém exemplos prontos para todos os endpoints.

Para executar os testes:

```powershell
dotnet test
```

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/comissoes/calcular` | Calcula e agrupa as comissões por vendedor |
| `GET` | `/api/produtos` | Lista os produtos e seus saldos atuais |
| `GET` | `/api/produtos/{codigoProduto}` | Consulta um produto |
| `POST` | `/api/movimentacoes` | Registra uma entrada ou saída de estoque |
| `GET` | `/api/movimentacoes` | Lista as movimentações persistidas |
| `GET` | `/api/movimentacoes/{id}` | Consulta uma movimentação pelo identificador |
| `POST` | `/api/juros/calcular` | Calcula juros simples até a data atual |

### Comissões

O corpo segue o mesmo formato do enunciado:

```json
{
  "vendas": [
    { "vendedor": "João Silva", "valor": 1200.50 },
    { "vendedor": "João Silva", "valor": 250.30 }
  ]
}
```

As faixas são aplicadas individualmente a cada venda:

- valor menor que R$ 100,00: 0%;
- valor a partir de R$ 100,00 e menor que R$ 500,00: 1%;
- valor a partir de R$ 500,00: 5%.

Os cálculos usam `decimal`. A comissão é somada sem arredondamento intermediário e o total de cada vendedor é arredondado para duas casas decimais com `MidpointRounding.AwayFromZero`. Para o JSON completo do desafio, o resultado é:

| Vendedor | Comissão |
|---|---:|
| João Silva | R$ 495,68 |
| Maria Souza | R$ 465,95 |
| Carlos Oliveira | R$ 379,37 |
| Ana Lima | R$ 404,98 |

### Estoque

Exemplo de entrada:

```json
{
  "codigoProduto": 101,
  "tipo": "entrada",
  "quantidade": 25,
  "descricao": "Compra do fornecedor"
}
```

O campo `tipo` aceita `entrada` ou `saida`. O identificador é numérico e gerado pelo SQL Server. A resposta informa o saldo anterior e o saldo final. Uma saída maior que o saldo retorna HTTP `409 Conflict` e não altera o produto.

As alterações e o histórico permanecem no banco após reiniciar a API. A operação usa transação serializável para manter o saldo e o histórico consistentes durante movimentações simultâneas.

### Juros

Exemplo de entrada:

```json
{
  "valor": 100.00,
  "dataVencimento": "2026-10-01"
}
```

Como o enunciado não define capitalização, foi adotado juro simples diário:

```text
juros = valor × 0,025 × dias de atraso
```

A data de cálculo é a data local do servidor. Vencimentos no dia atual ou no futuro retornam zero dia de atraso e zero de juros. Os valores monetários da resposta são arredondados para duas casas decimais.

## Validações e respostas HTTP

- Campos ausentes, valores não positivos e descrições vazias retornam `400 Bad Request`.
- Produto ou movimentação inexistente retorna `404 Not Found`.
- Saída sem saldo suficiente retorna `409 Conflict`.
- Uma movimentação criada retorna `201 Created` com a URL para consulta no cabeçalho `Location`.

Os testes cobrem os limites de R$ 100,00 e R$ 500,00, os totais do JSON fornecido, agrupamento de vendedores, entrada e saída, saldo insuficiente, concorrência e cálculo de juros com e sem atraso.
