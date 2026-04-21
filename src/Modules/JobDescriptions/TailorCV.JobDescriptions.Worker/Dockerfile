FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props .
COPY src/Directory.Build.props src/
COPY ["src/Modules/JobDescription/TailorCV.JobDescription.Worker/TailorCV.JobDescription.Worker.csproj", "src/Modules/JobDescription/TailorCV.JobDescription.Worker/"]
COPY ["src/Modules/JobDescription/TailorCV.JobDescription.Contracts/TailorCV.JobDescription.Contracts.csproj", "src/Modules/JobDescription/TailorCV.JobDescription.Contracts/"]
COPY ["src/TailorCV.Shared/TailorCV.Shared.csproj", "src/TailorCV.Shared/"]
RUN dotnet restore "src/Modules/JobDescription/TailorCV.JobDescription.Worker/TailorCV.JobDescription.Worker.csproj"

COPY . .
RUN dotnet publish "src/Modules/JobDescription/TailorCV.JobDescription.Worker/TailorCV.JobDescription.Worker.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/playwright/dotnet:v1.49.1-jammy AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TailorCV.JobDescription.Worker.dll"]