# Migration Guide: v1.x to v2.0

This guide covers all changes introduced in v2.0 and provides step-by-step instructions for upgrading from any v1.x release.

## Overview

v2.0 focuses on production hardening, security improvements, and operational excellence. Key highlights:

- **Security**: Non-root container execution
- **Standardization**: Default port alignment with .NET 10 container defaults
- **Operational**: Better Docker configuration and environment variable support
- **Documentation**: Comprehensive migration guide and updated documentation

## Breaking Changes

### 1. Default Port Changed: 5000 → 8080

**Impact**: The application now listens on port **8080** by default (previously 5000).

**Why**: This aligns with .NET 10 container defaults and avoids conflicts with macOS AirPlay Receiver on port 5000.

**Action Required**: Update all references to port 5000:

#### Reverse Proxy Configurations
```yaml
# Before (v1.x)
- "5000:5000"

# After (v2.0)
- "8080:8080"
```

#### Kubernetes/Helm Manifests
```yaml
# Before
ports:
  - containerPort: 5000
    protocol: TCP

# After
ports:
  - containerPort: 8080
    protocol: TCP
```

#### Docker Run Commands
```bash
# Before
 docker run -p 5000:5000 ...

# After
 docker run -p 8080:8080 ...
```

#### Environment Variables
```bash
# Before
ASPNETCORE_URLS=http://0.0.0.0:5000

# After
ASPNETCORE_URLS=http://0.0.0.0:8080
```

#### Health Check URLs
```yaml
# Kubernetes liveness probe
httpGet:
  path: /api/health
  port: 8080  # Changed from 5000
```

### 2. Docker Image Runs as Non-Root User

**Impact**: The container now runs as `appuser` (UID 1000) instead of root.

**Why**: Security best practice - principle of least privilege.

**Action Required**: Update volume permissions if you mount writable directories:

```bash
# Fix permissions for mounted volumes
chown -R 1000:1000 ./logs
chown -R 1000:1000 ./data
```

**Note**: If you're using SQLite, ensure the database file is writable by UID 1000.

### 3. Database Password via Environment Variable

**Impact**: `docker-compose.yml` now reads `DB_PASSWORD` from environment (or `.env` file) instead of hardcoding.

**Why**: Security best practice - avoid hardcoded secrets in configuration files.

**Action Required**: 

1. Create `.env` file in project root:
```bash
# .env
DB_PASSWORD=YourProductionPassword!ChangeThis
```

2. Or set in your environment:
```bash
export DB_PASSWORD="YourProductionPassword!ChangeThis"
```

**Default fallback**: The password `YourSafePassword123!ChangeMe` is still used if no password is set, but production deployments should explicitly set this variable.

### 4. Docker Compose Version Field Removed

**Impact**: The `version: '3.8'` key has been removed from `docker-compose.yml`.

**Why**: Docker Compose v2+ ignores this field and prints a deprecation warning. The field is not needed for modern Docker Compose.

**Action Required**: None - this is a no-op change. If you're still using Docker Compose v1 (which is EOL), upgrade to v2+.

### 5. Publish Uses `UseAppHost=false`

**Impact**: The Dockerfile publish step now passes `/p:UseAppHost=false` to produce a framework-dependent deployment.

**Why**: Reduces image size by ~50MB and aligns with .NET 10 container best practices.

**Action Required**: If you relied on the native host executable, set `UseAppHost=true` in your build override:

```dockerfile
FROM builder AS publish
RUN dotnet publish "src/JobScheduler.Core/JobScheduler.Core.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=true
```

## Upgrade Steps

Follow these steps to upgrade from v1.x to v2.0:

### Step 1: Update Package Reference

Update your `.csproj` file to reference v2.0:

```xml
<PackageReference Include="Zaiets.dotnet.job.scheduler" Version="2.0.0" />
```

Or via command line:
```bash
dotnet add package Zaiets.dotnet.job.scheduler --version 2.0.0
```

### Step 2: Update Port References

Replace all instances of port 5000 with 8080 in your configuration files.

**Files to update**:
- Reverse proxy configs (Nginx, Apache, Traefik, Caddy, etc.)
- Kubernetes manifests and Helm charts
- Docker run commands and compose files
- CI/CD pipeline configurations
- Monitoring and alerting systems

### Step 3: Set Database Password (Production Only)

For production deployments, explicitly set the database password:

```bash
# Create .env file
cat > .env << EOF
DB_PASSWORD=YourSecurePassword123!
EOF
```

**Important**: Add `.env` to your `.gitignore` file to avoid committing secrets.

### Step 4: Fix Volume Permissions

If you mount writable volumes into the container, update permissions:

```bash
# For logs directory
chown -R 1000:1000 ./logs

# For any custom data directories
chown -R 1000:1000 ./data
```

### Step 5: Rebuild Docker Images

```bash
# Clean and rebuild
docker compose build --no-cache
docker compose up -d
```

### Step 6: Verify Health

```bash
# Check health endpoint
curl http://localhost:8080/api/health

# Expected response:
{
  "status": "healthy",
  "uptime": "X days Y hours",
  "queuedJobs": 0,
  "runningJobs": 0,
  "failedRecently": 0
}
```

### Step 7: Test Job Execution

Create a test job and verify it executes:

```bash
# Create a simple job via API
curl -X POST http://localhost:8080/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TestJob",
    "description": "Test job for migration",
    "cronExpression": "* * * * *",
    "handlerType": "JobScheduler.Core.Tests.TestHandlers.SimpleTestHandler, JobScheduler.Core.Tests",
    "priority": 2,
    "isActive": true,
    "maxRetries": 1,
    "executionTimeoutSeconds": 30
  }'

# Check execution history
curl http://localhost:8080/api/executions?jobId=1 | jq .
```

## Rollback Plan

If you encounter issues, you can rollback to v1.1.0:

### Option 1: Package Downgrade
```bash
dotnet add package Zaiets.dotnet.job.scheduler --version 1.1.0
```

### Option 2: Restore Old Docker Files
```bash
# Restore from git history
git checkout v1.1.0 -- Dockerfile docker-compose.yml
```

### Option 3: Use Old Port Temporarily
```bash
# Set environment variable to use old port
ASPNETCORE_URLS=http://0.0.0.0:5000 dotnet run
```

## Common Issues and Solutions

### Issue 1: Container Won't Start - Permission Denied

**Error**: `Permission denied` when accessing mounted volumes

**Solution**: Fix permissions as shown in Step 4 above.

```bash
# Check container logs
 docker compose logs scheduler

# Typical error:
# touch: cannot touch '/app/logs/test.log': Permission denied
```

### Issue 2: Database Connection Fails

**Error**: `Login failed for user 'sa'` or similar

**Solution**: Verify the password is set correctly:

```bash
# Check environment variable
echo "$DB_PASSWORD"

# Or check .env file
cat .env
```

### Issue 3: Jobs Not Executing

**Error**: Jobs created but never run

**Solutions**:
1. Verify port is correct: Use 8080, not 5000
2. Check background service is running
3. Review application logs:
```bash
docker compose logs scheduler | grep -i error
```

### Issue 4: Health Check Failing

**Error**: Health check returns unhealthy

**Solutions**:
1. Verify container is running:
```bash
docker ps
```
2. Check health check endpoint:
```bash
curl -v http://localhost:8080/api/health
```
3. Review health check configuration in docker-compose.yml

## Configuration Changes Summary

| Configuration | v1.x | v2.0 |
|---------------|-------|------|
| Default Port | 5000 | 8080 |
| Container User | root | appuser (UID 1000) |
| DB Password | hardcoded | environment variable |
| Compose Version | 3.8 | removed |
| Image Size | larger | ~50MB smaller |

## API Compatibility

✅ **Fully backward compatible** - No API changes between v1.x and v2.0

All public APIs remain unchanged. You can upgrade the package without modifying your job handlers or application code.

## Database Compatibility

✅ **Fully compatible** - No database migration required

The v2.0 database schema is identical to v1.x. Existing jobs, executions, and metrics will continue to work.

## Docker Compatibility

✅ **Compatible** with both v1.x and v2.0 images

You can mix v1.x and v2.0 images during rollout if needed.

## Verification Checklist

Before deploying to production, verify:

- [ ] Port changed from 5000 to 8080 in all configs
- [ ] Database password set via environment variable
- [ ] Volume permissions updated for UID 1000
- [ ] Health check endpoint returns healthy
- [ ] Jobs execute correctly
- [ ] Monitoring/alerting updated to use port 8080
- [ ] Reverse proxies updated
- [ ] CI/CD pipelines updated

## Need Help?

If you encounter issues during migration:

1. Check the [FAQ](faq.md) for common questions
2. Review application logs: `docker compose logs scheduler`
3. Open an issue at [github.com/sarmkadan/dotnet-job-scheduler/issues](https://github.com/sarmkadan/dotnet-job-scheduler/issues)
4. Contact: rutova2@gmail.com

## Additional Resources

- [Deployment Guide](deployment.md) - Production deployment best practices
- [Getting Started Guide](getting-started.md) - First-time setup
- [Docker Guide](docker-guide.md) - Docker-specific documentation
