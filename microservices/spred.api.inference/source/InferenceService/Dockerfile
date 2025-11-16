# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
RUN apt-get update
RUN apt-get install -y curl && apt-get install sudo
RUN apt-get install -y rclone
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install the Credential Provider in the build stage
RUN curl -L https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | sh

ARG ASPNETCORE_ENVIRONMENT
ARG FEED_ACCESSTOKEN
ARG SERVICE_ACCOUNT_JSON

ARG ARTIFACTS_ENDPOINT="https://pkgs.dev.azure.com/SpredDeveloperGroup/Spred/_packaging/spred-private/nuget/v3/index.json"
RUN echo "Environment-$ASPNETCORE_ENVIRONMENT"

# Set environment variables for Credential Provider
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS="{\"endpointCredentials\": [{\"endpoint\":\"${ARTIFACTS_ENDPOINT}\", \"password\":\"${FEED_ACCESSTOKEN}\"}]}"

COPY "./InferenceService" "/src/InferenceService"
COPY "./ModelProcessor" "/src/ModelProcessor"
WORKDIR "/src/InferenceService"
COPY ["nuget.config", "."]
RUN dotnet restore "./InferenceService.csproj" --configfile "nuget.config"
RUN dotnet build "./InferenceService.csproj" -c $ASPNETCORE_ENVIRONMENT -o /app/build

FROM build AS publish
RUN dotnet publish "./InferenceService.csproj" -c $ASPNETCORE_ENVIRONMENT -o /app/publish /p:UseAppHost=false

FROM base AS development
COPY ["emulatortcert.sh", "."]
RUN chmod +x emulatortcert.sh
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./emulatortcert.sh", "dotnet", "InferenceService.dll"]

FROM base AS production
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InferenceService.dll"]