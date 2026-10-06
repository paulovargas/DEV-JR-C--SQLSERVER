# Frontend — Desafio Target

Interface Angular para a API do desafio técnico.

## Requisitos

- Node.js 22, a partir da versão 22.12, e npm compatível com o Node instalado.
- Backend disponível em `http://localhost:5080` para usar as funcionalidades na interface. Testes e build não dependem da API nem de SQL Server.

## Executar

Em um terminal na raiz do repositório:

```powershell
cd frontend
npm.cmd ci
npm.cmd start
```

A aplicação ficará disponível em `http://localhost:4200`. Os demais comandos deste README devem ser executados dentro de `frontend`.

O comando `npm.cmd ci` instala as versões do `package-lock.json`. `npm.cmd start` cria o build e usa o servidor local [`dev-server.mjs`](dev-server.mjs) para encaminhar `/api` ao backend. Isso mantém todas as requisições na mesma origem e funciona mesmo quando o caminho do projeto contém `#`. Esse servidor não recompila ao editar arquivos; encerre-o com `Ctrl+C` e execute `npm.cmd start` novamente após uma alteração.

Em um caminho sem caracteres especiais, também é possível usar o servidor com recarga automática do Angular:

```powershell
npm.cmd run start:angular
```

O servidor Angular usa [`proxy.conf.json`](proxy.conf.json). No servidor `dev-server.mjs`, as variáveis opcionais `PORT`, `API_HOST` e `API_PORT` alteram os padrões `4200`, `127.0.0.1` e `5080`, respectivamente; elas não configuram o servidor Angular.

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

Os testes de comissões usam a mesma compilação em memória e API simulada. Verificam importação de arquivos válidos e inválidos, JSON malformado, falhas de leitura, carregamento do exemplo e limpeza da seleção. A validação dos dados é aplicada uma única vez tanto na importação quanto no carregamento do exemplo, preservando as mensagens de cada fluxo.

## Validações e erros

O helper prioriza mensagens válidas de `errors`, depois `detail` e `title`, e mantém compatibilidade com o antigo campo `erro`. Falhas inesperadas exibem a mensagem genérica enviada pela API. Quando o servidor `dev-server.mjs` não consegue conectar ao backend, retorna HTTP `502` com `application/problem+json`, `traceId` e caminho sem parâmetros de consulta.

- Comissões: o arquivo deve ser JSON com uma lista `vendas` não vazia, vendedor com texto e valor numérico finito e positivo. A API valida também os limites monetários individuais e da soma.
- Estoque: código e quantidade devem ser positivos, a quantidade deve ser inteira e a descrição deve ter texto e até 500 caracteres após remover espaços externos. O envio normaliza a descrição e fica bloqueado durante carregamento ou salvamento. Saldo insuficiente e conflitos de concorrência são informados pela API.
- Juros: o formulário exige valor a partir de `0,01` e data de vencimento. A API valida a data e os limites do cálculo; dias de atraso e juros são calculados conforme a data local do servidor.

As regras do formulário e as da API têm validações próprias. O limite monetário técnico completo e os contratos HTTP estão no [README do backend](../backend/README.md#contratos-da-api). O frontend usa `number` do JavaScript; para conferir os limites técnicos de `decimal` sem arredondamento prévio pelo JavaScript, os [exemplos HTTP](../backend/src/DesafioTarget.Api/DesafioTarget.Api.http) enviam os números como JSON textual.

## Estrutura

- `core/models`: contratos TypeScript equivalentes às requisições e respostas da API.
- `core/services`: clientes HTTP separados por exercício.
- `features/comissoes`: upload do JSON e resumo por vendedor.
- `features/estoque`: saldos, formulário de movimentação e histórico.
- `features/juros`: formulário e detalhamento do cálculo.

O projeto usa componentes standalone, rotas carregadas sob demanda, Reactive Forms e `HttpClient`. Não há dependências visuais externas.
