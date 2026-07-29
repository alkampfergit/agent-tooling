#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build script for Agent Tools repository
.DESCRIPTION
    Builds all .NET CLI tools, copies binaries and dependencies to the tools/ directory
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$srcPath = Join-Path $PSScriptRoot 'src'
$toolsPath = Join-Path $PSScriptRoot 'tools'
$slnPath = Join-Path $srcPath 'Agent.Tools.sln'

Write-Host "Agent Tools Build Script" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan
Write-Host ""

# Verify solution file exists
if (-not (Test-Path $slnPath)) {
    Write-Host "ERROR: Solution file not found at $slnPath" -ForegroundColor Red
    exit 1
}

# Clean previous build artifacts if requested
if ($Clean) {
    Write-Host "Cleaning previous build artifacts..." -ForegroundColor Yellow

    if (Test-Path $toolsPath) {
        Remove-Item $toolsPath -Recurse -Force -ErrorAction SilentlyContinue
    }

    Push-Location $srcPath
    dotnet clean --configuration $Configuration -nologo --verbosity quiet
    Pop-Location

    Write-Host "Clean completed" -ForegroundColor Green
    Write-Host ""
}

# Create tools directory if it doesn't exist
if (-not (Test-Path $toolsPath)) {
    New-Item -ItemType Directory -Path $toolsPath -Force | Out-Null
}

Write-Host "Building solution (Configuration: $Configuration)..." -ForegroundColor Yellow

# Build the solution
Push-Location $srcPath
dotnet build --configuration $Configuration -nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit $LASTEXITCODE
}
Pop-Location

Write-Host "Build completed successfully" -ForegroundColor Green
Write-Host ""

# Copy binaries and dependencies
Write-Host "Copying binaries and dependencies to tools/..." -ForegroundColor Yellow

$projectDirs = @(Get-ChildItem -Path $srcPath -Directory | Where-Object { $_.Name -ne '.git' })

foreach ($projDir in $projectDirs) {
    $projectName = $projDir.Name
    $binPath = Join-Path $projDir.FullName "bin\$Configuration"

    if (Test-Path $binPath) {
        # Copy all files from bin output
        Get-ChildItem -Path $binPath -Recurse -File | ForEach-Object {
            $relativePath = $_.FullName.Substring($binPath.Length + 1)
            $targetPath = Join-Path $toolsPath $relativePath
            $targetDir = Split-Path $targetPath -Parent

            if (-not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            }

            Copy-Item $_.FullName -Destination $targetPath -Force
        }

        Write-Host "  ✓ $projectName" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Build and packaging completed successfully" -ForegroundColor Green
Write-Host "Output: $toolsPath"
