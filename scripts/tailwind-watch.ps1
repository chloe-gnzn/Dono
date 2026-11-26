# Rebuilds wwwroot\css\app.css whenever a .razor/.cs/.css file changes.
# Run in a second terminal next to `dotnet watch`.
$ErrorActionPreference = "Stop"
$Project = Join-Path (Split-Path -Parent $PSScriptRoot) "src\BloodMatch.Web"
Set-Location $Project
& .\tools\tailwindcss.exe -i Styles/app.css -o wwwroot/css/app.css --watch
