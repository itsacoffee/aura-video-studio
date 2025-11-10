.PHONY: help dev dev-detached test clean logs logs-api logs-web db-reset db-migrate health status stop restart build

# Default target
.DEFAULT_GOAL := help

# Colors for terminal output
BLUE := \033[0;34m
GREEN := \033[0;32m
YELLOW := \033[0;33m
RED := \033[0;31m
NC := \033[0m # No Color

help: ## Show this help message
	@echo "$(BLUE)Aura Video Studio - Local Development$(NC)"
	@echo ""
	@echo "$(GREEN)Available targets:$(NC)"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(YELLOW)%-20s$(NC) %s\n", $$1, $$2}'
	@echo ""
	@echo "$(GREEN)Quick Start:$(NC)"
	@echo "  1. Run '$(YELLOW)make dev$(NC)' to start all services"
	@echo "  2. Wait for services to be healthy (~60s)"
	@echo "  3. Open $(BLUE)http://localhost:3000$(NC) in your browser"
	@echo "  4. Run '$(YELLOW)make logs$(NC)' in another terminal to view logs"
	@echo ""

dev: ## Start all services (API, Web, Redis, FFmpeg) - attached to console
	@echo "$(GREEN)Starting Aura Video Studio development environment...$(NC)"
	@echo "$(BLUE)Checking for port conflicts...$(NC)"
	@./scripts/setup/check-ports.sh || true
	@echo "$(BLUE)Starting services...$(NC)"
	docker-compose up --build

dev-detached: ## Start all services in detached mode
	@echo "$(GREEN)Starting Aura Video Studio in background...$(NC)"
	@./scripts/setup/check-ports.sh || true
	docker-compose up --build -d
	@echo "$(GREEN)Services starting in background. Use '$(YELLOW)make logs$(NC)' to view logs.$(NC)"
	@echo "$(BLUE)Waiting for services to be healthy...$(NC)"
	@sleep 5
	@make health

test: ## Run all tests (unit, integration, E2E)
	@echo "$(GREEN)Running tests...$(NC)"
	@echo "$(BLUE)Running .NET tests...$(NC)"
	dotnet test Aura.Tests/Aura.Tests.csproj --configuration Release --verbosity minimal
	@echo "$(BLUE)Running Web tests...$(NC)"
	cd Aura.Web && npm run test
	@echo "$(GREEN)All tests completed!$(NC)"

test-coverage: ## Run all tests with coverage reports
	@echo "$(GREEN)Running tests with coverage...$(NC)"
	@./scripts/test-local.sh
	@echo "$(GREEN)Coverage reports generated!$(NC)"
	@echo "$(BLUE)View reports:$(NC)"
	@echo "  - .NET: TestResults/CoverageReport/index.html"
	@echo "  - Frontend: Aura.Web/coverage/index.html"

test-dotnet: ## Run .NET tests only
	@echo "$(BLUE)Running .NET tests...$(NC)"
	@./scripts/test-local.sh --dotnet-only

test-frontend: ## Run frontend tests only
	@echo "$(BLUE)Running frontend tests...$(NC)"
	@./scripts/test-local.sh --frontend-only

test-e2e: ## Run E2E tests (Playwright)
	@echo "$(BLUE)Running E2E tests...$(NC)"
	@./scripts/test-local.sh --e2e

test-watch: ## Run frontend tests in watch mode
	@echo "$(BLUE)Starting frontend test watcher...$(NC)"
	cd Aura.Web && npm run test:watch

clean: ## Stop and remove all containers, volumes, and temporary data
	@echo "$(YELLOW)Cleaning up development environment...$(NC)"
	docker-compose down -v
	@echo "$(YELLOW)Removing temporary files...$(NC)"
	rm -rf ./temp-media/*
	rm -rf ./logs/*
	@echo "$(GREEN)Cleanup complete!$(NC)"

logs: ## Show logs from all services
	docker-compose logs -f

logs-api: ## Show logs from API service only
	docker-compose logs -f api

logs-web: ## Show logs from Web service only
	docker-compose logs -f web

logs-redis: ## Show logs from Redis service only
	docker-compose logs -f redis

db-reset: ## Reset the database (WARNING: destroys all data)
	@echo "$(RED)WARNING: This will delete all data in the database!$(NC)"
	@read -p "Are you sure? (yes/no): " confirm; \
	if [ "$$confirm" = "yes" ]; then \
		echo "$(YELLOW)Resetting database...$(NC)"; \
		docker-compose stop api; \
		rm -f ./data/aura.db*; \
		echo "$(GREEN)Database reset complete. Restart with '$(YELLOW)make dev$(NC)'$(NC)"; \
	else \
		echo "$(BLUE)Database reset cancelled.$(NC)"; \
	fi

db-migrate: ## Run database migrations
	@echo "$(BLUE)Running database migrations...$(NC)"
	@if [ -f "./scripts/setup/migrate.sh" ]; then \
		./scripts/setup/migrate.sh; \
	else \
		docker-compose exec api dotnet ef database update; \
	fi
	@echo "$(GREEN)Migrations complete!$(NC)"

health: ## Check health status of all services
	@echo "$(BLUE)Checking service health...$(NC)"
	@echo ""
	@echo "$(YELLOW)API Health:$(NC)"
	@curl -f -s http://localhost:5005/health/live && echo " $(GREEN)✓ API is healthy$(NC)" || echo " $(RED)✗ API is unhealthy$(NC)"
	@echo ""
	@echo "$(YELLOW)Redis Health:$(NC)"
	@docker-compose exec redis redis-cli ping > /dev/null 2>&1 && echo " $(GREEN)✓ Redis is healthy$(NC)" || echo " $(RED)✗ Redis is unhealthy$(NC)"
	@echo ""
	@echo "$(YELLOW)Web UI:$(NC)"
	@curl -f -s http://localhost:3000 > /dev/null && echo " $(GREEN)✓ Web is accessible$(NC)" || echo " $(RED)✗ Web is not accessible$(NC)"
	@echo ""

status: ## Show status of all services
	@echo "$(BLUE)Service Status:$(NC)"
	@docker-compose ps

stop: ## Stop all services (without removing containers)
	@echo "$(YELLOW)Stopping services...$(NC)"
	docker-compose stop
	@echo "$(GREEN)Services stopped. Use '$(YELLOW)make dev$(NC)' to restart.$(NC)"

restart: ## Restart all services
	@echo "$(YELLOW)Restarting services...$(NC)"
	docker-compose restart
	@echo "$(GREEN)Services restarted!$(NC)"

build: ## Rebuild all Docker images without cache
	@echo "$(BLUE)Rebuilding Docker images...$(NC)"
	docker-compose build --no-cache
	@echo "$(GREEN)Build complete!$(NC)"

install: ## Install all dependencies (first-time setup)
	@echo "$(GREEN)Installing dependencies...$(NC)"
	@echo "$(BLUE)Installing .NET dependencies...$(NC)"
	dotnet restore
	@echo "$(BLUE)Installing Node.js dependencies...$(NC)"
	cd Aura.Web && npm ci
	@echo "$(GREEN)Dependencies installed!$(NC)"

shell-api: ## Open a shell in the API container
	docker-compose exec api /bin/sh

shell-web: ## Open a shell in the Web container
	docker-compose exec web /bin/sh

validate: ## Validate configuration files
	@echo "$(BLUE)Validating configuration...$(NC)"
	@./scripts/setup/validate-config.sh || true
	@echo "$(GREEN)Validation complete!$(NC)"
