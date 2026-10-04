FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# QuestPDF (SkiaSharp) y ClosedXML necesitan fontconfig y fuentes en Linux
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core fonts-liberation \
 && rm -rf /var/lib/apt/lists/*

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Ventas.Application/Ventas.Application.csproj", "src/Ventas.Application/"]
COPY ["src/Ventas.Infrastructure/Ventas.Infrastructure.csproj", "src/Ventas.Infrastructure/"]
COPY ["src/Ventas.WebApi/Ventas.WebApi.csproj", "src/Ventas.WebApi/"]
RUN dotnet restore "src/Ventas.WebApi/Ventas.WebApi.csproj"
COPY src/ src/
WORKDIR "/src/src/Ventas.WebApi"
RUN dotnet build "Ventas.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ---------- publish ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Ventas.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ---------- final ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ventas.WebApi.dll"]
