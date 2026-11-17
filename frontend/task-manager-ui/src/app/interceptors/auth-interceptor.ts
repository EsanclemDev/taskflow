
import { inject } from '@angular/core';
import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import {
  catchError,
  finalize,
  Observable,
  shareReplay,
  switchMap,
  throwError
} from 'rxjs';

import { AuthService } from '../services/auth';
import { AuthTokens } from '../models/auth';
import { environment } from '../environments/environments';

// Una única renovación para peticiones concurrentes.
let refreshInProgress$: Observable<AuthTokens> | null = null;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const apiBase = environment.apiUrl.replace(/\/+$/, '');

  const isApiRequest =
    req.url === apiBase ||
    req.url.startsWith(`${apiBase}/`);

  const isAuthEndpoint =
    req.url.startsWith(`${apiBase}/auth/`);

  // Login, registro, refresh y logout se gestionan
  // directamente para evitar bucles.
  if (!isApiRequest || isAuthEndpoint) {
    return next(req);
  }

  const token = authService.getAccessToken();

  const authenticatedRequest = token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : req;

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || !token) {
        return throwError(() => error);
      }

      const currentToken = authService.getAccessToken();

      // Si la sesión se ha cerrado mientras
      // la petición estaba en curso, no renovar.
      if (!currentToken) {
        return throwError(() => error);
      }

      // Otra petición ya ha actualizado el token.
      if (currentToken !== token) {
        return next(req.clone({
          setHeaders: {
            Authorization: `Bearer ${currentToken}`
          }
        }));
      }

      if (!refreshInProgress$) {
        refreshInProgress$ = authService.refresh().pipe(
          finalize(() => {
            refreshInProgress$ = null;
          }),
          shareReplay({
            bufferSize: 1,
            refCount: false
          })
        );
      }

      return refreshInProgress$.pipe(
        switchMap(tokens => {
          return next(req.clone({
            setHeaders: {
              Authorization: `Bearer ${tokens.accessToken}`
            }
          }));
        }),
        catchError((refreshError: unknown) => {
          // Solo invalidar cuando el servidor confirme
          // que la sesión ya no puede renovarse.
          if (
            refreshError instanceof HttpErrorResponse &&
            (refreshError.status === 401 ||
             refreshError.status === 403)
          ) {
            authService.invalidateSession(token);
          }

          return throwError(() => refreshError);
        })
      );
    })
  );
};
