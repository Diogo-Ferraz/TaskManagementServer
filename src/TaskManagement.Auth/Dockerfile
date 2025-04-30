FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/TaskManagement.Auth/TaskManagement.Auth.csproj", "src/TaskManagement.Auth/"]
COPY ["src/TaskManagement.Shared/TaskManagement.Shared.csproj", "src/TaskManagement.Shared/"]
RUN dotnet restore "src/TaskManagement.Auth/TaskManagement.Auth.csproj"
COPY . .
WORKDIR "/src/src/TaskManagement.Auth"
RUN dotnet build "TaskManagement.Auth.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TaskManagement.Auth.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

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
    CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "TaskManagement.Auth.dll"]