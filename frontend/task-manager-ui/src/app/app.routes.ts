
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
        title: 'Dashboard | TaskFlow',
        loadComponent: () =>
          import('./pages/dashboard/dashboard').then(m => m.Dashboard)
      },
      {
        path: 'tasks',
        title: 'Mis tareas | TaskFlow',
        loadComponent: () =>
          import('./pages/tasks/tasks').then(m => m.Tasks)
      },
      {
        path: 'statistics',
        title: 'Estadísticas | TaskFlow',
        loadComponent: () =>
          import('./pages/statistics/statistics')
            .then(m => m.Statistics)
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
