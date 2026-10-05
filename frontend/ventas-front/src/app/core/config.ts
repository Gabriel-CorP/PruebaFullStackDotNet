import { environment } from "../../environments/environment";

export const API_URL = environment.apiUrl;

export const IVA_RATE = 0.13;

export const ROLES = {
    Administrador: 'Administrador',
    Operador: 'Operador'
} as const;
