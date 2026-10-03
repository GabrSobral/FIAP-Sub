# Build context is the repository root, so global.json (which pins the SDK and the test runner)
# is available: docker build -f sales/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore using only the files that can change the restore graph, so editing code does not
# invalidate the (slow) restore layer.
COPY global.json ./
COPY sales/Directory.Build.props sales/Directory.Packages.props sales/
COPY sales/src/FiapSub.Sales.Domain/FiapSub.Sales.Domain.csproj sales/src/FiapSub.Sales.Domain/
COPY sales/src/FiapSub.Sales.Application/FiapSub.Sales.Application.csproj sales/src/FiapSub.Sales.Application/
COPY sales/src/FiapSub.Sales.Infrastructure.Database/FiapSub.Sales.Infrastructure.Database.csproj sales/src/FiapSub.Sales.Infrastructure.Database/
COPY sales/src/FiapSub.Sales.Infrastructure.Http/FiapSub.Sales.Infrastructure.Http.csproj sales/src/FiapSub.Sales.Infrastructure.Http/
COPY sales/src/FiapSub.Sales.Api/FiapSub.Sales.Api.csproj sales/src/FiapSub.Sales.Api/
RUN dotnet restore sales/src/FiapSub.Sales.Api/FiapSub.Sales.Api.csproj

COPY sales/src/ sales/src/
RUN dotnet publish sales/src/FiapSub.Sales.Api/FiapSub.Sales.Api.csproj \
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

ENTRYPOINT ["dotnet", "FiapSub.Sales.Api.dll"]
