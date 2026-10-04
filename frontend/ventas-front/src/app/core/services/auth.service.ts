import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { map, tap } from 'rxjs';
import { API_URL, ROLES } from '../config';
import { ApiResponse, LoginResponse } from '../models/api.models';

const STORAGE_KEY = 'ventas.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly router = inject(Router);

    private readonly _session = signal<LoginResponse | null>(this.restore());

    readonly session = this._session.asReadonly();
    readonly isAuthenticated = computed(() => {
        const s = this._session();
        return !!s && new Date(s.expiraEn).getTime() > Date.now();
    });
    readonly isAdmin = computed(() => this._session()?.rol === ROLES.Administrador);

    get token(): string | null {
        return this.isAuthenticated() ? this._session()!.token : null;
    }

    /** Ruta inicial según el rol: el operador solo puede vender. */
    homeUrl(): string {
        return this.isAdmin() ? '/productos' : '/ventas/nueva';
    }

    login(nombreUsuario: string, password: string) {
        return this.http.post<ApiResponse<LoginResponse>>(`${API_URL}/auth/login`, { nombreUsuario, password }).pipe(
            map((r) => r.data),
            tap((session) => {
                this._session.set(session);
                try {
                    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
                } catch {
                    /* almacenamiento no disponible: la sesión vive solo en memoria */
                }
            })
        );
    }

    logout(): void {
        this._session.set(null);
        try {
            sessionStorage.removeItem(STORAGE_KEY);
        } catch {
            /* nada */
        }
        this.router.navigate(['/auth/login']);
    }

    private restore(): LoginResponse | null {
        try {
            const raw = sessionStorage.getItem(STORAGE_KEY);
            return raw ? (JSON.parse(raw) as LoginResponse) : null;
        } catch {
            return null;
        }
    }
}
