# Aura Video Studio - Development Guide

This guide covers the local development environment setup, architecture overview, and development workflows for Aura Video Studio.

## Table of Contents

- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Development Environment](#development-environment)
- [Running Services](#running-services)
- [Development Workflows](#development-workflows)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)

## Quick Start

Get up and running in 3 commands:

```bash
# 1. Run the setup script
./scripts/setup-local.sh   # Linux/macOS
# OR
./scripts/setup-local.ps1  # Windows

# 2. Start all services
make dev

# 3. Open your browser
# Navigate to http://localhost:3000
```

**That's it!** The setup script handles all prerequisites checking, directory creation, and dependency installation.

## Architecture

### High-Level Overview

```
┌─────────────────────────────────────────────────────────────┐
│                         Aura.Web                            │
│                  (React + TypeScript + Vite)                │
│                      Port: 3000/5173                        │
└─────────────────────────┬───────────────────────────────────┘
                          │ HTTP/REST + SSE
┌─────────────────────────▼───────────────────────────────────┐
│                        Aura.Api                             │
│                   (ASP.NET Core 8.0)                        │
│                        Port: 5005                           │
└──┬────────────────┬──────────────────┬──────────────────┬───┘
   │                │                  │                  │
   ▼                ▼                  ▼                  ▼
┌──────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐
│SQLite│      │  Redis   │      │  FFmpeg  │      │Providers │
│  DB  │      │  Cache   │      │Container │      │ (LLM/TTS)│
└──────┘      └──────────┘      └──────────┘      └──────────┘
Port:None      Port:6379        Port:None         External APIs
```

### Project Structure

```
Aura/
├── Aura.Api/           # REST API + SSE endpoints
│   ├── Controllers/    # API controllers
│   ├── HealthChecks/   # Health check implementations
│   ├── Data/           # Database context and seed data
│   ├── Middleware/     # Custom middleware
│   └── Services/       # Application services
├── Aura.Core/          # Domain logic and orchestration
│   ├── Models/         # Domain models
│   ├── Orchestrator/   # Video generation orchestration
│   ├── Planner/        # Content planning
│   └── Data/           # Data access layer
├── Aura.Providers/     # External provider integrations
│   ├── Llm/            # Language model providers
│   ├── Tts/            # Text-to-speech providers
│   ├── Images/         # Image providers
│   └── Video/          # Video processing
├── Aura.Web/           # React frontend
│   ├── src/
│   │   ├── api/        # API client
│   │   ├── components/ # React components
│   │   ├── pages/      # Page components
│   │   ├── services/   # Frontend services
│   │   └── state/      # State management
│   └── public/         # Static assets
├── Aura.Tests/         # Unit and integration tests
└── Aura.E2E/           # End-to-end tests
```

## Development Environment

### Prerequisites

| Tool | Minimum Version | Required For | Notes |
|------|----------------|--------------|-------|
| **Docker Desktop** | 20.0+ | All development | Required |
| **Docker Compose** | 2.0+ | All development | Usually included with Docker Desktop |
| **.NET SDK** | 8.0+ | Local API development | Optional if using Docker only |
| **Node.js** | 20.0+ | Local Web development | Optional if using Docker only |
| **FFmpeg** | 4.0+ | Video rendering | Optional - container version available |

### First-Time Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-org/aura.git
   cd aura
   ```

2. **Run the setup script:**
   ```bash
   # Linux/macOS
   ./scripts/setup-local.sh
   
   # Windows (PowerShell)
   .\scripts\setup-local.ps1
   ```

3. **Configure environment (optional):**
   ```bash
   # Edit .env to add API keys for premium features
   nano .env  # or use your favorite editor
   ```

4. **Start services:**
   ```bash
   make dev
   ```

### Environment Variables

The `.env.example` file documents all available configuration options. Key variables:

```bash
# Core API
AURA_DATABASE_PATH=/app/data/aura.db
AURA_REDIS_CONNECTION=redis:6379
AURA_FFMPEG_PATH=/usr/bin/ffmpeg

# Provider API Keys (optional)
AURA_OPENAI_API_KEY=      # For GPT-4 script generation
AURA_STABILITY_API_KEY=   # For Stable Diffusion images
AURA_RUNWAY_API_KEY=      # For AI video generation

# Stock Media (free tiers available)
AURA_PIXABAY_API_KEY=
AURA_PEXELS_API_KEY=
AURA_UNSPLASH_API_KEY=

# Feature Flags
AURA_OFFLINE_MODE=false
AURA_ENABLE_ADVANCED_MODE=false
```

**Note:** Aura works without any API keys using free/local providers. API keys unlock premium features.

## Running Services

### Using Make (Recommended)

The `Makefile` provides convenient commands for all common operations:

```bash
# Show all available commands
make help

# Development
make dev              # Start all services (attached)
make dev-detached     # Start in background
make stop             # Stop services
make restart          # Restart services
make clean            # Stop and remove all data

# Logs
make logs             # View all logs
make logs-api         # View API logs only
make logs-web         # View Web logs only

# Database
make db-migrate       # Run migrations
make db-reset         # Reset database (WARNING: destroys data)

# Health & Status
make health           # Check service health
make status           # Show container status

# Testing
make test             # Run all tests
```

### Using Docker Compose Directly

If you prefer or `make` is not available:

```bash
# Start services
docker-compose up --build

# Start in background
docker-compose up --build -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Clean up everything
docker-compose down -v
```

### Running Services Locally (Without Docker)

For faster iteration when developing:

**Backend:**
```bash
cd Aura.Api
dotnet restore
dotnet run
# API available at http://localhost:5005
```

**Frontend:**
```bash
cd Aura.Web
npm ci
npm run dev
# Web available at http://localhost:5173
```

**Dependencies:**
- Start Redis: `docker run -p 6379:6379 redis:7-alpine`
- Ensure FFmpeg is installed locally

## Development Workflows

### Typical Development Session

```bash
# Start of day
make dev-detached     # Start services in background
make logs-api         # Monitor API logs in one terminal
# Work on code in your editor
# Changes are hot-reloaded automatically

# Check service health
make health

# Run tests
make test

# End of day
make stop
```

### Backend Development

```bash
# Make changes to .cs files in Aura.Api or Aura.Core

# Rebuild and restart API
docker-compose up --build -d api

# Or run locally for faster iteration
cd Aura.Api
dotnet watch run
```

### Frontend Development

```bash
# Make changes to .tsx/.ts files in Aura.Web/src

# Changes are automatically hot-reloaded by Vite
# No restart needed!

# Run type checking
cd Aura.Web
npm run type-check

# Run linting
npm run lint

# Run tests
npm run test
```

### Database Changes

```bash
# Add a new migration (local .NET required)
cd Aura.Api
dotnet ef migrations add YourMigrationName

# Apply migrations
make db-migrate

# Or via Docker
docker-compose exec api dotnet ef database update

# Reset database (WARNING: destroys all data)
make db-reset
```

### Adding a New API Endpoint

1. **Create/update controller:**
   ```csharp
   // Aura.Api/Controllers/YourController.cs
   [ApiController]
   [Route("api/v1/[controller]")]
   public class YourController : ControllerBase
   {
       [HttpGet]
       public IActionResult Get() { /* ... */ }
   }
   ```

2. **Update API client (frontend):**
   ```bash
   cd Aura.Web
   npm run generate:api-types
   ```

3. **Test the endpoint:**
   ```bash
   curl http://localhost:5005/api/v1/your-endpoint
   ```

## Testing

### Unit Tests

```bash
# Run all .NET tests
dotnet test

# Run specific test project
dotnet test Aura.Tests/Aura.Tests.csproj

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Frontend Tests

```bash
cd Aura.Web

# Run unit tests
npm run test

# Run with coverage
npm run test:coverage

# Run in watch mode
npm run test:watch
```

### E2E Tests

```bash
# Using test-specific compose file
docker-compose -f docker-compose.test.yml up --abort-on-container-exit

# Or run Playwright tests directly
cd Aura.Web
npm run playwright

# With UI
npm run playwright:ui
```

### Integration Tests

```bash
# Start services
make dev-detached

# Run integration tests
dotnet test Aura.E2E/Aura.E2E.csproj

# Cleanup
make clean
```

## Troubleshooting

### Services won't start

**Check port conflicts:**
```bash
make status
./scripts/setup/check-ports.sh
```

**Check Docker is running:**
```bash
docker ps
```

**View detailed logs:**
```bash
make logs
```

### API returns 502 Bad Gateway

The API might still be starting up. Wait 30-60 seconds and check:
```bash
make health
```

### Database errors

**Reset the database:**
```bash
make db-reset
make dev
```

**Check database file permissions:**
```bash
ls -la data/aura.db
```

### FFmpeg not found

**Check FFmpeg in container:**
```bash
docker-compose exec api which ffmpeg
docker-compose exec api ffmpeg -version
```

**Install FFmpeg locally (optional):**
```bash
# Ubuntu/Debian
sudo apt install ffmpeg

# macOS
brew install ffmpeg

# Windows
# See scripts/ffmpeg/install-ffmpeg-windows.ps1
```

### Redis connection failed

**Check Redis is running:**
```bash
docker-compose ps redis
docker-compose exec redis redis-cli ping
```

**Restart Redis:**
```bash
docker-compose restart redis
```

### Hot reload not working (Web)

**Check Vite dev server:**
```bash
make logs-web
```

**Restart web container:**
```bash
docker-compose restart web
```

### Port already in use

**Find and kill process using port:**
```bash
# Linux/macOS
lsof -ti:5005 | xargs kill -9
lsof -ti:3000 | xargs kill -9

# Windows
netstat -ano | findstr :5005
taskkill /PID <PID> /F
```

**Or change ports in `docker-compose.yml`**

### Clean slate reset

If all else fails:
```bash
make clean
docker system prune -af
make dev
```

## Performance Optimization

### Development Performance Tips

1. **Use local services when possible:**
   - Run API and Web locally for faster iteration
   - Only use Docker for dependencies (Redis, FFmpeg)

2. **Optimize Docker:**
   - Use volumes for node_modules
   - Enable BuildKit: `export DOCKER_BUILDKIT=1`
   - Use `.dockerignore` to exclude unnecessary files

3. **Database optimization:**
   - Keep SQLite file on SSD
   - Enable WAL mode (already configured)
   - Run `VACUUM` periodically

4. **Frontend optimization:**
   - Use `npm ci` instead of `npm install`
   - Clear Vite cache: `rm -rf Aura.Web/node_modules/.vite`

## Contributing

### Before Submitting a PR

1. **Run all checks:**
   ```bash
   make test
   cd Aura.Web && npm run quality-check
   ```

2. **Ensure clean state:**
   ```bash
   make clean
   make dev
   make health
   ```

3. **Update documentation:**
   - Add/update relevant docs
   - Update CHANGELOG.md
   - Include inline code comments

4. **Follow conventions:**
   - See [CONTRIBUTING.md](CONTRIBUTING.md) for code style
   - Use conventional commits
   - Write tests for new features

### Commit Message Format

```
type(scope): subject

body

footer
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `refactor`: Code refactoring
- `test`: Test changes
- `chore`: Build/tooling changes

**Example:**
```
feat(api): add video export queue endpoint

Implements a new endpoint for queuing video exports with priority support.
Includes rate limiting and authentication.

Closes #123
```

## Additional Resources

- **[README.md](README.md)** - Project overview and quick start
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Contribution guidelines
- **[BUILD_GUIDE.md](BUILD_GUIDE.md)** - Build from source
- **[docs/](docs/)** - Comprehensive documentation
- **[API Reference](docs/api/)** - REST API documentation
- **[Architecture Docs](docs/architecture/)** - System design details

## Getting Help

- **Issues:** GitHub Issues for bug reports and feature requests
- **Discussions:** GitHub Discussions for questions and ideas
- **Documentation:** Check `docs/troubleshooting/` for common issues
- **Logs:** Always include logs when reporting issues: `make logs > debug.log`

---

**Happy coding!** 🚀
