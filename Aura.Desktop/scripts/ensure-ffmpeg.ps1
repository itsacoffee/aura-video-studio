param(
    [switch]$Force
)

function Write-Step {
    param([string]$Message)
    Write-Host "[ffmpeg] $Message"
}

try {
    if ($PSVersionTable.PSVersion.Major -lt 5) {
        throw "PowerShell 5.0 or newer is required to download the bundled FFmpeg."
    }

    if ($IsWindows -ne $true) {
        Write-Step "Skipping bundled FFmpeg download (Windows-only requirement)."
        exit 0
    }

    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $projectRoot = Split-Path -Parent $scriptDir
    $resourcesDir = Join-Path $projectRoot "resources"
    $cacheDir = Join-Path $resourcesDir "cache"
    $ffmpegTargetDir = Join-Path $resourcesDir "ffmpeg\win-x64\bin"

    if (-not (Test-Path $ffmpegTargetDir)) {
        New-Item -ItemType Directory -Path $ffmpegTargetDir -Force | Out-Null
    }

    $ffmpegExePath = Join-Path $ffmpegTargetDir "ffmpeg.exe"
    if ((Test-Path $ffmpegExePath) -and -not $Force) {
        Write-Step "Bundled FFmpeg already present at $ffmpegExePath"
        exit 0
    }

    if (-not (Test-Path $cacheDir)) {
        New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
    }

    $downloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
    $tempDir = Join-Path $cacheDir "ffmpeg-download"
    $zipPath = Join-Path $tempDir "ffmpeg.zip"

    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    Write-Step "Downloading FFmpeg from $downloadUrl ..."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath -UseBasicParsing

    Write-Step "Extracting FFmpeg archive..."
    Expand-Archive -LiteralPath $zipPath -DestinationPath $tempDir -Force

    $extractedFolder = Get-ChildItem -Path $tempDir -Directory | Where-Object {
        $_.Name -like "ffmpeg*"
    } | Select-Object -First 1

    if (-not $extractedFolder) {
        throw "Unable to locate extracted FFmpeg folder."
    }

    $sourceBin = Join-Path $extractedFolder.FullName "bin"
    if (-not (Test-Path $sourceBin)) {
        throw "FFmpeg bin folder not found in the extracted archive."
    }

    Write-Step "Copying FFmpeg binaries to $ffmpegTargetDir ..."
    Copy-Item -Path (Join-Path $sourceBin "*") -Destination $ffmpegTargetDir -Recurse -Force

    Write-Step "Bundled FFmpeg is ready."
    exit 0
}
catch {
    Write-Host "[ffmpeg] ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    if ($tempDir -and (Test-Path $tempDir)) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

