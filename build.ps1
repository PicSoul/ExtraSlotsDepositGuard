<#
    Builds and packages ExtraSlotsDepositGuard for Thunderstore.

    Usage:
        .\build.ps1              # build + validate + zip
        .\build.ps1 -Install     # also copy into the local r2modman profile

    Thunderstore package versions are immutable: once a version is uploaded it can
    never be edited or replaced. Bump version_number in manifest.json AND <Version>
    in the .csproj before repackaging.
#>

[CmdletBinding()]
param(
    [switch]$Install,
    [string]$Profile = "1.0 Release"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

function Fail($msg) { Write-Host "FAIL: $msg" -ForegroundColor Red; exit 1 }
function Ok($msg)   { Write-Host "  ok   $msg" -ForegroundColor Green }

# ---------------------------------------------------------------- version sync
$manifest = Get-Content "$root\manifest.json" -Raw | ConvertFrom-Json
$csproj = [xml](Get-Content "$root\ExtraSlotsDepositGuard.csproj")
$csprojVersion = $csproj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

Write-Host "`nExtraSlotsDepositGuard $($manifest.version_number)" -ForegroundColor Cyan

if ($csprojVersion -ne $manifest.version_number) {
    Fail "version mismatch: manifest.json says $($manifest.version_number), csproj says $csprojVersion"
}
Ok "version $($manifest.version_number) matches in manifest.json and csproj"

# ---------------------------------------------------------------------- build
Write-Host "`nbuilding..." -ForegroundColor Cyan
$buildLog = & dotnet build "$root\ExtraSlotsDepositGuard.csproj" -c Release -v minimal --nologo 2>&1
if ($LASTEXITCODE -ne 0) { $buildLog; Fail "build failed" }
Ok "compiled"

$dll = "$root\bin\Release\ExtraSlotsDepositGuard.dll"
if (-not (Test-Path $dll)) { Fail "expected output missing: $dll" }

# --------------------------------------------------------------------- stage
$stage = "$root\package"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null

Copy-Item $dll $stage
foreach ($f in @("manifest.json", "icon.png", "README.md", "CHANGELOG.md", "LICENSE")) {
    if (Test-Path "$root\$f") { Copy-Item "$root\$f" $stage }
}

# ------------------------------------------------------------------ validate
Write-Host "`nvalidating against Thunderstore rules..." -ForegroundColor Cyan

# manifest.json, icon.png and README.md must exist at the ZIP ROOT
foreach ($required in @("manifest.json", "icon.png", "README.md")) {
    if (-not (Test-Path "$stage\$required")) { Fail "$required missing from package root" }
}
Ok "manifest.json, icon.png, README.md present at package root"

if ($manifest.name -notmatch '^[a-zA-Z0-9_]+$') { Fail "name '$($manifest.name)' has illegal characters (allowed: a-z A-Z 0-9 _)" }
if ($manifest.name.Length -gt 128)              { Fail "name exceeds 128 characters" }
Ok "name '$($manifest.name)' is valid"

if ($manifest.version_number -notmatch '^\d+\.\d+\.\d+$') { Fail "version_number must be Major.Minor.Patch" }
Ok "version_number is valid semver"

if ($manifest.description.Length -gt 250) { Fail "description is $($manifest.description.Length) chars (max 250)" }
Ok "description is $($manifest.description.Length)/250 chars"

if ($null -eq $manifest.website_url) { Fail "website_url must be present (use an empty string if unused)" }
Ok "website_url present"

foreach ($dep in $manifest.dependencies) {
    if ($dep -notmatch '^[a-zA-Z0-9_]+-[a-zA-Z0-9_]+-\d+\.\d+\.\d+$') {
        Fail "dependency '$dep' is not in {team}-{package}-{major.minor.patch} form"
    }
}
Ok "$($manifest.dependencies.Count) dependency string(s) well-formed"

Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile("$stage\icon.png")
$w = $img.Width; $h = $img.Height
$img.Dispose()
if ($w -ne 256 -or $h -ne 256) { Fail "icon.png is ${w}x${h}, must be exactly 256x256" }
Ok "icon.png is 256x256"

# README must be valid UTF-8
try { [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes("$stage\README.md")) | Out-Null }
catch { Fail "README.md is not valid UTF-8" }
Ok "README.md is UTF-8"

# ---------------------------------------------------------------------- zip
$zip = "$root\$($manifest.name)-$($manifest.version_number).zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$stage\*" -DestinationPath $zip
Ok "packaged $(Split-Path $zip -Leaf) ($([math]::Round((Get-Item $zip).Length / 1KB, 1)) KB)"

# ------------------------------------------------------------------- install
if ($Install) {
    Write-Host "`ninstalling to local profile '$Profile'..." -ForegroundColor Cyan

    if (Get-Process -Name "valheim" -ErrorAction SilentlyContinue) {
        Fail "Valheim is running; close it before installing"
    }

    $pluginRoot = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\$Profile\BepInEx\plugins"
    if (-not (Test-Path $pluginRoot)) { Fail "profile not found: $pluginRoot" }

    $target = Get-ChildItem $pluginRoot -Directory | Where-Object { $_.Name -like "*ExtraSlotsDepositGuard" } | Select-Object -First 1
    if (-not $target) { Fail "no existing *-ExtraSlotsDepositGuard folder under $pluginRoot; install it once via r2modman first" }

    Copy-Item "$stage\*" $target.FullName -Force
    Ok "installed to $($target.Name)"
}

Write-Host "`ndone.`n" -ForegroundColor Cyan
