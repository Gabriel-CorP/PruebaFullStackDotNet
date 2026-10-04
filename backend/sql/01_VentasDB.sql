IF DB_ID('VentasDB') IS NULL
    CREATE DATABASE VentasDB;
GO
USE VentasDB;
GO

/* ====================================================================
   TABLAS
   ===================================================================== */

CREATE TABLE dbo.Roles (
    Id      INT IDENTITY(1,1) CONSTRAINT PK_Roles PRIMARY KEY,
    Nombre  VARCHAR(50) NOT NULL CONSTRAINT UQ_Roles_Nombre UNIQUE
);
GO

CREATE TABLE dbo.Usuarios (
    Id             INT IDENTITY(1,1) CONSTRAINT PK_Usuarios PRIMARY KEY,
    RolId          INT           NOT NULL,
    NombreUsuario  VARCHAR(50)   NOT NULL CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE,
    NombreCompleto VARCHAR(150)  NOT NULL,
    PasswordHash   VARBINARY(64) NOT NULL,   
    PasswordSalt   VARCHAR(36)   NOT NULL, 
    Activo         BIT           NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
    FechaCreacion  DATETIME2(0)  NOT NULL CONSTRAINT DF_Usuarios_Fecha DEFAULT (DATEADD(HOUR, -6, SYSDATETIME())),
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES dbo.Roles(Id)
);
GO

CREATE TABLE dbo.Productos (
    Id             INT IDENTITY(1,1) CONSTRAINT PK_Productos PRIMARY KEY,
    Codigo         VARCHAR(30)    NOT NULL,
    Nombre         VARCHAR(150)   NOT NULL,
    Descripcion    VARCHAR(500)   NULL,
    Precio         DECIMAL(18,2)  NOT NULL CONSTRAINT CK_Productos_Precio CHECK (Precio >= 0),
    Stock          INT            NOT NULL CONSTRAINT CK_Productos_Stock  CHECK (Stock  >= 0),
    Activo         BIT            NOT NULL CONSTRAINT DF_Productos_Activo DEFAULT (1),
    FechaCreacion  DATETIME2(0)   NOT NULL CONSTRAINT DF_Productos_Fecha  DEFAULT (DATEADD(HOUR, -6, SYSDATETIME()))
);
GO
CREATE UNIQUE INDEX UX_Productos_Codigo ON dbo.Productos(Codigo);
GO

CREATE TABLE dbo.Ventas (
    Id           INT IDENTITY(1,1) CONSTRAINT PK_Ventas PRIMARY KEY,
    NumeroVenta  AS ('V-' + RIGHT('000000' + CAST(Id AS VARCHAR(10)), 6)) PERSISTED,
    Fecha        DATETIME2(0)  NOT NULL CONSTRAINT DF_Ventas_Fecha DEFAULT (DATEADD(HOUR, -6, SYSDATETIME())),
    UsuarioId    INT           NOT NULL,
    Subtotal     DECIMAL(18,2) NOT NULL,
    Iva          DECIMAL(18,2) NOT NULL,
    Total        DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_Ventas_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id)
);
GO
CREATE INDEX IX_Ventas_Fecha ON dbo.Ventas(Fecha);
GO

CREATE TABLE dbo.DetalleVentas (
    Id              INT IDENTITY(1,1) CONSTRAINT PK_DetalleVentas PRIMARY KEY,
    VentaId         INT           NOT NULL,
    ProductoId      INT           NOT NULL,
    Cantidad        INT           NOT NULL CONSTRAINT CK_Detalle_Cantidad CHECK (Cantidad > 0),
    PrecioUnitario  DECIMAL(18,2) NOT NULL,
    Subtotal        DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_Detalle_Ventas    FOREIGN KEY (VentaId)    REFERENCES dbo.Ventas(Id),
    CONSTRAINT FK_Detalle_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(Id)
);
GO
CREATE INDEX IX_Detalle_VentaId    ON dbo.DetalleVentas(VentaId);
CREATE INDEX IX_Detalle_ProductoId ON dbo.DetalleVentas(ProductoId);
GO

/* =====================================================================
   TIPO TABLA (TVP) PARA EL DETALLE DE VENTA
   ===================================================================== */
CREATE TYPE dbo.tvp_DetalleVenta AS TABLE (
    ProductoId INT NOT NULL,
    Cantidad   INT NOT NULL
);
GO

/* =====================================================================
   3. PROCEDIMIENTOS: USUARIOS
   ===================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_ObtenerPorNombreUsuario
    @NombreUsuario VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.Id, u.NombreUsuario, u.NombreCompleto, u.PasswordHash, u.PasswordSalt,
           u.Activo, u.RolId, r.Nombre AS Rol
    FROM dbo.Usuarios u
    INNER JOIN dbo.Roles r ON r.Id = u.RolId
    WHERE u.NombreUsuario = @NombreUsuario;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Usuario_Registrar
    @RolId          INT,
    @NombreUsuario  VARCHAR(50),
    @NombreCompleto VARCHAR(150),
    @PasswordHash   VARBINARY(64),
    @PasswordSalt   VARCHAR(36),
    @NuevoId        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.Usuarios WHERE NombreUsuario = @NombreUsuario)
        THROW 50010, 'El nombre de usuario ya existe.', 1;

    INSERT INTO dbo.Usuarios (RolId, NombreUsuario, NombreCompleto, PasswordHash, PasswordSalt)
    VALUES (@RolId, @NombreUsuario, @NombreCompleto, @PasswordHash, @PasswordSalt);

    SET @NuevoId = SCOPE_IDENTITY();
END
GO

/* =====================================================================
   PROCEDIMIENTOS (CRUD)
   ===================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_Producto_Listar
    @Busqueda VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Codigo, Nombre, Descripcion, Precio, Stock, Activo, FechaCreacion
    FROM dbo.Productos
    WHERE Activo = 1
      AND (@Busqueda IS NULL OR Codigo LIKE '%' + @Busqueda + '%' OR Nombre LIKE '%' + @Busqueda + '%')
    ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Producto_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Codigo, Nombre, Descripcion, Precio, Stock, Activo, FechaCreacion
    FROM dbo.Productos
    WHERE Id = @Id AND Activo = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Producto_ObtenerPorCodigo
    @Codigo VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Codigo, Nombre, Descripcion, Precio, Stock, Activo, FechaCreacion
    FROM dbo.Productos
    WHERE Codigo = @Codigo AND Activo = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Producto_Insertar
    @Codigo      VARCHAR(30),
    @Nombre      VARCHAR(150),
    @Descripcion VARCHAR(500) = NULL,
    @Precio      DECIMAL(18,2),
    @Stock       INT,
    @NuevoId     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.Productos WHERE Codigo = @Codigo)
        THROW 50001, 'Ya existe un producto con ese código.', 1;

    INSERT INTO dbo.Productos (Codigo, Nombre, Descripcion, Precio, Stock)
    VALUES (@Codigo, @Nombre, @Descripcion, @Precio, @Stock);

    SET @NuevoId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Producto_Actualizar
    @Id          INT,
    @Codigo      VARCHAR(30),
    @Nombre      VARCHAR(150),
    @Descripcion VARCHAR(500) = NULL,
    @Precio      DECIMAL(18,2),
    @Stock       INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Productos WHERE Id = @Id AND Activo = 1)
        THROW 50002, 'El producto no existe.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Productos WHERE Codigo = @Codigo AND Id <> @Id)
        THROW 50001, 'Ya existe otro producto con ese código.', 1;

    UPDATE dbo.Productos
    SET Codigo = @Codigo, Nombre = @Nombre, Descripcion = @Descripcion,
        Precio = @Precio, Stock = @Stock
    WHERE Id = @Id;
END
GO

-- Eliminación: física si nunca se vendió, lógica si ya tiene ventas (integridad histórica)
CREATE OR ALTER PROCEDURE dbo.usp_Producto_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Productos WHERE Id = @Id AND Activo = 1)
        THROW 50002, 'El producto no existe.', 1;

    IF EXISTS (SELECT 1 FROM dbo.DetalleVentas WHERE ProductoId = @Id)
        UPDATE dbo.Productos SET Activo = 0 WHERE Id = @Id;
    ELSE
        DELETE FROM dbo.Productos WHERE Id = @Id;
END
GO

/* =====================================================================
   PROCEDIMIENTOS: VENTAS
   ===================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_Venta_Registrar
    @UsuarioId INT,
    @Detalle   dbo.tvp_DetalleVenta READONLY,
    @PorcentajeIva DECIMAL(5,4) = 0.13,
    @NuevoId   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM @Detalle)
            THROW 50020, 'La venta debe tener al menos un producto.', 1;

        BEGIN TRANSACTION;

        -- Consolidar líneas repetidas y bloquear filas de productos
        SELECT d.ProductoId, SUM(d.Cantidad) AS Cantidad
        INTO #Lineas
        FROM @Detalle d
        GROUP BY d.ProductoId;

        IF EXISTS (
            SELECT 1 FROM #Lineas l
            LEFT JOIN dbo.Productos p WITH (UPDLOCK, ROWLOCK) ON p.Id = l.ProductoId AND p.Activo = 1
            WHERE p.Id IS NULL)
            THROW 50021, 'Uno o más productos no existen.', 1;

        IF EXISTS (
            SELECT 1 FROM #Lineas l
            INNER JOIN dbo.Productos p WITH (UPDLOCK, ROWLOCK) ON p.Id = l.ProductoId
            WHERE p.Stock < l.Cantidad)
            THROW 50022, 'Stock insuficiente para uno o más productos.', 1;

        DECLARE @Subtotal DECIMAL(18,2), @Iva DECIMAL(18,2), @Total DECIMAL(18,2);

        SELECT @Subtotal = SUM(l.Cantidad * p.Precio)
        FROM #Lineas l
        INNER JOIN dbo.Productos p ON p.Id = l.ProductoId;

        SET @Iva   = ROUND(@Subtotal * @PorcentajeIva, 2);
        SET @Total = @Subtotal + @Iva;

        INSERT INTO dbo.Ventas (UsuarioId, Subtotal, Iva, Total)
        VALUES (@UsuarioId, @Subtotal, @Iva, @Total);

        SET @NuevoId = SCOPE_IDENTITY();

        INSERT INTO dbo.DetalleVentas (VentaId, ProductoId, Cantidad, PrecioUnitario, Subtotal)
        SELECT @NuevoId, l.ProductoId, l.Cantidad, p.Precio, l.Cantidad * p.Precio
        FROM #Lineas l
        INNER JOIN dbo.Productos p ON p.Id = l.ProductoId;

        UPDATE p
        SET p.Stock = p.Stock - l.Cantidad
        FROM dbo.Productos p
        INNER JOIN #Lineas l ON l.ProductoId = p.Id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Cabecera de una venta
CREATE OR ALTER PROCEDURE dbo.usp_Venta_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Resultset 1: cabecera
    SELECT v.Id, v.NumeroVenta, v.Fecha, v.UsuarioId, u.NombreCompleto AS Usuario,
           v.Subtotal, v.Iva, v.Total
    FROM dbo.Ventas v
    INNER JOIN dbo.Usuarios u ON u.Id = v.UsuarioId
    WHERE v.Id = @Id;

    -- Resultset 2: detalle
    SELECT d.Id, d.VentaId, d.ProductoId, p.Codigo, p.Nombre AS Producto,
           d.Cantidad, d.PrecioUnitario, d.Subtotal
    FROM dbo.DetalleVentas d
    INNER JOIN dbo.Productos p ON p.Id = d.ProductoId
    WHERE d.VentaId = @Id;
END
GO

-- Listado de ventas (cabeceras) por rango de fechas
CREATE OR ALTER PROCEDURE dbo.usp_Venta_Listar
    @FechaInicio DATE = NULL,
    @FechaFin    DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT v.Id, v.NumeroVenta, v.Fecha, u.NombreCompleto AS Usuario,
           v.Subtotal, v.Iva, v.Total
    FROM dbo.Ventas v
    INNER JOIN dbo.Usuarios u ON u.Id = v.UsuarioId
    WHERE (@FechaInicio IS NULL OR v.Fecha >= @FechaInicio)
      AND (@FechaFin    IS NULL OR v.Fecha <  DATEADD(DAY, 1, @FechaFin))
    ORDER BY v.Fecha DESC;
END
GO

-- Reporte plano (cabecera + detalle) para exportar a PDF / Excel
CREATE OR ALTER PROCEDURE dbo.usp_Venta_Reporte
    @FechaInicio DATE = NULL,
    @FechaFin    DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT v.Id AS VentaId, v.NumeroVenta, v.Fecha, u.NombreCompleto AS Usuario,
           p.Codigo, p.Nombre AS Producto, d.Cantidad, d.PrecioUnitario,
           d.Subtotal AS SubtotalLinea, v.Subtotal, v.Iva, v.Total
    FROM dbo.Ventas v
    INNER JOIN dbo.Usuarios u       ON u.Id = v.UsuarioId
    INNER JOIN dbo.DetalleVentas d  ON d.VentaId = v.Id
    INNER JOIN dbo.Productos p      ON p.Id = d.ProductoId
    WHERE (@FechaInicio IS NULL OR v.Fecha >= @FechaInicio)
      AND (@FechaFin    IS NULL OR v.Fecha <  DATEADD(DAY, 1, @FechaFin))
    ORDER BY v.Fecha DESC, v.Id, p.Nombre;
END
GO

/* =====================================================================
   DATOS SEMILLA
   Hash = SHA2_512(Password + Salt), igual que en C#:
     SHA512.HashData(Encoding.UTF8.GetBytes(password + salt))
   Credenciales:  admin / Admin123*     operador / Operador123*
   ===================================================================== */
INSERT INTO dbo.Roles (Nombre) VALUES ('Administrador'), ('Operador');
GO

DECLARE @SaltAdmin VARCHAR(36) = CONVERT(VARCHAR(36), NEWID());
DECLARE @SaltOper  VARCHAR(36) = CONVERT(VARCHAR(36), NEWID());

INSERT INTO dbo.Usuarios (RolId, NombreUsuario, NombreCompleto, PasswordHash, PasswordSalt)
VALUES
 ((SELECT Id FROM dbo.Roles WHERE Nombre = 'Administrador'), 'admin',    'Administrador del Sistema',
   HASHBYTES('SHA2_512', 'Admin123*'    + @SaltAdmin), @SaltAdmin),
 ((SELECT Id FROM dbo.Roles WHERE Nombre = 'Operador'),      'operador', 'Operador de Ventas',
   HASHBYTES('SHA2_512', 'Operador123*' + @SaltOper),  @SaltOper);
GO

INSERT INTO dbo.Productos (Codigo, Nombre, Descripcion, Precio, Stock) VALUES
 ('P001', 'Teclado Mecánico',  'Teclado mecánico retroiluminado', 45.00, 50),
 ('P002', 'Mouse Inalámbrico', 'Mouse óptico 2.4GHz',             18.50, 80),
 ('P003', 'Monitor 24"',       'Monitor LED Full HD',            155.00, 25),
 ('P004', 'Audífonos USB',     'Audífonos con micrófono',         22.75, 60),
 ('P005', 'Webcam HD',         'Webcam 1080p',                    32.00, 40);
GO

-- Verificación: la contraseña se ve encriptada en la BD
SELECT Id, NombreUsuario, PasswordHash, PasswordSalt FROM dbo.Usuarios;
GO
