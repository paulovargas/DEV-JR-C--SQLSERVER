import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import {
  MovimentacaoEstoque,
  MovimentacaoEstoqueRequest,
  ProdutoEstoque,
  TipoMovimentacao
} from '../../core/models/api.models';
import { EstoqueApiService } from '../../core/services/estoque-api.service';
import { obterMensagemErro } from '../../core/utils/api-error';

@Component({
  selector: 'app-estoque',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './estoque.component.html',
  styleUrls: ['./estoque.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EstoqueComponent implements OnInit {
  private readonly api = inject(EstoqueApiService);

  readonly produtos = signal<ProdutoEstoque[]>([]);
  readonly movimentacoes = signal<MovimentacaoEstoque[]>([]);
  readonly codigoSelecionado = signal(0);
  readonly carregando = signal(false);
  readonly salvando = signal(false);
  readonly erro = signal<string | null>(null);
  readonly sucesso = signal<string | null>(null);

  readonly produtoSelecionado = computed(() =>
    this.produtos().find(produto => produto.codigoProduto === this.codigoSelecionado()) ?? null);

  readonly formulario = new FormGroup({
    codigoProduto: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1)]
    }),
    tipo: new FormControl<TipoMovimentacao>('entrada', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    quantidade: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]
    }),
    descricao: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(120)]
    })
  });

  ngOnInit(): void {
    this.carregarDados();
  }

  carregarDados(): void {
    this.carregando.set(true);
    this.erro.set(null);

    forkJoin({
      produtos: this.api.listarProdutos(),
      movimentacoes: this.api.listarMovimentacoes()
    })
      .pipe(finalize(() => this.carregando.set(false)))
      .subscribe({
        next: ({ produtos, movimentacoes }) => {
          this.produtos.set(produtos);
          this.movimentacoes.set([...movimentacoes].reverse());

          if (produtos.length > 0 && this.formulario.controls.codigoProduto.value === 0) {
            this.selecionarCodigo(produtos[0].codigoProduto);
          }
        },
        error: error => this.erro.set(obterMensagemErro(error))
      });
  }

  produtoAlterado(event: Event): void {
    const codigo = Number((event.target as HTMLSelectElement).value);
    this.codigoSelecionado.set(codigo);
  }

  registrar(): void {
    this.sucesso.set(null);
    this.erro.set(null);

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const request: MovimentacaoEstoqueRequest = this.formulario.getRawValue();
    this.salvando.set(true);

    this.api.registrar(request)
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe({
        next: movimentacao => {
          this.movimentacoes.update(itens => [movimentacao, ...itens]);
          this.produtos.update(itens => itens.map(produto =>
            produto.codigoProduto === movimentacao.codigoProduto
              ? { ...produto, estoque: movimentacao.estoqueFinal }
              : produto));

          const tipo = movimentacao.tipo === 'entrada' ? 'Entrada' : 'Saída';
          this.sucesso.set(`${tipo} registrada. Novo saldo: ${movimentacao.estoqueFinal}.`);
          this.formulario.controls.quantidade.setValue(1);
          this.formulario.controls.descricao.setValue('');
          this.formulario.controls.descricao.markAsUntouched();
        },
        error: error => this.erro.set(obterMensagemErro(error))
      });
  }

  private selecionarCodigo(codigo: number): void {
    this.formulario.controls.codigoProduto.setValue(codigo);
    this.codigoSelecionado.set(codigo);
  }
}
