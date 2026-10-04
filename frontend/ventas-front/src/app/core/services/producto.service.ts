import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map } from 'rxjs';
import { API_URL } from '../config';
import { ApiResponse, Producto, ProductoRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ProductoService {
    private readonly http = inject(HttpClient);
    private readonly url = `${API_URL}/productos`;

    listar(busqueda?: string) {
        let params = new HttpParams();
        if (busqueda) params = params.set('busqueda', busqueda);
        return this.http.get<ApiResponse<Producto[]>>(this.url, { params }).pipe(map((r) => r.data));
    }

    porCodigo(codigo: string) {
        return this.http.get<ApiResponse<Producto>>(`${this.url}/codigo/${encodeURIComponent(codigo)}`).pipe(map((r) => r.data));
    }

    crear(p: ProductoRequest) {
        return this.http.post<ApiResponse<Producto>>(this.url, p);
    }

    actualizar(id: number, p: ProductoRequest) {
        return this.http.put<ApiResponse<Producto>>(`${this.url}/${id}`, p);
    }

    eliminar(id: number) {
        return this.http.delete<ApiResponse<null>>(`${this.url}/${id}`);
    }
}
