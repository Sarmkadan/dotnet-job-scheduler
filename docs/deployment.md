# Deployment Guide

Production deployment strategies and considerations for dotnet-job-scheduler.

## Pre-Deployment Checklist

### Application Configuration

- [ ] Set `ASPNETCORE_ENVIRONMENT` to `Production`
- [ ] Configure appropriate `MaxConcurrentJobs` (default 10)
- [ ] Set reasonable timeout values (default 300s)
- [ ] Enable performance monitoring
- [ ] Configure cleanup retention (default 30 days)
- [ ] Set appropriate poll interval (default 5000ms)

### Database Setup

- [ ] Database server is running and accessible
- [ ] Connection string is correct
- [ ] Database credentials are secured
- [ ] Entity Framework migrations applied
- [ ] Database backups configured
- [ ] Database indices created
- [ ] Transaction log backups enabled

### Monitoring & Logging

- [ ] Structured logging configured (Serilog)
- [ ] Log retention policy set
- [ ] Error alerting configured
- [ ] Application Insights/Datadog integration
- [ ] Database query logging enabled
- [ ] Performance metrics collection

### Security

- [ ] HTTPS/TLS configured
- [ ] API authentication enabled (if applicable)
- [ ] Rate limiting configured
- [ ] Secrets managed securely (not in config)
- [ ] Database firewall rules configured
- [ ] Network isolation in place

## Docker Deployment

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY bin/Release/net10.0/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "JobScheduler.Core.dll"]
```

### Build and Run

```bash
# Build release binary
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./publish

# Build Docker image
docker build -t job-scheduler:latest .

# Run container
docker run -d \
  --name job-scheduler \
  -p 5000:5000 \
  -e "ConnectionStrings__DefaultConnection=Server=db;Database=scheduler;..." \
  -e "JobScheduler__MaxConcurrentJobs=20" \
  -e "ASPNETCORE_ENVIRONMENT=Production" \
  job-scheduler:latest
```

### Docker Compose

```yaml
version: '3.8'

services:
  database:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: Y
      SA_PASSWORD: YourSafePassword123!
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test: ["CMD", "/opt/mssql-tools/bin/sqlcmd", "-S", "localhost", 
             "-U", "sa", "-P", "YourSafePassword123!", "-Q", "SELECT 1"]
      interval: 10s
      timeout: 3s
      retries: 3

  scheduler:
    build: .
    depends_on:
      database:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Server=database;Database=JobScheduler;User Id=sa;Password=YourSafePassword123!;"
      JobScheduler__MaxConcurrentJobs: 20
      JobScheduler__DefaultTimeoutSeconds: 600
    ports:
      - "5000:5000"
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/api/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

volumes:
  sqldata:
```

### Start with Docker Compose

```bash
docker-compose up -d

# View logs
docker-compose logs -f scheduler

# Stop services
docker-compose down
```

## Kubernetes Deployment

### Deployment YAML

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: job-scheduler
  labels:
    app: job-scheduler
spec:
  replicas: 3
  selector:
    matchLabels:
      app: job-scheduler
  template:
    metadata:
      labels:
        app: job-scheduler
    spec:
      containers:
      - name: scheduler
        image: job-scheduler:latest
        ports:
        - containerPort: 5000
          name: http
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: scheduler-secrets
              key: connection-string
        - name: JobScheduler__MaxConcurrentJobs
          value: "20"
        - name: JobScheduler__DefaultTimeoutSeconds
          value: "600"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /api/health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /api/health
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3

---
apiVersion: v1
kind: Service
metadata:
  name: job-scheduler-service
spec:
  selector:
    app: job-scheduler
  type: LoadBalancer
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5000

---
apiVersion: v1
kind: Secret
metadata:
  name: scheduler-secrets
type: Opaque
stringData:
  connection-string: "Server=sql-server;Database=JobScheduler;User Id=sa;Password=..."
```

### Deploy to Kubernetes

```bash
# Create secret
kubectl create secret generic scheduler-secrets \
  --from-literal=connection-string="Server=...;Database=JobScheduler;..."

# Deploy
kubectl apply -f deployment.yaml

# Check status
kubectl get pods
kubectl logs -f deployment/job-scheduler

# Scale
kubectl scale deployment/job-scheduler --replicas=5

# Update image
kubectl set image deployment/job-scheduler \
  scheduler=job-scheduler:v1.2.0
```

## Cloud Platforms

### Azure App Service

1. **Create App Service Plan**:
```bash
az appservice plan create \
  --name job-scheduler-plan \
  --resource-group mygroup \
  --sku B2
```

2. **Create Web App**:
```bash
az webapp create \
  --resource-group mygroup \
  --plan job-scheduler-plan \
  --name job-scheduler-app \
  --runtime "dotnet|10.0"
```

3. **Configure Connection String**:
```bash
az webapp config appsettings set \
  --resource-group mygroup \
  --name job-scheduler-app \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=myserver.database.windows.net;..." \
    "JobScheduler__MaxConcurrentJobs=20"
```

4. **Deploy**:
```bash
az webapp up --name job-scheduler-app
```

### AWS Elastic Beanstalk

1. **Create application**:
```bash
eb create job-scheduler-env \
  --instance-type t3.medium \
  --envvars "ASPNETCORE_ENVIRONMENT=Production"
```

2. **Configure RDS database**:
```bash
eb setenv "ConnectionStrings__DefaultConnection=Server=db.*.rds.amazonaws.com;..."
```

3. **Deploy**:
```bash
eb deploy
eb open
```

### Google Cloud Run

```bash
# Build and push Docker image
gcloud builds submit --tag gcr.io/PROJECT_ID/job-scheduler

# Deploy
gcloud run deploy job-scheduler \
  --image gcr.io/PROJECT_ID/job-scheduler \
  --memory 512Mi \
  --cpu 1 \
  --region us-central1 \
  --set-env-vars "ConnectionStrings__DefaultConnection=..." \
  --allow-unauthenticated
```

## Windows Service Installation

### Create Windows Service

```powershell
# Create service
New-Service -Name "JobScheduler" `
  -BinaryPathName "C:\app\JobScheduler.Core.exe" `
  -DisplayName "Distributed Job Scheduler" `
  -Description "Executes scheduled jobs with cron expressions" `
  -StartupType Automatic

# Start service
Start-Service -Name "JobScheduler"

# View status
Get-Service -Name "JobScheduler"
```

### Using NSSM (Non-Sucking Service Manager)

```bash
# Download nssm
# Install service
nssm install JobScheduler C:\app\JobScheduler.Core.exe
nssm set JobScheduler AppDirectory C:\app
nssm set JobScheduler AppStdout C:\logs\out.log
nssm set JobScheduler AppStderr C:\logs\error.log
nssm set JobScheduler Start SERVICE_AUTO_START

# Start
nssm start JobScheduler

# View logs
nssm get JobScheduler AppStdout
```

## Linux Systemd Service

### Create Systemd Unit

Create `/etc/systemd/system/job-scheduler.service`:

```ini
[Unit]
Description=Distributed Job Scheduler
After=network.target
StartLimitIntervalSec=600
StartLimitBurst=3

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /opt/job-scheduler/JobScheduler.Core.dll
WorkingDirectory=/opt/job-scheduler
User=scheduler
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://0.0.0.0:5000"
Restart=on-failure
RestartSec=30

[Install]
WantedBy=multi-user.target
```

### Enable and Start

```bash
# Reload systemd
sudo systemctl daemon-reload

# Enable
sudo systemctl enable job-scheduler

# Start
sudo systemctl start job-scheduler

# Check status
sudo systemctl status job-scheduler

# View logs
sudo journalctl -u job-scheduler -f
```

## Configuration Management

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection=Server=localhost;Database=JobScheduler;...

# Scheduler Settings
JobScheduler__MaxConcurrentJobs=20
JobScheduler__DefaultTimeoutSeconds=600
JobScheduler__DefaultMaxRetries=3
JobScheduler__DefaultRetryBackoffSeconds=5
JobScheduler__QueuePollIntervalMs=5000
JobScheduler__EnableCleanup=true
JobScheduler__CleanupIntervalMs=300000
JobScheduler__ExecutionHistoryRetentionDays=60

# Logging
Serilog__MinimumLevel=Information
Serilog__WriteTo__0__Name=Console
Serilog__WriteTo__1__Name=File
```

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db-server;Database=JobScheduler;User Id=sa;Password=..."
  },
  "JobScheduler": {
    "MaxConcurrentJobs": 30,
    "DefaultTimeoutSeconds": 600,
    "DefaultMaxRetries": 3,
    "DefaultRetryBackoffSeconds": 5,
    "QueuePollIntervalMs": 5000,
    "EnableCleanup": true,
    "CleanupIntervalMs": 600000,
    "ExecutionHistoryRetentionDays": 90,
    "EnablePerformanceMonitoring": true
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/job-scheduler/-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

## Database Migrations

### Apply Migrations

```bash
# Using dotnet CLI
dotnet ef database update

# Using Package Manager Console
Update-Database

# Generate migration script (SQL)
dotnet ef migrations script -o migration.sql

# Apply to production database
sqlcmd -S prodserver -d JobScheduler -i migration.sql
```

### Backup Before Migration

```bash
# SQL Server
BACKUP DATABASE JobScheduler 
TO DISK = '/backups/JobScheduler_pre_upgrade.bak'

# PostgreSQL
pg_dump job_scheduler > backup_pre_upgrade.sql

# SQLite
cp scheduler.db scheduler_pre_upgrade.db
```

## Monitoring & Alerting

### Health Check Endpoint

```bash
curl http://localhost:5000/api/health

# Expected response
{
  "status": "Healthy",
  "checks": {
    "database": {"status": "Healthy"},
    "scheduler": {"status": "Healthy"}
  }
}
```

### Prometheus Metrics

```bash
curl http://localhost:5000/metrics

# Metrics available
- job_scheduler_total_jobs
- job_scheduler_running_jobs
- job_scheduler_failed_last_24h
- job_scheduler_success_rate
- job_scheduler_average_duration_ms
```

### Alerting Rules

**Alert if**:
- Health check fails for 5 minutes
- Success rate drops below 95%
- Average execution time exceeds threshold
- Queued jobs accumulate
- Database connection fails
- Memory usage exceeds limits

### Example Prometheus Alert

```yaml
groups:
  - name: job-scheduler
    rules:
      - alert: SchedulerDown
        expr: up{job="job-scheduler"} == 0
        for: 5m
        annotations:
          summary: "Job Scheduler is down"

      - alert: HighFailureRate
        expr: job_scheduler_success_rate < 0.95
        for: 10m
        annotations:
          summary: "Job failure rate above 5%"

      - alert: QueueBacklog
        expr: job_scheduler_queued_jobs > 50
        for: 15m
        annotations:
          summary: "{{ $value }} jobs queued"
```

## Performance Tuning

### Database Optimization

1. **Index Statistics** (SQL Server):
```sql
EXEC sp_updatestats;
```

2. **Rebuild Indices** (PostgreSQL):
```sql
REINDEX INDEX CONCURRENT idx_job_next_execution;
```

3. **Query Analysis**:
```sql
-- Find slow queries
SELECT * FROM sys.dm_exec_requests 
WHERE status = 'running'
```

### Application Tuning

1. **Increase concurrency** for high-throughput:
```csharp
options.MaxConcurrentJobs = 50; // For powerful servers
```

2. **Decrease poll interval** for responsive scheduling:
```csharp
options.QueuePollIntervalMs = 1000; // Check every second
```

3. **Tune connection pooling**:
```csharp
optionsBuilder.UseSqlServer(
    connectionString,
    options => options.UseConnectionPooling(
        poolSize: 20,
        maxPoolSize: 100
    )
);
```

## Backup & Disaster Recovery

### Daily Backup Strategy

```bash
#!/bin/bash
BACKUP_DIR="/backups/scheduler"
DATE=$(date +%Y%m%d_%H%M%S)

# Full backup daily
sqlcmd -S localhost -d JobScheduler \
  -Q "BACKUP DATABASE JobScheduler TO DISK = '$BACKUP_DIR/scheduler_$DATE.bak'"

# Keep 30 days
find $BACKUP_DIR -name "*.bak" -mtime +30 -delete
```

### Restore from Backup

```sql
-- SQL Server
RESTORE DATABASE JobScheduler 
FROM DISK = '/backups/scheduler_backup.bak'
WITH RECOVERY

-- PostgreSQL
psql job_scheduler < backup.sql
```

## Scaling Strategies

### Horizontal Scaling (Multiple Instances)

```
┌──────────────────┐
│  Load Balancer   │
└────────┬─────────┘
         │
    ┌────┼────┐
    │    │    │
    ▼    ▼    ▼
[Inst1][Inst2][Inst3]  ← All connect to shared DB
    │    │    │
    └────┼────┘
         │
    ┌────▼─────────┐
    │  Shared DB   │
    └──────────────┘
```

**Configuration**:
- Set different job subsets per instance
- Or use distributed locking for automatic distribution
- Ensure database can handle multiple connections

### Vertical Scaling (Larger Instances)

Increase `MaxConcurrentJobs` proportionally to resources:

```
Resource Multiplier → Concurrency Multiplier
2x CPU/Memory → ~1.8x MaxConcurrentJobs
```

## Rollback Strategy

### Zero-Downtime Deployment

1. **Deploy new version** to separate instance
2. **Run health checks** on new instance
3. **Route traffic** to new instance
4. **Monitor** for errors
5. **Rollback** if issues detected

```bash
# Kubernetes rolling update (automatic)
kubectl set image deployment/job-scheduler \
  scheduler=job-scheduler:v1.2.0

# Monitor rollout
kubectl rollout status deployment/job-scheduler
kubectl rollout history deployment/job-scheduler
kubectl rollout undo deployment/job-scheduler  # Rollback
```

## Troubleshooting Deployments

### Application Won't Start

1. Check logs: `docker logs scheduler` or `journalctl -u job-scheduler -f`
2. Verify database connection string
3. Ensure database is running and accessible
4. Check port not already in use
5. Verify .NET runtime installed

### Database Migration Fails

1. Backup database first
2. Review migration script for errors
3. Run migration manually with script review
4. Check for locks on database
5. Ensure sufficient disk space

### High Memory Usage

1. Reduce `MaxConcurrentJobs` setting
2. Lower execution history retention days
3. Enable aggressive cleanup
4. Monitor long-running jobs
5. Check for connection leaks

### Jobs Not Executing

1. Verify scheduler service is running
2. Check database connectivity
3. Review job status in database
4. Verify cron expressions are valid
5. Check application logs for errors

This deployment guide covers common deployment scenarios from Docker to Kubernetes to cloud platforms. Choose the strategy that best fits your infrastructure and requirements.
