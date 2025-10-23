# Documentation Build Scripts

This directory contains scripts for building and validating the Aura Video Studio documentation.

## Scripts

### `build-docs.sh` (Linux/macOS)

Builds all documentation including:
- .NET API documentation (DocFX)
- TypeScript API documentation (TypeDoc)
- Validates markdown links

**Usage**:
```bash
./scripts/docs/build-docs.sh
```

**Requirements**:
- .NET 8.0 SDK
- Node.js and npm (for TypeScript docs)
- DocFX (auto-installed if missing)

### `build-docs.ps1` (Windows)

PowerShell version of the documentation build script.

**Usage**:
```powershell
# Build documentation
.\scripts\docs\build-docs.ps1

# Build and serve locally
.\scripts\docs\build-docs.ps1 -Serve

# Skip .NET build (use existing binaries)
.\scripts\docs\build-docs.ps1 -SkipBuild
```

**Parameters**:
- `-SkipBuild` - Skip .NET solution build (faster if already built)
- `-Serve` - Automatically start local documentation server after build

## Manual Build Steps

If you prefer to build components individually:

### 1. .NET API Documentation

```bash
# Install DocFX (first time only)
dotnet tool install -g docfx

# Build solution with XML docs
dotnet build --configuration Release

# Generate documentation
docfx docfx.json

# Serve locally
docfx serve _site
```

Open http://localhost:8080 in your browser.

### 2. TypeScript API Documentation

```bash
cd Aura.Web

# Install dependencies (first time only)
npm install

# Generate TypeScript API docs
npm run docs
```

Documentation is output to `docs/api/typescript/`.

### 3. Link Validation

```bash
# Install markdown-link-check (first time only)
npm install -g markdown-link-check

# Check all documentation links
find docs -name "*.md" -exec markdown-link-check {} \;
```

## Output

Documentation is generated to:
- **_site/** - DocFX output (complete documentation site)
- **docs/api/typescript/** - TypeDoc output (TypeScript API reference)

## Viewing Documentation

### Local Preview

```bash
# Serve with DocFX
docfx serve _site

# Or use any HTTP server
python -m http.server 8000 --directory _site
```

### GitHub Pages

Documentation is automatically deployed to GitHub Pages on every push to `main` branch via GitHub Actions.

## Troubleshooting

### "DocFX not found"

Install globally:
```bash
dotnet tool install -g docfx
```

Or update:
```bash
dotnet tool update -g docfx
```

### "npm not found"

Install Node.js from https://nodejs.org/

### XML Documentation Warnings

XML documentation warnings are suppressed in project files to avoid build noise while documentation is being added incrementally. Specifically:

- **CS1591**: "Missing XML comment for publicly visible type or member" - This warning is suppressed via `<NoWarn>1591</NoWarn>` in `.csproj` files
- This allows the build to succeed even when not all public APIs have documentation yet
- As documentation coverage improves, consider removing this suppression to enforce complete documentation

### DocFX Build Warnings

Some warnings during DocFX build are expected and can be ignored:
- Missing cross-references for external types
- Duplicate member IDs (when using multi-targeting)

## CI/CD

Documentation is automatically built and validated in CI via `.github/workflows/documentation.yml`.

The workflow:
1. Builds .NET solution with XML docs
2. Runs DocFX to generate documentation
3. Validates markdown links
4. Runs spell checker
5. Deploys to GitHub Pages (on `main` branch)

## Contributing

When adding new documentation:

1. **For C# code**: Add XML documentation comments
   ```csharp
   /// <summary>
   /// Brief description of the class or method.
   /// </summary>
   /// <param name="paramName">Description of parameter</param>
   /// <returns>Description of return value</returns>
   public void MyMethod(string paramName) { }
   ```

2. **For TypeScript code**: Add JSDoc comments
   ```typescript
   /**
    * Brief description of the function.
    * @param paramName - Description of parameter
    * @returns Description of return value
    */
   export function myFunction(paramName: string): void { }
   ```

3. **For user guides**: Add markdown files to appropriate `docs/` subdirectory
   - `docs/getting-started/` - Installation and setup
   - `docs/features/` - Feature documentation
   - `docs/workflows/` - Workflow guides
   - `docs/troubleshooting/` - Troubleshooting
   - `docs/best-practices/` - Best practices

4. **Test your changes**: Run `build-docs.sh` or `build-docs.ps1` to verify

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for full guidelines.
