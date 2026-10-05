import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CalculoComissaoRequest,
  CalculoComissaoResponse
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ComissoesApiService {
  private readonly http = inject(HttpClient);

  calcular(request: CalculoComissaoRequest): Observable<CalculoComissaoResponse> {
    return this.http.post<CalculoComissaoResponse>('/api/comissoes/calcular', request);
  }

  carregarExemplo(): Observable<CalculoComissaoRequest> {
    return this.http.get<CalculoComissaoRequest>('/vendas-exemplo.json');
  }
}
