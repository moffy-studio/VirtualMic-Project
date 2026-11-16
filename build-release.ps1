param(
    [string]$Configuration = "Release",
    [string]$BuildTools = ""
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "VirtualMicMixer.sln"
$releaseRoot = Join-Path $root "bin\Release"
$packageRoot = Join-Path $root "release\package"
$installerRoot = Join-Path $root "release\installer"

Write-Host "Building MoffyVMic release package..."

if ([string]::IsNullOrWhiteSpace($BuildTools)) {
    $msbuildCandidates = @(
        (Get-Command msbuild -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source),
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\MSBuild\Current\Bin\MSBuild.exe"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    $BuildTools = $msbuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($BuildTools)) {
        throw "MSBuild was not found. Install Visual Studio Build Tools or open a Visual Studio Developer Command Prompt and run this script again."
    }
}

& $BuildTools $solution "/p:Configuration=$Configuration" "/nologo" "/m"
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

if (Test-Path $packageRoot) {
    Remove-Item $packageRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

Copy-Item (Join-Path $releaseRoot "*") $packageRoot -Recurse -Force
Copy-Item (Join-Path $root "icon.ico") (Join-Path $packageRoot "icon.ico") -Force

if (Test-Path (Join-Path $root "tools")) {
    Copy-Item (Join-Path $root "tools\*") (Join-Path $packageRoot "tools\") -Recurse -Force
}

if (Test-Path $installerRoot) {
    Remove-Item $installerRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $installerRoot -Force | Out-Null

$inno = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $inno) {
    $innoCandidates = @(
        "C:\Program Files\Inno Setup 6\iscc.exe",
        "C:\Program Files (x86)\Inno Setup 6\iscc.exe"
    ) | Where-Object { Test-Path $_ }
    if ($innoCandidates.Count -gt 0) {
        $inno = [pscustomobject]@{ Source = $innoCandidates[0] }
    }
}

if ($inno) {
    $iss = Join-Path $root "installer\MoffyVMic.iss"
    & $inno.Source "/O$installerRoot" $iss
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup installer compilation failed."
    }
    Write-Host "Installer output: $installerRoot"
} else {
    Write-Warning "Inno Setup wasn't found on PATH. The app package was prepared at $packageRoot, but the installer did not compile automatically."
}

Write-Host "Release package ready: $packageRoot"
