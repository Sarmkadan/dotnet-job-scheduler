# Docker Guide

This guide covers Docker deployment for dotnet-job-scheduler, including quick start instructions, environment variable reference, and production deployment recommendations.

## Quick Start with Docker

### Prerequisites

- Docker 20.10+
- Docker Compose v2+
- 4GB+ RAM recommended

### Method 1: Docker Compose (Recommended)

The easiest way to get started is using the provided `docker-compose.yml`:

```bash
# Clone the repository
git clone https://github.com/sarmkadan/dotnet-job-scheduler.git
cd dotnet-job-scheduler

# Start the services
docker compose up -d

# Check service health
curl http://localhost:8080/api/health
```

### Method 2: Custom Docker Setup

For custom deployments, create your own `docker-compose.yml`:

```yaml
version: '3.8'
services:
  database:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: Y
      SA_PASSWORD: ${DB_PASSWORD}
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

  scheduler:
    image: your-registry/dotnet-job-scheduler:latest
    depends_on:
      database:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Server=database;Database=JobScheduler;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=true;"
    ports:
      - "8080:8080"
```

## Docker Compose Usage

### Basic Usage

```bash
# Start services
docker compose up -d

# View logs
docker compose logs -f

# Check health
curl http://localhost:8080/api/health

# Scale the scheduler service
docker compose up -d --scale scheduler=3
```

### Environment Variables Reference

| Variable | Description | Default | Required |
|----------|-------------|---------|------------|
| `ASPNETCORE_ENVIRONMENT` | ASP.NET environment | `Production` | ✅ |
| `ConnectionStrings__DefaultConnection` | Database connection string | - | ✅ |
| `DB_PASSWORD` | Database password | `YourSafePassword123!ChangeMe` | ✅ |
| `JobScheduler__MaxConcurrentJobs` | Maximum concurrent jobs | `10` | |
| `JobScheduler__DefaultTimeoutSeconds` | Default job timeout | `300` | |
| `JobScheduler__DefaultMaxRetries` | Default retry attempts | `3` | |
| `JobScheduler__DefaultRetryBackoffSeconds` | Default retry backoff | `5` | |
| `JobScheduler__QueuePollIntervalMs` | Job queue poll interval (ms) | `5000` | |
| `JobScheduler__EnableCleanup` | Enable execution history cleanup | `true` | |
| `JobScheduler__CleanupIntervalMs` | Cleanup interval (ms) | `300000` | |
| `JobScheduler__ExecutionHistoryRetentionDays` | History retention (days) | `30` | |

### Complete docker-compose.yml Example

```yaml
services:
  database:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: Y
      SA_PASSWORD: ${DB_PASSWORD:-YourSafePassword123!ChangeMe}
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test: ["CMD", "/opt/mssql-tools/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", "${DB_PASSWORD:-YourSafePassword123!ChangeMe}", "-Q", "SELECT 1"]
      interval: 10s
      timeout: 3s
      retries: 5
      start_period: 30s

  scheduler:
    image: ghcr.io/sarmkadan/dotnet-job-scheduler:2.0
    depends_on:
      database:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://0.0.0.0:8080
      ConnectionStrings__DefaultConnection: "Server=database;Database=JobScheduler;User Id=sa;Password=${DB_PASSWORD:-YourSafePassword123!ChangeMe};TrustServerCertificate=true;"
      JobScheduler__MaxConcurrentJobs: "20"
      JobScheduler__DefaultTimeoutSeconds: "600"
      JobScheduler__DefaultMaxRetries: "3"
    ports:
      - "8080:8080"
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    volumes:
      - ./logs:/app/logs

volumes:
  sqldata:
    driver: local

networks:
  default:
    name: scheduler-network
```

## Production Deployment Checklist

### 1. Security Hardening

✅ **Use non-root user** (default in v2.0)
```dockerfile
# The v2.0 image already runs as non-root user
USER appuser
```

✅ **Secure database credentials**
```bash
# Create .env file for production
cat > .env << EOF
DB_PASSWORD=YourProductionPassword123!
EOF
```

✅ **Enable HTTPS in production**
```yaml
# Add HTTPS configuration
environment:
  ASPNETCORE_ENVIRONMENT: Production
  ASPNETCORE_URLS: https://0.0.0.0:8443
  # ... other variables
```

### 2. Resource Management

✅ **Set resource limits**
```yaml
services:
  scheduler:
    # ... other config
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '0.5'
        reservations:
          memory: 512M
          cpus: '0.25'
```

✅ **Configure health checks**
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/api/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

### 3. Monitoring and Logging

✅ **Enable structured logging**
```yaml
environment:
  # ... other config
  Logging__LogLevel__Default: "Information"
  Logging__LogLevel__Microsoft: "Warning"
  Logging__LogLevel__System: "Warning"
```

### 4. Volume Management

✅ **Persistent storage for logs**
```yaml
services:
  scheduler:
    # ... other config
    volumes:
      - ./logs:/app/logs
```

✅ **Database persistence**
```yaml
services:
  database:
    # ... other config
    volumes:
      - sqldata:/var/opt/mssql
```

## Environment Variables Reference

### Core Application Settings

| Variable | Description | Default | Environment |
|----------|-------------|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Production` | All |
| `ASPNETCORE_URLS` | URLs to bind to | `http://+:8080` | All |
| `Logging__LogLevel__Default` | Default log level | `Information` | All |

### Job Scheduler Settings

| Variable | Description | Default | Environment |
|----------|-------------|----------|-------------|
| `JobScheduler__MaxConcurrentJobs` | Max concurrent jobs | `10` | All |
| `JobScheduler__DefaultTimeoutSeconds` | Default job timeout | `300` | All |
| `JobScheduler__DefaultMaxRetries` | Default retry attempts | `3` | All |
| `JobScheduler__DefaultRetryBackoffSeconds` | Default retry backoff | `5` | All |
| `JobScheduler__QueuePollIntervalMs` | Queue poll interval (ms) | `5000` | All |
| `JobScheduler__EnableCleanup` | Enable history cleanup | `true` | All |
| `JobScheduler__CleanupIntervalMs` | Cleanup interval (ms) | `300000` | All |
| `JobScheduler__ExecutionHistoryRetentionDays` | History retention (days) | `30` | All |

### Database Settings

| Variable | Description | Default | Environment |
|----------|-------------|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Database connection string | - | All |
| `DB_PASSWORD` | Database password | `YourSafePassword123!ChangeMe` | All |

## Health Check Endpoints

### Application Health
```bash
curl http://localhost:8080/api/health
```

Expected response:
```json
{
  "status": "healthy",
  "uptime": "2 days 3 hours",
  "queuedJobs": 5,
  "runningJobs": 2,
  "failedRecently": 1
}
```

### Detailed Health Information
```bash
curl http://localhost:8080/api/health/detail
```

## Scaling and High Availability

### Horizontal Scaling

For production deployments, run multiple instances behind a load balancer:

```yaml
services:
  scheduler:
    # ... base config
    deploy:
      replicas: 3
```

### Resource Requirements

| Resource | Minimum | Recommended |
|----------|----------|-------------|
| CPU | 1 vCPU | 2 vCPU |
| Memory | 1 GB | 2 GB |
| Storage | 10 GB | 20 GB |

## Backup and Recovery

### Database Backup
```bash
# Backup SQL Server database
docker exec job-scheduler-db \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $DB_PASSWORD \
  -Q "BACKUP DATABASE JobScheduler TO DISK = '/var/opt/mssql/backups/jobscheduler.bak'"
```

### Configuration Backup
```bash
# Backup configuration files
docker cp scheduler:/app/appsettings.json ./backups/
```

## Troubleshooting Common Issues

### Container Won't Start

```bash
# Check logs
docker compose logs scheduler

# Common errors:
# - Permission denied (check volume permissions)
# - Database connection failed (check DB_PASSWORD)
# - Port already in use (check port mapping)
```

### Database Connection Issues

```bash
# Verify database connectivity
docker compose exec scheduler \
  curl -v "Server=database;Database=JobScheduler;User Id=sa;Password=${DB_PASSWORD};"
```

### Health Check Failures

```bash
# Test health endpoint
curl http://localhost:8080/api/health

# Check if container is healthy
docker compose ps
```

## Best Practices

### Security
1. Never hardcode passwords in docker-compose.yml
2. Use non-root user (default in v2.0)
3. Regularly rotate database passwords
4. Use network policies to restrict database access

### Performance
1. Set resource limits to prevent resource exhaustion
2. Monitor container metrics
3. Use appropriate instance types for your workload
4. Enable health checks for production monitoring

### Monitoring
1. Enable structured logging
2. Set up health checks
3. Monitor execution metrics
4. Set up alerting for job failures

## Advanced Configuration

### Custom Network Setup
```yaml
networks:
  scheduler-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

### Custom Health Check
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/api/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

## Environment-Specific Configuration

### Development
```bash
export DB_PASSWORD=devPassword123
```

### Production
```bash
export DB_PASSWORD=YourProductionPassword123!
```

### Kubernetes Integration
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: job-scheduler
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: scheduler
        image: job-scheduler:2.0
        env:
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: password
```

## Migration from v1.x

If you're upgrading from v1.x, see the [Migration Guide](migration-guide-v2.md) for detailed instructions on port changes, non-root user configuration, and environment variable updates.

## Next Steps

- Review the [Deployment Guide](deployment.md) for production deployment best practices
- Check the [Getting Started Guide](getting-started.md) for basic setup instructions
- See [API Reference](api-reference.md) for available endpoints
- Read [FAQ](faq.md) for common questions and troubleshooting