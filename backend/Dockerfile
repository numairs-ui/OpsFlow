FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for layer-cached restore. Directory.Build.props supplies
# TargetFramework etc. to every csproj, so it must be present before the restore.
COPY ["Directory.Build.props", "./"]
COPY ["OpsFlow.Api/OpsFlow.Api.csproj", "OpsFlow.Api/"]
COPY ["OpsFlow.Domain/OpsFlow.Domain.csproj", "OpsFlow.Domain/"]
COPY ["OpsFlow.Infrastructure/OpsFlow.Infrastructure.csproj", "OpsFlow.Infrastructure/"]
RUN dotnet restore "OpsFlow.Api/OpsFlow.Api.csproj"

COPY . .
RUN dotnet publish "OpsFlow.Api/OpsFlow.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render injects PORT; fall back to 8080 locally
ENV ASPNETCORE_URLS=http://+:8080
CMD ASPNETCORE_URLS="http://+:${PORT:-8080}" dotnet OpsFlow.Api.dll
