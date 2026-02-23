# Migration Guide: v1.x to v2.0.0

This document covers the breaking changes introduced in v2.0.0 and the steps required to upgrade from any v1.x release.

## Breaking Changes

### 1. Default Port Changed: 5000 -> 8080

The application now listens on port **8080** by default (previously 5000). This aligns with the .NET 10 container defaults and avoids conflicts with macOS AirPlay Receiver.

**Action required:**
- Update any reverse proxy configs (Caddy, Nginx, Traefik) pointing to the old port.
- Update Docker port mappings if you pinned `-p 5000:5000`.
- Update health check URLs in orchestrators (Kubernetes probes, Consul, etc.).
- If you override `ASPNETCORE_URLS`, change the port from 5000 to 8080.

```yaml
# Before (v1.x)
ports:
  - "5000:5000"

# After (v2.0)
ports:
  - "8080:8080"
```

### 2. Docker Compose: Removed `version` Field

The `version: '3.8'` key has been removed from `docker-compose.yml`. Docker Compose v2+ ignores it and prints a deprecation warning. No action needed unless you run Compose v1 (which is EOL).

### 3. Docker Image Runs as Non-Root

The container now runs as `appuser` (UID 1000) instead of root. If you mount volumes that require write access, ensure the host directory is owned by UID 1000 or is world-writable.

```bash
# Fix permissions for the logs volume
chown -R 1000:1000 ./logs
```

### 4. Database Password via Environment Variable

`docker-compose.yml` now reads `DB_PASSWORD` from the environment (or `.env` file) instead of hardcoding the password. The default fallback is unchanged for dev, but production deployments should set the variable explicitly.

```bash
# .env file
DB_PASSWORD=YourProductionPassword!
```

### 5. Publish Uses `UseAppHost=false`

The Dockerfile publish step now passes `/p:UseAppHost=false` to produce a framework-dependent deployment. This reduces image size. If you relied on the native host executable, set `UseAppHost=true` in your build override.

## Upgrade Steps

1. **Update your `.csproj`** - bump the package reference to `2.0.0`:
   ```xml
   <PackageReference Include="Zaiets.dotnet.job.scheduler" Version="2.0.0" />
   ```

2. **Change port references** - replace `5000` with `8080` in:
   - Reverse proxy configs
   - Kubernetes manifests / Helm values
   - Docker run commands
   - Environment variables (`ASPNETCORE_URLS`)

3. **Set `DB_PASSWORD`** in your environment or `.env` file for production.

4. **Fix volume permissions** if you mount writable directories into the container.

5. **Rebuild the Docker image**:
   ```bash
   docker compose build --no-cache
   docker compose up -d
   ```

6. **Verify health**:
   ```bash
   curl http://localhost:8080/api/health
   ```

## Rollback

If you need to revert, pin the package version to `1.1.0` and restore the old `Dockerfile` / `docker-compose.yml` from git history:

```bash
git checkout v1.1.0 -- Dockerfile docker-compose.yml
```

## Questions

Open an issue at [github.com/sarmkadan/dotnet-job-scheduler](https://github.com/sarmkadan/dotnet-job-scheduler/issues) if you hit problems during migration.
