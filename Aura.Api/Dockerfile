# Aura.Api Dockerfile for Local Development

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Aura.sln", "./"]
COPY ["Aura.Api/Aura.Api.csproj", "Aura.Api/"]
COPY ["Aura.Core/Aura.Core.csproj", "Aura.Core/"]
COPY ["Aura.Providers/Aura.Providers.csproj", "Aura.Providers/"]
COPY ["Aura.Analyzers/Aura.Analyzers.csproj", "Aura.Analyzers/"]
COPY ["Directory.Build.props", "./"]

# Restore dependencies
RUN dotnet restore "Aura.Api/Aura.Api.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/Aura.Api"
RUN dotnet build "Aura.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Aura.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install FFmpeg and curl for health checks
RUN apt-get update && apt-get install -y \
    ffmpeg \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create directories for data and logs
RUN mkdir -p /app/data /app/logs /app/temp-media

# Expose port
EXPOSE 5005

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5005
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:5005/health/live || exit 1

ENTRYPOINT ["dotnet", "Aura.Api.dll"]
