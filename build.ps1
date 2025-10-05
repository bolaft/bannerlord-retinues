# build.ps1
[CmdletBinding()]
param(
  [ValidateSet('core','mcm','prefabs','all')]
  [string]$Target = 'core',

  [ValidateSet('Debug','Release')]
  [string]$Config, # if omitted, MSBuild defaults (DefaultConfiguration)

  [string]$GameDir, # overrides BannerlordGameDir
  [switch]$NoDeploy, # sets DeployToGame=false
  [switch]$Deploy, # sets DeployToGame=true (explicit)
  [ValidateSet('auto','yes','no')]
  [string]$Prefabs = 'auto',
  [switch]$Help
)

if ($Help) {
  @"
Usage:
  ./build.ps1 [-Target core|mcm|prefabs|all] [-Config Debug|Release] [-GameDir <path>] [--NoDeploy|--Deploy] [-Prefabs auto|yes|no]

Examples:
  ./build.ps1 -Target core -Config Release
  ./build.ps1 -Target mcm -NoDeploy
  ./build.ps1 -Target all -Prefabs yes -GameDir 'D:\Games\Bannerlord'
"@ | Write-Host
  exit 0
}

$ErrorActionPreference = 'Stop'
$ROOT = Split-Path -Parent $MyInvocation.MyCommand.Path

$PREFAB_PROJ = Join-Path $ROOT 'src/PrefabBuilder/PrefabBuilder.csproj'
$CORE_PROJ   = Join-Path $ROOT 'src/Retinues/Core/Retinues.Core.csproj'
$MCM_PROJ    = Join-Path $ROOT 'src/Retinues/MCM/Retinues.MCM.csproj'

# Build props to pass to MSBuild
$msbuildProps = @()

if ($GameDir) {
  $msbuildProps += "-p:BannerlordGameDir=$GameDir"
}

if ($NoDeploy.IsPresent) {
  $msbuildProps += "-p:DeployToGame=false"
} elseif ($Deploy.IsPresent) {
  $msbuildProps += "-p:DeployToGame=true"
}

# Config: if omitted, MSBuild can use $(DefaultConfiguration) from Directory.Build.Props
$msbuildConfig = @()
if ($Config) { $msbuildConfig = @('-c', $Config) } else { $msbuildConfig = @('-c','Debug') }

# Prefabs auto: run if project exists and target is core/all/prefabs
if ($Prefabs -eq 'auto') {
  if ((Test-Path $PREFAB_PROJ) -and ($Target -in @('core','all','prefabs'))) {
    $Prefabs = 'yes'
  } else {
    $Prefabs = 'no'
  }
}

Write-Host "== Retinues build =="
Write-Host " Target : $Target"
Write-Host " Config : $($msbuildConfig[1])"
if ($GameDir) { Write-Host " Game   : $GameDir" }
Write-Host " Deploy : $((if ($NoDeploy) {'false'} elseif ($Deploy) {'true'} else {'(default)'}))"
Write-Host " Prefab : $Prefabs"
Write-Host ""

function Build-Project {
  param([string]$proj)
  & dotnet build $proj @msbuildConfig @msbuildProps
}

# 1) Prefabs
if ($Prefabs -eq 'yes' -and (Test-Path $PREFAB_PROJ)) {
  Write-Host "== Building PrefabBuilder =="
  & dotnet build $PREFAB_PROJ @msbuildConfig
  Write-Host "== Running PrefabBuilder =="
  & dotnet run --no-build --project $PREFAB_PROJ
  Write-Host ""
}

# 2) Main targets
switch ($Target) {
  'core'    { Build-Project $CORE_PROJ }
  'mcm'     { Build-Project $MCM_PROJ }
  'prefabs' { } # already handled above
  'all'     { Build-Project $CORE_PROJ; Build-Project $MCM_PROJ }
  default   { throw "Unknown target: $Target" }
}

Write-Host "== Build finished ✅ =="
