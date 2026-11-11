
import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {
    Observable,
    defer,
    finalize,
    firstValueFrom,
    from,
    shareReplay,
    tap
} from 'rxjs';

import {
    RegisterRequest,
    LoginRequest,
    AuthTokens,
    CurrentUser
} from '../models/auth';

import { environment } from '../environments/environments';

type AuthChannelMessage =
    | {
        type: 'TOKEN_UPDATED';
        tokens: AuthTokens;
        generation: string;
        rotationId: string;
    }
    | {
        type: 'LOGOUT';
        generation: string | null;
    };

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/auth`;

    private readonly tokens = signal<AuthTokens | null>(null);
    readonly isAuthenticated = signal(false);

    private sessionVersion = 0;
    private generation: string | null = null;
    private rotationId: string | null = null;

    private refreshInProgress$: Observable<AuthTokens> | null = null;

    private readonly channel =
        typeof BroadcastChannel !== 'undefined'
            ? new BroadcastChannel('taskflow-auth')
            : null;

    constructor() {
        this.channel?.addEventListener(
            'message',
            (event: MessageEvent<AuthChannelMessage>) => {
                this.handleChannelMessage(event.data);
            }
        );
    }

    register(request: RegisterRequest): Observable<unknown> {
        return this.http.post(
            `${this.apiUrl}/register`,
            request
        );
    }

    login(request: LoginRequest): Observable<AuthTokens> {
        return defer(() => {
            const version = ++this.sessionVersion;

            return this.http.post<AuthTokens>(
                `${this.apiUrl}/login`,
                request,
                { withCredentials: true }
            ).pipe(
                tap(tokens => {
                    if (version !== this.sessionVersion) {
                        return;
                    }

                    this.generation = crypto.randomUUID();
                    this.rotationId = crypto.randomUUID();

                    this.setSession(tokens);

                    this.channel?.postMessage({
                        type: 'TOKEN_UPDATED',
                        tokens,
                        generation: this.generation,
                        rotationId: this.rotationId
                    } satisfies AuthChannelMessage);
                })
            );
        });
    }

    refresh(): Observable<AuthTokens> {
        return defer(() => {
            if (this.refreshInProgress$) {
                return this.refreshInProgress$;
            }

            const version = this.sessionVersion;

            const request$ = from(
                this.refreshWithLock(version)
            ).pipe(
                finalize(() => {
                    if (this.refreshInProgress$ === request$) {
                        this.refreshInProgress$ = null;
                    }
                }),
                shareReplay({
                    bufferSize: 1,
                    refCount: false
                })
            );

            this.refreshInProgress$ = request$;

            return request$;
        });
    }

    private async refreshWithLock(
        version: number
    ): Promise<AuthTokens> {
        const initialRotationId = this.rotationId;

        const performRefresh = async (): Promise<AuthTokens> => {
            this.assertSessionVersion(version);

            const tokens = await firstValueFrom(
                this.http.post<AuthTokens>(
                    `${this.apiUrl}/refresh`,
                    {},
                    { withCredentials: true }
                )
            );

            this.assertSessionVersion(version);

            if (!this.generation) {
                this.generation = crypto.randomUUID();
            }

            this.rotationId = crypto.randomUUID();
            this.setSession(tokens);

            this.channel?.postMessage({
                type: 'TOKEN_UPDATED',
                tokens,
                generation: this.generation,
                rotationId: this.rotationId
            } satisfies AuthChannelMessage);

            return tokens;
        };

        if (!navigator.locks) {
            return performRefresh();
        }

        return navigator.locks.request(
            'taskflow-auth-operation',
            { mode: 'exclusive' },
            async () => {
                this.assertSessionVersion(version);

                const currentTokens = this.tokens();

                // Otra pestaña ha renovado mientras
                // esperábamos el bloqueo.
                if (
                    currentTokens &&
                    this.rotationId !== initialRotationId
                ) {
                    return currentTokens;
                }

                return performRefresh();
            }
        );
    }

    async restoreSession(): Promise<void> {
        const version = this.sessionVersion;

        try {
            await firstValueFrom(this.refresh());
        } catch {
            if (version === this.sessionVersion) {
                this.clearSession();
            }
        }
    }

    getCurrentUser(): Observable<CurrentUser> {
        return this.http.get<CurrentUser>(
            `${this.apiUrl}/me`
        );
    }

    logout(): Observable<void> {
        return defer((): Observable<void> => {
            const previousGeneration = this.generation;

            this.sessionVersion++;
            this.clearSession();

            this.channel?.postMessage({
                type: 'LOGOUT',
                generation: previousGeneration
            } satisfies AuthChannelMessage);

            const performLogout = async (): Promise<void> => {
                await firstValueFrom(
                    this.http.post<void>(
                        `${this.apiUrl}/logout`,
                        {},
                        { withCredentials: true }
                    )
                );
            };

            const executeLogout = async (): Promise<void> => {
                if (typeof navigator.locks === 'undefined') {
                    await performLogout();
                    return;
                }

                await navigator.locks.request<void>(
                    'taskflow-auth-operation',
                    { mode: 'exclusive' },
                    async (): Promise<void> => {
                        await performLogout();
                    }
                );
            };

            return from(executeLogout());
        });
    }


    getAccessToken(): string | null {
        return this.tokens()?.accessToken ?? null;
    }

    private handleChannelMessage(
        message: AuthChannelMessage
    ): void {
        if (message.type === 'LOGOUT') {
            if (
                message.generation === null ||
                this.generation === null ||
                message.generation === this.generation
            ) {
                this.sessionVersion++;
                this.clearSession();
            }

            return;
        }

        if (message.type === 'TOKEN_UPDATED') {
            // No aceptar tokens de otra sesión
            // si ya tenemos una generación distinta.
            if (
                this.generation &&
                this.generation !== message.generation
            ) {
                return;
            }

            if (this.rotationId === message.rotationId) {
                return;
            }

            this.generation = message.generation;
            this.rotationId = message.rotationId;

            this.setSession(message.tokens);
        }
    }

    private assertSessionVersion(version: number): void {
        if (version !== this.sessionVersion) {
            throw new Error(
                'La sesión cambió durante la operación.'
            );
        }
    }

    private setSession(tokens: AuthTokens): void {
        this.tokens.set(tokens);
        this.isAuthenticated.set(true);
    }

    private clearSession(): void {
        this.tokens.set(null);
        this.isAuthenticated.set(false);

        this.generation = null;
        this.rotationId = null;
    }
}
