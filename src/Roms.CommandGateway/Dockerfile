FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Roms.Domain/Roms.Domain.csproj src/Roms.Domain/
COPY src/Roms.Application/Roms.Application.csproj src/Roms.Application/
COPY src/Roms.CommandGateway/Roms.CommandGateway.csproj src/Roms.CommandGateway/
RUN dotnet restore src/Roms.CommandGateway/Roms.CommandGateway.csproj
COPY src/ src/
RUN dotnet publish src/Roms.CommandGateway/Roms.CommandGateway.csproj -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build --chown=$APP_UID:$APP_UID /app .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Roms.CommandGateway.dll"]
