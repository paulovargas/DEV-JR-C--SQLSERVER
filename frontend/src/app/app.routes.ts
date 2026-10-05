import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'estoque'
  },
  {
    path: 'comissoes',
    title: 'Comissões | Desafio Target',
    loadComponent: () => import('./features/comissoes/comissoes.component')
      .then(module => module.ComissoesComponent)
  },
  {
    path: 'estoque',
    title: 'Estoque | Desafio Target',
    loadComponent: () => import('./features/estoque/estoque.component')
      .then(module => module.EstoqueComponent)
  },
  {
    path: 'juros',
    title: 'Juros | Desafio Target',
    loadComponent: () => import('./features/juros/juros.component')
      .then(module => module.JurosComponent)
  },
  {
    path: '**',
    redirectTo: 'estoque'
  }
];
