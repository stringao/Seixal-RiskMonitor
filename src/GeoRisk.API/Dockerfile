# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY src/GeoRisk.API/GeoRisk.API.csproj ./GeoRisk.API/
RUN dotnet restore "GeoRisk.API/GeoRisk.API.csproj"

# Copy source and build
COPY src/GeoRisk.API/ ./GeoRisk.API/
RUN dotnet publish "GeoRisk.API/GeoRisk.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

# Install Kerberos/GSSAPI libs required by Npgsql PostgreSQL driver
# Create symlink so 'python' works (some libraries look for 'python' not 'python3')
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    libkrb5-3 \
    python3 \
    python3-pip \
    python3-rasterio \
    && ln -s /usr/bin/python3 /usr/bin/python \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

COPY --from=build /app/publish .
COPY --from=build /src/GeoRisk.API/appsettings.json ./appsettings.json
COPY --from=build /src/GeoRisk.API/appsettings.Development.json ./appsettings.Development.json

ENTRYPOINT ["dotnet", "GeoRisk.API.dll"]
