#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet:3.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["Equinor.ProCoSys.Config.csproj", ""]
RUN dotnet restore "./Equinor.ProCoSys.Config.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Equinor.ProCoSys.Config.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Equinor.ProCoSys.Config.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true