FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Ventas.Web/Ventas.Web.csproj", "src/Ventas.Web/"]
RUN dotnet restore "src/Ventas.Web/Ventas.Web.csproj"
COPY src/Ventas.Web/ src/Ventas.Web/
WORKDIR "/src/src/Ventas.Web"
RUN dotnet build "Ventas.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Ventas.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ventas.Web.dll"]
