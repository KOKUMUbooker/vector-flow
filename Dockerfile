# ────────────────────────────────────────────────────────────────────────────
# Stage 1 — Build
# Uses the full SDK image so it can compile all three projects.
# The API references Client and Shared, so a single publish of the API
# produces everything: the API DLLs + the Blazor WASM static files.
# ────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file and all csproj files first.
# Docker caches this layer; the expensive restore only re-runs when
# a .csproj changes, not on every source file change.
COPY VectorFlow.sln ./
COPY VectorFlow.Api/VectorFlow.Api.csproj             ./VectorFlow.Api/
COPY VectorFlow.Client/VectorFlow.Client.csproj       ./VectorFlow.Client/
COPY VectorFlow.Shared/VectorFlow.Shared.csproj       ./VectorFlow.Shared/
COPY VectorFlow.Tests/VectorFlow.Tests.csproj         ./VectorFlow.Tests/

RUN dotnet restore

# Copy the full source and publish the API.
# Because the API csproj references Client and Shared, dotnet publish
# builds all three and places the Blazor WASM output under
# publish/wwwroot (via the WebAssembly.Server hosting package).
COPY . .

RUN dotnet publish VectorFlow.Api/VectorFlow.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ────────────────────────────────────────────────────────────────────────────
# Stage 2 — Runtime
# Lean ASP.NET Core runtime image — no SDK, no build tools.
# ────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build  /app/publish .

# ASP.NET Core listens on 8080 inside the container.
# Nginx on the host proxies to this port — we never expose it directly.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "VectorFlow.Api.dll"]
