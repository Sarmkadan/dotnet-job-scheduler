# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
#
# Makefile for dotnet-job-scheduler
# Provides convenient commands for common development tasks
# =============================================================================

.PHONY: help build test clean restore format docker-build docker-run docker-stop db-migrate publish

# Variables
DOTNET := dotnet
SOLUTION := dotnet-job-scheduler.sln
CONFIGURATION := Release
OUTPUT_DIR := ./bin/$(CONFIGURATION)

# Default target
help:
	@echo "dotnet-job-scheduler - Build and Development Commands"
	@echo "====================================================="
	@echo ""
	@echo "Build and Compilation:"
	@echo "  make build          Build project in Release configuration"
	@echo "  make build-debug    Build project in Debug configuration"
	@echo "  make clean          Clean build artifacts"
	@echo "  make restore        Restore NuGet packages"
	@echo ""
	@echo "Testing:"
	@echo "  make test           Run all unit tests"
	@echo "  make test-coverage  Run tests with code coverage"
	@echo "  make test-watch     Run tests in watch mode"
	@echo ""
	@echo "Code Quality:"
	@echo "  make format         Format code to follow conventions"
	@echo "  make format-check   Check code formatting"
	@echo "  make lint           Run code analysis"
	@echo ""
	@echo "Database:"
	@echo "  make db-migrate     Apply EF Core migrations"
	@echo "  make db-rollback    Rollback last migration"
	@echo "  make db-add-migration NAME=MyMigration   Add new migration"
	@echo ""
	@echo "Docker:"
	@echo "  make docker-build   Build Docker image"
	@echo "  make docker-run     Start services with docker-compose"
	@echo "  make docker-stop    Stop docker-compose services"
	@echo "  make docker-logs    View docker-compose logs"
	@echo ""
	@echo "Publishing:"
	@echo "  make publish        Publish application"
	@echo "  make publish-docker Publish to Docker registry"
	@echo ""

# Build commands
build:
	@echo "Building $(SOLUTION) in Release configuration..."
	$(DOTNET) build $(SOLUTION) --configuration $(CONFIGURATION)
	@echo "✓ Build completed successfully"

build-debug:
	@echo "Building $(SOLUTION) in Debug configuration..."
	$(DOTNET) build $(SOLUTION) --configuration Debug
	@echo "✓ Build completed successfully"

clean:
	@echo "Cleaning build artifacts..."
	$(DOTNET) clean $(SOLUTION)
	@rm -rf $(OUTPUT_DIR) obj/
	@echo "✓ Cleanup completed"

restore:
	@echo "Restoring NuGet packages..."
	$(DOTNET) restore $(SOLUTION)
	@echo "✓ Packages restored"

# Test commands
test:
	@echo "Running unit tests..."
	$(DOTNET) test $(SOLUTION) --configuration $(CONFIGURATION) --no-build

test-coverage:
	@echo "Running tests with code coverage..."
	$(DOTNET) test $(SOLUTION) --configuration $(CONFIGURATION) \
		--collect:"XPlat Code Coverage" \
		/p:CollectCoverage=true \
		/p:CoverageFormat=cobertura

test-watch:
	@echo "Running tests in watch mode..."
	$(DOTNET) watch -p tests/JobScheduler.Core.Tests/JobScheduler.Core.Tests.csproj test

# Code quality commands
format:
	@echo "Formatting code..."
	$(DOTNET) format $(SOLUTION)
	@echo "✓ Code formatted"

format-check:
	@echo "Checking code formatting..."
	$(DOTNET) format $(SOLUTION) --verify-no-changes --verbosity diagnostic

lint:
	@echo "Running code analysis..."
	$(DOTNET) build $(SOLUTION) /p:TreatWarningsAsErrors=true
	@echo "✓ Code analysis completed"

# Database commands
db-migrate:
	@echo "Applying Entity Framework migrations..."
	$(DOTNET) ef database update --project src/JobScheduler.Core/JobScheduler.Core.csproj
	@echo "✓ Migrations applied"

db-rollback:
	@echo "Rolling back to previous migration..."
	$(DOTNET) ef database update 0 --project src/JobScheduler.Core/JobScheduler.Core.csproj
	@echo "✓ Rollback completed"

db-add-migration:
	@if [ -z "$(NAME)" ]; then \
		echo "Error: NAME is required. Usage: make db-add-migration NAME=MyMigration"; \
		exit 1; \
	fi
	@echo "Creating migration: $(NAME)"
	$(DOTNET) ef migrations add $(NAME) --project src/JobScheduler.Core/JobScheduler.Core.csproj
	@echo "✓ Migration created"

# Docker commands
docker-build:
	@echo "Building Docker image..."
	docker build -t job-scheduler:latest .
	@echo "✓ Docker image built"

docker-run:
	@echo "Starting services with docker-compose..."
	docker-compose up -d
	@echo "✓ Services started"
	@echo "Access the service at http://localhost:5000"

docker-stop:
	@echo "Stopping docker-compose services..."
	docker-compose down
	@echo "✓ Services stopped"

docker-logs:
	@echo "Viewing docker-compose logs..."
	docker-compose logs -f

docker-clean:
	@echo "Removing Docker containers and volumes..."
	docker-compose down -v
	@echo "✓ Containers and volumes removed"

# Publishing commands
publish:
	@echo "Publishing application to Release..."
	$(DOTNET) publish src/JobScheduler.Core/JobScheduler.Core.csproj \
		--configuration $(CONFIGURATION) \
		--output ./publish
	@echo "✓ Published to ./publish"

publish-docker:
	@echo "Publishing to Docker registry..."
	@if [ -z "$(REGISTRY)" ]; then \
		echo "Error: REGISTRY is required. Usage: make publish-docker REGISTRY=myregistry/image"; \
		exit 1; \
	fi
	@echo "Building Docker image: $(REGISTRY)"
	docker build -t $(REGISTRY) .
	docker push $(REGISTRY)
	@echo "✓ Published to $(REGISTRY)"

# Development helpers
run:
	@echo "Running application..."
	$(DOTNET) run --project src/JobScheduler.Core/JobScheduler.Core.csproj

watch:
	@echo "Running application in watch mode..."
	$(DOTNET) watch -p src/JobScheduler.Core/JobScheduler.Core.csproj run

# All-in-one commands
full-build: restore build test
	@echo "✓ Full build completed successfully"

full-clean: docker-clean clean
	@echo "✓ Full cleanup completed"

ci: restore build-debug test format-check lint
	@echo "✓ CI checks passed"

# Information commands
info:
	@echo "Project Information:"
	@echo "==================="
	@$(DOTNET) --version
	@echo "Solution: $(SOLUTION)"
	@echo "Configuration: $(CONFIGURATION)"
	@echo "Output Directory: $(OUTPUT_DIR)"

# Rules that don't require other files with the same name
.PHONY: all clean restore build test help info run watch full-build full-clean ci
