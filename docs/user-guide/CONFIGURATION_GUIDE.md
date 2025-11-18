# Aura Video Studio - Configuration Guide

This guide covers the new configuration options added as part of the service registration fixes.

## Table of Contents

1. [Database Configuration](#database-configuration)
2. [Background Jobs (Hangfire)](#background-jobs-hangfire)
3. [Caching (Redis)](#caching-redis)
4. [Real-Time Updates (SignalR)](#real-time-updates-signalr)
5. [Provider Configuration](#provider-configuration)

---

## Database Configuration

### SQLite (Default)

SQLite is the default database provider, suitable for development and small-scale deployments.

```json
{
  "Database": {
    "Provider": "SQLite",
    "SQLitePath": "aura.db"
  }
}
```

**Options:**
- `Provider`: Must be "SQLite"
- `SQLitePath`: Path to the database file (relative or absolute). Defaults to `aura.db` in the application directory.

### PostgreSQL (Production)

PostgreSQL is recommended for production environments with multiple users or high concurrency.

```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "ConnectionString": "Host=localhost;Port=5432;Database=aura;Username=aura_user;Password=your_password"
  },
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=aura;Username=aura_user;Password=your_password"
  }
}
```

**Options:**
- `Provider`: Must be "PostgreSQL"
- `ConnectionString`: Full PostgreSQL connection string (can use either location)
- `ConnectionStrings:PostgreSQL`: Alternative location for connection string

**Connection String Format:**
```
Host=<hostname>;Port=<port>;Database=<dbname>;Username=<user>;Password=<password>;Include Error Detail=true
```

**Setup Steps:**

1. Install PostgreSQL on your server
2. Create a database and user:
   ```sql
   CREATE DATABASE aura;
   CREATE USER aura_user WITH ENCRYPTED PASSWORD 'your_password';
   GRANT ALL PRIVILEGES ON DATABASE aura TO aura_user;
   ```
3. Update `appsettings.json` with the connection string
4. Run migrations (if applicable):
   ```bash
   dotnet ef database update --project Aura.Api
   ```

---

## Background Jobs (Hangfire)

Hangfire provides background job processing for long-running tasks like video generation, exports, and cleanup operations.

### Configuration

```json
{
  "ConnectionStrings": {
    "Hangfire": "Host=localhost;Database=aura_hangfire;Username=aura_user;Password=your_password"
  },
  "Hangfire": {
    "SQLitePath": "hangfire.db"
  }
}
```

**Options:**
- `ConnectionStrings:Hangfire`: Connection string for Hangfire storage
  - If using PostgreSQL: Full PostgreSQL connection string
  - If using SQLite: Leave empty (will use SQLitePath)
  - If empty: Hangfire is disabled (graceful degradation)
- `Hangfire:SQLitePath`: Path to SQLite database file if not using PostgreSQL

### Disabling Hangfire

To disable Hangfire (synchronous processing only):

```json
{
  "ConnectionStrings": {
    "Hangfire": ""
  }
}
```

### Accessing the Dashboard

When Hangfire is enabled, the dashboard is available at:
```
http://localhost:5005/hangfire
```

**Note:** The dashboard currently has no authentication. Implement authorization in production:

```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new YourAuthorizationFilter() }
});
```

### Job Queues

Hangfire is configured with multiple queues for different job types:
- `default`: General background jobs
- `video-generation`: Video generation tasks
- `exports`: Export operations
- `cleanup`: Cleanup and maintenance tasks

---

## Caching (Redis)

Redis provides distributed caching for improved performance in multi-instance deployments.

### In-Memory Cache (Default)

```json
{
  "Caching": {
    "Enabled": true,
    "UseRedis": false
  }
}
```

### Redis Cache (Production)

```json
{
  "Caching": {
    "Enabled": true,
    "UseRedis": true,
    "RedisConnection": "localhost:6379,abortConnect=false,connectTimeout=5000",
    "DefaultTtlMinutes": 60
  }
}
```

**Options:**
- `Enabled`: Enable/disable caching entirely
- `UseRedis`: Use Redis instead of in-memory cache
- `RedisConnection`: Redis connection string
- `DefaultTtlMinutes`: Default cache expiration time

**Redis Connection String Format:**
```
<host>:<port>,password=<password>,abortConnect=false,connectTimeout=5000,ssl=true
```

**Setup Steps:**

1. Install Redis:
   ```bash
   # Ubuntu/Debian
   sudo apt-get install redis-server
   
   # macOS
   brew install redis
   
   # Windows (via Chocolatey)
   choco install redis-64
   ```

2. Start Redis:
   ```bash
   redis-server
   ```

3. Update configuration and restart the application

### Disabling Cache

```json
{
  "Caching": {
    "Enabled": false
  }
}
```

---

## Real-Time Updates (SignalR)

SignalR provides real-time bidirectional communication between server and clients.

### Configuration

SignalR is automatically registered and requires no additional configuration. It uses the following default settings:

- **Keep-Alive Interval:** 15 seconds
- **Client Timeout:** 30 seconds
- **Max Message Size:** 100 KB

### Creating SignalR Hubs

To add real-time functionality, create hub classes in `Aura.Api/Hubs/`:

**Example: GenerationProgressHub.cs**
```csharp
using Microsoft.AspNetCore.SignalR;

namespace Aura.Api.Hubs;

public class GenerationProgressHub : Hub
{
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
    }
    
    public async Task UnsubscribeFromJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId);
    }
}
```

**Map the hub in Program.cs:**
```csharp
app.MapHub<GenerationProgressHub>("/hubs/generation-progress");
```

### Client Connection

**JavaScript Example:**
```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/generation-progress")
    .withAutomaticReconnect()
    .build();

await connection.start();

// Subscribe to job updates
await connection.invoke("SubscribeToJob", jobId);

// Listen for progress updates
connection.on("ProgressUpdate", (data) => {
    console.log("Progress:", data);
});
```

**Server Broadcasting:**
```csharp
private readonly IHubContext<GenerationProgressHub> _hubContext;

public async Task NotifyProgress(string jobId, int percent)
{
    await _hubContext.Clients.Group(jobId).SendAsync("ProgressUpdate", new
    {
        JobId = jobId,
        Percent = percent,
        Timestamp = DateTime.UtcNow
    });
}
```

---

## Provider Configuration

### Image Providers

Image providers are automatically selected based on configured API keys:

**Priority Order:** Stability AI > Runway

```json
{
  "Providers": {
    "Stability": {
      "ApiKey": "sk-..."
    },
    "Runway": {
      "ApiKey": "..."
    }
  }
}
```

If no API keys are configured, the `IImageProvider` will return null, and image generation will be skipped.

### Stock Media Providers

Stock media providers are automatically selected based on configured API keys:

**Priority Order:** Pexels > Unsplash > Pixabay > Local

```json
{
  "Providers": {
    "Pexels": {
      "ApiKey": "..."
    },
    "Unsplash": {
      "AccessKey": "..."
    },
    "Pixabay": {
      "ApiKey": "..."
    }
  }
}
```

If no API keys are configured, the `LocalStockProvider` is used (reads from `{AuraDataDirectory}/Stock`).

### LLM Providers

LLM providers are registered via the existing `AddAuraProviders()` extension method:

```json
{
  "Providers": {
    "OpenAI": {
      "ApiKey": "sk-...",
      "Organization": "",
      "BaseUrl": "https://api.openai.com/v1"
    },
    "Anthropic": {
      "ApiKey": "sk-ant-..."
    },
    "Gemini": {
      "ApiKey": "..."
    },
    "Ollama": {
      "BaseUrl": "http://localhost:11434"
    }
  }
}
```

### TTS Providers

TTS providers are registered via the existing `AddAuraProviders()` extension method:

```json
{
  "Providers": {
    "ElevenLabs": {
      "ApiKey": "..."
    },
    "Azure": {
      "SpeechKey": "...",
      "SpeechRegion": "eastus"
    },
    "PlayHT": {
      "ApiKey": "...",
      "UserId": "..."
    }
  }
}
```

### Minimum Configuration for Basic AI Video Generation

The following components are strictly required for core video generation functionality:

#### Required Components:
1. **LLM Provider** (at least one):
   - RuleBased (always available, no setup required)
   - OR Ollama (local, free)
   - OR OpenAI/Anthropic/Gemini (cloud, requires API key)

2. **TTS Provider** (at least one):
   - Windows SAPI (always available on Windows, no setup required)
   - OR Piper (local, free, cross-platform)
   - OR ElevenLabs/PlayHT (cloud, requires API key)

3. **FFmpeg** (required for video rendering):
   - Available via system PATH
   - OR configured path in Settings
   - OR managed install via Aura

#### Optional Components:
- **Image Providers**: When missing, videos render with placeholder visuals
  - Stable Diffusion (local GPU)
  - Stock providers (Pexels, Unsplash, Pixabay)
  - Stability AI (cloud)

**Important**: Videos will always render successfully even if no image providers are configured. The pipeline gracefully degrades to use placeholder visuals when images are unavailable.

#### Example: Minimal Free-Only Configuration
```json
{
  "Providers": {
    "Llm": {
      "RuleBased": {
        "Enabled": true
      }
    },
    "Tts": {
      "WindowsSapi": {
        "Enabled": true
      }
    }
  },
  "FFmpeg": {
    "Path": "ffmpeg"
  }
}
```

---

## Environment-Specific Configuration

You can override settings per environment using environment-specific files:

- `appsettings.Development.json`
- `appsettings.Production.json`
- `appsettings.Staging.json`

**Example: appsettings.Production.json**
```json
{
  "Database": {
    "Provider": "PostgreSQL"
  },
  "Caching": {
    "UseRedis": true,
    "RedisConnection": "prod-redis.example.com:6379,password=...,ssl=true"
  },
  "Monitoring": {
    "EnableApplicationInsights": true,
    "ApplicationInsightsConnectionString": "InstrumentationKey=..."
  }
}
```

---

## Environment Variables

Configuration can also be set via environment variables:

```bash
export Database__Provider="PostgreSQL"
export ConnectionStrings__PostgreSQL="Host=localhost;Database=aura;..."
export Caching__UseRedis="true"
export ConnectionStrings__Hangfire="Host=localhost;Database=aura_hangfire;..."
```

**Format:** Use double underscores (`__`) to represent nested configuration keys.

---

## Verification

### Check Active Configuration

View active configuration at startup in the logs:

```
[Information] Using PostgreSQL database at localhost
[Information] Hangfire background job processing enabled
[Information] Redis distributed cache configured
[Information] SignalR hubs configured
```

### Health Checks

Verify services via health check endpoints:

```bash
# Full health check
curl http://localhost:5005/health | jq

# Ready check (for k8s readiness probes)
curl http://localhost:5005/health/ready | jq

# Live check (for k8s liveness probes)
curl http://localhost:5005/health/live | jq
```

---

## Troubleshooting

### Database Connection Errors

**PostgreSQL:**
```
Error: 3D000: database "aura" does not exist
```
→ Create the database: `CREATE DATABASE aura;`

**SQLite:**
```
Error: SQLite Error 14: 'unable to open database file'
```
→ Check file permissions and directory existence

### Hangfire Errors

```
Warning: Failed to configure Hangfire, background jobs will be disabled
```
→ Check Hangfire connection string and database permissions

### Redis Errors

```
Warning: Failed to configure Redis, falling back to in-memory cache
```
→ Verify Redis is running: `redis-cli ping` should return `PONG`

### SignalR Connection Errors

```
Error: Failed to start connection: Error: WebSocket failed to connect
```
→ Check CORS configuration and ensure WebSocket support is enabled

---

## Production Checklist

- [ ] Switch to PostgreSQL for database
- [ ] Configure Redis for caching
- [ ] Enable Hangfire with PostgreSQL storage
- [ ] Configure Application Insights monitoring
- [ ] Set up proper Hangfire dashboard authorization
- [ ] Enable HTTPS for production
- [ ] Configure rate limiting appropriately
- [ ] Set up backup strategy for database
- [ ] Configure log retention policies
- [ ] Test SignalR through load balancer/reverse proxy

---

## Additional Resources

- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Entity Framework Core - PostgreSQL](https://www.npgsql.org/efcore/)
- [Hangfire Documentation](https://docs.hangfire.io/)
- [Redis Documentation](https://redis.io/documentation)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)

---

*Last Updated: 2025-11-10*
