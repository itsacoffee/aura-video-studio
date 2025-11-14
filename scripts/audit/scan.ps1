<#
scripts/audit/scan.ps1
Comprehensive repo audit for merge integrity and basic quality gates.
#>

param(
  [string]$RepoRoot = ".",
  [string[]]$JsonPatterns = @("**/appsettings.json","**/appsettings.*.json")
)

$ErrorActionPreference = "Stop"
Set-Location $RepoRoot

$artifacts = "artifacts/audit"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$reportPath = Join-Path $artifacts "merge_audit_report.md"
$effectivePath = Join-Path $artifacts "effective_appsettings.json"

function Add-Report([string]$line) { Add-Content -LiteralPath $reportPath -Value $line }
function Get-TextFiles {
  Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object {
      $_.Extension -notin @(".png",".jpg",".jpeg",".gif",".bmp",".ico",
                            ".mp4",".mov",".mkv",".webm",".mp3",".wav",
                            ".ttf",".otf",".woff",".woff2",".dll",".exe") -and
      $_.Name -notin @("package-lock.json","yarn.lock")
    }
}

"## Merge Audit Report" | Out-File -FilePath $reportPath -Encoding ascii
Add-Report ("Generated: {0}" -f (Get-Date).ToString("s"))
Add-Report ""

$hardFail = $false

Add-Report "### Conflict Markers"
$conflicts = Get-TextFiles | Where-Object { $_.FullName -notlike "*\scripts\audit\*" } | Select-String -Pattern '^(<<<<<<<|=======|>>>>>>>)' -SimpleMatch
if ($conflicts) { $hardFail = $true; Add-Report "**FOUND conflict markers:**"; $conflicts | ForEach-Object { Add-Report ("- {0}:{1}" -f $_.Path, $_.LineNumber) } } else { Add-Report "No conflict markers found." }
Add-Report ""

Add-Report "### Duplicate Files (by normalized name)"
$files = Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '[\\/](bin|obj|node_modules)[\\/]' }
$norm = @{}; foreach ($f in $files) { $key = (([string]$f.BaseName).ToLowerInvariant()); if (-not $norm.ContainsKey($key)) { $norm[$key] = @() }; $norm[$key] += $f.FullName }
$dupes = $norm.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 }
if ($dupes) { $hardFail = $true; Add-Report "**FOUND potential duplicate file basenames:**"; foreach ($d in $dupes) { Add-Report ("- {0}" -f $d.Key); $d.Value | ForEach-Object { Add-Report ("  - {0}" -f $_) } } } else { Add-Report "No duplicate basenames detected." }
Add-Report ""

Add-Report "### Duplicate C# Type Names"
$csFiles = Get-ChildItem -Recurse -Include *.cs -File -ErrorAction SilentlyContinue
$typeNames = @{}; $csRegex = '^\s*(public|internal|protected|private)?\s*(sealed\s+|abstract\s+)?(class|interface|record|enum)\s+([A-Za-z_][A-Za-z0-9_]*)'
foreach ($f in $csFiles) { try { $i=0; Get-Content -LiteralPath $f.FullName -Encoding UTF8 | ForEach-Object { $i++; if ($_ -match $csRegex) { $name = $Matches[4]; if (-not $typeNames.ContainsKey($name)) { $typeNames[$name] = @() }; $typeNames[$name] += ("{0}:{1}" -f $f.FullName,$i) } } } catch { Write-Verbose "Error reading $($f.FullName): $_" } }
$dupTypes = $typeNames.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 }
if ($dupTypes) { $hardFail = $true; Add-Report "**FOUND duplicate C# types:**"; foreach ($d in $dupTypes) { Add-Report ("- {0}" -f $d.Key); $d.Value | ForEach-Object { Add-Report ("  - {0}" -f $_) } } } else { Add-Report "No duplicate C# type names detected." }
Add-Report ""

Add-Report "### Duplicate TS/TSX Default Export Names"
$tsFiles = Get-ChildItem -Recurse -Include *.ts,*.tsx -File -ErrorAction SilentlyContinue
$tsNames = @{}; $tsRegex1 = 'export\s+default\s+function\s+([A-Za-z_][A-Za-z0-9_]*)'; $tsRegex2 = 'export\s+default\s+class\s+([A-Za-z_][A-Za-z0-9_]*)'
foreach ($f in $tsFiles) { try { $name = $null; $text = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8; if ($text -match $tsRegex1) { $name = $Matches[1] } elseif ($text -match $tsRegex2) { $name = $Matches[1] }; if ($name) { if (-not $tsNames.ContainsKey($name)) { $tsNames[$name] = @() }; $tsNames[$name] += $f.FullName } } catch { Write-Verbose "Error reading $($f.FullName): $_" } }
$dupTs = $tsNames.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 }
if ($dupTs) { $hardFail = $true; Add-Report "**FOUND duplicate TS default export names:**"; foreach ($d in $dupTs) { Add-Report ("- {0}" -f $d.Key); $d.Value | ForEach-Object { Add-Report ("  - {0}" -f $_) } } } else { Add-Report "No duplicate TS default export names detected." }
Add-Report ""

Add-Report "### Duplicate XAML Resource Keys"
$xamlFiles = Get-ChildItem -Recurse -Include *.xaml -File -ErrorAction SilentlyContinue
$xamlKeys = @{}; $xamlRegex = 'x:Key\s*=\s*"([^"]+)"'
foreach ($f in $xamlFiles) { try { $text = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8; $matches = [System.Text.RegularExpressions.Regex]::Matches($text, $xamlRegex); foreach ($m in $matches) { $k = $m.Groups[1].Value; if (-not $xamlKeys.ContainsKey($k)) { $xamlKeys[$k] = @() }; $xamlKeys[$k] += $f.FullName } } catch { Write-Verbose "Error reading $($f.FullName): $_" } }
$dupKeys = $xamlKeys.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 }
if ($dupKeys) { $hardFail = $true; Add-Report "**FOUND duplicate XAML resource keys:**"; foreach ($d in $dupKeys) { Add-Report ("- {0}" -f $d.Key); $d.Value | ForEach-Object { Add-Report ("  - {0}" -f $_) } } } else { Add-Report "No duplicate XAML resource keys detected." }
Add-Report ""

Add-Report "### TODO/FIXME/HACK markers"
$marks = Get-TextFiles | Select-String -Pattern 'TODO|FIXME|HACK' -SimpleMatch
if ($marks) { Add-Report "Found markers:"; $marks | ForEach-Object { Add-Report ("- {0}:{1}: {2}" -f $_.Path, $_.LineNumber, ($_.Line).Trim()) } } else { Add-Report "No markers found." }
Add-Report ""

Add-Report "### JSON validation and effective appsettings"
$allJson = @(); foreach ($pat in $JsonPatterns) { $allJson += (Get-ChildItem -Recurse -File -Filter (Split-Path $pat -Leaf) -ErrorAction SilentlyContinue | Where-Object { $_.FullName -like (Join-Path (Get-Location) (Split-Path $pat -Parent) + "*") }) }
$seen = New-Object System.Collections.Generic.HashSet[string]; $validJson = @()
foreach ($j in $allJson) { try { $txt = Get-Content -LiteralPath $j.FullName -Raw -Encoding UTF8; $obj = $txt | ConvertFrom-Json -ErrorAction Stop; if ($seen.Add($j.FullName)) { $validJson += [pscustomobject]@{Path=$j.FullName; Json=$obj} } } catch { $hardFail = $true; Add-Report ("**INVALID JSON:** {0}" -f $j.FullName) } }
$effective = @{}; foreach ($entry in ($validJson | Sort-Object Path)) { foreach ($prop in $entry.Json.PSObject.Properties) { $effective[$prop.Name] = $prop.Value } }
($effective | ConvertTo-Json -Depth 10) | Out-File -FilePath $effectivePath -Encoding ascii
Add-Report ("Wrote effective appsettings: {0}" -f $effectivePath)
Add-Report ""

Add-Report "### DI registration summary (best-effort)"
$ifaces = @("ILlmProvider","ITtsProvider","IImageProvider","IStockProvider","IVideoComposer")
$implMap = @{}; foreach ($iface in $ifaces) { $implMap[$iface] = @() }
foreach ($f in $csFiles) { $text = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8; foreach ($iface in $ifaces) { $rx = [regex]("class\s+([A-Za-z_][A-Za-z0-9_]*)\s*:\s*$iface"); $m = $rx.Matches($text); foreach ($mm in $m) { $impl = $mm.Groups[1].Value; $implMap[$iface] += ("{0}  ({1})" -f $impl, $f.FullName) } } }
foreach ($k in $implMap.Keys) { Add-Report ("- {0} implementations:" -f $k); if ($implMap[$k].Count -eq 0) { Add-Report "  (none found)" } else { $implMap[$k] | ForEach-Object { Add-Report ("  - {0}" -f $_) } } }
Add-Report ""

if ($hardFail) { Add-Report "**RESULT: FAIL**"; Write-Host "Merge audit found problems. See $reportPath" -ForegroundColor Red; exit 1 } else { Add-Report "**RESULT: PASS**"; Write-Host "Merge audit passed. See $reportPath" -ForegroundColor Green; exit 0 }
