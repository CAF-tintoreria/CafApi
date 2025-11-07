# ===== build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo .csproj
COPY CafApi.csproj ./
RUN dotnet restore

# Copiar el resto del código
COPY . .
RUN dotnet publish -c Release -o /app/out

# ===== runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080

CMD ["dotnet", "CafApi.dll"]
