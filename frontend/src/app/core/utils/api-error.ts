import { HttpErrorResponse } from '@angular/common/http';

export function obterMensagemErro(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Não foi possível concluir a operação.';
  }

  if (error.status === 0) {
    return 'Não foi possível conectar à API. Confira se o backend está em execução.';
  }

  const problema: unknown = error.error;

  if (!ehObjeto(problema)) {
    return 'Não foi possível concluir a operação.';
  }

  const erros = problema['errors'];

  if (ehObjeto(erros)) {
    const mensagens = Object.values(erros)
      .filter(Array.isArray)
      .flat()
      .filter(ehMensagem);

    if (mensagens.length > 0) {
      return mensagens.join(' ');
    }
  }

  const mensagem = [problema['detail'], problema['title'], problema['erro']].find(ehMensagem);
  return mensagem ?? 'Não foi possível concluir a operação.';
}

function ehObjeto(valor: unknown): valor is Record<string, unknown> {
  return valor !== null && typeof valor === 'object' && !Array.isArray(valor);
}

function ehMensagem(valor: unknown): valor is string {
  return typeof valor === 'string' && valor.trim().length > 0;
}
