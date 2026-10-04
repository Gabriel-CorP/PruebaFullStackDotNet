# Ventas Web (Angular 19 + PrimeNG 19 + plantilla Sakai)

Frontend de la API de ventas. Base: plantilla Sakai-NG 19.0.1 (MIT) con las pantallas demo reemplazadas.

## Pantallas
| Ruta | Rol | Descripción |
|---|---|---|
| /auth/login | público | Inicio de sesión (JWT) |
| /ventas/nueva | Operador, Administrador | Búsqueda por código, detalle, cálculo de subtotal/IVA/total en pantalla y registro |
| /productos | Administrador | CRUD de productos, confirmación antes de eliminar |
| /ventas/historial | Administrador | Ventas por rango de fechas, detalle y descarga de reportes PDF / Excel |

## Estructura relevante
```
src/app/core        -> config, modelos, servicios (auth, producto, venta, reporte), interceptores (JWT + errores), guards (sesión y rol)
src/app/pages       -> login, productos, ventas (nueva-venta, historial-ventas)
src/app/layout      -> layout de Sakai (menú por rol y topbar con usuario)
```
- `authInterceptor` agrega el Bearer token; `errorInterceptor` muestra los errores del backend como toast y cierra sesión en 401.
- Las llamadas van a `/api/...`: en desarrollo las reenvía `proxy.conf.json` y en Docker lo hace nginx (no hay CORS).

## Desarrollo
```
npm install
npm start          # http://localhost:4200
```
`proxy.conf.json` apunta a `http://localhost:8080` (API en Docker). Si corres la API con `dotnet run` / Visual Studio,
cambia el `target` por la URL de la API (si usa HTTPS: `https://localhost:PUERTO` con `"secure": false`).

## Docker
Agregar al `docker-compose.yml` de la solución (con esta carpeta como `ventas-web/` junto a `src/`):
```yaml
  web:
    build: ./ventas-web
    container_name: ventas-web
    depends_on:
      - api
    ports:
      - "4200:80"
```
Luego `docker compose up --build` y abrir http://localhost:4200
