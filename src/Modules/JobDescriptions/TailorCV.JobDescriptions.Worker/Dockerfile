FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props .
COPY src/Directory.Build.props src/
COPY ["src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/TailorCV.JobDescriptions.Worker.csproj", "src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/"]
COPY ["src/Modules/JobDescriptions/TailorCV.JobDescriptions.Contracts/TailorCV.JobDescriptions.Contracts.csproj", "src/Modules/JobDescriptions/TailorCV.JobDescriptions.Contracts/"]
COPY ["src/TailorCV.Shared/TailorCV.Shared.csproj", "src/TailorCV.Shared/"]
RUN dotnet restore "src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/TailorCV.JobDescriptions.Worker.csproj"

COPY . .
RUN dotnet publish "src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/TailorCV.JobDescriptions.Worker.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/playwright/dotnet:v1.49.1-jammy AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TailorCV.JobDescriptions.Worker.dll"]
