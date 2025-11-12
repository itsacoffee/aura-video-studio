# .NET Runtime Version Requirement

## Current Requirement: .NET 8.0 Runtime

The Aura Video Studio application **requires .NET 8.0 Runtime**, not .NET 9.0.

## Why the Confusion?

The project uses some NuGet packages with version numbers in the 9.0.x range, which may have caused confusion about the required .NET version. However, these packages are backward compatible with .NET 8.

### Package Versions Analysis

**Core Framework**:
- All `.csproj` files specify `<TargetFramework>net8.0</TargetFramework>`
- ASP.NET Core packages: 8.0.x versions
- Entity Framework Core: 8.0.11

**Microsoft.Extensions Packages** (9.0.x but compatible):
- `Microsoft.Extensions.Caching.StackExchangeRedis` 9.0.10
- `Microsoft.Extensions.Http.Resilience` 9.1.0
- `Microsoft.Extensions.Caching.Abstractions` 9.0.10
- `Microsoft.Extensions.Caching.Memory` 9.0.10
- `Microsoft.Extensions.Hosting.Abstractions` 9.0.0
- `Microsoft.Extensions.Logging.Abstractions` 9.0.10
- `Microsoft.Extensions.Http` 9.0.10
- `Microsoft.Extensions.ObjectPool` 9.0.10

**Other Updated Packages**:
- `Npgsql` 9.0.0
- `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.0

## Why These 9.0.x Packages Work with .NET 8

Microsoft follows a versioning strategy where:

1. **Core framework packages** (e.g., `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*`) are tightly coupled to the .NET version
2. **Extension packages** (e.g., `Microsoft.Extensions.*`) are forward-versioned and backward compatible

The `Microsoft.Extensions.*` packages in the 9.0.x series are designed to work with both .NET 8 and .NET 9. They provide new features for .NET 9 while maintaining compatibility with .NET 8.

## Installer Configuration

The installer script (`Aura.Desktop/build/installer.nsh`) correctly checks for and prompts to download **.NET 8 Runtime**:

```nsis
DetailPrint "Checking for .NET 8 Runtime..."
nsExec::ExecToStack 'powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $runtimes = dotnet --list-runtimes 2>&1; if ($runtimes -match \"Microsoft\\.NETCore\\.App 8\\.\") { exit 0 } else { exit 1 } } catch { exit 1 }"'
```

This is **correct** and should **not be changed** to .NET 9.

## Verification

To confirm the .NET version requirement:

1. **Check all .csproj files**:
   ```bash
   grep -r "TargetFramework" *.csproj
   ```
   Result: All specify `net8.0`

2. **Check published application**:
   After publishing, the `.deps.json` file will reference `.NETCoreApp,Version=v8.0`

3. **Runtime check**:
   The application will only run on machines with .NET 8.0 Runtime or later (9.0+ is backward compatible)

## When to Upgrade to .NET 9

If the team decides to upgrade to .NET 9:

1. Update all `<TargetFramework>net8.0</TargetFramework>` to `net9.0`
2. Update ASP.NET Core packages to 9.0.x versions
3. Update Entity Framework Core packages to 9.0.x versions
4. Update the installer script to check for .NET 9
5. Test thoroughly for breaking changes

## Conclusion

- ✅ **Installer is correct**: Requests .NET 8.0 Runtime
- ✅ **Application targets**: .NET 8.0
- ✅ **Some packages are 9.0.x**: But backward compatible with .NET 8.0
- ❌ **Do NOT upgrade installer to .NET 9**: Not needed and would be incorrect

The presence of 9.0.x package versions does not mean the application requires .NET 9 Runtime.
