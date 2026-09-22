[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishedApiPath,

    [Parameter(Mandatory = $true)]
    [string]$ServerProgId,

    [Parameter(Mandatory = $true)]
    [string]$BrowseItemId,

    [Parameter(Mandatory = $true)]
    [string]$ReadItemId,

    [Parameter(Mandatory = $true)]
    [string]$EvidencePath,

    [switch]$HomologatedEnvironmentConfirmed,
    [switch]$OpcAccessAuthorized,
    [switch]$NonProductionServerConfirmed,
    [switch]$NonProductionTagsConfirmed,
    [switch]$IncludeWrite,
    [string]$WriteItemId,
    [switch]$WriteAuthorized,
    [switch]$RestoreValueCaptured
)

$ErrorActionPreference = "Stop"

function Test-PeX86 {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    $stream = [System.IO.File]::OpenRead($Path)
    $reader = [System.IO.BinaryReader]::new($stream)
    try {
        if ($stream.Length -lt 64) {
            return $false
        }

        $stream.Position = 0x3c
        $peOffset = $reader.ReadInt32()
        if ($peOffset -lt 0 -or ($peOffset + 6) -gt $stream.Length) {
            return $false
        }

        $stream.Position = $peOffset
        if ($reader.ReadUInt32() -ne 0x00004550) {
            return $false
        }

        return $reader.ReadUInt16() -eq 0x014c
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

function Get-Registry32Value {
    param(
        [Parameter(Mandatory = $true)][string]$SubKey,
        [Parameter(Mandatory = $true)][string]$ValueName
    )

    $baseKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey(
        [Microsoft.Win32.RegistryHive]::LocalMachine,
        [Microsoft.Win32.RegistryView]::Registry32)
    try {
        $key = $baseKey.OpenSubKey($SubKey, $false)
        if ($null -eq $key) {
            return $null
        }

        try {
            return $key.GetValue($ValueName, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        }
        finally {
            $key.Dispose()
        }
    }
    finally {
        $baseKey.Dispose()
    }
}

$isWindowsHost = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [System.Runtime.InteropServices.OSPlatform]::Windows)
$isX86Process = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture -eq [System.Runtime.InteropServices.Architecture]::X86
$apiExists = Test-Path -LiteralPath $PublishedApiPath -PathType Leaf
$apiIsX86 = Test-PeX86 -Path $PublishedApiPath

$dcomValue = if ($isWindowsHost) {
    Get-Registry32Value -SubKey "SOFTWARE\Microsoft\Ole" -ValueName "EnableDCOM"
} else {
    $null
}
$opcEnumClsid = if ($isWindowsHost) {
    Get-Registry32Value -SubKey "SOFTWARE\Classes\CLSID\{13486D51-4821-11D2-A494-3CB306C10000}" -ValueName ""
} else {
    $null
}
$serverClsid = if ($isWindowsHost -and -not [string]::IsNullOrWhiteSpace($ServerProgId)) {
    Get-Registry32Value -SubKey ("SOFTWARE\Classes\{0}\CLSID" -f $ServerProgId) -ValueName ""
} else {
    $null
}

$writeSafetySatisfied = -not $IncludeWrite -or (
    -not [string]::IsNullOrWhiteSpace($WriteItemId) -and
    $WriteAuthorized -and
    $RestoreValueCaptured)

$checks = [ordered]@{
    WindowsHost = $isWindowsHost
    ProcessX86 = $isX86Process
    PublishedApiExists = [bool]$apiExists
    PublishedApiIsX86 = [bool]$apiIsX86
    DcomEnabled = $dcomValue -eq "Y"
    OpcEnumRegistered32Bit = -not [string]::IsNullOrWhiteSpace([string]$opcEnumClsid)
    ServerProgIdRegistered32Bit = -not [string]::IsNullOrWhiteSpace([string]$serverClsid)
    HomologatedEnvironmentConfirmed = [bool]$HomologatedEnvironmentConfirmed
    OpcAccessAuthorized = [bool]$OpcAccessAuthorized
    NonProductionServerConfirmed = [bool]$NonProductionServerConfirmed
    NonProductionTagsConfirmed = [bool]$NonProductionTagsConfirmed
    BrowseItemConfigured = -not [string]::IsNullOrWhiteSpace($BrowseItemId)
    ReadItemConfigured = -not [string]::IsNullOrWhiteSpace($ReadItemId)
    WriteRequested = [bool]$IncludeWrite
    WriteSafetySatisfied = [bool]$writeSafetySatisfied
}

$failedChecks = @($checks.GetEnumerator() | Where-Object { -not $_.Value } | ForEach-Object { $_.Key })
# WriteRequested descreve o recorte escolhido; false nao e falha de preflight.
$failedChecks = @($failedChecks | Where-Object { $_ -ne "WriteRequested" })

$evidence = [ordered]@{
    SchemaVersion = 1
    CollectedAtUtc = [DateTime]::UtcNow.ToString("O")
    Result = if ($failedChecks.Count -eq 0) { "PASS" } else { "BLOCKED" }
    Checks = $checks
    FailedChecks = $failedChecks
    SensitiveValuesRecorded = $false
    OpcConnectionAttempted = $false
}

$resolvedEvidencePath = [System.IO.Path]::GetFullPath($EvidencePath)
if ([System.IO.File]::Exists($resolvedEvidencePath)) {
    throw "EvidencePath ja existe; escolha um novo arquivo para preservar a evidencia anterior."
}

$parentDirectory = [System.IO.Path]::GetDirectoryName($resolvedEvidencePath)
if (-not [string]::IsNullOrWhiteSpace($parentDirectory)) {
    [System.IO.Directory]::CreateDirectory($parentDirectory) | Out-Null
}

$json = $evidence | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText($resolvedEvidencePath, $json, [System.Text.UTF8Encoding]::new($false))
$json

if ($failedChecks.Count -ne 0) {
    exit 2
}
