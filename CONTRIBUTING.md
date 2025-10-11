# Contributing to Aura Video Studio

Thank you for your interest in contributing to Aura Video Studio! This guide outlines the development workflow and standards for the project.

## Development Standards

### No Placeholder Policy

This project maintains a **zero-tolerance policy for placeholder text** in the codebase and documentation. All features described in the repository must be fully implemented and tested.

#### Forbidden Phrases

The following phrases are not allowed in code or documentation (except in meta-documentation about the cleanup process itself):

- `TODO`
- `FIXME`
- `Future Enhancements`
- `Planned Features`
- `Nice-to-Have`
- `Future implementation` / `FUTURE IMPLEMENTATION`
- `Next steps` / `Next Steps` / `NEXT STEPS` (except in user-facing instructional guides)
- `Optional Enhancements` / `OPTIONAL ENHANCEMENTS`

#### Audit Script

The repository includes an audit script that enforces this policy:

```powershell
pwsh scripts/audit/no_future_text.ps1
```

**Features:**
- Scans all source files (`.md`, `.cs`, `.ts`, `.tsx`, `.js`, `.jsx`)
- Detects forbidden placeholder patterns
- Smart allowlist for legitimate uses (meta-docs, user guides)
- CI-friendly exit codes (0 = pass, 1 = fail)
- Detailed reporting with verbose mode

**Usage:**

```powershell
# Run the audit
pwsh scripts/audit/no_future_text.ps1

# Run with verbose output
pwsh scripts/audit/no_future_text.ps1 -Verbose
```

#### Allowed Exceptions

The following types of files are allowed to contain these phrases:
- Meta-documentation about the cleanup process itself
- User-facing instructional guides with "Next Steps" for users
- CI documentation explaining workflow patterns

See the `$AllowedFiles` array in `scripts/audit/no_future_text.ps1` for the complete list.

### CI Integration

The audit script runs automatically in CI pipelines:
- **Windows CI** (`.github/workflows/ci-windows.yml`)
- **Linux CI** (`.github/workflows/ci-linux.yml`)
- **No Placeholders Workflow** (`.github/workflows/no-placeholders.yml`)

Pull requests that introduce forbidden placeholder text will fail the CI checks.

### What to Do Instead

Instead of adding placeholder text:

1. **For unimplemented features:** Don't mention them in documentation until they're implemented
2. **For work in progress:** Use feature branches and don't merge until complete
3. **For improvements:** Document only what currently exists; file GitHub issues for future work
4. **For user instructions:** Use "To continue:" or "To complete:" instead of "Next steps:"

## Building and Testing

See [BUILD_AND_RUN.md](BUILD_AND_RUN.md) for detailed build and test instructions.

### Quick Start

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Run audit scripts
pwsh scripts/audit/no_future_text.ps1
pwsh scripts/audit/scan.ps1
```

## Code Quality

- **Line Coverage:** Maintain at least 60% line coverage for .NET code
- **Web Coverage:** Maintain at least 70% coverage for tested files
- **Build Warnings:** Fix new warnings introduced by your changes
- **Analyzer Rules:** Follow the analyzer rules defined in `Directory.Build.props`

## Pull Request Guidelines

1. **Run audits locally** before submitting PR
2. **Ensure tests pass** on all platforms
3. **Update documentation** if changing user-facing features
4. **Keep changes focused** - one feature/fix per PR
5. **No placeholder text** - implement features completely or don't include them

## Getting Help

- Review existing documentation in the `docs/` directory
- Check implementation summaries (e.g., `STABILIZATION_SWEEP_SUMMARY.md`)
- Examine existing code patterns for guidance
- File issues for questions or discussions

---

**Remember:** If it's not implemented, don't document it. This keeps the project credible and maintainable.
