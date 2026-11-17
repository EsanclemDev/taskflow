
import {
  Component,
  effect,
  inject,
  untracked
} from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';

import { AuthService } from './services/auth';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  private wasAuthenticated = false;

  constructor() {
    effect(() => {
      const authenticated = this.authService.isAuthenticated();

      const sessionJustEnded =
        this.wasAuthenticated && !authenticated;

      this.wasAuthenticated = authenticated;

      if (sessionJustEnded) {
        untracked(() => {
          void this.router.navigateByUrl('/login');
        });
      }
    });
  }
}
