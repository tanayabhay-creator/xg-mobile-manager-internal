# Read-only CPU and memory sample of a process that is already running.
# Ally Console does not start this script, and the app does not sample itself.
# This does not install a service and it exits when the timer ends.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File ally-console\publish\measure-process.ps1 -DurationSeconds 60
#
# cpuPercentSincePrevious is this process's processor-time delta divided by
# the wall-clock gap and the processor count. The first row has no delta.
# workingSetBytes and privateBytes are the values Windows returned.
# An empty result is not a measurement: if the process is missing, this script
# writes no file and exits with an error.

param(
    [string]$ProcessName = "AllyConsole",
    [int]$DurationSeconds = 60,
    [int]$IntervalSeconds = 1,
    [string]$OutFile = ""
)

$ErrorActionPreference = "Stop"

if ($DurationSeconds -lt 1) {
    Write-Error "DurationSeconds must be at least 1."
    exit 1
}
if ($IntervalSeconds -lt 1) {
    Write-Error "IntervalSeconds must be at least 1."
    exit 1
}

$proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) {
    Write-Error "Process '$ProcessName' is not running. No sample file was written."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($OutFile)) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutFile = Join-Path (Get-Location) "ally-console-process-$stamp.csv"
}

$cpuCount = [Math]::Max(1, [Environment]::ProcessorCount)
$rows = New-Object System.Collections.Generic.List[string]
$rows.Add("utc,processId,workingSetBytes,privateBytes,processorTimeSeconds,cpuPercentSincePrevious")

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$previousCpu = $null
$previousStamp = $null
$invariant = [System.Globalization.CultureInfo]::InvariantCulture

while ($sw.Elapsed.TotalSeconds -lt $DurationSeconds) {
    try {
        $proc.Refresh()
    }
    catch {
        break
    }
    if ($proc.HasExited) {
        break
    }

    $now = [DateTime]::UtcNow
    $cpu = $proc.TotalProcessorTime.TotalSeconds
    $percent = ""
    if ($null -ne $previousCpu -and $null -ne $previousStamp) {
        $deltaSeconds = ($now - $previousStamp).TotalSeconds
        if ($deltaSeconds -gt 0) {
            $percent = [Math]::Round((($cpu - $previousCpu) / $deltaSeconds) / $cpuCount * 100, 2).ToString($invariant)
        }
    }

    $rows.Add(("{0:o},{1},{2},{3},{4},{5}" -f $now, $proc.Id, $proc.WorkingSet64, $proc.PrivateMemorySize64, $cpu.ToString($invariant), $percent))
    $previousCpu = $cpu
    $previousStamp = $now

    if (($sw.Elapsed.TotalSeconds + $IntervalSeconds) -ge $DurationSeconds) {
        break
    }
    Start-Sleep -Seconds $IntervalSeconds
}

Set-Content -LiteralPath $OutFile -Value $rows -Encoding utf8
Write-Output "Wrote $OutFile ($($rows.Count - 1) samples). These numbers are this run only."
