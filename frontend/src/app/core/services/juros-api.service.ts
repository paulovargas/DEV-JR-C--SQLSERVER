import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CalculoJurosRequest,
  CalculoJurosResponse
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class JurosApiService {
  private readonly http = inject(HttpClient);

  calcular(request: CalculoJurosRequest): Observable<CalculoJurosResponse> {
    return this.http.post<CalculoJurosResponse>('/api/juros/calcular', request);
  }
}
