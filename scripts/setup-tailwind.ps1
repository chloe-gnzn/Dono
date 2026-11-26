# Downloads the Tailwind CSS standalone CLI into src\BloodMatch.Web\tools\.
# Run once after cloning:  .\scripts\setup-tailwind.ps1
$ErrorActionPreference = "Stop"

$Version = "v3.4.17"
$Root = Split-Path -Parent $PSScriptRoot
$ToolsDir = Join-Path $Root "src\BloodMatch.Web\tools"
$Dest = Join-Path $ToolsDir "tailwindcss.exe"

$Arch = if ($env:PROCESSOR_ARCHITECTURE -eq "ARM64") { "arm64" } else { "x64" }
$Url = "https://github.com/tailwindlabs/tailwindcss/releases/download/$Version/tailwindcss-windows-$Arch.exe"

New-Item -ItemType Directory -Force -Path $ToolsDir | Out-Null
Write-Host "Downloading Tailwind $Version (windows-$Arch)..."
Invoke-WebRequest -Uri $Url -OutFile $Dest
Write-Host "Done: $Dest"
