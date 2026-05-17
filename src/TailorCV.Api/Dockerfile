# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY .editorconfig .editorconfig
COPY Directory.Packages.props .
COPY src/Directory.Build.props src/
COPY ["src/TailorCV.Api/TailorCV.Api.csproj", "src/TailorCV.Api/"]
COPY ["src/TailorCV.Shared/TailorCV.Shared.csproj", "src/TailorCV.Shared/"]
COPY ["src/TailorCV.Infrastructure/TailorCV.Infrastructure.csproj", "src/TailorCV.Infrastructure/"]
COPY ["src/Modules/Identity/TailorCV.Identity/TailorCV.Identity.csproj", "src/Modules/Identity/TailorCV.Identity/"]
COPY ["src/Modules/Identity/TailorCV.Identity.Contracts/TailorCV.Identity.Contracts.csproj", "src/Modules/Identity/TailorCV.Identity.Contracts/"]
COPY ["src/Modules/Profile/TailorCV.Profile/TailorCV.Profile.csproj", "src/Modules/Profile/TailorCV.Profile/"]
COPY ["src/Modules/Profile/TailorCV.Profile.Contracts/TailorCV.Profile.Contracts.csproj", "src/Modules/Profile/TailorCV.Profile.Contracts/"]
COPY ["src/Modules/JobDescriptions/TailorCV.JobDescriptions/TailorCV.JobDescriptions.csproj", "src/Modules/JobDescriptions/TailorCV.JobDescriptions/"]
COPY ["src/Modules/JobDescriptions/TailorCV.JobDescriptions.Contracts/TailorCV.JobDescriptions.Contracts.csproj", "src/Modules/JobDescriptions/TailorCV.JobDescriptions.Contracts/"]
COPY ["src/Modules/Templates/TailorCV.Templates/TailorCV.Templates.csproj", "src/Modules/Templates/TailorCV.Templates/"]
COPY ["src/Modules/Templates/TailorCV.Templates.Contracts/TailorCV.Templates.Contracts.csproj", "src/Modules/Templates/TailorCV.Templates.Contracts/"]
COPY ["src/Modules/CVGenerator/TailorCV.CVGenerator/TailorCV.CVGenerator.csproj", "src/Modules/CVGenerator/TailorCV.CVGenerator/"]
COPY ["src/Modules/CVGenerator/TailorCV.CVGenerator.Contracts/TailorCV.CVGenerator.Contracts.csproj", "src/Modules/CVGenerator/TailorCV.CVGenerator.Contracts/"]
RUN dotnet restore "src/TailorCV.Api/TailorCV.Api.csproj"

COPY . .
RUN dotnet publish "src/TailorCV.Api/TailorCV.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TailorCV.Api.dll"]
