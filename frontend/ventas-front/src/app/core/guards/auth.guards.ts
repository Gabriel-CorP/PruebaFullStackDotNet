import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** Exige sesión iniciada. */
export const authGuard: CanActivateFn = () => {
    const auth = inject(AuthService);
    return auth.isAuthenticated() ? true : inject(Router).createUrlTree(['/auth/login']);
};

/** Exige uno de los roles indicados. */
export const roleGuard =
    (...roles: string[]): CanActivateFn =>
    () => {
        const auth = inject(AuthService);
        const rol = auth.session()?.rol;
        return rol && roles.includes(rol) ? true : inject(Router).createUrlTree(['/auth/access']);
    };

/** El login no se muestra si ya hay sesión. */
export const guestGuard: CanActivateFn = () => {
    const auth = inject(AuthService);
    return auth.isAuthenticated() ? inject(Router).parseUrl(auth.homeUrl()) : true;
};

/** Redirige "/" a la pantalla inicial del rol. */
export const homeRedirectGuard: CanActivateFn = () => {
    const auth = inject(AuthService);
    return inject(Router).parseUrl(auth.homeUrl());
};
