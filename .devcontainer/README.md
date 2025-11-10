# Development Container Configuration

This directory contains the configuration for developing Aura Video Studio using [Development Containers](https://containers.dev/).

## What is a Dev Container?

A development container provides a fully configured development environment in a container, ensuring all developers have the same tools and dependencies regardless of their host operating system.

## Features

This dev container includes:

- **.NET 8 SDK** - For backend development
- **Node.js 18** - For frontend development
- **Redis 7** - For caching and sessions
- **FFmpeg** - For video processing
- **PowerShell** - For cross-platform scripting
- **Git** and **GitHub CLI** - For version control
- **Docker-in-Docker** - For container operations

## Prerequisites

To use this dev container, you need:

1. [Docker Desktop](https://www.docker.com/products/docker-desktop) or Docker Engine
2. [Visual Studio Code](https://code.visualstudio.com/)
3. [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)

## Getting Started

### Option 1: Open in VS Code

1. Open the repository in VS Code
2. When prompted, click "Reopen in Container"
   - Or press `F1` and run "Dev Containers: Reopen in Container"
3. Wait for the container to build and initialize (first time may take 5-10 minutes)
4. The environment is ready when you see "Development environment is ready!"

### Option 2: Command Line

```bash
# Clone the repository
git clone https://github.com/yourusername/aura-video-studio.git
cd aura-video-studio

# Open in VS Code with dev container
code .
# Then use Command Palette: "Dev Containers: Reopen in Container"
```

## What Happens During Setup

### Post-Create (runs once after container is created)

1. Restores .NET dependencies (`dotnet restore`)
2. Installs Node.js dependencies (`npm ci` in Aura.Web)
3. Sets up git hooks
4. Creates necessary directories (logs, output, temp)

### Post-Start (runs every time container starts)

1. Checks Redis connectivity
2. Verifies FFmpeg availability
3. Displays status and helpful commands

## Development Workflow

Once the dev container is running:

```bash
# Start all services
make dev

# Run tests
make test

# View logs
make logs

# Check health
make health

# Stop services
make stop
```

## Port Forwarding

The following ports are automatically forwarded:

- **5005** - API server
- **3000** - Web UI (opens automatically in browser)
- **6379** - Redis (silent)

## Persistent Data

The following are mounted from your host machine to preserve data:

- `.aspnet` - ASP.NET user secrets
- `.nuget` - NuGet package cache
- Redis data volume - Database persistence

## Customization

### Adding VS Code Extensions

Edit `.devcontainer/devcontainer.json` and add to the `extensions` array:

```json
"customizations": {
  "vscode": {
    "extensions": [
      "your-extension-id"
    ]
  }
}
```

### Modifying Services

Edit `.devcontainer/docker-compose.yml` to add or modify services.

### Changing Setup Scripts

- `post-create.sh` - Runs once after container creation
- `post-start.sh` - Runs every time the container starts

## Troubleshooting

### Container Fails to Build

1. Check Docker is running: `docker ps`
2. Rebuild container: Command Palette → "Dev Containers: Rebuild Container"
3. Check Docker Desktop resources (RAM, disk space)

### Services Not Starting

1. Check service status: `docker-compose ps`
2. View logs: `docker-compose logs`
3. Restart services: `docker-compose restart`

### Port Already in Use

If ports 5005, 3000, or 6379 are already in use on your host:

1. Stop the conflicting service
2. Or modify the port mappings in `docker-compose.yml`

### Performance Issues

1. Allocate more resources to Docker Desktop
2. Use the cached volume mount option (already configured)
3. Consider excluding large directories from sync

## Differences from Local Development

The dev container is optimized for:

- **Consistency** - Everyone has the same environment
- **Isolation** - No conflicts with host system
- **Speed** - Pre-configured tools and dependencies

Limitations:

- First build takes longer (5-10 minutes)
- Requires Docker Desktop resources
- Windows-specific features (WinUI) not available

## Platform Compatibility

This dev container works on:

- ✅ **Windows** - Full support (except WinUI development)
- ✅ **macOS** - Full support (backend and web development)
- ✅ **Linux** - Full support (backend and web development)

For WinUI development, you still need Windows 11 native environment.

## Additional Resources

- [Development Containers Documentation](https://containers.dev/)
- [VS Code Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Aura Build Guide](../BUILD_GUIDE.md)
- [Aura Contributing Guide](../CONTRIBUTING.md)
