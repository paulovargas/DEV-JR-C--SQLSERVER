# Desafio técnico — Target Sistemas

API REST em ASP.NET Core que resolve os três exercícios propostos: comissão de vendedores, movimentação de estoque e juros por atraso.

## Tecnologias

- .NET 8 e ASP.NET Core Minimal APIs
- Entity Framework Core e SQL Server para persistência do estoque
- `System.Text.Json` para leitura do arquivo de vendas e da carga inicial de produtos
- xUnit para testes automatizados

O estoque é persistido no SQL Server. Ao iniciar, a API aplica as migrations pendentes e inclui os produtos de `Data/estoque.json` quando a tabela de produtos está vazia.

## Organização do código

- `Program.cs`: configuração, injeção de dependências, middleware e composição da aplicação.
- `Endpoints/`: mapeamento das rotas de informações, comissões, estoque e juros, incluindo a tradução dos resultados para respostas HTTP.
- `Validation/`: validação das entradas, utilizada pelas calculadoras e pelo serviço de estoque.
- `Models/Comissoes/`, `Models/Estoque/` e `Models/Juros/`: requests, responses, enums e resultados por funcionalidade, com um arquivo por tipo.
- `Services/`: cálculos e operações de estoque; `Services/Interfaces/` reúne os contratos dos serviços.
- `Errors/`: respostas de erro padronizadas e tratamento centralizado de exceções.
- `Data/`: contexto EF Core e carga inicial; `Data/Entities/` contém as entidades de persistência em arquivos próprios.
- `Migrations/`: histórico de evolução do esquema.

## Como executar

É necessário ter um SDK compatível com [`global.json`](global.json): `8.0.100` ou um patch da família `8.0.1xx`, além de SQL Server acessível. A configuração portátil de um container e a alternativa com Compose externo estão no [README raiz](../README.md#sql-server-local). Também é possível usar uma instância SQL Server existente, ajustando servidor, porta e credenciais.

Em um terminal na raiz do repositório, entre em `backend`, configure a conexão e inicie a API:

```powershell
cd backend
$env:ConnectionStrings__SqlServer = 'Server=localhost,1433;Database=DesafioTarget;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True'
dotnet restore DesafioTarget.sln
dotnet run --project src/DesafioTarget.Api/DesafioTarget.Api.csproj
```

Substitua `SUA_SENHA` pela senha configurada na instância e aguarde o SQL Server aceitar conexões. A conexão deve permitir criar o banco, aplicar migrations e ler e gravar os dados. `appsettings.json` mantém a conexão vazia; a variável vale somente para o terminal atual e deve ser configurada novamente em outro terminal. `TrustServerCertificate=True` é utilizado neste exemplo local; não versione credenciais reais.

A API ficará disponível em `http://localhost:5080`, conforme o perfil de execução. O arquivo [`DesafioTarget.Api.http`](src/DesafioTarget.Api/DesafioTarget.Api.http) contém exemplos para todos os endpoints, incluindo validações. Os exemplos válidos de entrada e saída persistem alterações no banco; execute uma requisição por vez, conforme as pré-condições descritas no arquivo.

## Testes

Todos os comandos desta seção partem da pasta `backend`. Os testes não precisam de uma API em execução.

| Tipo | Projeto | Pré-requisitos e cobertura |
|---|---|---|
| Unitários e chamadas diretas | `tests/DesafioTarget.Api.Tests` | SDK e pacotes restaurados; calculadoras e validações. O serviço de estoque usa EF InMemory. |
| HTTP em memória | `tests/DesafioTarget.Api.Tests` | SDK e pacotes restaurados; host de testes com EF InMemory, rotas, JSON, códigos HTTP e erros. Não depende de SQL Server. |
| Integração SQL Server | `tests/DesafioTarget.Api.IntegrationTests` | SQL Server ativo e `DESAFIO_TARGET_SQLSERVER_TEST_CONNECTION` configurada; migrations, persistência, transações e concorrência reais. |

Para executar apenas os testes rápidos (unitários, diretos e HTTP):

```powershell
dotnet test tests/DesafioTarget.Api.Tests/DesafioTarget.Api.Tests.csproj
```

Para executar toda a solução:

```powershell
dotnet test DesafioTarget.sln
```

Sem a variável de conexão dos testes, os cenários SQL são marcados como ignorados; esse resultado não comprova transações ou concorrência no SQL Server. O banco InMemory é isolado por teste/host e não reproduz o comportamento relacional.

Para executar os testes de integração, mantenha o SQL Server ativo e configure uma conexão com permissão para criar e remover bancos. Na pasta `backend`:

```powershell
$env:DESAFIO_TARGET_SQLSERVER_TEST_CONNECTION = 'Server=localhost,1433;Database=master;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True'
dotnet test tests/DesafioTarget.Api.IntegrationTests/DesafioTarget.Api.IntegrationTests.csproj
```

Cada cenário gera um banco com o nome `DesafioTarget_IntegrationTests_` seguido de um GUID, aplica as migrations reais e inclui seus próprios dados. O catálogo informado na conexão é substituído pelo nome exclusivo; o banco da aplicação não é utilizado. Ao final, a limpeza valida o nome e o catálogo e remove somente o banco criado pelo cenário. A variável é utilizada apenas pelos testes de integração.

Os cenários verificam persistência conjunta de saldo e histórico, rollback de gravações anteriores ao commit, falha SQL inesperada, saídas concorrentes e deadlocks reais. Uma barreira assíncrona sincroniza duas transações após a leitura do saldo; o teste confirma o erro SQL `1205` e verifica os dados persistidos em outro contexto, sem depender de qual operação foi escolhida como vítima.

Para voltar a executar a solução sem os testes SQL nesse terminal:

```powershell
Remove-Item Env:DESAFIO_TARGET_SQLSERVER_TEST_CONNECTION -ErrorAction SilentlyContinue
```

Após restaurar e compilar, `--no-restore` evita repetir o restore; `--no-build` também dispensa a compilação e deve ser usado somente quando o código compilado estiver atualizado.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/` | Informa o nome da aplicação e as rotas funcionais |
| `POST` | `/api/comissoes/calcular` | Calcula e agrupa as comissões por vendedor |
| `GET` | `/api/produtos` | Lista os produtos e seus saldos atuais |
| `GET` | `/api/produtos/{codigoProduto}` | Consulta um produto |
| `POST` | `/api/movimentacoes` | Registra uma entrada ou saída de estoque |
| `GET` | `/api/movimentacoes` | Lista as movimentações persistidas |
| `GET` | `/api/movimentacoes/{id}` | Consulta uma movimentação pelo identificador |
| `POST` | `/api/juros/calcular` | Calcula juros simples até a data atual |

## Contratos da API

Envie os corpos de requisição como `application/json`. As respostas de sucesso usam `application/json`, propriedades em camelCase e enums como texto. Erros usam o contrato descrito em [Validações e respostas HTTP](#validações-e-respostas-http).

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

A lista `vendas` é obrigatória, não pode ser vazia nem conter itens nulos. Cada vendedor deve conter texto; espaços externos são removidos no agrupamento, que ignora diferenças entre maiúsculas e minúsculas. Cada valor deve ser positivo e respeitar os limites monetários individuais e da soma.

As faixas são aplicadas individualmente a cada venda:

- valor menor que R$ 100,00: 0%;
- valor a partir de R$ 100,00 e menor que R$ 500,00: 1%;
- valor a partir de R$ 500,00: 5%.

Os cálculos usam `decimal`. Comissões e juros compartilham a política `ArredondamentoMonetario`, com duas casas decimais e `MidpointRounding.AwayFromZero`. A comissão é somada sem arredondamento intermediário e o total de cada vendedor é arredondado ao final. Para o JSON completo do desafio, o resultado é:

| Vendedor | Comissão |
|---|---:|
| João Silva | R$ 495,68 |
| Maria Souza | R$ 465,95 |
| Carlos Oliveira | R$ 379,37 |
| Ana Lima | R$ 404,98 |

A resposta contém `vendedores` (vendedor, quantidade de vendas, total vendido e comissão) e `comissaoTotalGeral`; o total do arquivo do desafio é R$ 1.745,98.

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

O campo `tipo` é obrigatório e aceita `entrada` ou `saida`. Valores ausentes, nulos, desconhecidos ou numéricos retornam HTTP `400 Bad Request`, sem alterar saldo ou histórico. Código e quantidade devem ser inteiros positivos, representáveis em `int` (máximo `2147483647`). O identificador da movimentação é gerado pelo SQL Server e representado como `long`.

Uma criação retorna `201 Created`, cabeçalho `Location: /api/movimentacoes/{id}` e JSON com identificador, produto, tipo, quantidade, descrição, saldo anterior, saldo final e `realizadaEm` em UTC. Uma saída maior que o saldo retorna `409 Conflict` e não altera o produto. Uma entrada que ultrapasse o saldo máximo de `2147483647` retorna `400` com `detail`, sem erros por campo. Não há endpoint para desfazer uma movimentação.

As alterações e o histórico permanecem no banco após reiniciar a API. A operação usa transação serializável para manter o saldo e o histórico consistentes durante movimentações simultâneas.

O serviço valida código do produto, tipo, quantidade positiva e descrição não vazia de até 500 caracteres após remover espaços externos, inclusive em chamadas diretas. Dados inválidos retornam um resultado com erros por campo, convertido pelo endpoint em HTTP `400`, antes de acessar ou alterar o banco. Entradas que ultrapassem o limite de `int` são rejeitadas; uma saída igual ao saldo é permitida e zera o estoque.

Quando o SQL Server identifica um deadlock e desfaz uma das transações, a API retorna `409 Conflict` com a mensagem "Outra operação de estoque ocorreu ao mesmo tempo. Tente novamente.". O handler reconhece o código SQL `1205` na cadeia de exceções e registra o conflito com `traceId`. A operação rejeitada pode ser reenviada pelo usuário; sua tentativa anterior não altera saldo ou histórico. Os demais erros de banco usam o tratamento genérico de `500`. O desfazimento da transação vítima é descrito no [guia de deadlocks da Microsoft](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-deadlocks-guide?view=sql-server-ver15).

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

A data de vencimento é obrigatória no formato `yyyy-MM-dd`; a data padrão `0001-01-01` é rejeitada. O request aceita valor positivo e data de vencimento; a data de cálculo é definida pela data local do servidor. Vencimentos no dia atual ou no futuro retornam zero dia de atraso e zero de juros. A resposta contém valor original, vencimento, data de cálculo, dias de atraso, taxa diária percentual (`2.5`), juros e valor atualizado. Os valores monetários da resposta são arredondados para duas casas decimais.

### Limites dos cálculos monetários

O limite técnico é `792281625142643375935439503`, definido em `LimitesMonetarios.ValorMaximo` como a parte inteira de `decimal.MaxValue / 100`. Essa reserva permite representar centavos nos valores retornados.

- Cada valor de venda ou principal de juros deve ser positivo e não ultrapassar esse limite.
- A soma de todas as vendas de uma requisição, mesmo de vendedores diferentes, deve respeitar o mesmo limite. A validação verifica o saldo disponível do limite antes de somar.
- Os juros e o valor atualizado, após arredondamento, também devem respeitar o limite. Um principal aceito pode ser rejeitado quando o período de atraso produz um resultado acima do limite.

As calculadoras aplicam essas validações também em chamadas diretas e sinalizam rejeições com `CalculoInvalidoException`, contendo erros por campo. O tratamento centralizado converte essa exceção em HTTP `400 Bad Request`. Overflow aritmético nos juros é convertido na mesma rejeição, sem expor a exceção interna. As faixas de comissão, os juros simples de 2,5% ao dia e a política de arredondamento permanecem iguais.

## Validações e respostas HTTP

- Campos obrigatórios ausentes, valores não positivos, descrições vazias ou acima de 500 caracteres normalizados e cálculos acima dos limites retornam `400 Bad Request`.
- No cálculo de comissões, listas ausentes, nulas ou vazias e itens nulos também retornam `400 Bad Request`. Os erros de cada venda indicam seu índice na lista.
- Produto ou movimentação inexistente retorna `404 Not Found`.
- Saída sem saldo suficiente ou deadlock de concorrência retorna `409 Conflict`.
- Uma movimentação criada retorna `201 Created` com a URL para consulta no cabeçalho `Location`.
- Método não permitido retorna `405`; corpo com tipo de conteúdo não suportado retorna `415`.

Os erros HTTP da API usam `application/problem+json`, com `type`, `title`, `status`, `detail`, `instance` e `traceId`. As validações das regras incluem `errors`, um objeto de arrays de mensagens por campo. Os índices das vendas começam em zero. `instance` informa somente o caminho, sem incluir parâmetros de consulta. Erros de binding do JSON e overflow do saldo retornam `400` seguro com `detail`, sem exigir `errors`.

Exemplo de lista de vendas vazia:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Dados inválidos.",
  "status": 400,
  "detail": "Corrija os campos indicados e tente novamente.",
  "instance": "/api/comissoes/calcular",
  "traceId": "identificador-da-requisicao",
  "errors": {
    "vendas": ["A lista de vendas deve conter pelo menos uma venda."]
  }
}
```

Exemplo de conflito por estoque insuficiente:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflito na operação.",
  "status": 409,
  "detail": "Estoque insuficiente. Saldo atual: 150.",
  "instance": "/api/movimentacoes",
  "traceId": "identificador-da-requisicao"
}
```

Falhas inesperadas retornam `500` com mensagem genérica, inclusive em desenvolvimento. A resposta não inclui stack trace, SQL ou detalhes internos. O servidor registra a exceção com método, caminho e o mesmo `traceId` da resposta para permitir investigação. JSON inválido, corpo ausente e erros de desserialização são tratados como falhas de requisição com mensagem segura; rotas inexistentes e métodos não permitidos também recebem o contrato padronizado.

Os testes rápidos cobrem faixas de comissão, agrupamento e totais do arquivo, arredondamento de meio centavo, limites monetários individuais e agregados, dados inválidos, estoque insuficiente e overflow. Os testes HTTP conferem os contratos de erro e o logging com `traceId`, inclusive falhas simuladas em desenvolvimento e produção.

A garantia transacional é verificada separadamente no SQL Server: uma falha provocada após saldo e histórico serem gravados, mas antes do commit, deve desfazer ambas as gravações. Outro cenário provoca uma violação de constraint somente no banco exclusivo e confirma SQL `547`, resposta `500` segura e rollback. Os testes de concorrência verificam saldo e histórico finais e tratamento controlado dos deadlocks.
