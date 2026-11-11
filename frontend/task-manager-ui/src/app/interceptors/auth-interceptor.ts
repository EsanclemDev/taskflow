
import { inject } from '@angular/core';
import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import { catchError, finalize, Observable, shareReplay, switchMap, throwError } from 'rxjs';

import { AuthService } from '../services/auth';
import { AuthTokens } from '../models/auth';
import { environment } from '../environments/environments';

// Comparte una única renovación entre peticiones concurrentes.
let refreshInProgress$: Observable<AuthTokens> | null = null;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const apiBase = environment.apiUrl.replace(/\/+$/, '');

  const isApiRequest =
    req.url === apiBase ||
    req.url.startsWith(`${apiBase}/`);

  const isAuthEndpoint =
    req.url.startsWith(`${apiBase}/auth/`);

  // No interceptamos endpoints de autenticación para evitar
  // bucles de renovación (refresh, login, logout, etc.).
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
      // Solo intentamos renovar ante un 401 de una petición
      // que realmente llevaba un access token.
      if (error.status !== 401 || !token) {
        return throwError(() => error);
      }

      // Si otra petición ya renovó el token, reutilizamos
      // el nuevo sin volver a llamar a /refresh.
      const currentToken = authService.getAccessToken();

      if (currentToken && currentToken !== token) {
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
          shareReplay({ bufferSize: 1, refCount: false })
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
        catchError(refreshError => {
          return throwError(() => refreshError);
        })
      );
    })
  );
};
