#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Minimal-output test runner for Agent Tools repository.
.DESCRIPTION
    Runs dotnet test against test projects under src/, suppressing dotnet's
    own console output and reporting results only from the TRX file: one
    summary line per project, plus a compact failure list (test name + first
    line of error message) when something failed. Designed to minimize
    tokens returned to an LLM caller.
#>

[CmdletBinding()]
param(
    [string] $tool = "",
    [string] $filter = "",
    [string] $outDirectory = "",
    [ValidateRange(1, [int]::MaxValue)]
    [int] $maxFailuresToShow = 20,
    [switch] $noBuild
)

$ErrorActionPreference = 'Stop'
$BuildConfiguration = "Debug"

if ("" -eq $outDirectory) {
    $outDirectory = Join-Path $PSScriptRoot 'testresults'
}

if (Test-Path $outDirectory) {
    Remove-Item -Recurse -Force $outDirectory
}
New-Item -ItemType Directory -Path $outDirectory | Out-Null

function Get-TrxSummary {
    param([string]$TrxFilePath)

    [xml]$trx = Get-Content -Path $TrxFilePath
    $counters = $trx.TestRun.ResultSummary.Counters
    $failures = @()

    $failedResults = $trx.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq "Failed" }
    foreach ($result in $failedResults) {
        $message = $result.Output.ErrorInfo.Message
        if ($message) {
            $message = ($message -split "`n")[0].Trim()
        }
        $failures += [PSCustomObject]@{
            TestName = $result.testName
            Message  = $message
        }
    }

    return [PSCustomObject]@{
        Total    = if ($counters) { [int]$counters.total } else { 0 }
        Passed   = if ($counters) { [int]$counters.passed } else { 0 }
        Failed   = if ($counters) { [int]$counters.failed } else { 0 }
        Failures = $failures
    }
}

function Invoke-TestProject {
    param(
        [string] $ProjectFullName,
        [string] $Filter,
        [bool] $NoBuild
    )

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectFullName)

    $testArgs = @(
        "test",
        $ProjectFullName,
        "--configuration", $BuildConfiguration,
        "--results-directory", $outDirectory,
        "--logger", "trx;LogFilePrefix=$projectName",
        "-v", "q", "--nologo"
    )

    if ($Filter) {
        $testArgs += @("--filter", $Filter)
    }

    if ($NoBuild) {
        $testArgs += @("--no-build", "--no-restore")
    }

    $output = & dotnet @testArgs 2>&1
    $exitCode = $LASTEXITCODE

    $trxFile = Get-ChildItem -Path $outDirectory -Filter "$projectName*.trx" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if (-not $trxFile) {
        # No TRX means dotnet couldn't even run (build error, crash, etc.) - this is the
        # one case where we need the raw output to diagnose.
        Write-Host "ERROR: $projectName - no test results produced (exit $exitCode)"
        Write-Host ($output | Select-Object -Last 40 | Out-String)
        return $false
    }

    $summary = Get-TrxSummary -TrxFilePath $trxFile.FullName
    Write-Host "$($projectName): Total=$($summary.Total) Passed=$($summary.Passed) Failed=$($summary.Failed)"

    if ($summary.Total -eq 0) {
        Write-Host "ERROR: NO TEST FOUND"
        return $false
    }

    if ($summary.Failed -gt 0) {
        $toShow = $summary.Failures | Select-Object -First $maxFailuresToShow
        foreach ($f in $toShow) {
            Write-Host "  FAIL $($f.TestName): $($f.Message)"
        }
        if ($summary.Failures.Count -gt $maxFailuresToShow) {
            Write-Host "  ... and $($summary.Failures.Count - $maxFailuresToShow) more"
        }
    }

    return ($exitCode -eq 0)
}

$testProjects = Get-ChildItem -Path (Join-Path $PSScriptRoot 'src') -Filter *Tests.csproj -Recurse

$effectiveFilter = $filter

if ($tool) {
    # If a dedicated test project exists for the tool (e.g. Smtp.Tests.csproj), scope to it.
    $toolProjects = $testProjects | Where-Object { $_.BaseName -like "*$tool*" }
    if ($toolProjects.Count -gt 0) {
        $testProjects = $toolProjects
    }
    else {
        # Shared test project: narrow via NUnit Category trait instead.
        # The VSTest filter spec documents value lookups as case-insensitive, but the
        # NUnit3TestAdapter's Category/TestCategory matching is case-sensitive in practice.
        # Category values in this repo are lowercase (e.g. "smtp", "hellotool"), so
        # normalize the incoming tool name to lowercase before building the filter.
        $toolFilter = "Category=$($tool.ToLowerInvariant())"
        $effectiveFilter = if ($effectiveFilter) { "($effectiveFilter)&($toolFilter)" } else { $toolFilter }
    }
}

if ($testProjects.Count -eq 0) {
    Write-Host "No test projects found."
    exit 0
}

$allPassed = $true
foreach ($project in $testProjects) {
    $passed = Invoke-TestProject -ProjectFullName $project.FullName -Filter $effectiveFilter -NoBuild $noBuild.IsPresent
    if (-not $passed) { $allPassed = $false }
}

if (-not $allPassed) { exit 1 }
exit 0
