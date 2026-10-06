import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import http from 'node:http';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';

test('retorna ProblemDetails sem expor a query quando a API interrompe a conexão', { timeout: 10000 }, async contexto => {
  const api = http.createServer(request => request.socket.destroy());
  contexto.after(async () => {
    api.closeAllConnections();
    await new Promise(resolve => api.close(resolve));
  });

  api.listen(0, '127.0.0.1');
  await once(api, 'listening');
  const apiPort = api.address().port;

  const reserva = http.createServer();
  reserva.listen(0, '127.0.0.1');
  await once(reserva, 'listening');
  const frontendPort = reserva.address().port;
  await new Promise(resolve => reserva.close(resolve));

  const proxy = spawn(process.execPath, [fileURLToPath(new URL('../dev-server.mjs', import.meta.url))], {
    env: { ...process.env, PORT: String(frontendPort), API_HOST: '127.0.0.1', API_PORT: String(apiPort) },
    stdio: ['ignore', 'pipe', 'pipe']
  });

  contexto.after(async () => {
    if (proxy.exitCode === null) {
      const encerramento = once(proxy, 'exit');
      proxy.kill();
      await encerramento;
    }
  });

  let erroInicializacao = '';
  proxy.stderr.on('data', trecho => { erroInicializacao += trecho; });

  await Promise.race([
    once(proxy.stdout, 'data', { signal: AbortSignal.timeout(5000) }),
    once(proxy, 'exit').then(([codigo]) => {
      throw new Error(`Proxy de teste encerrou com código ${codigo}: ${erroInicializacao}`);
    })
  ]);

  const resposta = await fetch(`http://127.0.0.1:${frontendPort}/api/estoque/produtos?token=segredo-teste`);
  const problema = await resposta.json();

  assert.equal(resposta.status, 502);
  assert.match(resposta.headers.get('content-type'), /^application\/problem\+json/);
  assert.equal(problema.type, 'about:blank');
  assert.equal(problema.title, 'Não foi possível conectar à API.');
  assert.equal(problema.status, 502);
  assert.equal(problema.detail, `Não foi possível conectar à API em http://127.0.0.1:${apiPort}.`);
  assert.equal(problema.instance, '/api/estoque/produtos');
  assert.match(problema.traceId, /^[\da-f]{8}-[\da-f]{4}-4[\da-f]{3}-[89ab][\da-f]{3}-[\da-f]{12}$/i);
  assert.ok(!JSON.stringify(problema).includes('segredo-teste'));
});
