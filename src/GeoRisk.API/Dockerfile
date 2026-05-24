# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["GeoRisk.API.csproj", "./"]
RUN dotnet restore "GeoRisk.API.csproj"

# Copy source and build
COPY . .
RUN dotnet publish "GeoRisk.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "GeoRisk.API.dll"]
