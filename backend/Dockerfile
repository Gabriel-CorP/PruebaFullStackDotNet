# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaurar primero (cachea paquetes NuGet si no cambian los .csproj)
COPY src/Ventas.Application/Ventas.Application.csproj       Ventas.Application/
COPY src/Ventas.Infrastructure/Ventas.Infrastructure.csproj Ventas.Infrastructure/
COPY src/Ventas.WebApi/Ventas.WebApi.csproj                 Ventas.WebApi/
RUN dotnet restore Ventas.WebApi/Ventas.WebApi.csproj

COPY src/ .
RUN dotnet publish Ventas.WebApi/Ventas.WebApi.csproj -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# QuestPDF (SkiaSharp) y ClosedXML necesitan fontconfig y fuentes en Linux
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core fonts-liberation \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# .NET 8: la imagen corre como usuario no-root y escucha en el puerto 8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ventas.WebApi.dll"]
