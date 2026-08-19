import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'produtos', pathMatch: 'full' },
  // Futuramente apontaremos para nossos componentes standalone de Produtos e Notas Fiscais
  { 
    path: 'produtos', 
    loadComponent: () => import('./components/produtos/produtos.component').then(m => m.ProdutosComponent) 
  },
  { 
    path: 'notas', 
    loadComponent: () => import('./components/notas/notas.component').then(m => m.NotasComponent) 
  }
];