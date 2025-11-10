# Developer Troubleshooting Guide

This guide covers issues specific to developers working on Aura Video Studio.

## Table of Contents

- [Build Issues](#build-issues)
- [Test Issues](#test-issues)
- [Development Environment](#development-environment)
- [Debugging Issues](#debugging-issues)
- [Git and Version Control](#git-and-version-control)
- [IDE Issues](#ide-issues)
- [Database Migrations](#database-migrations)

## Build Issues

### Issue: Build Fails with "Assets file not found"

**Symptoms**: NuGet restore errors

**Solution**:
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

### Issue: Frontend Build Fails

**Symptoms**: npm build errors

**Solution**:
```bash
cd Aura.Web

# Clear cache
rm -rf node_modules
rm package-lock.json

# Reinstall
npm install

# Rebuild
npm run build
```

### Issue: TypeScript Compilation Errors

**Symptoms**: Type errors in VS Code but build succeeds

**Solution**:
```bash
cd Aura.Web

# Restart TS server in VS Code
# Command Palette (Ctrl+Shift+P) -> "TypeScript: Restart TS Server"

# Or rebuild types
npm run typecheck
```

### Issue: "Could not find a part of the path"

**Symptoms**: Long path names causing build failures on Windows

**Solution**:
```powershell
# Enable long paths in Windows
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" `
  -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force

# Or move repository closer to root
# C:\dev\aura instead of C:\Users\VeryLongUserName\Documents\Projects\aura
```

## Test Issues

### Issue: Tests Fail with "Database locked"

**Symptoms**: Integration tests fail intermittently

**Solution**:
```csharp
// Use in-memory database for tests
services.AddDbContext<AuraDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));

// Or use unique database per test
var dbName = $"test_{Guid.NewGuid()}.db";
```

### Issue: E2E Tests Timing Out

**Symptoms**: Playwright tests timeout

**Solution**:
```typescript
// Increase timeout in playwright.config.ts
export default defineConfig({
  timeout: 60000, // 60 seconds
  expect: {
    timeout: 10000 // 10 seconds for assertions
  }
});

// Or specific test
test('my test', async ({ page }) => {
  test.setTimeout(120000); // 2 minutes
});
```

### Issue: Flaky Tests

**Symptoms**: Tests pass/fail intermittently

**Diagnostic**:
```bash
# Run test multiple times
for i in {1..10}; do
  dotnet test --filter "TestName~FlakyTest"
done

# Detect flaky tests automatically
./scripts/test/detect-flaky-tests.sh
```

**Solutions**:
1. Add explicit waits:
   ```typescript
   await page.waitForSelector('[data-testid="submit"]');
   await page.click('[data-testid="submit"]');
   ```
2. Avoid hardcoded sleeps:
   ```typescript
   // ❌ Bad
   await page.waitForTimeout(5000);
   
   // ✅ Good
   await page.waitForSelector('[data-testid="loaded"]');
   ```
3. Clean up state between tests:
   ```csharp
   public void Dispose()
   {
       _context.Database.EnsureDeleted();
       _context.Dispose();
   }
   ```

### Issue: Mock Provider Not Working

**Symptoms**: Tests call real APIs instead of mocks

**Solution**:
```csharp
// Ensure mock is registered before real service
services.AddSingleton<ILlmProvider>(new MockLlmProvider());

// Or use test configuration
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string>
    {
        ["Providers:OpenAI:Mock"] = "true"
    })
    .Build();
```

## Development Environment

### Issue: Hot Reload Not Working

**Symptoms**: Changes not reflected without restart

**Solution**:

**Backend**:
```bash
# Ensure using dotnet watch
dotnet watch run --project Aura.Api

# Check launchSettings.json
{
  "profiles": {
    "Aura.Api": {
      "hotReloadEnabled": true
    }
  }
}
```

**Frontend**:
```bash
# Ensure dev server is running
npm run dev

# Check vite.config.ts
export default defineConfig({
  server: {
    hmr: true
  }
});
```

### Issue: Port Already in Use

**Symptoms**: "Address already in use" error

**Solution**:
```bash
# Find process using port
lsof -i :5005  # macOS/Linux
netstat -ano | findstr :5005  # Windows

# Kill process
kill -9 <PID>  # macOS/Linux
taskkill /F /PID <PID>  # Windows

# Or use different port
dotnet run --urls "http://localhost:5006"
```

### Issue: Environment Variables Not Loading

**Symptoms**: Configuration values not being read

**Solution**:
```bash
# Check .env file exists
ls -la .env

# Verify format (no spaces around =)
# ✅ Good
API_KEY=abc123

# ❌ Bad
API_KEY = abc123

# Check loading in code
var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json")
    .Build();
```

### Issue: Docker Build Fails

**Symptoms**: Docker image build errors

**Solution**:
```bash
# Clear Docker cache
docker system prune -a

# Rebuild without cache
docker-compose build --no-cache

# Check Dockerfile syntax
docker build --no-cache -t test -f Dockerfile .

# View build logs
docker-compose build --progress=plain
```

## Debugging Issues

### Issue: Debugger Won't Attach

**Symptoms**: Breakpoints not hitting

**Solution**:

**VS Code**:
```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/Aura.Api/bin/Debug/net8.0/Aura.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/Aura.Api",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

**Visual Studio**:
1. Right-click project → Properties
2. Debug → Enable "Launch browser"
3. Ensure Debug configuration (not Release)

### Issue: Source Maps Not Working

**Symptoms**: Can't debug TypeScript in browser

**Solution**:
```typescript
// vite.config.ts
export default defineConfig({
  build: {
    sourcemap: true
  }
});

// Check browser dev tools
// Settings → Enable source maps
```

### Issue: Debugging Async Code

**Symptoms**: Stepping through async code jumps around

**Solution**:
```csharp
// Disable "Just My Code" in Visual Studio
// Tools → Options → Debugging → General → Uncheck "Enable Just My Code"

// Use conditional breakpoints
// Right-click breakpoint → Conditions → e.g., "userId == 123"

// Use data breakpoints
// Right-click variable → Break when value changes
```

## Git and Version Control

### Issue: Merge Conflicts

**Symptoms**: Git merge fails with conflicts

**Solution**:
```bash
# View conflicts
git status
git diff

# Use merge tool
git mergetool

# Or resolve manually
# Edit files, then:
git add .
git commit

# Abort merge if needed
git merge --abort
```

### Issue: Accidentally Committed Secrets

**Symptoms**: API keys in git history

**Solution**:
```bash
# Remove from current commit
git reset HEAD~1
git add .
git commit

# Remove from history (DANGER: rewrites history)
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch path/to/secret/file" \
  --prune-empty --tag-name-filter cat -- --all

# Rotate compromised credentials immediately!
```

### Issue: Git Hooks Failing

**Symptoms**: Pre-commit hook blocks commit

**Solution**:
```bash
# View hook output
cat .husky/pre-commit

# Fix issues or bypass (not recommended)
git commit --no-verify -m "message"

# Debug hook
bash -x .husky/pre-commit
```

## IDE Issues

### Issue: VS Code IntelliSense Not Working

**Symptoms**: No autocomplete or suggestions

**Solution**:
```bash
# Restart OmniSharp
# Command Palette → "OmniSharp: Restart OmniSharp"

# Check .NET SDK
dotnet --version

# Reinstall C# extension
# Extensions → C# → Uninstall → Install

# Check workspace settings
# .vscode/settings.json
{
  "omnisharp.path": "latest"
}
```

### Issue: Rider Performance Issues

**Symptoms**: IDE slow or unresponsive

**Solution**:
1. Increase memory:
   - Help → Edit Custom VM Options
   - `-Xmx4096m` (4GB)
2. Invalidate caches:
   - File → Invalidate Caches / Restart
3. Exclude directories:
   - Settings → Directories → Exclude
   - Add: node_modules, bin, obj, .vs

### Issue: Visual Studio Debug Symbols Not Loading

**Symptoms**: "No symbols loaded" warning

**Solution**:
```bash
# Clean bin/obj folders
dotnet clean

# Rebuild
dotnet build --configuration Debug

# Check .pdb files exist
ls -la Aura.Api/bin/Debug/net8.0/*.pdb

# Enable symbol loading
# Tools → Options → Debugging → Symbols
# Check "Microsoft Symbol Servers"
```

## Database Migrations

### Issue: Migration Already Applied

**Symptoms**: "Migration '...' already applied"

**Solution**:
```bash
# List migrations
dotnet ef migrations list

# Remove last migration (if not pushed)
dotnet ef migrations remove

# Or create new migration with different name
dotnet ef migrations add AddUserProfile_v2
```

### Issue: Migration Rollback Failed

**Symptoms**: Can't revert migration

**Solution**:
```bash
# Restore database from backup
cp backup/aura-backup.db aura.db

# Or manually revert
dotnet ef database update PreviousMigrationName

# Force reset (CAUTION: data loss)
dotnet ef database drop
dotnet ef database update
```

### Issue: Migration Generated Incorrectly

**Symptoms**: Migration script has errors

**Solution**:
```bash
# Remove bad migration
dotnet ef migrations remove

# Check model state
dotnet ef dbcontext list

# Regenerate
dotnet ef migrations add FixedMigration

# Review SQL before applying
dotnet ef migrations script
```

## Performance Profiling

### Issue: Slow API Endpoints

**Diagnostic**:
```bash
# Use dotnet-trace
dotnet tool install --global dotnet-trace

# Record trace
dotnet-trace collect --process-id $(pgrep -f Aura.Api)

# Analyze with PerfView or SpeedScope
```

**Solution**:
1. Add caching:
   ```csharp
   [ResponseCache(Duration = 300)]
   public async Task<IActionResult> GetData()
   ```
2. Use async properly:
   ```csharp
   // ❌ Bad
   var result = Task.Run(() => SlowOperation()).Result;
   
   // ✅ Good
   var result = await SlowOperation();
   ```
3. Profile with Application Insights or custom timing:
   ```csharp
   using var timer = new MetricTimer("operation-name");
   ```

### Issue: Memory Leaks

**Diagnostic**:
```bash
# Use dotnet-counters
dotnet tool install --global dotnet-counters

# Monitor GC
dotnet-counters monitor --process-id $(pgrep -f Aura.Api) \
  System.Runtime gc-heap-size

# Collect memory dump
dotnet-dump collect --process-id $(pgrep -f Aura.Api)

# Analyze with dotnet-gcdump
dotnet tool install --global dotnet-gcdump
dotnet-gcdump analyze memory.dump
```

**Common Causes**:
1. Event handlers not unsubscribed
2. Static references to objects
3. Circular references
4. IDisposable not called

## Getting Help

### Internal Resources

- Team chat: #aura-dev channel
- Documentation: docs/ directory
- Architecture decisions: docs/adr/
- Code review guidelines: CONTRIBUTING.md

### External Resources

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [React Documentation](https://react.dev/)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)

### Reporting Issues

When reporting development issues:

1. Include exact error messages
2. Provide minimal reproduction steps
3. List your environment:
   ```bash
   dotnet --info
   node --version
   npm --version
   git --version
   ```
4. Attach relevant logs
5. Show what you've already tried

---

**Last Updated**: 2024-11-10  
**Maintained by**: Development Team
