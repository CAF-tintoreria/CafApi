# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos el csproj primero para aprovechar la cache
COPY ./CafApi.csproj ./ 
RUN dotnet restore ./CafApi.csproj

# Copiamos el resto del código y publicamos
COPY . .
RUN dotnet publish ./CafApi.csproj -c Release -o /app/out

# ===== runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

# Railway setea $PORT; .NET debe escuchar ahí
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV DOTNET_EnableDiagnostics=0

# Cortesía local (Railway usará $PORT)
EXPOSE 8080

CMD ["dotnet", "CafApi.dll"]
