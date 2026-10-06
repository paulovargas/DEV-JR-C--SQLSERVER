import '@angular/compiler';
import { HttpErrorResponse } from '@angular/common/http';
import { Injector, runInInjectionContext } from '@angular/core';
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { of, Subject } from 'rxjs';
import { compilarComponente } from './helpers/compilar-componente.mjs';

const { EstoqueComponent, EstoqueApiService } = await compilarComponente(`
  export { EstoqueComponent } from './src/app/features/estoque/estoque.component';
  export { EstoqueApiService } from './src/app/core/services/estoque-api.service';
`);

const produto = { codigoProduto: 101, descricaoProduto: 'Caneta', estoque: 150 };

function criarComponente(contexto, { produtos$ = of([produto]), movimentacoes$ = of([]) } = {}) {
  const chamadas = [];
  const respostas = [];
  const api = {
    listarProdutos: () => produtos$,
    listarMovimentacoes: () => movimentacoes$,
    registrar(request) {
      chamadas.push(request);
      const resposta = new Subject();
      respostas.push(resposta);
      return resposta;
    }
  };
  const injector = Injector.create({ providers: [{ provide: EstoqueApiService, useValue: api }] });
  contexto.after(() => injector.destroy());
  const componente = runInInjectionContext(injector, () => new EstoqueComponent());
  componente.formulario.setValue({ codigoProduto: 101, tipo: 'entrada', quantidade: 2, descricao: 'Reposição' });

  return { componente, chamadas, respostas };
}

for (const descricao of ['', '   \t\n ']) {
  test(`impede registro com descrição ${descricao === '' ? 'vazia' : 'composta apenas por espaços'}`, contexto => {
    const { componente, chamadas } = criarComponente(contexto);
    componente.formulario.controls.descricao.setValue(descricao);

    componente.registrar();

    assert.equal(componente.formulario.controls.descricao.hasError('required'), true);
    assert.equal(componente.formulario.controls.descricao.touched, true);
    assert.equal(componente.salvando(), false);
    assert.equal(chamadas.length, 0);
  });
}

test('aceita descrição com exatamente 500 caracteres', contexto => {
  const { componente, chamadas } = criarComponente(contexto);
  const descricao = 'a'.repeat(500);
  componente.formulario.controls.descricao.setValue(descricao);

  componente.registrar();

  assert.equal(componente.formulario.valid, true);
  assert.equal(chamadas.length, 1);
  assert.equal(chamadas[0].descricao, descricao);
  assert.equal(componente.salvando(), true);
});

test('impede registro com descrição de 501 caracteres', contexto => {
  const { componente, chamadas } = criarComponente(contexto);
  componente.formulario.controls.descricao.setValue('a'.repeat(501));

  componente.registrar();

  assert.deepEqual(componente.formulario.controls.descricao.getError('maxlength'), {
    requiredLength: 500,
    actualLength: 501
  });
  assert.equal(componente.formulario.controls.descricao.touched, true);
  assert.equal(chamadas.length, 0);
  assert.equal(componente.salvando(), false);
});

test('valida o limite após remover espaços externos e envia a descrição normalizada', contexto => {
  const { componente, chamadas } = criarComponente(contexto);
  const descricao = 'a'.repeat(500);
  componente.formulario.controls.descricao.setValue(`  ${descricao} \t`);

  componente.registrar();

  assert.equal(componente.formulario.valid, true);
  assert.deepEqual(chamadas, [{ codigoProduto: 101, tipo: 'entrada', quantidade: 2, descricao }]);
});

test('envia uma única movimentação durante uma requisição pendente', contexto => {
  const { componente, chamadas } = criarComponente(contexto);

  componente.registrar();
  componente.registrar();
  componente.registrar();

  assert.equal(chamadas.length, 1);
  assert.equal(componente.salvando(), true);
});

test('aguarda o carregamento dos produtos e movimentações antes de registrar', contexto => {
  const produtos$ = new Subject();
  const movimentacoes$ = new Subject();
  const { componente, chamadas } = criarComponente(contexto, { produtos$, movimentacoes$ });
  componente.ngOnInit();

  componente.registrar();
  componente.registrar();
  assert.equal(chamadas.length, 0);
  assert.equal(componente.carregando(), true);
  assert.equal(componente.salvando(), false);

  produtos$.next([produto]);
  produtos$.complete();
  componente.registrar();
  assert.equal(chamadas.length, 0);

  movimentacoes$.next([]);
  movimentacoes$.complete();
  componente.registrar();

  assert.equal(componente.carregando(), false);
  assert.equal(chamadas.length, 1);
});

test('libera o envio após sucesso e atualiza o saldo e o histórico', contexto => {
  const { componente, chamadas, respostas } = criarComponente(contexto);
  componente.ngOnInit();
  componente.registrar();
  const movimentacao = {
    id: 1,
    codigoProduto: 101,
    descricaoProduto: 'Caneta',
    tipo: 'entrada',
    quantidade: 2,
    descricao: 'Reposição',
    estoqueAnterior: 150,
    estoqueFinal: 152,
    realizadaEm: '2026-10-06T12:00:00Z'
  };

  respostas[0].next(movimentacao);
  respostas[0].complete();

  assert.equal(componente.salvando(), false);
  assert.equal(componente.produtos()[0].estoque, 152);
  assert.deepEqual(componente.movimentacoes(), [movimentacao]);
  assert.equal(componente.sucesso(), 'Entrada registrada. Novo saldo: 152.');
  assert.equal(componente.formulario.controls.descricao.value, '');
  assert.equal(componente.formulario.controls.quantidade.value, 1);

  componente.formulario.controls.descricao.setValue('Nova reposição');
  componente.registrar();
  assert.equal(chamadas.length, 2);
});

test('libera nova tentativa após falha sem alterar o estoque ou apagar o formulário', contexto => {
  const { componente, chamadas, respostas } = criarComponente(contexto);
  componente.ngOnInit();
  componente.registrar();

  respostas[0].error(new HttpErrorResponse({
    status: 409,
    error: { detail: 'Saldo insuficiente para a saída solicitada.' }
  }));

  assert.equal(componente.salvando(), false);
  assert.equal(componente.erro(), 'Saldo insuficiente para a saída solicitada.');
  assert.equal(componente.produtos()[0].estoque, 150);
  assert.deepEqual(componente.movimentacoes(), []);
  assert.equal(componente.formulario.controls.descricao.value, 'Reposição');
  assert.equal(componente.sucesso(), null);

  componente.registrar();
  assert.equal(chamadas.length, 2);
  assert.equal(componente.salvando(), true);
});
