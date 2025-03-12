FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/TaskManagement.Api/TaskManagement.Api.csproj", "src/TaskManagement.Api/"]
COPY ["src/TaskManagement.Shared/TaskManagement.Shared.csproj", "src/TaskManagement.Shared/"]
RUN dotnet restore "src/TaskManagement.Api/TaskManagement.Api.csproj"
COPY . .
WORKDIR "/src/src/TaskManagement.Api"
RUN dotnet build "TaskManagement.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TaskManagement.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN apt-get update && \
    apt-get --yes install curl && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*
USER app

# Add healthcheck
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
    CMD curl -k https://localhost:8081/health || exit 1
ENTRYPOINT ["dotnet", "TaskManagement.Api.dll"]