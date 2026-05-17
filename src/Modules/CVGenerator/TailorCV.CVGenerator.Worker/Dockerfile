# syntax=docker/dockerfile:1

# ── Build stage ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY .editorconfig .editorconfig
COPY Directory.Packages.props .
COPY src/Directory.Build.props src/
COPY ["src/Modules/CVGenerator/TailorCV.CVGenerator.Worker/TailorCV.CVGenerator.Worker.csproj", "src/Modules/CVGenerator/TailorCV.CVGenerator.Worker/"]
COPY ["src/Modules/CVGenerator/TailorCV.CVGenerator.Contracts/TailorCV.CVGenerator.Contracts.csproj", "src/Modules/CVGenerator/TailorCV.CVGenerator.Contracts/"]
COPY ["src/Modules/Profile/TailorCV.Profile.Contracts/TailorCV.Profile.Contracts.csproj", "src/Modules/Profile/TailorCV.Profile.Contracts/"]
COPY ["src/Modules/JobDescriptions/TailorCV.JobDescriptions.Contracts/TailorCV.JobDescriptions.Contracts.csproj", "src/Modules/JobDescriptions/TailorCV.JobDescriptions.Contracts/"]
COPY ["src/Modules/Templates/TailorCV.Templates.Contracts/TailorCV.Templates.Contracts.csproj", "src/Modules/Templates/TailorCV.Templates.Contracts/"]
COPY ["src/TailorCV.Shared/TailorCV.Shared.csproj", "src/TailorCV.Shared/"]
COPY ["src/TailorCV.Infrastructure/TailorCV.Infrastructure.csproj", "src/TailorCV.Infrastructure/"]
RUN dotnet restore "src/Modules/CVGenerator/TailorCV.CVGenerator.Worker/TailorCV.CVGenerator.Worker.csproj"

COPY . .
RUN dotnet publish "src/Modules/CVGenerator/TailorCV.CVGenerator.Worker/TailorCV.CVGenerator.Worker.csproj" -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    chromium \
    fonts-liberation \
    libappindicator3-1 \
    libasound2 \
    libatk-bridge2.0-0 \
    libdrm2 \
    libgbm1 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libxss1 \
    xdg-utils \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV PuppeteerSharp__SkipBrowserDownload=true
ENTRYPOINT ["dotnet", "TailorCV.CVGenerator.Worker.dll"]
