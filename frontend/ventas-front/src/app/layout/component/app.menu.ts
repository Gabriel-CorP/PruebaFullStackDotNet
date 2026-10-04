import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AuthService } from '../../core/services/auth.service';
import { AppMenuitem } from './app.menuitem';

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [CommonModule, AppMenuitem, RouterModule],
    template: `<ul class="layout-menu">
        <ng-container *ngFor="let item of model; let i = index">
            <li app-menuitem *ngIf="!item.separator" [item]="item" [index]="i" [root]="true"></li>
            <li *ngIf="item.separator" class="menu-separator"></li>
        </ng-container>
    </ul> `
})
export class AppMenu implements OnInit {
    private readonly auth = inject(AuthService);
    model: MenuItem[] = [];

    ngOnInit() {
        // El operador solo ve "Nueva venta"; el administrador ve además el mantenimiento y los reportes.
        this.model = [
            {
                label: 'Ventas',
                items: [{ label: 'Nueva venta', icon: 'pi pi-fw pi-shopping-cart', routerLink: ['/ventas/nueva'] }]
            }
        ];

        if (this.auth.isAdmin()) {
            this.model.push({
                label: 'Administración',
                items: [
                    { label: 'Productos', icon: 'pi pi-fw pi-box', routerLink: ['/productos'] },
                    { label: 'Ventas y reportes', icon: 'pi pi-fw pi-file-export', routerLink: ['/ventas/historial'] }
                ]
            });
        }
    }
}
