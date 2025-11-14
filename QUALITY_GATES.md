# Quality Gates Reference

Quick reference for Aura Video Studio's warning-free build enforcement system.

## Current Status

🟡 **Monitoring Mode** - Gates configured, warnings reported but not enforced yet
🎯 **Target:** Full enforcement after cleanup PRs merge

## Pre-commit Checks

| Check | Status | Can Enable |
|-------|--------|------------|
| Placeholder Scan | ✅ Always Enforced | N/A |
| lint-staged | ✅ Always Enforced | N/A |
| TypeScript Check | 🔧 Optional | `export ENFORCE_TYPE_CHECK=1` |
| Zero Warnings | 🔧 Optional | `export ENFORCE_ZERO_WARNINGS=1` |

## CI Quality Gates

### Frontend

| Gate | Command | Status |
|------|---------|--------|
| ESLint | `npm run lint` | 🟡 Monitoring (347 warnings) |
| TypeScript | `npm run typecheck` | 🟡 Monitoring (errors found) |
| Stylelint | `npm run lint:css` | 🟡 Monitoring |
| Prettier | `npm run format:check` | 🟡 Monitoring |

### Backend

| Gate | Command | Status |
|------|---------|--------|
| Compiler Warnings | `dotnet build --configuration Release` | 🟡 Monitoring |
| Code Formatting | `dotnet format --verify-no-changes` | 🟡 Monitoring |
| Analyzers (CA rules) | Via build | 🟡 Monitoring |

### Documentation

| Gate | Command | Status |
|------|---------|--------|
| DocFX Build | `docfx build docfx.json --warningsAsErrors` | 🟡 Monitoring |
| Broken Links | `markdown-link-check` | 🟡 Monitoring |
| Markdown Lint | `markdownlint '**/*.md'` | 🟡 Monitoring |

### Scripts

| Gate | Command | Status |
|------|---------|--------|
| Shell Scripts | `shellcheck scripts/**/*.sh` | 🟡 Monitoring |
| PowerShell | `Invoke-ScriptAnalyzer` | 🟡 Monitoring |

## Quick Fixes

### Frontend
```bash
cd Aura.Web

# Auto-fix linting
npm run lint:fix

# Auto-fix formatting
npm run format

# See all issues
npm run quality-check
```

### Backend
```bash
# Auto-fix formatting
dotnet format Aura.sln

# See warnings
dotnet build --configuration Release --verbosity detailed
```

### Documentation
```bash
# Build and see warnings
docfx build docfx.json --warningsAsErrors

# Check links
npx markdown-link-check docs/**/*.md
```

## Enabling Strict Mode Locally

Add to `~/.bashrc` or `~/.zshrc`:

```bash
# Enable strict pre-commit checks
export ENFORCE_TYPE_CHECK=1
export ENFORCE_ZERO_WARNINGS=1
```

Then reload: `source ~/.bashrc`

## Full Documentation

See [BUILD_GUIDE.md](BUILD_GUIDE.md) section "Quality Gates and CI Enforcement" for complete details.

## Phased Rollout

1. ✅ **Phase 1:** Gate configuration (this PR)
2. 🎯 **Phase 2:** Warning cleanup (upcoming PRs)
3. 🎯 **Phase 3:** Enforcement activation (after cleanup)

## Contact

Questions about quality gates? See [CONTRIBUTING.md](CONTRIBUTING.md) or [BUILD_GUIDE.md](BUILD_GUIDE.md).
