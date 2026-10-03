# Build context is the repository root, so global.json (which pins the SDK and the test runner)
# is available: docker build -f core/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore using only the files that can change the restore graph, so editing code does not
# invalidate the (slow) restore layer.
COPY global.json ./
COPY core/Directory.Build.props core/Directory.Packages.props core/
COPY core/src/FiapSub.Core.Domain/FiapSub.Core.Domain.csproj core/src/FiapSub.Core.Domain/
COPY core/src/FiapSub.Core.Application/FiapSub.Core.Application.csproj core/src/FiapSub.Core.Application/
COPY core/src/FiapSub.Core.Infrastructure.Database/FiapSub.Core.Infrastructure.Database.csproj core/src/FiapSub.Core.Infrastructure.Database/
COPY core/src/FiapSub.Core.Infrastructure.Http/FiapSub.Core.Infrastructure.Http.csproj core/src/FiapSub.Core.Infrastructure.Http/
COPY core/src/FiapSub.Core.Api/FiapSub.Core.Api.csproj core/src/FiapSub.Core.Api/
RUN dotnet restore core/src/FiapSub.Core.Api/FiapSub.Core.Api.csproj

COPY core/src/ core/src/
RUN dotnet publish core/src/FiapSub.Core.Api/FiapSub.Core.Api.csproj \
        --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is here for the container health check and for poking at the API from inside the network.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app ./

# APP_UID comes from the base image: run as a non-root user.
USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "FiapSub.Core.Api.dll"]
