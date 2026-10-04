import { CommonModule } from '@angular/common';
import { Component, computed, ElementRef, inject, signal, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { IVA_RATE } from '../../core/config';
import { Producto, Venta } from '../../core/models/api.models';
import { ProductoService } from '../../core/services/producto.service';
import { VentaService } from '../../core/services/venta.service';

interface Linea {
    producto: Producto;
    cantidad: number;
}

const redondear = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;

@Component({
    selector: 'app-nueva-venta',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, InputNumberModule, DialogModule],
    template: `
        <div class="grid grid-cols-12 gap-6">
            <!-- Búsqueda + detalle -->
            <div class="col-span-12 xl:col-span-8">
                <div class="card">
                    <div class="font-semibold text-xl mb-4">Nueva venta</div>

                    <form class="flex gap-2 mb-6" (ngSubmit)="agregarPorCodigo()">
                        <input #codigoInput pInputText name="codigo" class="flex-1" placeholder="Digite el código del producto y presione Enter" [(ngModel)]="codigo" [disabled]="buscando()" autocomplete="off" autofocus />
                        <p-button type="submit" label="Agregar" icon="pi pi-plus" [loading]="buscando()" />
                    </form>

                    <p-table [value]="lineas()" dataKey="producto.id">
                        <ng-template #header>
                            <tr>
                                <th style="width: 8rem">Código</th>
                                <th>Producto</th>
                                <th class="text-right" style="width: 8rem">Precio</th>
                                <th class="text-center" style="width: 11rem">Cantidad</th>
                                <th class="text-right" style="width: 9rem">Subtotal</th>
                                <th style="width: 4rem"></th>
                            </tr>
                        </ng-template>
                        <ng-template #body let-l>
                            <tr>
                                <td class="font-medium">{{ l.producto.codigo }}</td>
                                <td>
                                    {{ l.producto.nombre }}
                                    <div class="text-xs text-muted-color">Stock disponible: {{ l.producto.stock }}</div>
                                </td>
                                <td class="text-right">{{ l.producto.precio | currency: 'USD' }}</td>
                                <td class="text-center">
                                    <p-inputnumber
                                        [ngModel]="l.cantidad"
                                        (ngModelChange)="cambiarCantidad(l, $event)"
                                        [showButtons]="true"
                                        buttonLayout="horizontal"
                                        [min]="1"
                                        [max]="l.producto.stock"
                                        [useGrouping]="false"
                                        inputStyleClass="text-center"
                                        [inputStyle]="{ width: '4rem' }"
                                        decrementButtonClass="p-button-secondary"
                                        incrementButtonClass="p-button-secondary"
                                        incrementButtonIcon="pi pi-plus"
                                        decrementButtonIcon="pi pi-minus"
                                    />
                                </td>
                                <td class="text-right font-medium">{{ l.producto.precio * l.cantidad | currency: 'USD' }}</td>
                                <td class="text-right"><p-button icon="pi pi-trash" severity="danger" [text]="true" [rounded]="true" (onClick)="quitar(l)" /></td>
                            </tr>
                        </ng-template>
                        <ng-template #emptymessage>
                            <tr>
                                <td colspan="6" class="text-center py-10 text-muted-color">
                                    <i class="pi pi-shopping-cart text-3xl mb-3 block"></i>
                                    Busque un producto por código para agregarlo al detalle.
                                </td>
                            </tr>
                        </ng-template>
                    </p-table>
                </div>
            </div>

            <!-- Resumen -->
            <div class="col-span-12 xl:col-span-4">
                <div class="card">
                    <div class="font-semibold text-xl mb-4">Resumen</div>
                    <div class="flex justify-between mb-3 text-lg">
                        <span>Artículos</span><span>{{ totalArticulos() }}</span>
                    </div>
                    <div class="flex justify-between mb-3 text-lg">
                        <span>Subtotal</span><span>{{ subtotal() | currency: 'USD' }}</span>
                    </div>
                    <div class="flex justify-between mb-3 text-lg">
                        <span>IVA ({{ ivaPorcentaje }}%)</span><span>{{ iva() | currency: 'USD' }}</span>
                    </div>
                    <hr class="my-4 border-surface" />
                    <div class="flex justify-between mb-6 text-2xl font-bold">
                        <span>Total</span><span class="text-primary">{{ total() | currency: 'USD' }}</span>
                    </div>
                    <div class="flex flex-col gap-3">
                        <p-button label="Registrar venta" icon="pi pi-check" size="large" styleClass="w-full" [disabled]="lineas().length === 0" [loading]="guardando()" (onClick)="registrar()" />
                        <p-button label="Limpiar" icon="pi pi-times" severity="secondary" [outlined]="true" styleClass="w-full" [disabled]="lineas().length === 0 || guardando()" (onClick)="limpiar()" />
                    </div>
                </div>
            </div>
        </div>

        <!-- Comprobante -->
        <p-dialog [(visible)]="comprobanteVisible" header="Venta registrada" [modal]="true" [style]="{ width: '38rem' }" (onHide)="enfocar()">
            @if (comprobante(); as v) {
                <div class="flex justify-between mb-4">
                    <div>
                        <div class="text-2xl font-bold text-primary">{{ v.numeroVenta }}</div>
                        <div class="text-muted-color">{{ v.fecha | date: 'dd/MM/yyyy HH:mm' }} · {{ v.usuario }}</div>
                    </div>
                    <i class="pi pi-check-circle text-green-500" style="font-size: 2.5rem"></i>
                </div>
                <p-table [value]="v.detalle" size="small">
                    <ng-template #header>
                        <tr>
                            <th>Producto</th>
                            <th class="text-right">Cant.</th>
                            <th class="text-right">Precio</th>
                            <th class="text-right">Subtotal</th>
                        </tr>
                    </ng-template>
                    <ng-template #body let-d>
                        <tr>
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
            <ng-template #footer>
                <p-button label="Nueva venta" icon="pi pi-plus" (onClick)="comprobanteVisible = false" />
            </ng-template>
        </p-dialog>
    `
})
export class NuevaVenta {
    private readonly productos = inject(ProductoService);
    private readonly ventas = inject(VentaService);
    private readonly toast = inject(MessageService);

    @ViewChild('codigoInput') codigoInput?: ElementRef<HTMLInputElement>;

    readonly ivaPorcentaje = IVA_RATE * 100;
    readonly lineas = signal<Linea[]>([]);
    readonly buscando = signal(false);
    readonly guardando = signal(false);
    readonly comprobante = signal<Venta | null>(null);

    codigo = '';
    comprobanteVisible = false;

    // ----- Cálculos en pantalla -----
    readonly totalArticulos = computed(() => this.lineas().reduce((s, l) => s + l.cantidad, 0));
    readonly subtotal = computed(() => redondear(this.lineas().reduce((s, l) => s + l.producto.precio * l.cantidad, 0)));
    readonly iva = computed(() => redondear(this.subtotal() * IVA_RATE));
    readonly total = computed(() => redondear(this.subtotal() + this.iva()));

    agregarPorCodigo() {
        const codigo = this.codigo.trim();
        if (!codigo) return;

        this.buscando.set(true);
        this.productos.porCodigo(codigo).subscribe({
            next: (p) => {
                this.buscando.set(false);
                this.agregar(p);
                this.codigo = '';
                this.enfocar();
            },
            error: () => {
                this.buscando.set(false);
                this.enfocar();
            }
        });
    }

    private agregar(p: Producto) {
        if (p.stock <= 0) {
            this.toast.add({ severity: 'warn', summary: `"${p.nombre}" no tiene stock disponible.` });
            return;
        }

        const existente = this.lineas().find((l) => l.producto.id === p.id);
        if (existente) {
            if (existente.cantidad + 1 > p.stock) {
                this.toast.add({ severity: 'warn', summary: `Stock máximo alcanzado para "${p.nombre}" (${p.stock}).` });
                return;
            }
            existente.cantidad++;
            this.lineas.update((l) => [...l]);
        } else {
            this.lineas.update((l) => [...l, { producto: p, cantidad: 1 }]);
        }
    }

    cambiarCantidad(linea: Linea, cantidad: number | null) {
        linea.cantidad = Math.min(Math.max(cantidad ?? 1, 1), linea.producto.stock);
        this.lineas.update((l) => [...l]);
    }

    quitar(linea: Linea) {
        this.lineas.update((l) => l.filter((x) => x !== linea));
    }

    limpiar() {
        this.lineas.set([]);
        this.enfocar();
    }

    registrar() {
        if (this.lineas().length === 0) return;

        this.guardando.set(true);
        this.ventas.registrar(this.lineas().map((l) => ({ productoId: l.producto.id, cantidad: l.cantidad }))).subscribe({
            next: (resp) => {
                this.guardando.set(false);
                this.toast.add({ severity: 'success', summary: resp.message });
                this.comprobante.set(resp.data); // totales oficiales calculados por el servidor
                this.comprobanteVisible = true;
                this.lineas.set([]);
            },
            error: () => this.guardando.set(false)
        });
    }

    enfocar() {
        setTimeout(() => this.codigoInput?.nativeElement.focus(), 0);
    }
}
