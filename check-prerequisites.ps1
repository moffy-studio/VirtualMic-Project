param(
    [switch]$OpenDownload
)

$ErrorActionPreference = "Stop"

$vbCableInstalled = $false

$hklm = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
$subKey = $hklm.OpenSubKey("SOFTWARE\\VB-Audio")
if ($subKey -ne $null) {
    $vbCableInstalled = $true
    $subKey.Close()
}

if (-not $vbCableInstalled) {
    $msg = @"
MoffyVMic requires VB-Audio Virtual Cable to be installed.

Required component:
- VB-Audio Virtual Cable

Please install it before launching the application.

Download:
https://vb-audio.com/Cable/
"@

    Write-Host $msg

    if ($OpenDownload) {
        Start-Process "https://vb-audio.com/Cable/"
    }

    exit 1
}

Write-Host "VB-Audio Virtual Cable detected. MoffyVMic can continue."
exit 0
