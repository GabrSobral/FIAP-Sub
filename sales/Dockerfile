# The build context is this service's own folder, so the image builds the same way whether this
# repository is the monorepo or the standalone service repository:
#   docker build -t fiapsub-sales .      (from sales/)
#   docker compose build sales-api       (compose points the context at ./sales)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore using only the files that can change the restore graph, so editing code does not
# invalidate the (slow) restore layer.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/FiapSub.Sales.Domain/FiapSub.Sales.Domain.csproj src/FiapSub.Sales.Domain/
COPY src/FiapSub.Sales.Application/FiapSub.Sales.Application.csproj src/FiapSub.Sales.Application/
COPY src/FiapSub.Sales.Infrastructure.Database/FiapSub.Sales.Infrastructure.Database.csproj src/FiapSub.Sales.Infrastructure.Database/
COPY src/FiapSub.Sales.Infrastructure.Http/FiapSub.Sales.Infrastructure.Http.csproj src/FiapSub.Sales.Infrastructure.Http/
COPY src/FiapSub.Sales.Api/FiapSub.Sales.Api.csproj src/FiapSub.Sales.Api/
RUN dotnet restore src/FiapSub.Sales.Api/FiapSub.Sales.Api.csproj

COPY src/ src/
RUN dotnet publish src/FiapSub.Sales.Api/FiapSub.Sales.Api.csproj \
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
