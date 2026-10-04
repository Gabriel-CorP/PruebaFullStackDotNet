export interface ApiResponse<T> {
    success: boolean;
    message: string;
    data: T;
    errors?: string[] | null;
}

export interface LoginResponse {
    token: string;
    expiraEn: string;
    usuarioId: number;
    nombreUsuario: string;
    nombreCompleto: string;
    rol: string;
}

export interface Producto {
    id: number;
    codigo: string;
    nombre: string;
    descripcion?: string | null;
    precio: number;
    stock: number;
    activo: boolean;
    fechaCreacion: string;
}

export interface ProductoRequest {
    codigo: string;
    nombre: string;
    descripcion?: string | null;
    precio: number;
    stock: number;
}

export interface VentaItemRequest {
    productoId: number;
    cantidad: number;
}

export interface VentaDetalle {
    id: number;
    productoId: number;
    codigo: string;
    producto: string;
    cantidad: number;
    precioUnitario: number;
    subtotal: number;
}

export interface Venta {
    id: number;
    numeroVenta: string;
    fecha: string;
    usuarioId: number;
    usuario: string;
    subtotal: number;
    iva: number;
    total: number;
    detalle: VentaDetalle[];
}

export interface VentaResumen {
    id: number;
    numeroVenta: string;
    fecha: string;
    usuario: string;
    subtotal: number;
    iva: number;
    total: number;
}
