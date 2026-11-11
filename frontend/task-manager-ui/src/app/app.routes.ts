
import { Routes } from '@angular/router';
import { authGuard } from './guards/auth-guard';
import { guestGuard } from './guards/guest-guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Iniciar sesión | TaskFlow',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./pages/login/login').then(m => m.Login)
  },
  {
    path: '',
    title: 'Mis tareas | TaskFlow',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/tasks/tasks').then(m => m.Tasks)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
