#!/bin/bash
# Crea VentasDB con /sql/01_VentasDB.sql si no existe (o si quedó a medias) y termina.
set -u

SQLCMD_BIN="${SQLCMD_BIN:-/opt/mssql-tools18/bin/sqlcmd}"
HOST="${SQL_HOST:-db}"
PAUSA="${SLEEP_SECONDS:-2}"

# -I = QUOTED_IDENTIFIER ON (igual que SSMS; sqlcmd lo trae en OFF por defecto)
run_sql() { "$SQLCMD_BIN" -C -I -S "$HOST" -U sa -P "$MSSQL_SA_PASSWORD" -h -1 -W "$@"; }

# ---------- 1. Esperar a que SQL Server acepte el login de 'sa' ----------
echo ">> Conectando a SQL Server ($HOST)..."
for i in $(seq 1 30); do
  if out=$(run_sql -Q "SELECT 1" 2>&1); then break; fi

  if echo "$out" | grep -qi "login failed"; then
    echo "!! SQL Server rechazó la contraseña de 'sa':"
    echo "$out"
    echo "!! La contraseña de 'sa' se fija al crear el volumen. Si cambió MSSQL_SA_PASSWORD después,"
    echo "!! elimine el volumen para recrearlo:  docker compose down -v"
    exit 1
  fi
  if [ "$i" -eq 30 ]; then
    echo "!! No se pudo conectar a SQL Server tras 60 s:"
    echo "$out"
    exit 1
  fi
  sleep "$PAUSA"
done
echo ">> Conexión correcta."

# ---------- 2. Esperar a que TODAS las bases estén en línea ----------
# SQL Server acepta conexiones antes de terminar de recuperar las bases de usuario:
# consultar VentasDB en ese momento falla con "Msg 904 ... cannot be autostarted".
echo ">> Esperando a que las bases de datos terminen de iniciar..."
for i in $(seq 1 60); do
  PENDIENTES=$(run_sql -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE state <> 0" 2>&1 | tr -d '[:space:]')
  if [ "$PENDIENTES" = "0" ]; then break; fi
  if [ "$i" -eq 60 ]; then
    echo "!! Las bases de datos no pasaron a ONLINE tras 2 minutos (respuesta: $PENDIENTES)."
    exit 1
  fi
  sleep "$PAUSA"
done
echo ">> Bases de datos en línea."

# ---------- 3. Estado de VentasDB: 0 = no existe, 1 = incompleta, 3 = completa ----------
ESTADO=""
for intento in 1 2 3 4 5; do
  ESTADO=$(run_sql -Q "SET NOCOUNT ON; IF DB_ID('VentasDB') IS NULL SELECT 0 ELSE IF OBJECT_ID('VentasDB.dbo.Usuarios','U') IS NULL SELECT 1 ELSE EXEC('SELECT CASE WHEN EXISTS (SELECT 1 FROM VentasDB.dbo.Usuarios) THEN 3 ELSE 1 END')" 2>&1 | tr -d '[:space:]')
  case "$ESTADO" in
    0|1|3) break ;;
  esac
  echo ">> La base aún no responde (intento $intento/5): $ESTADO"
  sleep "$PAUSA"
done

case "$ESTADO" in
  3)
    echo ">> VentasDB ya existe y tiene datos. No se vuelve a crear."
    exit 0
    ;;
  1)
    echo ">> VentasDB existe pero está incompleta (ejecución anterior fallida). Se recrea..."
    if ! run_sql -b -Q "ALTER DATABASE VentasDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE VentasDB;"; then
      echo "!! No se pudo eliminar la base incompleta."
      exit 1
    fi
    ;;
  0)
    echo ">> VentasDB no existe. Se crea..."
    ;;
  *)
    echo "!! Respuesta inesperada al consultar el estado de la base: $ESTADO"
    exit 1
    ;;
esac

# ---------- 4. Ejecutar el script ----------
if ! run_sql -b -i /sql/01_VentasDB.sql; then
  echo "!! Falló la ejecución de 01_VentasDB.sql (ver el mensaje de SQL Server arriba)."
  exit 1
fi

echo ">> VentasDB creada correctamente."
