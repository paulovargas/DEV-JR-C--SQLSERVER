# Desafio técnico — Target Sistemas

Solução full stack para os três exercícios do desafio técnico.

- [`backend`](backend/README.md): API REST em ASP.NET Core e testes xUnit.
- [`frontend`](frontend/README.md): interface Angular para consumir a API.

## Executar o projeto

Abra dois terminais na raiz deste repositório.

No primeiro, inicie a API:

```powershell
cd backend
dotnet run --project src/DesafioTarget.Api
```

No segundo, instale as dependências e inicie o Angular:

```powershell
cd frontend
npm.cmd install
npm.cmd start
```

Acesse `http://localhost:4200`. O servidor local do frontend encaminha as chamadas `/api` para `http://localhost:5080`.

## Funcionalidades

- Cálculo de comissão por vendedor a partir de um arquivo JSON.
- Consulta de produtos, entradas, saídas e histórico de estoque.
- Cálculo de juros simples de 2,5% ao dia.
- Validação no cliente e no servidor, com mensagens para erros da API.
- Layout responsivo para computador e celular.
