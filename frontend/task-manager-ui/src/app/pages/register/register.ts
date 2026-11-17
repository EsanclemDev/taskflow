
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../services/auth';

function passwordsMatch(
  control: AbstractControl
): ValidationErrors | null {
  const password = control.get('password')?.value;
  const confirmation = control.get('confirmPassword')?.value;

  return password === confirmation
    ? null
    : { passwordsMismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  readonly form = this.fb.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(
            /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$/
          )
        ]
      ],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordsMatch }
  );

  onSubmit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);

        void this.router.navigate(['/login'], {
          queryParams: { registered: '1' }
        });
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);

        if (error.status === 400) {
          this.errorMessage.set(
            'No se ha podido completar el registro. ' +
            'Comprueba los datos o utiliza otro correo electrónico.'
          );
        } else {
          this.errorMessage.set(
            'Ha ocurrido un error. Inténtalo de nuevo.'
          );
        }
      }
    });
  }
}
