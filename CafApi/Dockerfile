# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# IMPORTANTE: el contexto de Railway es la RAÍZ del repo
# Por eso copiamos desde la carpeta 'CafApi/...'
COPY CafApi/CafApi.csproj ./CafApi.csproj
RUN dotnet restore ./CafApi.csproj

# Copiamos TODO el código del proyecto (no el repo entero)
COPY CafApi/. ./
RUN dotnet publish -c Release -o /app/out

# ===== runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

# Railway inyecta $PORT: hacemos que Kestrel escuche ahí
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080

CMD ["dotnet", "CafApi.dll"]
