import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../core/services/auth.service';
import { AppFloatingConfigurator } from '../../layout/component/app.floatingconfigurator';
import { MessageService } from 'primeng/api';

@Component({
    selector: 'app-login',
    standalone: true,
    imports: [ButtonModule, InputTextModule, PasswordModule, FormsModule, AppFloatingConfigurator],
    template: `
        <app-floating-configurator />
        <div class="bg-surface-50 dark:bg-surface-950 flex items-center justify-center min-h-screen min-w-[100vw] overflow-hidden">
            <div class="flex flex-col items-center justify-center">
                <div style="border-radius: 56px; padding: 0.3rem; background: linear-gradient(180deg, var(--primary-color) 10%, rgba(33, 150, 243, 0) 30%)">
                    <div class="w-full bg-surface-0 dark:bg-surface-900 py-16 px-8 sm:px-20" style="border-radius: 53px">
                        <div class="text-center mb-8">
                            <i class="pi pi-shopping-cart text-primary mb-6" style="font-size: 3.5rem"></i>
                            <div class="text-surface-900 dark:text-surface-0 text-3xl font-medium mb-3">Sistema de Ventas</div>
                            <span class="text-muted-color font-medium">Inicie sesión para continuar</span>
                        </div>

                        <form (ngSubmit)="entrar()" class="w-full md:w-[26rem]">
                            <label for="usuario" class="block text-surface-900 dark:text-surface-0 text-xl font-medium mb-2">Usuario</label>
                            <input pInputText id="usuario" name="usuario" type="text" autocomplete="username" placeholder="Nombre de usuario" class="w-full mb-6" [(ngModel)]="usuario" [disabled]="cargando()" autofocus />

                            <label for="password" class="block text-surface-900 dark:text-surface-0 font-medium text-xl mb-2">Contraseña</label>
                            <p-password inputId="password" name="password" [(ngModel)]="password" placeholder="Contraseña" [toggleMask]="true" [fluid]="true" [feedback]="false" [disabled]="cargando()" styleClass="mb-8" />

                            <p-button type="submit" label="Ingresar" icon="pi pi-sign-in" styleClass="w-full" [loading]="cargando()" />
                        </form>
                    </div>
                </div>
            </div>
        </div>
    `
})
export class Login {
    private readonly auth = inject(AuthService);
    private readonly router = inject(Router);
    private readonly toast = inject(MessageService);

    usuario = '';
    password = '';
    readonly cargando = signal(false);

    entrar() {
        if (!this.usuario.trim() || !this.password) {
            this.toast.add({ severity: 'warn', summary: 'Ingrese usuario y contraseña.' });
            return;
        }

        this.cargando.set(true);
        this.auth.login(this.usuario.trim(), this.password).subscribe({
            next: (s) => {
                this.toast.add({ severity: 'success', summary: `Bienvenido, ${s.nombreCompleto}` });
                this.router.navigateByUrl(this.auth.homeUrl());
            },
            error: () => this.cargando.set(false) // el mensaje lo muestra el interceptor
        });
    }
}
