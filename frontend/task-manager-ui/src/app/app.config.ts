
import {
  ApplicationConfig,
  inject,
  provideAppInitializer
} from '@angular/core';

import { provideRouter } from '@angular/router';

import {
  provideHttpClient,
  withInterceptors
} from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth-interceptor';
import { AuthService } from './services/auth';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),

    provideHttpClient(
      withInterceptors([authInterceptor])
    ),

    provideAppInitializer(() => {
      const authService = inject(AuthService);
      return authService.restoreSession();
    })
  ]
};
