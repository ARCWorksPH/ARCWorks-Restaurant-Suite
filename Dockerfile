FROM mcr.microsoft.com/dotnet/sdk@sha256:ed034a8bf0b24ded0cbbac07e17825d8e9ebfe21e308191d0f7421eaf5ad4664 AS build
WORKDIR /src
COPY Roms.slnx ./
COPY src/Roms.Domain/Roms.Domain.csproj src/Roms.Domain/
COPY src/Roms.Application/Roms.Application.csproj src/Roms.Application/
COPY src/Roms.Infrastructure/Roms.Infrastructure.csproj src/Roms.Infrastructure/
COPY src/Roms.Web/Roms.Web.csproj src/Roms.Web/
RUN dotnet restore src/Roms.Web/Roms.Web.csproj
COPY src/ src/
RUN dotnet publish src/Roms.Web/Roms.Web.csproj -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet@sha256:1fa23fc4872d95fd71c2833ebe65d7e84a43b2d51a31d119516852f13d9505a7 AS runtime
WORKDIR /app
RUN mkdir -p /app/keys && chown $APP_UID:$APP_UID /app/keys
COPY --from=build --chown=$APP_UID:$APP_UID /app .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD bash -c 'exec 3<>/dev/tcp/127.0.0.1/8080; printf "GET /health HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n" >&3; read -r status <&3; [[ "$status" == *" 200 "* ]]'
ENTRYPOINT ["dotnet", "Roms.Web.dll"]
