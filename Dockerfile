# Multi-stage, hardened build for the DDW API.
# Trivy scans the final image for OS/library CVEs, secrets and misconfig.

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first for better layer caching.
COPY src/Ddw.Api/*.csproj src/Ddw.Api/
RUN dotnet restore src/Ddw.Api/Ddw.Api.csproj

COPY . .
RUN dotnet publish src/Ddw.Api/Ddw.Api.csproj -c Release -o /app --no-restore

# ---- runtime ----
# Chiselled/distroless-style image: no shell, non-root by default, smaller CVE surface.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS runtime
WORKDIR /app

# Run as the built-in non-root user provided by the chiselled image.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

COPY --from=build /app .

# Lightweight liveness for the App Service health check.
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD ["/app/Ddw.Api", "--health-check"]

ENTRYPOINT ["dotnet", "Ddw.Api.dll"]
