import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { Venta, VentaResumen } from '../../core/models/api.models';
import { FormatoReporte, ReporteService } from '../../core/services/reporte.service';
import { VentaService } from '../../core/services/venta.service';

/** yyyy-MM-dd en hora local (evita el corrimiento de día de toISOString). */
const fechaIso = (d: Date | null): string | undefined => {
    if (!d) return undefined;
    const p = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
};

@Component({
    selector: 'app-historial-ventas',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DatePickerModule, DialogModule],
    template: `
        <div class="card">
            <div class="font-semibold text-xl mb-4">Ventas realizadas y reportes</div>

            <div class="flex flex-wrap items-end gap-4 mb-6">
                <div class="flex flex-col gap-2">
                    <label for="desde" class="font-medium">Desde</label>
                    <p-datepicker inputId="desde" [(ngModel)]="desde" dateFormat="dd/mm/yy" [showIcon]="true" [showButtonBar]="true" [maxDate]="hasta ?? hoy" />
                </div>
                <div class="flex flex-col gap-2">
                    <label for="hasta" class="font-medium">Hasta</label>
                    <p-datepicker inputId="hasta" [(ngModel)]="hasta" dateFormat="dd/mm/yy" [showIcon]="true" [showButtonBar]="true" [minDate]="desde ?? undefined" [maxDate]="hoy" />
                </div>
                <p-button label="Buscar" icon="pi pi-search" [loading]="cargando()" (onClick)="cargar()" />
                <div class="flex gap-2 ml-auto">
                    <p-button label="PDF" icon="pi pi-file-pdf" severity="danger" [loading]="descargando() === 'pdf'" [disabled]="!!descargando()" (onClick)="descargar('pdf')" />
                    <p-button label="Excel" icon="pi pi-file-excel" severity="success" [loading]="descargando() === 'excel'" [disabled]="!!descargando()" (onClick)="descargar('excel')" />
                </div>
            </div>

            <p-table [value]="ventas()" [loading]="cargando()" [paginator]="true" [rows]="10" [rowHover]="true" dataKey="id">
                <ng-template #header>
                    <tr>
                        <th pSortableColumn="numeroVenta">N° Venta <p-sortIcon field="numeroVenta" /></th>
                        <th pSortableColumn="fecha">Fecha <p-sortIcon field="fecha" /></th>
                        <th>Usuario</th>
                        <th class="text-right">Subtotal</th>
                        <th class="text-right">IVA</th>
                        <th class="text-right">Total</th>
                        <th style="width: 5rem"></th>
                    </tr>
                </ng-template>
                <ng-template #body let-v>
                    <tr>
                        <td class="font-medium">{{ v.numeroVenta }}</td>
                        <td>{{ v.fecha | date: 'dd/MM/yyyy HH:mm' }}</td>
                        <td>{{ v.usuario }}</td>
                        <td class="text-right">{{ v.subtotal | currency: 'USD' }}</td>
                        <td class="text-right">{{ v.iva | currency: 'USD' }}</td>
                        <td class="text-right font-bold">{{ v.total | currency: 'USD' }}</td>
                        <td class="text-right"><p-button icon="pi pi-eye" [rounded]="true" [text]="true" (onClick)="verDetalle(v)" /></td>
                    </tr>
                </ng-template>
                <ng-template #footer>
                    @if (ventas().length) {
                        <tr>
                            <td colspan="5" class="text-right font-bold">Total del período ({{ ventas().length }} ventas)</td>
                            <td class="text-right font-bold text-primary">{{ totalPeriodo() | currency: 'USD' }}</td>
                            <td></td>
                        </tr>
                    }
                </ng-template>
                <ng-template #emptymessage>
                    <tr>
                        <td colspan="7" class="text-center py-8 text-muted-color">No hay ventas en el rango seleccionado.</td>
                    </tr>
                </ng-template>
            </p-table>
        </div>

        <p-dialog [(visible)]="detalleVisible" [header]="detalle()?.numeroVenta ?? 'Detalle de venta'" [modal]="true" [style]="{ width: '40rem' }">
            @if (detalle(); as v) {
                <div class="text-muted-color mb-4">{{ v.fecha | date: 'dd/MM/yyyy HH:mm' }} · {{ v.usuario }}</div>
                <p-table [value]="v.detalle" size="small">
                    <ng-template #header>
                        <tr>
                            <th>Código</th>
                            <th>Producto</th>
                            <th class="text-right">Cant.</th>
                            <th class="text-right">Precio</th>
                            <th class="text-right">Subtotal</th>
                        </tr>
                    </ng-template>
                    <ng-template #body let-d>
                        <tr>
                            <td>{{ d.codigo }}</td>
                            <td>{{ d.producto }}</td>
                            <td class="text-right">{{ d.cantidad }}</td>
                            <td class="text-right">{{ d.precioUnitario | currency: 'USD' }}</td>
                            <td class="text-right">{{ d.subtotal | currency: 'USD' }}</td>
                        </tr>
                    </ng-template>
                </p-table>
                <div class="text-right mt-4 leading-8">
                    <div>Subtotal: {{ v.subtotal | currency: 'USD' }}</div>
                    <div>IVA: {{ v.iva | currency: 'USD' }}</div>
                    <div class="text-xl font-bold">Total: {{ v.total | currency: 'USD' }}</div>
                </div>
            }
        </p-dialog>
    `
})
export class HistorialVentas implements OnInit {
    private readonly ventasService = inject(VentaService);
    private readonly reportes = inject(ReporteService);
    private readonly toast = inject(MessageService);

    readonly hoy = new Date();
    desde: Date | null = null;
    hasta: Date | null = null;

    readonly ventas = signal<VentaResumen[]>([]);
    readonly cargando = signal(false);
    readonly descargando = signal<FormatoReporte | null>(null);
    readonly detalle = signal<Venta | null>(null);
    detalleVisible = false;

    ngOnInit() {
        this.cargar();
    }

    totalPeriodo() {
        return this.ventas().reduce((s, v) => s + v.total, 0);
    }

    cargar() {
        this.cargando.set(true);
        this.ventasService.listar(fechaIso(this.desde), fechaIso(this.hasta)).subscribe({
            next: (data) => {
                this.ventas.set(data);
                this.cargando.set(false);
            },
            error: () => this.cargando.set(false)
        });
    }

    verDetalle(v: VentaResumen) {
        this.ventasService.obtener(v.id).subscribe((venta) => {
            this.detalle.set(venta);
            this.detalleVisible = true;
        });
    }

    descargar(formato: FormatoReporte) {
        this.descargando.set(formato);
        this.reportes.descargar(formato, fechaIso(this.desde), fechaIso(this.hasta)).subscribe({
            next: () => {
                this.descargando.set(null);
                this.toast.add({ severity: 'success', summary: `Reporte ${formato === 'pdf' ? 'PDF' : 'Excel'} generado correctamente.` });
            },
            error: () => this.descargando.set(null)
        });
    }
}
