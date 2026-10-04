import { Routes } from '@angular/router';
import { guestGuard } from '../../core/guards/auth.guards';
import { Access } from './access';
import { Login } from './login';
import { Error } from './error';

export default [
    { path: 'access', component: Access },
    { path: 'error', component: Error },
    { path: 'login', component: Login, canActivate: [guestGuard] }
] as Routes;
