FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore for layer caching
COPY ["PharmacyAPI.csproj", "./"]
RUN dotnet restore "PharmacyAPI.csproj"

# Copy remaining sources and publish
COPY . .
RUN dotnet publish "PharmacyAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Run the published assembly
ENTRYPOINT ["dotnet", "PharmacyAPI.dll"]

# Swagger UI (common for Web API)
curl http://localhost:5000/swagger/index.html
# or just check root
curl -v http://localhost:5000/

# Compose
docker compose logs -f pharmacyapi

# Or get container id and view logs
docker ps
docker logs -f <container-id>

# bash docker-entrypoint.sh
#!/usr/bin/env bash
set -e

# run EF Core migrations (adjust project assembly or use tools as needed)
# dotnet ef database update --project ./PathToProject/ --startup-project ./PathToProject/

# If using migrations via the runtime assembly, you can run a small migration runner:
# dotnet PharmacyAPI.dll migrate  # (if you implement CLI support)

# Finally start the app
exec dotnet PharmacyAPI.dll