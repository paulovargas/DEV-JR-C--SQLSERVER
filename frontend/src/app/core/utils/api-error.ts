import { HttpErrorResponse } from '@angular/common/http';

interface ApiProblemDetails {
  erro?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

export function obterMensagemErro(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Não foi possível concluir a operação.';
  }

  if (error.status === 0) {
    return 'Não foi possível conectar à API. Confira se o backend está em execução.';
  }

  const problema = error.error as ApiProblemDetails | null;

  if (problema?.erro) {
    return problema.erro;
  }

  if (problema?.errors) {
    const mensagens = Object.values(problema.errors).flat();
    if (mensagens.length > 0) {
      return mensagens.join(' ');
    }
  }

  return problema?.title ?? 'Não foi possível concluir a operação.';
}
