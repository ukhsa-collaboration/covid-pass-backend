#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet:3.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["CovidCertificate.IngestionPipelines/CovidCertificate.Backend.IngestionPipelines.csproj", "CovidCertificate.IngestionPipelines/"]
COPY ["CovidCertificate.Backend.Services/CovidCertificate.Backend.Services.csproj", "CovidCertificate.Backend.Services/"]
COPY ["CovidCertificate.Backend.Interfaces/CovidCertificate.Backend.Interfaces.csproj", "CovidCertificate.Backend.Interfaces/"]
COPY ["CovidCertificate.Backend.Models/CovidCertificate.Backend.Models.csproj", "CovidCertificate.Backend.Models/"]
COPY ["CovidCertificate.Utils/CovidCertificate.Backend.Utils.csproj", "CovidCertificate.Utils/"]
COPY ["CovidCertificate.Configuration/CovidCertificate.Backend.Configuration.csproj", "CovidCertificate.Configuration/"]
COPY ["CovidCertificate.Backend.Mocks/CovidCertificate.Backend.Mocks.csproj", "CovidCertificate.Backend.Mocks/"]
RUN dotnet restore "CovidCertificate.IngestionPipelines/CovidCertificate.Backend.IngestionPipelines.csproj"
COPY . .
WORKDIR "/src/CovidCertificate.IngestionPipelines"
RUN dotnet build "CovidCertificate.Backend.IngestionPipelines.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CovidCertificate.Backend.IngestionPipelines.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true