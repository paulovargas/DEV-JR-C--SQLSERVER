import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { finalize } from 'rxjs';
import {
  CalculoComissaoRequest,
  CalculoComissaoResponse
} from '../../core/models/api.models';
import { ComissoesApiService } from '../../core/services/comissoes-api.service';
import { obterMensagemErro } from '../../core/utils/api-error';

@Component({
  selector: 'app-comissoes',
  imports: [CommonModule],
  templateUrl: './comissoes.component.html',
  styleUrls: ['./comissoes.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ComissoesComponent implements OnInit {
  private readonly api = inject(ComissoesApiService);

  readonly dados = signal<CalculoComissaoRequest | null>(null);
  readonly resultado = signal<CalculoComissaoResponse | null>(null);
  readonly nomeArquivo = signal('vendas-exemplo.json');
  readonly carregandoDados = signal(false);
  readonly calculando = signal(false);
  readonly erro = signal<string | null>(null);

  ngOnInit(): void {
    this.carregarExemplo();
  }

  carregarExemplo(): void {
    this.carregandoDados.set(true);
    this.erro.set(null);

    this.api.carregarExemplo()
      .pipe(finalize(() => this.carregandoDados.set(false)))
      .subscribe({
        next: dados => this.definirDados(dados, 'vendas-exemplo.json'),
        error: error => this.erro.set(obterMensagemErro(error))
      });
  }

  async selecionarArquivo(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const arquivo = input.files?.[0];

    if (!arquivo) {
      return;
    }

    this.erro.set(null);
    this.resultado.set(null);

    try {
      const conteudo = await arquivo.text();
      const dados: unknown = JSON.parse(conteudo);

      if (!this.ehRequestValido(dados)) {
        throw new Error('O arquivo deve conter uma lista "vendas" com vendedor e valor válidos.');
      }

      this.definirDados(dados, arquivo.name);
    } catch (error) {
      const mensagem = error instanceof SyntaxError
        ? 'O arquivo selecionado não contém um JSON válido.'
        : error instanceof Error
          ? error.message
          : 'Não foi possível ler o arquivo selecionado.';

      this.dados.set(null);
      this.erro.set(mensagem);
    } finally {
      input.value = '';
    }
  }

  calcular(): void {
    const request = this.dados();

    if (!request || this.calculando()) {
      return;
    }

    this.calculando.set(true);
    this.erro.set(null);

    this.api.calcular(request)
      .pipe(finalize(() => this.calculando.set(false)))
      .subscribe({
        next: resultado => this.resultado.set(resultado),
        error: error => this.erro.set(obterMensagemErro(error))
      });
  }

  private definirDados(dados: CalculoComissaoRequest, nomeArquivo: string): void {
    if (!this.ehRequestValido(dados)) {
      this.dados.set(null);
      this.erro.set('Os dados carregados não possuem vendas válidas.');
      return;
    }

    this.dados.set(dados);
    this.nomeArquivo.set(nomeArquivo);
    this.resultado.set(null);
  }

  private ehRequestValido(dados: unknown): dados is CalculoComissaoRequest {
    if (!dados || typeof dados !== 'object') {
      return false;
    }

    const vendas = (dados as { vendas?: unknown }).vendas;

    return Array.isArray(vendas) && vendas.length > 0 && vendas.every(venda => {
      if (!venda || typeof venda !== 'object') {
        return false;
      }

      const item = venda as { vendedor?: unknown; valor?: unknown };
      return typeof item.vendedor === 'string'
        && item.vendedor.trim().length > 0
        && typeof item.valor === 'number'
        && Number.isFinite(item.valor)
        && item.valor > 0;
    });
  }
}
