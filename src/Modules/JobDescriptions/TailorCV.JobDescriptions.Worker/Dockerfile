# syntax=docker/dockerfile:1
ARG PLAYWRIGHT_VERSION=1.59.0

# ── Build stage ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY .editorconfig .editorconfig
COPY Directory.Packages.props .
COPY src/Directory.Build.props src/
COPY ["src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/TailorCV.JobDescriptions.Worker.csproj", "src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/"]
COPY ["src/Modules/JobDescriptions/TailorCV.JobDescriptions.Contracts/TailorCV.JobDescriptions.Contracts.csproj", "src/Modules/JobDescriptions/TailorCV.JobDescriptions.Contracts/"]
COPY ["src/TailorCV.Shared/TailorCV.Shared.csproj", "src/TailorCV.Shared/"]
RUN dotnet restore "src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/TailorCV.JobDescriptions.Worker.csproj"

COPY . .
RUN dotnet publish "src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/TailorCV.JobDescriptions.Worker.csproj" -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────
FROM mcr.microsoft.com/playwright/dotnet:v${PLAYWRIGHT_VERSION}-jammy AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV PLAYWRIGHT_SKIP_INSTALL=true
RUN curl -sSL https://dot.net/v1/dotnet-install.sh \
    | bash /dev/stdin --channel 10.0 --install-dir /usr/share/dotnet \
    && ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
ENV PATH="/usr/share/dotnet:$PATH"
ENTRYPOINT ["dotnet", "TailorCV.JobDescriptions.Worker.dll"]
