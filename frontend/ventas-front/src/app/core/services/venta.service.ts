import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map } from 'rxjs';
import { API_URL } from '../config';
import { ApiResponse, Venta, VentaItemRequest, VentaResumen } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class VentaService {
    private readonly http = inject(HttpClient);
    private readonly url = `${API_URL}/ventas`;

    registrar(items: VentaItemRequest[]) {
        return this.http.post<ApiResponse<Venta>>(this.url, { items });
    }

    obtener(id: number) {
        return this.http.get<ApiResponse<Venta>>(`${this.url}/${id}`).pipe(map((r) => r.data));
    }

    listar(desde?: string, hasta?: string) {
        let params = new HttpParams();
        if (desde) params = params.set('desde', desde);
        if (hasta) params = params.set('hasta', hasta);
        return this.http.get<ApiResponse<VentaResumen[]>>(this.url, { params }).pipe(map((r) => r.data));
    }
}
