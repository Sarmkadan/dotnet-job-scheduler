# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

COPY ["src/JobScheduler.Core/JobScheduler.Core.csproj", "src/JobScheduler.Core/"]
RUN dotnet restore "src/JobScheduler.Core/JobScheduler.Core.csproj"

COPY . .
RUN dotnet build "src/JobScheduler.Core/JobScheduler.Core.csproj" -c Release -o /app/build

FROM builder AS publish
RUN dotnet publish "src/JobScheduler.Core/JobScheduler.Core.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD dotnet --version || exit 1

ENTRYPOINT ["dotnet", "JobScheduler.Core.dll"]
