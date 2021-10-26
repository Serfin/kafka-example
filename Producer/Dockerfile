# \repos\WeatherService> 
# docker build -t station:0.1 -f .\Station\Dockerfile .

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

# Restore/Build layer
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["./Station/Station.csproj", "Station/"] # Copy .csproj for restore
COPY ["./Common/Common.csproj", "Common/"] # Copy .csproj for restore
RUN dotnet restore "./Station/Station.csproj"

# Copy rest of application files (previously we copied only .csproj to download dependencies)
COPY . .
WORKDIR "/src/Station"
RUN dotnet build "./Station.csproj" -c Release -o /app/build

# Publish layer
FROM build AS publish
RUN dotnet publish "./Station.csproj" -c Release -o /app/publish

# Final directory cleanup
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "./Station.dll"]