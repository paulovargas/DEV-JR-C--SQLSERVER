import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  MovimentacaoEstoque,
  MovimentacaoEstoqueRequest,
  ProdutoEstoque
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class EstoqueApiService {
  private readonly http = inject(HttpClient);

  listarProdutos(): Observable<ProdutoEstoque[]> {
    return this.http.get<ProdutoEstoque[]>('/api/produtos');
  }

  listarMovimentacoes(): Observable<MovimentacaoEstoque[]> {
    return this.http.get<MovimentacaoEstoque[]>('/api/movimentacoes');
  }

  registrar(request: MovimentacaoEstoqueRequest): Observable<MovimentacaoEstoque> {
    return this.http.post<MovimentacaoEstoque>('/api/movimentacoes', request);
  }
}
