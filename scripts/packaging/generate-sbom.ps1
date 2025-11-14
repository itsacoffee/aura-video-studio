# Generate Software Bill of Materials (SBOM) and License Attributions

param(
    [string]$OutputDir = "artifacts/windows"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Generating SBOM and Attributions ===" -ForegroundColor Cyan

$rootDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$outputDir = Join-Path $rootDir $OutputDir
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

# Read version from version.json
$versionFile = Join-Path $rootDir "version.json"
$appVersion = "1.0.0"
if (Test-Path $versionFile) {
    $versionData = Get-Content $versionFile | ConvertFrom-Json
    $appVersion = $versionData.version
    Write-Host "Using version from version.json: $appVersion" -ForegroundColor Green
} else {
    Write-Host "Warning: version.json not found, using default version: $appVersion" -ForegroundColor Yellow
}

# Generate basic SBOM in CycloneDX format
$sbom = @{
    bomFormat = "CycloneDX"
    specVersion = "1.4"
    version = 1
    metadata = @{
        timestamp = (Get-Date -Format "o")
        component = @{
            type = "application"
            name = "Aura Video Studio"
            version = $appVersion
            description = "AI-powered video creation tool for Windows 11"
        }
    }
    components = @(
        @{
            type = "library"
            name = ".NET Runtime"
            version = "8.0"
            licenses = @(@{ license = @{ id = "MIT" } })
        },
        @{
            type = "library"
            name = "FFmpeg"
            version = "6.0"
            licenses = @(@{ license = @{ id = "LGPL-2.1" } })
        },
        @{
            type = "library"
            name = "Windows App SDK"
            version = "1.5"
            licenses = @(@{ license = @{ id = "MIT" } })
        },
        @{
            type = "library"
            name = "React"
            version = "18.2"
            licenses = @(@{ license = @{ id = "MIT" } })
        },
        @{
            type = "library"
            name = "Fluent UI React"
            version = "9.47"
            licenses = @(@{ license = @{ id = "MIT" } })
        }
    )
}

$sbomPath = Join-Path $outputDir "sbom.json"
$sbom | ConvertTo-Json -Depth 10 | Out-File $sbomPath -Encoding utf8
Write-Host "✓ SBOM generated: $sbomPath" -ForegroundColor Green

# Generate attributions file
$attributions = @"
AURA VIDEO STUDIO - Third-Party Software Attributions
======================================================

This software includes or depends on the following third-party components:

1. .NET Runtime
   Version: 8.0
   License: MIT License
   Copyright (c) .NET Foundation and Contributors
   https://github.com/dotnet/runtime

2. FFmpeg
   Version: 6.0+
   License: LGPL 2.1 or later (depending on build configuration)
   Copyright (c) FFmpeg team
   https://ffmpeg.org/

   Note: This software uses code of <a href=http://ffmpeg.org>FFmpeg</a>
   licensed under the <a href=http://www.gnu.org/licenses/old-licenses/lgpl-2.1.html>
   LGPLv2.1</a> and its source can be downloaded from the FFmpeg website.

3. Windows App SDK
   Version: 1.5
   License: MIT License
   Copyright (c) Microsoft Corporation
   https://github.com/microsoft/WindowsAppSDK

4. React
   Version: 18.2
   License: MIT License
   Copyright (c) Facebook, Inc. and its affiliates
   https://reactjs.org/

5. Fluent UI React (@fluentui/react-components)
   Version: 9.47
   License: MIT License
   Copyright (c) Microsoft Corporation
   https://github.com/microsoft/fluentui

6. ASP.NET Core
   Version: 8.0
   License: MIT License
   Copyright (c) .NET Foundation and Contributors
   https://github.com/dotnet/aspnetcore

7. Serilog
   Version: 3.x
   License: Apache License 2.0
   Copyright (c) Serilog Contributors
   https://serilog.net/

8. NAudio
   Version: 2.x
   License: MIT License
   Copyright (c) Mark Heath
   https://github.com/naudio/NAudio

9. SkiaSharp
   Version: 2.x
   License: MIT License
   Copyright (c) Microsoft Corporation
   https://github.com/mono/SkiaSharp

For complete license texts, see the respective project repositories.

Additional licenses for bundled assets:

- Default music pack: CC0 1.0 Universal (Public Domain)
- Stock placeholder images: CC0 1.0 Universal (Public Domain)

Contact: https://github.com/Coffee285/aura-video-studio/issues
"@

$attributionsPath = Join-Path $outputDir "attributions.txt"
Set-Content -Path $attributionsPath -Value $attributions -Encoding utf8
Write-Host "✓ Attributions generated: $attributionsPath" -ForegroundColor Green

Write-Output ""
Write-Host "=== SBOM Generation Complete ===" -ForegroundColor Cyan
