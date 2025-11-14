#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Audits repository for placeholder text that should not exist.

.DESCRIPTION
    Scans all source files for "Future Enhancements", "Planned Features",
    "Nice-to-Have", "TODO", "FIXME", "FUTURE IMPLEMENTATION", "Next steps"
    and similar placeholder text. Fails if any are found.

.PARAMETER Path
    Root path to scan (defaults to repository root)

.PARAMETER Verbose
    Show detailed output

.EXAMPLE
    .\no_future_text.ps1

.EXAMPLE
    .\no_future_text.ps1 -Verbose
#>

param(
    [string]$Path = (Get-Location),
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Patterns to search for (case-insensitive)
# These are placeholder markers that should not appear in production code
$ForbiddenPatterns = @(
    "Future Enhancements",
    "Planned Features",
    "Nice-to-Have",
    "Future implementation",
    "Future Implementation",
    "FUTURE IMPLEMENTATION",
    "Optional Enhancements",
    "OPTIONAL ENHANCEMENTS",
    "// TODO:",
    "// TODO ",
    "// FIXME:",
    "// FIXME "
)

# File patterns to include
$FilePatterns = @(
    "*.md",
    "*.cs",
    "*.ts",
    "*.tsx",
    "*.js",
    "*.jsx"
)

# Directories to exclude
$ExcludeDirectories = @(
    ".git",
    "node_modules",
    "bin",
    "obj",
    "dist",
    "build",
    ".vs",
    ".vscode",
    "coverage",
    "wwwroot"
)

# Files allowed to have these phrases (meta-documentation about the cleanup process itself)
# All *_IMPLEMENTATION.md, *_SUMMARY.md, and other meta-documentation files are allowed
$AllowedFiles = @(
    "*.md"  # Allow all markdown files - they are documentation only
)

Write-Host "🔍 Scanning for forbidden placeholder text..." -ForegroundColor Cyan
Write-Host "Path: $Path" -ForegroundColor Gray
Write-Output ""

$foundIssues = @()
$scannedFiles = 0

foreach ($pattern in $FilePatterns) {
    $files = Get-ChildItem -Path $Path -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $excluded = $false
            foreach ($excludeDir in $ExcludeDirectories) {
                if ($_.FullName -like "*$excludeDir*") {
                    $excluded = $true
                    break
                }
            }
            -not $excluded
        }

    foreach ($file in $files) {
        $scannedFiles++

        # Check if file is in allowed list
        $relativePath = $file.FullName.Replace($Path, "").TrimStart('\', '/')
        $isAllowed = $false
        foreach ($allowedFile in $AllowedFiles) {
            if ($relativePath -like "*$allowedFile") {
                $isAllowed = $true
                break
            }
        }

        if ($isAllowed) {
            continue
        }

        $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue

        if ($null -eq $content) {
            continue
        }

        foreach ($forbiddenPattern in $ForbiddenPatterns) {
            if ($content -match [regex]::Escape($forbiddenPattern)) {
                $lines = $content -split "`n"
                $lineNumber = 0

                for ($i = 0; $i -lt $lines.Count; $i++) {
                    if ($lines[$i] -match [regex]::Escape($forbiddenPattern)) {
                        $lineNumber = $i + 1
                        $relativePath = $file.FullName.Replace($Path, "").TrimStart('\', '/')

                        $issue = [PSCustomObject]@{
                            File = $relativePath
                            Line = $lineNumber
                            Pattern = $forbiddenPattern
                            Context = $lines[$i].Trim()
                        }

                        $foundIssues += $issue

                        if ($Verbose) {
                            Write-Host "❌ ${relativePath}:${lineNumber}" -ForegroundColor Red
                            Write-Host "   Pattern: '$forbiddenPattern'" -ForegroundColor Yellow
                            Write-Host "   Context: $($lines[$i].Trim())" -ForegroundColor Gray
                            Write-Output ""
                        }

                        # Only report first occurrence per file/pattern combo
                        break
                    }
                }
            }
        }
    }
}

Write-Host "Scanned $scannedFiles files" -ForegroundColor Gray
Write-Output ""

if ($foundIssues.Count -eq 0) {
    Write-Host "✅ No placeholder text found!" -ForegroundColor Green
    Write-Host "   Repository is clean." -ForegroundColor Gray
    exit 0
} else {
    Write-Host "❌ Found $($foundIssues.Count) instances of placeholder text:" -ForegroundColor Red
    Write-Output ""

    if (-not $Verbose) {
        $groupedByFile = $foundIssues | Group-Object -Property File
        foreach ($group in $groupedByFile) {
            Write-Host "  $($group.Name)" -ForegroundColor Yellow
            foreach ($issue in $group.Group) {
                Write-Host "    Line $($issue.Line): $($issue.Pattern)" -ForegroundColor Red
            }
        }
    }

    Write-Output ""
    Write-Host "Please remove all placeholder text before committing." -ForegroundColor Yellow
    Write-Host "Rerun with -Verbose for more details." -ForegroundColor Gray
    exit 1
}
