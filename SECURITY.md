# Security Policy

## Reporting a Vulnerability

**Do NOT open public GitHub Issues for security vulnerabilities.** This could expose the vulnerability to potential attackers before a fix is available.

### Reporting Process

**Option 1: GitHub Private Vulnerability Reporting (Recommended)**

Use GitHub's private vulnerability reporting feature:

1. Go to [Security Advisories](https://github.com/sarmkadan/dotnet-job-scheduler/security/advisories/new)
2. Click "Report a vulnerability"
3. Fill in the vulnerability details
4. Submit the report

GitHub will automatically notify the maintainers and create a private discussion thread for coordination.

**Option 2: Email**

Email your vulnerability report to: **rutova2@gmail.com**

Include:
- Description of the vulnerability
- Steps to reproduce (if applicable)
- Affected version(s)
- Potential impact
- Suggested fix (if you have one)

### Response Timeline

- **48 hours**: Initial acknowledgment of your report
- **1 week**: Assessment and initial response
- **Coordinated disclosure**: We'll work with you on a timeline for public disclosure

## Supported Versions

Security updates are provided for:

- **v1.x**: Currently supported
- **Earlier versions**: No longer supported; users are encouraged to upgrade

## Security Considerations

### Database Security

- The job scheduler stores sensitive information (execution logs, job configurations)
- **Protect your database credentials** in `appsettings.json`
- Use environment variables or secure vaults (Azure Key Vault, AWS Secrets Manager) for secrets
- Enable encryption at rest for sensitive data

Example:
```csharp
// Good: Use environment variables
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

// Avoid: Hardcoding credentials
var connectionString = "Server=localhost;User=admin;Password=password123";
```

### API Security

- Enable HTTPS in production
- Use authentication/authorization on scheduler endpoints
- Implement rate limiting (built-in via middleware)
- Validate all webhook URLs before storing
- Use HTTPS for webhook notifications

### Job Execution

- Job handlers execute in the application's process; review custom handlers carefully
- External dependencies in job handlers should be security-audited
- Monitor execution logs for suspicious activity
- Use timeout limits to prevent resource exhaustion

### Dependency Updates

- Keep .NET SDK updated to the latest stable version
- Monitor dependencies for vulnerabilities
- Use `dotnet list package --outdated` to identify stale packages
- Review breaking changes before upgrading major versions

## Best Practices

### Development

- Never commit credentials or secrets to the repository
- Use `.gitignore` to exclude sensitive files:
  ```
  appsettings.*.json
  *.user
  bin/
  obj/
  .env
  ```

- Validate user input on job creation
- Use parameterized queries (EF Core handles this)
- Sanitize webhook URLs and headers

### Deployment

- Use environment-specific configuration:
  - `appsettings.Development.json` (local dev only, git-ignored)
  - `appsettings.Production.json` (deployed securely)

- Enable audit logging to track all job operations
- Monitor database growth and performance
- Set up alerting for failed job executions
- Use strong database credentials
- Enable database backups and test restore procedures

### Monitoring

- Review execution logs regularly for errors
- Monitor for repeated job failures (potential DoS or misconfiguration)
- Track API request patterns for anomalies
- Set up alerts for critical job failures

## Known Security Limitations

None currently known. Please report any discovered issues privately.

## Security Headers

If exposing the scheduler dashboard over HTTP(S), consider adding security headers:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    await next();
});
```

## Third-Party Dependencies

The project uses the following key dependencies:
- **Entity Framework Core**: For database access
- **Microsoft.Extensions.DependencyInjection**: For IoC
- **CronExpressionDescriptor**: For cron parsing

These are regularly monitored for security updates.

## Questions or Concerns?

For non-vulnerability security questions, use [GitHub Discussions](https://github.com/sarmkadan/dotnet-job-scheduler/discussions).

---

Thank you for helping keep the dotnet-job-scheduler project secure.
