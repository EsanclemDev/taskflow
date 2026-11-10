
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/tasks/tasks').then(m => m.Tasks)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
