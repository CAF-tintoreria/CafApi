# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos el csproj (ruta relativa al contexto: raíz del repo)
COPY ./CafApi/CafApi.csproj ./
RUN dotnet restore ./CafApi.csproj

# Copiamos el resto del código del proyecto y publicamos
COPY ./CafApi/ ./
RUN dotnet publish ./CafApi.csproj -c Release -o /app/out

# ===== runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080

CMD ["dotnet", "CafApi.dll"]
