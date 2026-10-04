import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { tap } from 'rxjs';
import { API_URL } from '../config';

export type FormatoReporte = 'pdf' | 'excel';

@Injectable({ providedIn: 'root' })
export class ReporteService {
    private readonly http = inject(HttpClient);

    /** Descarga el reporte de ventas (PDF o Excel) respetando el filtro de fechas. */
    descargar(formato: FormatoReporte, desde?: string, hasta?: string) {
        let params = new HttpParams();
        if (desde) params = params.set('desde', desde);
        if (hasta) params = params.set('hasta', hasta);

        return this.http
            .get(`${API_URL}/reportes/ventas/${formato}`, { params, responseType: 'blob', observe: 'response' })
            .pipe(
                tap((resp) => {
                    const ext = formato === 'pdf' ? 'pdf' : 'xlsx';
                    const nombre = this.nombreArchivo(resp.headers.get('content-disposition')) ?? `ReporteVentas.${ext}`;
                    this.guardar(resp.body!, nombre);
                })
            );
    }

    private nombreArchivo(header: string | null): string | null {
        const match = header?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
        return match ? decodeURIComponent(match[1]) : null;
    }

    private guardar(blob: Blob, nombre: string): void {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = nombre;
        a.click();
        URL.revokeObjectURL(url);
    }
}
