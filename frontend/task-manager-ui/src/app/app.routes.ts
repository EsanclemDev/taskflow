
import { Routes } from '@angular/router';
import { Tasks } from './pages/tasks/tasks';

export const routes: Routes = [
  { path: '', component: Tasks },
  { path: '**', redirectTo: '' }
];
