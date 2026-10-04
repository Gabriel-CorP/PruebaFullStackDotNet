/** Base de la API. En desarrollo la resuelve proxy.conf.json y en Docker el nginx (evita CORS). */
export const API_URL = '/api';

/** Debe coincidir con @PorcentajeIva de usp_Venta_Registrar (el servidor recalcula igualmente). */
export const IVA_RATE = 0.13;

export const ROLES = {
    Administrador: 'Administrador',
    Operador: 'Operador'
} as const;
