<#
Quick smoke tests for Localization and Ideation endpoints against the local API.

Prereqs:
- Backend running on http://127.0.0.1:5005
- Ollama running if using local models (e.g., qwen3:4b, llama3.1)

Usage:
  pwsh Scripts/test-llm-smoke.ps1

Notes:
- Each call times out after 3 minutes to allow model warm-up.
- Exits with non-zero on first failure.
#>

$ErrorActionPreference = 'Stop'

$BaseUrl = 'http://127.0.0.1:5005'
$TimeoutSec = 180

function Invoke-Json($method, $url, $body = $null) {
  $params = @{
    Method      = $method
    Uri         = $url
    ContentType = 'application/json'
    TimeoutSec  = $TimeoutSec
  }
  if ($body) {
    $params.Body = ($body | ConvertTo-Json -Depth 10)
  }
  return Invoke-RestMethod @params
}

function Assert-True($condition, $message) {
  if (-not $condition) {
    Write-Error $message
  }
}

Write-Host "=== Localization health ==="
$health = Invoke-Json 'GET' "$BaseUrl/api/localization/health"
$health | ConvertTo-Json -Depth 5 | Write-Host
Assert-True ($health.isAvailable -eq $true) "Localization provider unavailable"
Assert-True ($health.supportsTranslation -eq $true) "Provider does not support translation"

Write-Host "`n=== Localization simple translate ==="
$translateBody = @{
  sourceText     = "Hello from Aura!"
  sourceLanguage = "en"
  targetLanguage = "es"
  provider       = ""
  modelId        = ""
}
$translation = Invoke-Json 'POST' "$BaseUrl/api/localization/translate/simple" $translateBody
$translation | ConvertTo-Json -Depth 5 | Write-Host
Assert-True ($translation.translatedText -and $translation.translatedText.Length -gt 0) "Translation empty"

Write-Host "`n=== Ideation brainstorm ==="
$brainstormBody = @{
  topic        = "Create a 60s TikTok about sustainable travel"
  platform     = "tiktok"
  conceptCount = 3
}
$brainstorm = Invoke-Json 'POST' "$BaseUrl/api/ideation/brainstorm" $brainstormBody
$brainstorm | ConvertTo-Json -Depth 5 | Write-Host
Assert-True ($brainstorm.success -eq $true) "Brainstorm success flag false"
Assert-True ($brainstorm.concepts.Count -ge 1) "No concepts returned"

Write-Host "`nAll smoke tests passed."
