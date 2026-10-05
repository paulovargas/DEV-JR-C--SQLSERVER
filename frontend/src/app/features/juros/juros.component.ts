import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { CalculoJurosRequest, CalculoJurosResponse } from '../../core/models/api.models';
import { JurosApiService } from '../../core/services/juros-api.service';
import { obterMensagemErro } from '../../core/utils/api-error';

@Component({
  selector: 'app-juros',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './juros.component.html',
  styleUrls: ['./juros.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JurosComponent {
  private readonly api = inject(JurosApiService);

  readonly resultado = signal<CalculoJurosResponse | null>(null);
  readonly calculando = signal(false);
  readonly erro = signal<string | null>(null);

  readonly formulario = new FormGroup({
    valor: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.01)]
    }),
    dataVencimento: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  calcular(): void {
    this.erro.set(null);
    this.resultado.set(null);

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const request: CalculoJurosRequest = this.formulario.getRawValue();
    this.calculando.set(true);

    this.api.calcular(request)
      .pipe(finalize(() => this.calculando.set(false)))
      .subscribe({
        next: resultado => this.resultado.set(resultado),
        error: error => this.erro.set(obterMensagemErro(error))
      });
  }

  formatarData(data: string): string {
    const partes = data.split('-').map(Number);

    if (partes.length !== 3 || partes.some(parte => !Number.isInteger(parte))) {
      return data;
    }

    const [ano, mes, dia] = partes;
    return new Intl.DateTimeFormat('pt-BR').format(new Date(ano, mes - 1, dia));
  }
}
