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

## Testes

```powershell
npm.cmd test
```

Os testes usam o runner nativo do Node, sem dependências adicionais. Verificam as mensagens de validação por campo, `detail`, `title`, compatibilidade com o antigo campo `erro`, falhas de conexão e respostas malformadas. Também verificam o contrato `502` do proxy em um processo isolado com portas efêmeras.

Os testes do componente de estoque usam a API simulada e verificam descrição vazia ou composta apenas por espaços, os limites de 500 e 501 caracteres após remover espaços externos, normalização do envio e bloqueio durante carregamento ou salvamento. Também conferem atualização após sucesso e nova tentativa após falha. O componente real é compilado em memória com o esbuild já utilizado pelo Angular; esses testes não acessam o banco nem renderizam o template.

O helper prioriza mensagens válidas de `errors`, depois `detail` e `title`. Falhas inesperadas exibem a mensagem genérica enviada pela API. Quando o proxy não consegue conectar ao backend, retorna `application/problem+json` com `traceId` e caminho sem parâmetros de consulta.

## Estrutura

- `core/models`: contratos TypeScript equivalentes às requisições e respostas da API.
- `core/services`: clientes HTTP separados por exercício.
- `features/comissoes`: upload do JSON e resumo por vendedor.
- `features/estoque`: saldos, formulário de movimentação e histórico.
- `features/juros`: formulário e detalhamento do cálculo.

O projeto usa componentes standalone, rotas carregadas sob demanda, Reactive Forms e `HttpClient`. Não há dependências visuais externas.

No formulário de estoque, a descrição deve conter texto e ter até 500 caracteres após remover espaços externos. O contador segue essa regra e o envio normaliza a descrição. Novos registros ficam bloqueados enquanto os dados carregam ou uma movimentação está sendo salva.
