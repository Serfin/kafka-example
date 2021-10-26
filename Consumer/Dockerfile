# \repos\WeatherService> 
# docker build -t central-hub:0.1 -f .\CentralHub\Dockerfile .

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

# Restore/Build layer
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["./CentralHub/CentralHub.csproj", "CentralHub/"] # Copy .csproj for restore
COPY ["./Common/Common.csproj", "Common/"] # Copy .csproj for restore
RUN dotnet restore "./CentralHub/CentralHub.csproj"

# Copy rest of application files (previously we copied only .csproj to download dependencies)
COPY . .
WORKDIR "/src/CentralHub"
RUN dotnet build "./CentralHub.csproj" -c Release -o /app/build

# Publish layer
FROM build AS publish
RUN dotnet publish "./CentralHub.csproj" -c Release -o /app/publish

# Final directory cleanup
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "./CentralHub.dll"]