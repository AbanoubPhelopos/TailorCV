FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TailorCV/src/TailorCV.Api/TailorCV.Api.csproj", "TailorCV/src/TailorCV.Api/"]
COPY ["TailorCV/src/TailorCV.Infrastructure/TailorCV.Infrastructure.csproj", "TailorCV/src/TailorCV.Infrastructure/"]
COPY ["TailorCV/src/TailorCV.SharedKernel/TailorCV.SharedKernel.csproj", "TailorCV/src/TailorCV.SharedKernel/"]
COPY ["TailorCV/src/Modules/TailorCV.Modules.Identity/TailorCV.Modules.Identity.csproj", "TailorCV/src/Modules/TailorCV.Modules.Identity/"]
COPY ["TailorCV/Directory.Packages.props", "TailorCV/"]
RUN dotnet restore "TailorCV/src/TailorCV.Api/TailorCV.Api.csproj"

COPY . .
WORKDIR "/src/TailorCV/src/TailorCV.Api"
RUN dotnet build "TailorCV.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TailorCV.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TailorCV.Api.dll"]
