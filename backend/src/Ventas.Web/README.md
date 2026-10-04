# Ventas.Web — aplicación ASP.NET Core MVC (.NET 8)

Aplicación web MVC que consume la **API REST de ventas** (la misma que usa el front Angular).

## Arquitectura (MVC)
```
Controllers/      -> Account, Home, Productos, Ventas, Reportes   (C)
Views/            -> Razor (layout, login, productos, ventas)      (V)
Models/           -> Api (DTOs de la API) y ViewModels             (M)
Services/         -> IVentasApiClient / VentasApiClient (HttpClient tipado), BearerTokenHandler, ApiException
Infrastructure/   -> ApiExceptionFilter (manejo de errores centralizado)
wwwroot/js        -> site.js (notificaciones, confirmar eliminación), ventas.js (POS), historial.js
```

## Cómo funciona
- **Login:** `AccountController` llama a `POST /api/auth/login`; el JWT se guarda **dentro de la cookie de sesión cifrada**
  (no queda accesible desde JavaScript) y la cookie vence junto con el token.
- **Cada llamada a la API** lleva `Authorization: Bearer <jwt>` (lo agrega `BearerTokenHandler`).
- **Roles:** Operador → solo "Nueva venta". Administrador → además Productos y Ventas/Reportes. La política por defecto exige sesión en todo el sitio.
- **Nueva venta:** búsqueda por código (AJAX), detalle y subtotal/IVA/total calculados en pantalla con JavaScript;
  al registrar, el servidor (API) recalcula todo y se muestra el comprobante.
- **Productos:** CRUD con validación (servidor + cliente) y **alerta de confirmación (SweetAlert2) antes de eliminar**.
- **Reportes:** botones PDF y Excel que descargan lo generado por la API (respeta el rango de fechas).
- **Notificaciones y errores:** mensajes de éxito/aviso/error como toast; si la API cae se muestra una pantalla amigable;
  si el token vence se cierra la sesión y se vuelve al login.
- **Seguridad:** antiforgery en todos los POST (incluido AJAX), HTML escapado (Razor / `textContent`), cookie HttpOnly.

## Configuración (`appsettings.json`)
| Clave | Descripción |
|---|---|
| `Api:BaseUrl` | URL de la API. Docker: `http://api:8080`. Local con la API en Docker: `http://localhost:8080` |
| `Ventas:IvaRate` | Debe coincidir con `@PorcentajeIva` del SP (0.13) |

## Ejecutar
```
dotnet run --project src/Ventas.Web
```
Si la API usa HTTPS con certificado de desarrollo, confíe en él: `dotnet dev-certs https --trust`.

## Docker (agregar al docker-compose.yml de la solución)
```yaml
  mvc:
    build: ./src/Ventas.Web
    container_name: ventas-mvc
    depends_on:
      - api
    environment:
      Api__BaseUrl: http://api:8080
    ports:
      - "5100:8080"
```
Abrir http://localhost:5100
