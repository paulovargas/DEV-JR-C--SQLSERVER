import '@angular/compiler';
import { Injector, runInInjectionContext } from '@angular/core';
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { of } from 'rxjs';
import { compilarComponente } from './helpers/compilar-componente.mjs';

const { ComissoesComponent, ComissoesApiService } = await compilarComponente(`
  export { ComissoesComponent } from './src/app/features/comissoes/comissoes.component';
  export { ComissoesApiService } from './src/app/core/services/comissoes-api.service';
`);

const vendas = { vendas: [{ vendedor: 'Ana', valor: 200 }] };
const resultado = {
  vendedores: [{ vendedor: 'Ana', quantidadeVendas: 1, valorTotalVendas: 200, comissaoTotal: 2 }],
  comissaoTotalGeral: 2
};

function criarComponente(contexto, exemplo = vendas) {
  const chamadas = [];
  const api = {
    carregarExemplo: () => of(exemplo),
    calcular(request) {
      chamadas.push(request);
      return of(resultado);
    }
  };
  const injector = Injector.create({ providers: [{ provide: ComissoesApiService, useValue: api }] });
  contexto.after(() => injector.destroy());
  const componente = runInInjectionContext(injector, () => new ComissoesComponent());
  componente.dados.set(vendas);
  componente.resultado.set(resultado);
  componente.erro.set('Erro anterior.');
  const validacao = contexto.mock.method(componente, 'ehRequestValido');

  return { componente, chamadas, validacao };
}

function eventoArquivo(texto, nome = 'minhas-vendas.json') {
  const input = { files: [{ name: nome, text: texto }], value: nome };
  return { input, evento: { target: input } };
}

test('importa vendas válidas com uma única validação e limpa o resultado anterior', async contexto => {
  const { componente, chamadas, validacao } = criarComponente(contexto);
  const dadosImportados = { vendas: [{ vendedor: 'Beatriz', valor: 350 }] };
  const { input, evento } = eventoArquivo(async () => JSON.stringify(dadosImportados));

  await componente.selecionarArquivo(evento);

  assert.deepEqual(componente.dados(), dadosImportados);
  assert.equal(componente.nomeArquivo(), 'minhas-vendas.json');
  assert.equal(componente.resultado(), null);
  assert.equal(componente.erro(), null);
  assert.equal(input.value, '');
  assert.equal(validacao.mock.calls.length, 1);
  assert.deepEqual(chamadas, []);
});

test('rejeita arquivos com vendas inválidas sem manter dados ou resultado anteriores', async contexto => {
  const { componente, chamadas, validacao } = criarComponente(contexto);
  const invalidos = [
    null,
    [],
    {},
    { vendas: [] },
    { vendas: [{ vendedor: '   ', valor: 200 }] },
    { vendas: [{ vendedor: 'Ana', valor: '200' }] },
    { vendas: [{ vendedor: 'Ana', valor: 0 }] }
  ];

  for (const dados of invalidos) {
    componente.dados.set(vendas);
    componente.resultado.set(resultado);
    const { input, evento } = eventoArquivo(async () => JSON.stringify(dados));
    const validacoesAntes = validacao.mock.calls.length;

    await componente.selecionarArquivo(evento);

    assert.equal(componente.dados(), null);
    assert.equal(componente.resultado(), null);
    assert.equal(componente.erro(), 'O arquivo deve conter uma lista "vendas" com vendedor e valor válidos.');
    assert.equal(input.value, '');
    assert.equal(validacao.mock.calls.length - validacoesAntes, 1);
  }

  assert.deepEqual(chamadas, []);
});

test('informa JSON malformado, limpa os dados e permite selecionar o arquivo novamente', async contexto => {
  const { componente, validacao } = criarComponente(contexto);
  const { input, evento } = eventoArquivo(async () => '{"vendas":');

  await componente.selecionarArquivo(evento);

  assert.equal(componente.dados(), null);
  assert.equal(componente.resultado(), null);
  assert.equal(componente.erro(), 'O arquivo selecionado não contém um JSON válido.');
  assert.equal(input.value, '');
  assert.equal(validacao.mock.calls.length, 0);
});

test('preserva a mensagem de falha na leitura do arquivo e limpa a seleção', async contexto => {
  const { componente, validacao } = criarComponente(contexto);
  const { input, evento } = eventoArquivo(async () => { throw new Error('Não foi possível acessar o arquivo.'); });

  await componente.selecionarArquivo(evento);

  assert.equal(componente.dados(), null);
  assert.equal(componente.resultado(), null);
  assert.equal(componente.erro(), 'Não foi possível acessar o arquivo.');
  assert.equal(input.value, '');
  assert.equal(validacao.mock.calls.length, 0);
});

test('usa a mensagem padrão quando a leitura rejeita sem uma exceção conhecida', async contexto => {
  const { componente, validacao } = criarComponente(contexto);
  const { input, evento } = eventoArquivo(async () => { throw null; });

  await componente.selecionarArquivo(evento);

  assert.equal(componente.dados(), null);
  assert.equal(componente.resultado(), null);
  assert.equal(componente.erro(), 'Não foi possível ler o arquivo selecionado.');
  assert.equal(input.value, '');
  assert.equal(validacao.mock.calls.length, 0);
});

test('carrega o exemplo válido usando a mesma validação uma única vez', contexto => {
  const { componente, chamadas, validacao } = criarComponente(contexto);

  componente.carregarExemplo();

  assert.deepEqual(componente.dados(), vendas);
  assert.equal(componente.nomeArquivo(), 'vendas-exemplo.json');
  assert.equal(componente.resultado(), null);
  assert.equal(componente.erro(), null);
  assert.equal(componente.carregandoDados(), false);
  assert.equal(validacao.mock.calls.length, 1);
  assert.deepEqual(chamadas, []);
});

test('rejeita exemplo inválido com a mensagem específica do carregamento', contexto => {
  const { componente, chamadas, validacao } = criarComponente(contexto, { vendas: [] });

  componente.carregarExemplo();

  assert.equal(componente.dados(), null);
  assert.equal(componente.erro(), 'Os dados carregados não possuem vendas válidas.');
  assert.equal(componente.carregandoDados(), false);
  assert.equal(validacao.mock.calls.length, 1);
  assert.deepEqual(chamadas, []);
});
