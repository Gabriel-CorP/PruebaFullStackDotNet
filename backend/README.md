# Sistema de Ventas

Prueba técnica: aplicación de ventas con inicio de sesión, mantenimiento de productos, registro de ventas y reportes en PDF y Excel.

La solución tiene **una API REST** (.NET 8) y **dos interfaces web** que consumen la misma API:

| Componente | Tecnología | Carpeta |
|---|---|---|
| Base de datos | SQL Server 2022, procedimientos almacenados | `sql/` |
| API REST | ASP.NET Core Web API (.NET 8), EF Core Database First, CQRS, UnitOfWork, JWT | `src/Ventas.Application`, `src/Ventas.Infrastructure`, `src/Ventas.WebApi` |
| Aplicación web MVC | ASP.NET Core MVC (.NET 8), Razor, JavaScript, Bootstrap | `src/Ventas.Web` |
| Aplicación web SPA (opcional) | Angular 19, PrimeNG 19, plantilla Sakai | `ventas-web/` |

---

## 1. Cumplimiento de los requisitos

| Requisito | Dónde se resuelve |
|---|---|
| Inicio de sesión con usuario y contraseña previamente registrados | `POST /api/auth/login` (JWT). Usuarios semilla en `sql/01_VentasDB.sql`. Pantalla de login en ambos frontends |
| El operador solo puede realizar ventas | Roles `Administrador` y `Operador`. La API restringe los endpoints y los frontends ocultan el resto del menú |
| Aplicación web que consuma la API REST y haga CRUD de productos | MVC (`/Productos`) y Angular (`/productos`) llaman a `api/productos` |
| Búsqueda de productos por código, agregar al detalle y calcular en pantalla | Pantalla "Nueva venta": búsqueda por código (Enter), cantidades editables, subtotal, IVA y total calculados con JavaScript |
| Guardar la venta usando la API | `POST /api/ventas` |
| Tablas `Productos`, `Ventas`, `DetalleVentas` con procedimientos almacenados | `sql/01_VentasDB.sql` (todo el acceso a datos pasa por SP) |
| Notificaciones de la operación y manejo de errores | Notificaciones tipo toast en ambos frontends. API con middleware global de errores y respuestas `{ success, message, data, errors }` |
| Reporte de ventas en PDF y Excel | `GET /api/reportes/ventas/pdf` y `/excel` (QuestPDF y ClosedXML), con filtro de fechas |
| C# en .NET Core | .NET 8 |
| Contraseña encriptada, visible en la base de datos | `PasswordHash` (SHA-512) y `PasswordSalt` por usuario. Ver sección 7 |
| Alerta de confirmación al eliminar un producto | Diálogo "¿Está seguro?" en MVC (SweetAlert2) y en Angular (ConfirmDialog) |
| Patrón MVC | Aplicación `Ventas.Web` (Controllers / Views / Models). El front Angular es una alternativa SPA sobre la misma API |
| JavaScript si se requiere | Cálculos de la venta, búsquedas AJAX, confirmaciones y notificaciones |
| Entity Framework | EF Core 8, Database First (`dotnet ef dbcontext scaffold`) |

---

## 2. Estructura del repositorio

```
.
├── docker-compose.yml          Servicios base: BD, API, MVC y Angular (opcional)
├── docker-compose.override.yml Puertos y entorno de desarrollo
├── docker-compose.dcproj       Proyecto de Visual Studio (botón Run con Docker)
├── .dockerignore
├── .env.example                Variables opcionales (contraseña de SQL Server, clave JWT)
├── sql/
│   ├── 01_VentasDB.sql         Tablas, tipo TVP, procedimientos almacenados y datos semilla
│   └── init.sh                 Crea la BD dentro de Docker (solo la primera vez)
├── src/
│   ├── Ventas.Application/     CQRS (MediatR), validaciones (FluentValidation), DTOs, interfaces
│   ├── Ventas.Infrastructure/  DbContext, UnitOfWork, repositorios con SP, JWT, hash, reportes
│   ├── Ventas.WebApi/          Controllers, middleware de errores, JWT, Swagger, CORS  (+ Dockerfile)
│   └── Ventas.Web/             Aplicación MVC                                          (+ Dockerfile)
└── ventas-web/                 Aplicación Angular (opcional)                           (+ Dockerfile)
```

### Arquitectura de la API

```
WebApi  ──►  Application  ◄──  Infrastructure
(Controllers)  (casos de uso, interfaces)  (EF Core, SQL Server, JWT, reportes)
```

- **Application** no conoce EF ni SQL. Solo depende de interfaces (`IUnitOfWork`, repositorios, `IStoredProcedureExecutor`).
- **CQRS con MediatR**: cada operación es un *Command* o un *Query* con su handler. Un *pipeline behavior* ejecuta las validaciones de FluentValidation antes de cada handler.
- **UnitOfWork con soporte de procedimientos almacenados**:
  - `IStoredProcedureExecutor` ofrece `QueryAsync` (un resultset), `ExecuteAsync` (con parámetros `OUTPUT` y TVP) y `QueryMultipleAsync` (varios resultsets).
  - `ExecuteInTransactionAsync` agrupa varias operaciones (EF y/o SP) en una sola transacción.
  - Los errores `THROW 50000+` de los SP se convierten en `BusinessException` y el middleware los traduce a 400, 404 o 409.
- **Entity Framework Database First**: el contexto y las entidades se generan con el scaffold (ver sección 4). Las operaciones de datos usan los procedimientos almacenados.

### Reglas de negocio importantes

- `usp_Venta_Registrar` guarda cabecera, detalle y descuento de stock en **una sola transacción**.
- El servidor **no confía en el cliente**: toma los precios de la base de datos y recalcula subtotal, IVA (13%) y total.
- Se valida la existencia de los productos y el stock suficiente (con bloqueo de filas para evitar ventas simultáneas sobre el mismo stock).
- Al eliminar un producto: si nunca se vendió se borra; si ya tiene ventas se desactiva (`Activo = 0`) para no romper el historial.

---

## 3. Ejecución con Docker (recomendada)

Requisitos: Docker Desktop (o Docker Engine con Compose v2).

### 3.1 Desde la línea de comandos

```bash
docker compose up --build
```

Funciona sin configuración adicional (el compose trae valores por defecto de desarrollo). Para cambiar la contraseña de SQL Server o la clave JWT, copie `.env.example` a `.env` y edítelo.

La primera vez se crea la base de datos `VentasDB` con el script SQL. Para recrearla desde cero: `docker compose down -v`.

| Servicio | URL |
|---|---|
| Aplicación MVC | http://localhost:5100 |
| API + Swagger | http://localhost:8080/swagger |
| Aplicación Angular (opcional) | http://localhost:4200 |
| SQL Server | `localhost,1434` (usuario `sa`) |

- El front **Angular** no arranca por defecto. Para incluirlo: `docker compose --profile angular up --build`, o agregue `COMPOSE_PROFILES=angular` al archivo `.env`.
- SQL Server se publica en el puerto **1434** para no chocar con otra instancia en 1433. Se puede cambiar en `docker-compose.override.yml`.
- La contraseña de `sa` debe cumplir la complejidad de SQL Server y no debe contener `;`, `$` ni `"`. `JWT_KEY` debe tener al menos 32 caracteres.
- El contenedor `db-init` crea la base y termina solo: es normal verlo en estado "Exited (0)".

### 3.2 Desde Visual Studio (botón Run)

Requisitos: Visual Studio con la carga de trabajo **"Desarrollo web y ASP.NET"** (incluye *Herramientas de desarrollo de contenedores*) y Docker Desktop en ejecución.

1. Abrir la solución.
2. Agregar el proyecto de orquestación: clic derecho en la solución → **Agregar → Proyecto existente...** → seleccionar `docker-compose.dcproj`.
3. Clic derecho en `docker-compose` → **Establecer como proyecto de inicio**.
4. En cada proyecto (`Ventas.WebApi` y `Ventas.Web`), agregar en su `.csproj`:

   ```xml
   <PropertyGroup>
     <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
     <DockerfileContext>..\..</DockerfileContext>
     <DockerComposeProjectPath>..\..\docker-compose.dcproj</DockerComposeProjectPath>
   </PropertyGroup>
   <ItemGroup>
     <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.21.0" />
   </ItemGroup>
   ```

5. Pulsar **F5** (o el botón verde **Docker Compose**). Visual Studio construye las imágenes, levanta SQL Server, crea la base de datos y arranca la API y el MVC. Abre el navegador en el MVC y se pueden usar **puntos de interrupción** tanto en la API como en el MVC.

Notas:
- La API queda en http://localhost:8080/swagger y SQL Server en `localhost,1434`.
- Si Visual Studio sugiere una versión más reciente del paquete `Microsoft.VisualStudio.Azure.Containers.Tools.Targets`, puede actualizarla.
- Para incluir también Angular, agregue `COMPOSE_PROFILES=angular` en `.env` (la primera construcción tarda varios minutos por `npm ci`).
- Si la solución se ejecutó antes con `docker compose up`, detenga esos contenedores para evitar conflictos de puertos (`docker compose down`).

---

## 4. Ejecución local (sin Docker)

Requisitos: .NET 8 SDK, SQL Server, Node 20+ (solo para Angular).

**1. Base de datos**

Ejecutar `sql/01_VentasDB.sql` en SQL Server (crea la base `VentasDB`).

**2. API**

Ajustar `src/Ventas.WebApi/appsettings.json`:

```json
"ConnectionStrings": {
  "VentasDb": "Server=localhost;Database=VentasDB;User ID=sa;Password=SU_CLAVE;TrustServerCertificate=True"
},
"Jwt": { "Key": "una-clave-secreta-de-minimo-32-caracteres" }
```

> Con usuario y contraseña **no** incluya `Trusted_Connection=True`: esa opción fuerza autenticación de Windows e ignora el usuario (error "Failed to generate SSPI context").

Scaffold de Entity Framework (genera el contexto y las entidades; sobrescribe el `VentasDbContext` de ejemplo):

```bash
dotnet tool install --global dotnet-ef
dotnet ef dbcontext scaffold "Name=ConnectionStrings:VentasDb" Microsoft.EntityFrameworkCore.SqlServer \
  --project src/Ventas.Infrastructure --startup-project src/Ventas.WebApi \
  --context VentasDbContext --context-dir Persistence --output-dir Persistence/Entities \
  --namespace Ventas.Infrastructure.Persistence.Entities --context-namespace Ventas.Infrastructure.Persistence \
  --no-onconfiguring --force
```

Ejecutar:

```bash
dotnet run --project src/Ventas.WebApi
```

**3. Aplicación MVC**

Verificar `Api:BaseUrl` en `src/Ventas.Web/appsettings.json` (URL donde corre la API) y ejecutar:

```bash
dotnet run --project src/Ventas.Web
```

Si la API usa HTTPS con certificado de desarrollo: `dotnet dev-certs https --trust`.

**4. Aplicación Angular (opcional)**

```bash
cd ventas-web
npm install
npm start          # http://localhost:4200
```

`proxy.conf.json` redirige `/api` hacia la API (por defecto `http://localhost:8080`). Si la API corre en otro puerto, cambie el `target`.

---

## 5. Usuarios de prueba

| Usuario | Contraseña | Rol | Acceso |
|---|---|---|---|
| `admin` | `Admin123*` | Administrador | Productos, ventas, historial y reportes |
| `operador` | `Operador123*` | Operador | Solo "Nueva venta" |

Son datos semilla de la prueba. En un entorno real deben cambiarse.

---

## 6. Endpoints de la API

Todas las respuestas JSON siguen el formato `{ "success": bool, "message": "...", "data": ..., "errors": [...] }`.

| Método | Ruta | Rol |
|---|---|---|
| POST | `/api/auth/login` | Público |
| POST | `/api/auth/registrar` | Administrador |
| GET | `/api/productos?busqueda=` | Administrador |
| GET | `/api/productos/{id}` | Administrador |
| GET | `/api/productos/codigo/{codigo}` | Administrador, Operador |
| POST / PUT / DELETE | `/api/productos` / `/{id}` | Administrador |
| POST | `/api/ventas` | Administrador, Operador |
| GET | `/api/ventas/{id}` | Administrador, Operador |
| GET | `/api/ventas?desde=&hasta=` | Administrador |
| GET | `/api/reportes/ventas/pdf?desde=&hasta=` | Administrador |
| GET | `/api/reportes/ventas/excel?desde=&hasta=` | Administrador |

Para probar desde Swagger: ejecutar `login`, copiar el token y pulsar **Authorize**.

---

## 7. Contraseñas encriptadas

Las contraseñas nunca se guardan en texto plano. Cada usuario tiene:

- `PasswordSalt`: un GUID único.
- `PasswordHash`: `SHA-512(contraseña + salt)`.

Para mostrarlo en la base de datos:

```sql
SELECT Id, NombreUsuario, PasswordHash, PasswordSalt FROM dbo.Usuarios;
```

La verificación se hace en la API con comparación en tiempo constante.

---

## 8. Estructura de la base de datos

**Tablas:** `Roles`, `Usuarios`, `Productos`, `Ventas`, `DetalleVentas` (con claves foráneas, restricciones `CHECK` e índices). El número de venta (`V-000001`) es una columna calculada.

**Tipo de tabla:** `dbo.tvp_DetalleVenta` (ProductoId, Cantidad), usado para enviar el detalle de la venta en un solo parámetro.

**Procedimientos almacenados:**

| Área | Procedimientos |
|---|---|
| Usuarios | `usp_Usuario_ObtenerPorNombreUsuario`, `usp_Usuario_Registrar` |
| Productos | `usp_Producto_Listar`, `_ObtenerPorId`, `_ObtenerPorCodigo`, `_Insertar`, `_Actualizar`, `_Eliminar` |
| Ventas | `usp_Venta_Registrar`, `_ObtenerPorId`, `_Listar`, `_Reporte` |

---

## 9. Solución de problemas

| Síntoma | Causa y solución |
|---|---|
| `Failed to generate SSPI context` | La cadena de conexión tiene `Trusted_Connection=True` junto con usuario y contraseña. Quitar `Trusted_Connection` |
| `Bind for 0.0.0.0:<puerto> failed: port is already allocated` | Otro contenedor o servicio usa ese puerto (8080, 5100, 1434...). Cambiar el mapeo en `docker-compose.override.yml` o detener el otro |
| `Login failed for user 'sa'` | Contraseña incorrecta o autenticación mixta deshabilitada en SQL Server |
| El MVC muestra "Servicio no disponible" | La API no está accesible. Revisar `Api:BaseUrl` y que la API esté en ejecución |
| El reporte devuelve "No se encontraron ventas" | No hay ventas en el rango de fechas elegido |
| Reportes PDF/Excel fallan en Docker | Faltan fuentes en el contenedor. El `Dockerfile` de la API ya instala `libfontconfig1` y fuentes |
| Visual Studio no muestra la opción "Docker Compose" | Verificar que `docker-compose.dcproj` esté agregado a la solución y sea el proyecto de inicio, y que la carga de trabajo "Desarrollo web y ASP.NET" esté instalada |
| Error de VS: `failed to reach build target base` | El `Dockerfile` del proyecto debe conservar la etapa `base` (ya incluida en los Dockerfiles entregados) |

---

## Anexo: archivos de Docker

Los archivos están en la raíz del repositorio:

- `docker-compose.yml`: servicios `db`, `db-init`, `api`, `mvc` y `web` (Angular, con perfil `angular`).
- `docker-compose.override.yml`: puertos y variables de desarrollo (`ASPNETCORE_ENVIRONMENT=Development`, que habilita Swagger).
- `docker-compose.dcproj`: proyecto de Visual Studio que permite ejecutar todo con el botón Run.
- `src/Ventas.WebApi/Dockerfile` y `src/Ventas.Web/Dockerfile`: etapas estándar de Visual Studio (`base`, `build`, `publish`, `final`) con contexto en la raíz de la solución.
- `ventas-web/Dockerfile`: compila Angular y lo sirve con nginx, que además redirige `/api` hacia la API.

Para un despliegue real: use `ASPNETCORE_ENVIRONMENT=Production`, defina `MSSQL_SA_PASSWORD` y `JWT_KEY` propios en `.env` y no publique el puerto de SQL Server.
