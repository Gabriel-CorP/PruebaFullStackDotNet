import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToolbarModule } from 'primeng/toolbar';
import { Producto, ProductoRequest } from '../../core/models/api.models';
import { ProductoService } from '../../core/services/producto.service';

@Component({
    selector: 'app-productos',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, InputNumberModule, TextareaModule, ToolbarModule, TagModule, IconFieldModule, InputIconModule],
    template: `
        <div class="card">
            <p-toolbar styleClass="mb-6">
                <ng-template #start>
                    <p-button label="Nuevo producto" icon="pi pi-plus" (onClick)="nuevo()" />
                </ng-template>
                <ng-template #end>
                    <p-button label="Actualizar" icon="pi pi-refresh" severity="secondary" [outlined]="true" (onClick)="cargar()" />
                </ng-template>
            </p-toolbar>

            <p-table
                #dt
                [value]="productos()"
                [loading]="cargando()"
                [rows]="10"
                [paginator]="true"
                [rowsPerPageOptions]="[10, 20, 50]"
                [globalFilterFields]="['codigo', 'nombre', 'descripcion']"
                [rowHover]="true"
                dataKey="id"
                currentPageReportTemplate="Mostrando {first} a {last} de {totalRecords} productos"
                [showCurrentPageReport]="true"
            >
                <ng-template #caption>
                    <div class="flex items-center justify-between">
                        <h5 class="m-0">Mantenimiento de productos</h5>
                        <p-iconfield>
                            <p-inputicon styleClass="pi pi-search" />
                            <input pInputText type="text" (input)="dt.filterGlobal($any($event.target).value, 'contains')" placeholder="Buscar por código o nombre..." />
                        </p-iconfield>
                    </div>
                </ng-template>
                <ng-template #header>
                    <tr>
                        <th pSortableColumn="codigo" style="min-width: 8rem">Código <p-sortIcon field="codigo" /></th>
                        <th pSortableColumn="nombre" style="min-width: 14rem">Nombre <p-sortIcon field="nombre" /></th>
                        <th style="min-width: 14rem">Descripción</th>
                        <th pSortableColumn="precio" class="text-right">Precio <p-sortIcon field="precio" /></th>
                        <th pSortableColumn="stock" class="text-center">Stock <p-sortIcon field="stock" /></th>
                        <th style="width: 8rem"></th>
                    </tr>
                </ng-template>
                <ng-template #body let-p>
                    <tr>
                        <td class="font-medium">{{ p.codigo }}</td>
                        <td>{{ p.nombre }}</td>
                        <td>{{ p.descripcion }}</td>
                        <td class="text-right">{{ p.precio | currency: 'USD' : 'symbol' : '1.2-2' }}</td>
                        <td class="text-center"><p-tag [value]="p.stock.toString()" [severity]="severidadStock(p.stock)" /></td>
                        <td class="text-right">
                            <p-button icon="pi pi-pencil" class="mr-2" [rounded]="true" [outlined]="true" (onClick)="editar(p)" />
                            <p-button icon="pi pi-trash" severity="danger" [rounded]="true" [outlined]="true" (onClick)="confirmarEliminar(p)" />
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage>
                    <tr>
                        <td colspan="6" class="text-center py-8 text-muted-color">No hay productos registrados.</td>
                    </tr>
                </ng-template>
            </p-table>
        </div>

        <p-dialog [(visible)]="dialogo" [header]="editandoId() ? 'Editar producto' : 'Nuevo producto'" [modal]="true" [style]="{ width: '34rem' }" [closable]="!guardando()">
            <div class="flex flex-col gap-5 pt-2">
                <div>
                    <label for="codigo" class="block font-bold mb-2">Código *</label>
                    <input pInputText id="codigo" class="w-full" maxlength="30" [(ngModel)]="form.codigo" [class.ng-invalid]="enviado && !form.codigo.trim()" (blur)="form.codigo = form.codigo.toUpperCase()" />
                    @if (enviado && !form.codigo.trim()) {
                        <small class="text-red-500">El código es obligatorio.</small>
                    }
                </div>
                <div>
                    <label for="nombre" class="block font-bold mb-2">Nombre *</label>
                    <input pInputText id="nombre" class="w-full" maxlength="150" [(ngModel)]="form.nombre" [class.ng-invalid]="enviado && !form.nombre.trim()" />
                    @if (enviado && !form.nombre.trim()) {
                        <small class="text-red-500">El nombre es obligatorio.</small>
                    }
                </div>
                <div>
                    <label for="descripcion" class="block font-bold mb-2">Descripción</label>
                    <textarea pTextarea id="descripcion" rows="3" class="w-full" maxlength="500" [(ngModel)]="form.descripcion"></textarea>
                </div>
                <div class="grid grid-cols-2 gap-4">
                    <div>
                        <label for="precio" class="block font-bold mb-2">Precio *</label>
                        <p-inputnumber inputId="precio" [(ngModel)]="form.precio" mode="currency" currency="USD" locale="en-US" [min]="0" [fluid]="true" />
                        @if (enviado && (form.precio === null || form.precio < 0)) {
                            <small class="text-red-500">Ingrese un precio válido.</small>
                        }
                    </div>
                    <div>
                        <label for="stock" class="block font-bold mb-2">Stock *</label>
                        <p-inputnumber inputId="stock" [(ngModel)]="form.stock" [min]="0" [useGrouping]="false" [fluid]="true" />
                        @if (enviado && (form.stock === null || form.stock < 0)) {
                            <small class="text-red-500">Ingrese un stock válido.</small>
                        }
                    </div>
                </div>
            </div>
            <ng-template #footer>
                <p-button label="Cancelar" icon="pi pi-times" severity="secondary" [text]="true" [disabled]="guardando()" (onClick)="dialogo = false" />
                <p-button label="Guardar" icon="pi pi-check" [loading]="guardando()" (onClick)="guardar()" />
            </ng-template>
        </p-dialog>
    `
})
export class Productos implements OnInit {
    private readonly service = inject(ProductoService);
    private readonly toast = inject(MessageService);
    private readonly confirm = inject(ConfirmationService);

    readonly productos = signal<Producto[]>([]);
    readonly cargando = signal(false);
    readonly guardando = signal(false);
    readonly editandoId = signal<number | null>(null);

    dialogo = false;
    enviado = false;
    form: FormProducto = this.formVacio();

    ngOnInit() {
        this.cargar();
    }

    cargar() {
        this.cargando.set(true);
        this.service.listar().subscribe({
            next: (data) => {
                this.productos.set(data);
                this.cargando.set(false);
            },
            error: () => this.cargando.set(false)
        });
    }

    severidadStock(stock: number): 'danger' | 'warn' | 'success' {
        return stock === 0 ? 'danger' : stock < 10 ? 'warn' : 'success';
    }

    nuevo() {
        this.editandoId.set(null);
        this.form = this.formVacio();
        this.enviado = false;
        this.dialogo = true;
    }

    editar(p: Producto) {
        this.editandoId.set(p.id);
        this.form = { codigo: p.codigo, nombre: p.nombre, descripcion: p.descripcion ?? '', precio: p.precio, stock: p.stock };
        this.enviado = false;
        this.dialogo = true;
    }

    guardar() {
        this.enviado = true;
        const f = this.form;
        if (!f.codigo.trim() || !f.nombre.trim() || f.precio === null || f.precio < 0 || f.stock === null || f.stock < 0) return;

        const request: ProductoRequest = {
            codigo: f.codigo.trim().toUpperCase(),
            nombre: f.nombre.trim(),
            descripcion: f.descripcion?.trim() || null,
            precio: f.precio,
            stock: f.stock
        };

        const id = this.editandoId();
        const operacion = id ? this.service.actualizar(id, request) : this.service.crear(request);

        this.guardando.set(true);
        operacion.subscribe({
            next: (resp) => {
                this.toast.add({ severity: 'success', summary: resp.message });
                this.guardando.set(false);
                this.dialogo = false;
                this.cargar();
            },
            error: () => this.guardando.set(false)
        });
    }

    /** Alerta de confirmación antes de eliminar. */
    confirmarEliminar(p: Producto) {
        this.confirm.confirm({
            header: 'Confirmar eliminación',
            message: `¿Está seguro de eliminar el producto "${p.nombre}" (${p.codigo})? Esta acción no se puede deshacer.`,
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Sí, eliminar',
            rejectLabel: 'Cancelar',
            acceptButtonProps: { severity: 'danger' },
            rejectButtonProps: { severity: 'secondary', outlined: true },
            accept: () =>
                this.service.eliminar(p.id).subscribe({
                    next: (resp) => {
                        this.toast.add({ severity: 'success', summary: resp.message });
                        this.cargar();
                    }
                })
        });
    }

    private formVacio(): FormProducto {
        return { codigo: '', nombre: '', descripcion: '', precio: 0, stock: 0 };
    }
}

interface FormProducto {
    codigo: string;
    nombre: string;
    descripcion: string;
    precio: number | null;
    stock: number | null;
}
