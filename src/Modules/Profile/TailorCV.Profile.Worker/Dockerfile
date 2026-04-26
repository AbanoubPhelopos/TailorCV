# syntax=docker/dockerfile:1

# ── Build stage ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY .editorconfig .editorconfig
COPY Directory.Packages.props .
COPY src/Directory.Build.props src/
COPY ["src/Modules/Profile/TailorCV.Profile.Worker/TailorCV.Profile.Worker.csproj", "src/Modules/Profile/TailorCV.Profile.Worker/"]
COPY ["src/Modules/Profile/TailorCV.Profile.Contracts/TailorCV.Profile.Contracts.csproj", "src/Modules/Profile/TailorCV.Profile.Contracts/"]
COPY ["src/TailorCV.Shared/TailorCV.Shared.csproj", "src/TailorCV.Shared/"]
COPY ["src/TailorCV.Infrastructure/TailorCV.Infrastructure.csproj", "src/TailorCV.Infrastructure/"]
RUN dotnet restore "src/Modules/Profile/TailorCV.Profile.Worker/TailorCV.Profile.Worker.csproj"

COPY . .
RUN dotnet publish "src/Modules/Profile/TailorCV.Profile.Worker/TailorCV.Profile.Worker.csproj" -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TailorCV.Profile.Worker.dll"]
