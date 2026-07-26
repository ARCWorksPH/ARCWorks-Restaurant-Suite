FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Roms.slnx ./
COPY src/Roms.Domain/Roms.Domain.csproj src/Roms.Domain/
COPY src/Roms.Application/Roms.Application.csproj src/Roms.Application/
COPY src/Roms.Infrastructure/Roms.Infrastructure.csproj src/Roms.Infrastructure/
COPY src/Roms.Web/Roms.Web.csproj src/Roms.Web/
RUN dotnet restore src/Roms.Web/Roms.Web.csproj
COPY src/ src/
RUN dotnet publish src/Roms.Web/Roms.Web.csproj -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN addgroup --system roms && adduser --system --ingroup roms roms
COPY --from=build --chown=roms:roms /app .
USER roms
ENV ASPNETCORE_URLS=http://+:8080 ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080
ENTRYPOINT ["dotnet", "Roms.Web.dll"]
