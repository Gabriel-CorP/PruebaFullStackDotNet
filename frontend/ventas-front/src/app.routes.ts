import { Routes } from '@angular/router';
import { AppLayout } from './app/layout/component/app.layout';
import { authGuard, homeRedirectGuard, roleGuard } from './app/core/guards/auth.guards';
import { ROLES } from './app/core/config';
import { Notfound } from './app/pages/notfound/notfound';

export const appRoutes: Routes = [
    {
        path: '',
        component: AppLayout,
        canActivate: [authGuard],
        children: [
            { path: '', pathMatch: 'full', canActivate: [homeRedirectGuard], children: [] },
            {
                path: 'productos',
                canActivate: [roleGuard(ROLES.Administrador)],
                loadComponent: () => import('./app/pages/productos/productos').then((m) => m.Productos)
            },
            {
                path: 'ventas/nueva',
                canActivate: [roleGuard(ROLES.Administrador, ROLES.Operador)],
                loadComponent: () => import('./app/pages/ventas/nueva-venta').then((m) => m.NuevaVenta)
            },
            {
                path: 'ventas/historial',
                canActivate: [roleGuard(ROLES.Administrador)],
                loadComponent: () => import('./app/pages/ventas/historial-ventas').then((m) => m.HistorialVentas)
            }
        ]
    },
    { path: 'notfound', component: Notfound },
    { path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
