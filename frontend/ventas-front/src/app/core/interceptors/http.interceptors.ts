import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MessageService } from 'primeng/api';
import { catchError, from, switchMap, tap, throwError } from 'rxjs';
import { API_URL } from '../config';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const token = inject(AuthService).token;
    if (token && req.url.startsWith(API_URL)) {
        req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    }
    return next(req);
};

interface ErrorInfo {
    message: string;
    errors: string[];
}

async function readError(err: HttpErrorResponse): Promise<ErrorInfo> {
    let body: any = err.error;
    if (body instanceof Blob) {
        try {
            body = JSON.parse(await body.text());
        } catch {
            body = null;
        }
    }

    if (err.status === 0) return { message: 'No se pudo conectar con el servidor. Verifique que la API esté en ejecución.', errors: [] };
    if (err.status === 403) return { message: 'No tiene permisos para realizar esta operación.', errors: [] };
    if (err.status === 401 && !body?.message) return { message: 'Su sesión expiró. Inicie sesión nuevamente.', errors: [] };

    return {
        message: body?.message ?? 'Ocurrió un error inesperado. Intente nuevamente.',
        errors: Array.isArray(body?.errors) ? body.errors : []
    };
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const toast = inject(MessageService);
    const auth = inject(AuthService);

    return next(req).pipe(
        catchError((err: HttpErrorResponse) =>
            from(readError(err)).pipe(
                tap((info) => {
                    const severity = err.status >= 500 || err.status === 0 ? 'error' : 'warn';
                    toast.add({
                        severity,
                        summary: info.message,
                        detail: info.errors.join('\n') || undefined,
                        life: 6000
                    });
                    const esLogin = req.url.endsWith('/auth/login');
                    if (err.status === 401 && !esLogin) auth.logout();
                }),
                switchMap(() => throwError(() => err))
            )
        )
    );
};
