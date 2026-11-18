
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute,RouterLink , Router } from '@angular/router';

import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly demoLoading = signal(false);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });
  readonly registeredSuccessfully =
  this.route.snapshot.queryParamMap.get('registered') === '1';

  startDemo(): void {
    if (this.loading() || this.demoLoading()) return;

    this.demoLoading.set(true);
    this.errorMessage.set('');

    this.authService.startDemo().subscribe({
      next: () => {
        this.demoLoading.set(false);
        void this.router.navigateByUrl('/');
      },
      error: error => {
        this.demoLoading.set(false);
        this.errorMessage.set(
          error.status === 429
            ? 'Demasiados intentos. Prueba de nuevo dentro de una hora.'
            : 'No se ha podido iniciar la demostración.'
        );
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid || this.loading() || this.demoLoading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl('/');
      },
      error: (error) => {
        this.loading.set(false);

        if (error.status === 401) {
          this.errorMessage.set('Email o contraseña incorrectos.');
        } else if (error.status === 423) {
          this.errorMessage.set('Cuenta bloqueada temporalmente.');
        } else {
          this.errorMessage.set(
            'No se ha podido iniciar sesión. Inténtalo de nuevo.'
          );
        }
      }
    });
  }
}
