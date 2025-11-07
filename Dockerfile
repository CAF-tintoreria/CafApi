# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos el .csproj a una carpeta con el mismo nombre del proyecto
COPY CafApi/CafApi.csproj CafApi/
RUN dotnet restore CafApi/CafApi.csproj

# Copiamos el resto del código del proyecto
COPY CafApi/ CafApi/
WORKDIR /src/CafApi
RUN dotnet publish -c Release -o /app/out

# ===== runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

# Railway inyecta $PORT — hacemos que Kestrel escuche ahí
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080

CMD ["dotnet", "CafApi.dll"]
