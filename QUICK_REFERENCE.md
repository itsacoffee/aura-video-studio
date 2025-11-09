# Aura Video Studio - Quick Reference

Fast reference for common development tasks.

## Getting Started

```bash
# First time setup
./scripts/setup-local.sh      # Linux/macOS
.\scripts\setup-local.ps1     # Windows

# Start development
make dev
```

## Common Commands

### Service Management

```bash
make dev              # Start all services (attached)
make dev-detached     # Start in background
make stop             # Stop services
make restart          # Restart services
make clean            # Remove all containers and data
```

### Logs and Monitoring

```bash
make logs             # View all logs
make logs-api         # API logs only
make logs-web         # Web logs only
make health           # Check health
make status           # Container status
```

### Database

```bash
make db-migrate       # Run migrations
make db-reset         # Reset database (destroys data!)
```

### Testing

```bash
make test             # Run all tests
cd Aura.Web && npm run test          # Web unit tests
cd Aura.Web && npm run playwright     # E2E tests
dotnet test Aura.Tests                # API tests
```

### Development

```bash
# Backend (local)
cd Aura.Api && dotnet watch run

# Frontend (local)
cd Aura.Web && npm run dev

# Type checking
cd Aura.Web && npm run type-check

# Linting
cd Aura.Web && npm run lint
cd Aura.Web && npm run lint:fix
```

## URLs

- **Web UI:** http://localhost:3000
- **API:** http://localhost:5005
- **API Swagger:** http://localhost:5005/swagger
- **API Health:** http://localhost:5005/health/live

## Directory Structure

```
aura/
├── data/              # SQLite database
├── logs/              # Application logs
├── temp-media/        # Temporary media files
├── Aura.Api/          # Backend API
├── Aura.Web/          # Frontend
├── Aura.Core/         # Domain logic
├── Aura.Providers/    # Provider integrations
└── scripts/           # Utility scripts
```

## Environment Variables

Key variables in `.env`:

```bash
# Core
AURA_DATABASE_PATH=/app/data/aura.db
AURA_REDIS_CONNECTION=redis:6379
AURA_FFMPEG_PATH=/usr/bin/ffmpeg

# Providers (optional)
AURA_OPENAI_API_KEY=
AURA_STABILITY_API_KEY=
AURA_RUNWAY_API_KEY=

# Features
AURA_OFFLINE_MODE=false
AURA_ENABLE_ADVANCED_MODE=false
```

## Troubleshooting

### Services won't start
```bash
# Check Docker
docker ps

# Check ports
./scripts/setup/check-ports.sh

# View logs
make logs
```

### Port conflicts
```bash
# Linux/macOS
lsof -i :5005
lsof -i :3000

# Windows
netstat -ano | findstr :5005
```

### Database issues
```bash
# Reset database
make db-reset

# Check permissions
ls -la data/aura.db
```

### Clean slate
```bash
make clean
docker system prune -af
make dev
```

## VS Code

### Debugging

1. Press `F5` or use Debug panel
2. Select configuration:
   - "Full Stack (Docker)" - Debug all services
   - "Launch API (Local)" - Debug API locally
   - "Launch Web (Chrome)" - Debug frontend

### Tasks

- `Ctrl+Shift+B` - Start all services
- `Ctrl+Shift+P` → "Tasks: Run Task"
  - Start/stop services
  - View logs
  - Run tests
  - Database operations

### Recommended Extensions

Install all recommended extensions:
`Ctrl+Shift+P` → "Extensions: Show Recommended Extensions"

## Git Workflow

```bash
# Create feature branch
git checkout -b feature/your-feature

# Make changes and commit
git add .
git commit -m "feat: your feature description"

# Run tests before pushing
make test
cd Aura.Web && npm run quality-check

# Push
git push origin feature/your-feature
```

## Help

- **Full guide:** [DEVELOPMENT.md](DEVELOPMENT.md)
- **Troubleshooting:** [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- **API docs:** http://localhost:5005/swagger
- **Issues:** GitHub Issues
- **Questions:** GitHub Discussions
