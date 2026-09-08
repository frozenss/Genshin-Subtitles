[CmdletBinding()]
param(
    [string]$ExePath = ""
)

# Issue #37 acceptance: the Release build must not contain the region-adjust
# diagnostic code. The diagnostic log messages live in the #US string heap as
# UTF-16LE; the diagnostic-only Win32 imports live in metadata as ASCII. Both
# must be absent from the Release exe. (Some names such as GetDpiForMonitor or
# GetWindowRect are pre-existing production imports and cannot be probed.)

$ErrorActionPreference = "Stop"

$exe = $ExePath
if ([string]::IsNullOrEmpty($exe)) {
    $exe = Join-Path $PSScriptRoot "..\GI-Subtitles\bin\Release\GI-Subtitles.exe"
}
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Release build not found at $exe. Build GI-Subtitles (Release) first."
}

$utf16Probes = @(
    'AdjustTrace',
    'hit-mode interactive',
    'arm accepted',
    'arm refused',
    'arm dismissed',
    'verdict=',
    'input-never-reached-window',
    'window input:',
    'element mouse-down',
    'store write:',
    'display set:',
    'TRANSPARENT-BIT-STILL-SET',
    'FOREIGN-WINDOW',
    'window input probe'
)

# Imported only by the Debug-only diagnostics (verified unique in the repo).
$asciiProbes = @(
    'WindowFromPoint',
    'EnumDisplayMonitors',
    'GetAncestor'
)

function Find-Bytes {
    param([byte[]]$Haystack, [byte[]]$Needle)

    for ($i = 0; $i -le $Haystack.Length - $Needle.Length; $i++) {
        $ok = $true
        for ($j = 0; $j -lt $Needle.Length; $j++) {
            if ($Haystack[$i + $j] -ne $Needle[$j]) {
                $ok = $false
                break
            }
        }
        if ($ok) {
            return $true
        }
    }
    return $false
}

$bytes = [IO.File]::ReadAllBytes($exe)
$found = @()
foreach ($probe in $utf16Probes) {
    if (Find-Bytes $bytes ([Text.Encoding]::Unicode.GetBytes($probe))) {
        $found += "UTF16 string: '$probe'"
    }
}
foreach ($probe in $asciiProbes) {
    if (Find-Bytes $bytes ([Text.Encoding]::ASCII.GetBytes($probe))) {
        $found += "metadata import: '$probe'"
    }
}

if ($found.Count -gt 0) {
    $found
    throw "Release build contains region-adjust diagnostic code: $($exe)"
}

"OK: no region-adjust diagnostic code in $($exe)"
