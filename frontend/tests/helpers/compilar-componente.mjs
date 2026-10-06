import { build } from 'esbuild';
import { fileURLToPath } from 'node:url';

// O Node não remove decorators Angular; compilamos o componente real em memória.
export async function compilarComponente(conteudo) {
  const compilacao = await build({
    stdin: {
      contents: conteudo,
      resolveDir: fileURLToPath(new URL('../../', import.meta.url))
    },
    bundle: true,
    write: false,
    platform: 'node',
    format: 'esm',
    plugins: [{
      name: 'dependencias-angular',
      setup(builder) {
        builder.onResolve({ filter: /^(?:@angular\/|rxjs(?:\/|$)|tslib$)/ }, args => ({
          path: import.meta.resolve(args.path),
          external: true
        }));
      }
    }]
  });

  return import(
    `data:text/javascript;base64,${Buffer.from(compilacao.outputFiles[0].text).toString('base64')}`
  );
}
