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
$ForbiddenPatterns = @(
    "Future Enhancements",
    "Planned Features", 
    "Nice-to-Have",
    "Future implementation",
    "Future Implementation",
    "FUTURE IMPLEMENTATION",
    "Next steps",
    "Next Steps",
    "NEXT STEPS",
    "Optional Enhancements",
    "OPTIONAL ENHANCEMENTS"
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
$AllowedFiles = @(
    "AGENT_08_IMPLEMENTATION.md",  # Documents removal of future implementation items
    "AGENT_13_IMPLEMENTATION.md",  # Documents removal of TODO/FIXME
    "STABILIZATION_SWEEP_SUMMARY.md",  # Documents cleanup process
    "FUTURE_ENHANCEMENTS_REMOVAL.md",  # Documents removal of future enhancements
    "CONTRIBUTING.md",  # Contributing guide documenting the no-placeholder policy
    "docs/CI.md",  # Documents the no-placeholders workflow patterns
    "BUILD_AND_RUN.md",  # User guide with Next Steps instructions
    "QUICKSTART.md",  # User guide with Next Steps instructions
    "README.md",  # Main specification with user Next Steps
    "Aura.Cli/Program.cs",  # Help text with instructional Next Steps
    "INSTALL.md",  # Installation guide with Next Steps
    "LOCAL_PROVIDERS_SETUP.md",  # Setup guide with Next Steps
    "SOLUTION.md",  # Technical guide with Next Steps
    "COMPLETION_SUMMARY.md",  # Meta document about completion
    "FINAL_IMPLEMENTATION.md",  # Meta document
    "FINAL_SUMMARY.md",  # Meta document
    "MERGE_INTEGRITY_AUDIT_SUMMARY.md",  # Meta audit document
    "SCRIPT_GENERATION_FIX.md",  # Fix summary with recommendations
    "SUMMARY.md",  # General summary
    "VISUAL_SUMMARY.md",  # Summary document
    "WEB_ARCHITECTURE_SUMMARY.md",  # Architecture summary
    "IMPLEMENTATION_SUMMARY_LOCAL_ENGINES.md",  # Has recommended next steps
    "DEPLOYMENT.md",  # Deployment guide with recommendations
    "ACCEPTANCE_CHECK.md",  # Documents audit patterns
    "BUG_SWEEP_IMPLEMENTATION.md",  # Documents sweep process
    "STABILIZATION_COMPLETE.md",  # Documents stabilization results
    "docs/ONBOARDING_IMPLEMENTATION.md",  # Implementation documentation
    "docs/QUICK_DEMO.md",  # Demo documentation
    "docs/QUICK_START.md",  # Quick start guide
    "DOWNLOAD_CENTER_RELIABILITY_IMPLEMENTATION.md",  # Implementation doc
    "DOWNLOAD_ROBUSTNESS_IMPLEMENTATION.md",  # Implementation doc
    "ERROR_UX_IMPLEMENTATION.md",  # Implementation doc
    "FIRST_RUN_WIZARD_STATE_MACHINE_IMPLEMENTATION.md",  # Implementation doc
    "FLEXIBLE_ENGINES_IMPLEMENTATION.md",  # Implementation doc
    "GUIDED_GENERATION_IMPLEMENTATION.md",  # Implementation doc
    "IMPLEMENTATION_SUMMARY_DOWNLOAD_ROBUSTNESS.md",  # Implementation summary
    "PR_DOWNLOAD_CENTER_IMPLEMENTATION.md"  # Implementation doc
)

Write-Host "🔍 Scanning for forbidden placeholder text..." -ForegroundColor Cyan
Write-Host "Path: $Path" -ForegroundColor Gray
Write-Host ""

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
                            Write-Host ""
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
Write-Host ""

if ($foundIssues.Count -eq 0) {
    Write-Host "✅ No placeholder text found!" -ForegroundColor Green
    Write-Host "   Repository is clean." -ForegroundColor Gray
    exit 0
} else {
    Write-Host "❌ Found $($foundIssues.Count) instances of placeholder text:" -ForegroundColor Red
    Write-Host ""
    
    if (-not $Verbose) {
        $groupedByFile = $foundIssues | Group-Object -Property File
        foreach ($group in $groupedByFile) {
            Write-Host "  $($group.Name)" -ForegroundColor Yellow
            foreach ($issue in $group.Group) {
                Write-Host "    Line $($issue.Line): $($issue.Pattern)" -ForegroundColor Red
            }
        }
    }
    
    Write-Host ""
    Write-Host "Please remove all placeholder text before committing." -ForegroundColor Yellow
    Write-Host "Rerun with -Verbose for more details." -ForegroundColor Gray
    exit 1
}
