# Desafio técnico — Target Sistemas

Solução full stack para os três exercícios do desafio técnico.

- [`backend`](backend/README.md): API REST em ASP.NET Core e testes xUnit.
- [`frontend`](frontend/README.md): interface Angular para consumir a API.

## Executar o projeto

Os comandos abaixo usam PowerShell e partem da raiz deste repositório. São necessários:

- SDK .NET compatível com [`backend/global.json`](backend/global.json): versão `8.0.100` ou um patch da família `8.0.1xx`.
- Node.js 22, a partir da versão 22.12, e npm compatível com o Node instalado.
- SQL Server acessível pela API. Docker Desktop com containers Linux é uma opção para o desenvolvimento local.

### SQL Server local

Se já houver uma instância do SQL Server, utilize seu servidor, porta e credenciais na conexão da API. Para criar um container de desenvolvimento pela primeira vez, com a porta 1433 livre:

```powershell
$env:MSSQL_SA_PASSWORD = 'SUA_SENHA_FORTE'
docker run --name desafio-target-sqlserver `
  --env ACCEPT_EULA=Y --env MSSQL_SA_PASSWORD `
  --publish 127.0.0.1:1433:1433 `
  --volume desafio-target-sqlserver-data:/var/opt/mssql `
  --detach mcr.microsoft.com/mssql/server:2022-latest
```

Substitua o exemplo por uma senha com pelo menos oito caracteres e três categorias entre maiúsculas, minúsculas, números e símbolos. O volume conserva os dados entre execuções. Os parâmetros seguem o [guia da Microsoft para SQL Server em Docker](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver16&tabs=cli) e sua [configuração de volumes](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-docker-container-configure?view=sql-server-ver16).

Nas execuções seguintes, inicie o container já criado:

```powershell
docker start desafio-target-sqlserver
```

Se sua infraestrutura já usa um arquivo Compose externo, informe o caminho desse arquivo, sem mudar a pasta do terminal:

```powershell
docker compose -f 'C:\caminho\da\infraestrutura\compose.yaml' up -d
```

Nesse caso, mantenha o `.env` junto da infraestrutura e utilize a senha configurada nele. Aguarde o SQL Server aceitar conexões antes de iniciar a API. Não versione senhas ou conexões reais.

### API

Em um terminal na raiz do repositório:

```powershell
cd backend
$env:ConnectionStrings__SqlServer = 'Server=localhost,1433;Database=DesafioTarget;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True'
dotnet restore DesafioTarget.sln
dotnet run --project src/DesafioTarget.Api/DesafioTarget.Api.csproj
```

Substitua `SUA_SENHA` pela senha da instância. A variável vale para esse terminal; configure-a novamente em um novo terminal. A API aplica as migrations e inclui os produtos iniciais quando a tabela de produtos está vazia; a conexão precisa permitir essas operações. A API fica em `http://localhost:5080`. `TrustServerCertificate=True` é utilizado neste exemplo de conexão local.

### Frontend

Abra outro terminal na raiz do repositório:

```powershell
cd frontend
npm.cmd ci
npm.cmd start
```

Acesse `http://localhost:4200`. O servidor local do frontend encaminha as chamadas `/api` para `http://localhost:5080`. `npm.cmd start` gera o build antes de servir os arquivos; após editar o frontend, reinicie-o para gerar um novo build. As instruções de desenvolvimento com recarga automática estão no [README do frontend](frontend/README.md).

## Validar o projeto

Em um terminal na raiz do repositório, execute os testes rápidos do backend e os testes e build do frontend:

```powershell
Push-Location backend
try {
  dotnet test tests/DesafioTarget.Api.Tests/DesafioTarget.Api.Tests.csproj
} finally {
  Pop-Location
}

Push-Location frontend
try {
  npm.cmd test
  npm.cmd run build
} finally {
  Pop-Location
}
```

As dependências do frontend devem estar instaladas. Esses testes não precisam de uma API em execução nem de SQL Server. Os testes rápidos do backend cobrem calculadoras, serviço de estoque com EF InMemory e endpoints HTTP hospedados em memória. InMemory não comprova transações ou concorrência relacional.

Os testes de integração com SQL Server exigem uma conexão separada em `DESAFIO_TARGET_SQLSERVER_TEST_CONNECTION`, com permissão para criar e remover bancos exclusivos de teste. Veja o [comando e os pré-requisitos no README do backend](backend/README.md#testes). Sem essa variável, `dotnet test DesafioTarget.sln`, na pasta `backend`, executa os testes rápidos e ignora os testes SQL.

Os [exemplos HTTP](backend/src/DesafioTarget.Api/DesafioTarget.Api.http) documentam chamadas válidas e rejeições esperadas. Os exemplos válidos de movimentação gravam saldo e histórico; execute-os em um ambiente de desenvolvimento destinado a essas alterações. Os limites de entrada e o contrato `ProblemDetails` estão no [README do backend](backend/README.md#contratos-da-api).

## Funcionalidades

- Cálculo de comissão por vendedor a partir de um arquivo JSON.
- Consulta de produtos, entradas, saídas e histórico de estoque.
- Cálculo de juros simples de 2,5% ao dia.
- Validação no cliente e no servidor, com mensagens para erros da API.
- Layout responsivo para computador e celular.
