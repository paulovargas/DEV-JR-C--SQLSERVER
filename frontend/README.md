# Frontend — Desafio Target

Interface Angular para a API do desafio técnico.

## Requisitos

- Node.js 22.12 ou superior
- npm 11 ou compatível
- Backend disponível em `http://localhost:5080`

## Executar

```powershell
npm.cmd install
npm.cmd start
```

A aplicação ficará disponível em `http://localhost:4200`.

O comando `npm start` cria o build e usa o servidor local [`dev-server.mjs`](dev-server.mjs) para encaminhar `/api` ao backend. Isso mantém todas as requisições na mesma origem e funciona mesmo quando o caminho do projeto contém `#`.

Em um caminho sem caracteres especiais, também é possível usar o servidor com recarga automática do Angular:

```powershell
npm.cmd run start:angular
```

## Build de produção

```powershell
npm.cmd run build
```

Os arquivos gerados ficam em `dist/desafio-target/browser`.

## Estrutura

- `core/models`: contratos TypeScript equivalentes às requisições e respostas da API.
- `core/services`: clientes HTTP separados por exercício.
- `features/comissoes`: upload do JSON e resumo por vendedor.
- `features/estoque`: saldos, formulário de movimentação e histórico.
- `features/juros`: formulário e detalhamento do cálculo.

O projeto usa componentes standalone, rotas carregadas sob demanda, Reactive Forms e `HttpClient`. Não há dependências visuais externas.
