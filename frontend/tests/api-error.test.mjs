import '@angular/compiler';
import { HttpErrorResponse } from '@angular/common/http';
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { obterMensagemErro } from '../src/app/core/utils/api-error.ts';

function erroHttp(problema, status = 400) {
  return new HttpErrorResponse({ error: problema, status });
}

test('prioriza erros de validação por campo sobre detail e title', () => {
  const erro = erroHttp({
    errors: {
      descricao: ['A descrição é obrigatória.'],
      quantidade: ['A quantidade deve ser positiva.', 'A quantidade deve ser inteira.']
    },
    detail: 'Requisição inválida.',
    title: 'Falha de validação.',
    erro: 'Erro legado.'
  });

  assert.equal(
    obterMensagemErro(erro),
    'A descrição é obrigatória. A quantidade deve ser positiva. A quantidade deve ser inteira.'
  );
});

test('ignora mensagens de validação malformadas e preserva as válidas', () => {
  const erro = erroHttp({
    errors: {
      descricao: [null, 23, {}, '', '   ', 'A descrição é obrigatória.'],
      quantidade: 'Erro fora de uma lista.',
      valor: null,
      tipo: ['Tipo inválido.']
    },
    detail: 'Requisição inválida.'
  });

  assert.equal(obterMensagemErro(erro), 'A descrição é obrigatória. Tipo inválido.');
});

test('exibe detail quando não há erros válidos por campo', () => {
  const erro = erroHttp({
    errors: { quantidade: [null, '   '] },
    detail: 'Saldo insuficiente para a saída solicitada.',
    title: 'Conflito.'
  }, 409);

  assert.equal(obterMensagemErro(erro), 'Saldo insuficiente para a saída solicitada.');
});

test('exibe title quando detail está ausente ou inválido', () => {
  for (const detail of [undefined, null, 42, [], {}, '', '   ']) {
    const erro = erroHttp({ detail, title: 'Produto não encontrado.' }, 404);

    assert.equal(obterMensagemErro(erro), 'Produto não encontrado.');
  }
});

test('mantém suporte à mensagem erro do contrato legado', () => {
  assert.equal(obterMensagemErro(erroHttp({ erro: 'Falha no servidor legado.' })), 'Falha no servidor legado.');
});

test('prioriza o contrato ProblemDetails sobre a mensagem legada', () => {
  const erro = erroHttp({ detail: 'Falha ao conectar à API.', erro: 'Erro legado.' }, 502);

  assert.equal(obterMensagemErro(erro), 'Falha ao conectar à API.');
});

test('usa a mensagem padrão para respostas malformadas sem lançar exceções', () => {
  for (const problema of [null, undefined, 'erro', 42, false, [], ['erro'], {}]) {
    assert.equal(obterMensagemErro(erroHttp(problema)), 'Não foi possível concluir a operação.');
  }
});

test('usa a mensagem padrão quando propriedades do problema têm tipos inválidos', () => {
  for (const errors of ['erro', [], 42, true, null, { campo: [null, {}, 1, '', '   '] }]) {
    const erro = erroHttp({ errors, detail: [], title: false, erro: {} });

    assert.equal(obterMensagemErro(erro), 'Não foi possível concluir a operação.');
  }
});

test('explica falha de conexão quando o status é zero', () => {
  const erro = erroHttp({ detail: 'Mensagem do servidor.' }, 0);

  assert.equal(
    obterMensagemErro(erro),
    'Não foi possível conectar à API. Confira se o backend está em execução.'
  );
});

test('usa a mensagem padrão para erros fora do HttpClient', () => {
  for (const erro of [new Error('Falha inesperada.'), null, undefined, 'erro', { status: 500 }]) {
    assert.equal(obterMensagemErro(erro), 'Não foi possível concluir a operação.');
  }
});
