
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
    path: 'register',
    title: 'Crear cuenta | TaskFlow',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./pages/register/register').then(m => m.Register)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layouts/dashboard-layout/dashboard-layout')
        .then(m => m.DashboardLayout),
    children: [
      {
        path: '',
        pathMatch: 'full',
        title: 'Mis tareas | TaskFlow',
        loadComponent: () =>
          import('./pages/tasks/tasks').then(m => m.Tasks)
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
