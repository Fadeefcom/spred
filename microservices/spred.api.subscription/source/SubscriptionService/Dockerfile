#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y curl && apt-get install sudo
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install the Credential Provider in the build stage
RUN curl -L https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | sh

ARG ASPNETCORE_ENVIRONMENT=Development
ARG FEED_ACCESSTOKEN
ARG ARTIFACTS_ENDPOINT="https://pkgs.dev.azure.com/SpredDeveloperGroup/Spred/_packaging/spred-private/nuget/v3/index.json"
RUN echo "Environment-$ASPNETCORE_ENVIRONMENT"

COPY ["nuget.config", "."]

# Set environment variables for Credential Provider
ENV NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED true
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS="{\"endpointCredentials\": [{\"endpoint\":\"${ARTIFACTS_ENDPOINT}\", \"password\":\"${FEED_ACCESSTOKEN}\"}]}"

#RUN echo $VSS_NUGET_EXTERNAL_FEED_ENDPOINTS
COPY ["SubscriptionService/SubscriptionService.csproj", "SubscriptionService/"]
RUN dotnet restore "./SubscriptionService/./SubscriptionService.csproj" --configfile nuget.config

COPY . .
WORKDIR "/src/SubscriptionService"
RUN dotnet build "./SubscriptionService.csproj" -c $ASPNETCORE_ENVIRONMENT -o /app/build

FROM build AS publish
RUN dotnet publish "./SubscriptionService.csproj" -c $ASPNETCORE_ENVIRONMENT -o /app/publish /p:UseAppHost=false

FROM base AS development
COPY ["emulatortcert.sh", "."]
RUN chmod +x emulatortcert.sh
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./emulatortcert.sh", "dotnet", "SubscriptionService.dll"]

FROM base AS production
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SubscriptionService.dll"]