import { randomUUID } from 'node:crypto';
import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import http from 'node:http';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const currentDirectory = path.dirname(fileURLToPath(import.meta.url));
const publicDirectory = path.resolve(currentDirectory, 'dist', 'desafio-target', 'browser');
const indexPath = path.join(publicDirectory, 'index.html');
const frontendPort = Number(process.env.PORT ?? 4200);
const apiHost = process.env.API_HOST ?? '127.0.0.1';
const apiPort = Number(process.env.API_PORT ?? 5080);

const mimeTypes = new Map([
  ['.css', 'text/css; charset=utf-8'],
  ['.html', 'text/html; charset=utf-8'],
  ['.ico', 'image/x-icon'],
  ['.jpeg', 'image/jpeg'],
  ['.jpg', 'image/jpeg'],
  ['.js', 'text/javascript; charset=utf-8'],
  ['.json', 'application/json; charset=utf-8'],
  ['.png', 'image/png'],
  ['.svg', 'image/svg+xml'],
  ['.webp', 'image/webp'],
  ['.woff', 'font/woff'],
  ['.woff2', 'font/woff2']
]);

function proxyToApi(request, response) {
  const headers = { ...request.headers, host: `${apiHost}:${apiPort}` };
  delete headers.connection;

  const upstream = http.request({
    hostname: apiHost,
    port: apiPort,
    path: request.url,
    method: request.method,
    headers
  }, upstreamResponse => {
    response.writeHead(upstreamResponse.statusCode ?? 502, upstreamResponse.headers);
    upstreamResponse.pipe(response);
  });

  upstream.on('error', () => {
    if (response.headersSent) {
      response.destroy();
      return;
    }

    response.writeHead(502, { 'Content-Type': 'application/problem+json; charset=utf-8' });
    response.end(JSON.stringify({
      type: 'about:blank',
      title: 'Não foi possível conectar à API.',
      status: 502,
      detail: `Não foi possível conectar à API em http://${apiHost}:${apiPort}.`,
      instance: request.url?.split('?')[0] ?? '/',
      traceId: randomUUID()
    }));
  });

  request.pipe(upstream);
}

async function serveStatic(request, response) {
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    response.writeHead(405, { Allow: 'GET, HEAD' });
    response.end();
    return;
  }

  const requestUrl = new URL(request.url ?? '/', `http://${request.headers.host ?? 'localhost'}`);
  const decodedPath = decodeURIComponent(requestUrl.pathname);
  let filePath = path.resolve(publicDirectory, `.${decodedPath}`);

  if (filePath !== publicDirectory && !filePath.startsWith(`${publicDirectory}${path.sep}`)) {
    response.writeHead(403);
    response.end('Acesso negado.');
    return;
  }

  try {
    const fileInfo = await stat(filePath);
    if (fileInfo.isDirectory()) {
      filePath = path.join(filePath, 'index.html');
    }
  } catch {
    filePath = indexPath;
  }

  try {
    const fileInfo = await stat(filePath);
    const contentType = mimeTypes.get(path.extname(filePath).toLowerCase())
      ?? 'application/octet-stream';

    response.writeHead(200, {
      'Content-Type': contentType,
      'Content-Length': fileInfo.size,
      'Cache-Control': filePath === indexPath ? 'no-cache' : 'public, max-age=3600'
    });

    if (request.method === 'HEAD') {
      response.end();
      return;
    }

    createReadStream(filePath).pipe(response);
  } catch {
    response.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
    response.end('Arquivo não encontrado. Execute npm run build antes de iniciar o servidor.');
  }
}

const server = http.createServer((request, response) => {
  if (request.url?.startsWith('/api')) {
    proxyToApi(request, response);
    return;
  }

  void serveStatic(request, response);
});

server.listen(frontendPort, '127.0.0.1', () => {
  console.log(`Frontend disponível em http://127.0.0.1:${frontendPort}`);
  console.log(`Chamadas /api encaminhadas para http://${apiHost}:${apiPort}`);
});

process.on('SIGINT', () => server.close());
