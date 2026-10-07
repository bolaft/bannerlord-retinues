#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$StableRoot = (Join-Path $PSScriptRoot '../../bannerlord-retinues'),
    [string]$HarmonyRoot = 'C:/Program Files (x86)/Steam/steamapps/common/Mount & Blade II Bannerlord/Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client',
    [int]$Seed = 12345,
    [ValidateRange(1, 100)][int]$Repeat = 3,
    [ValidateRange(1, 3600)][int]$TimeoutSeconds = 180,
    [string]$OutputDirectory,
    [switch]$NoRestore,
    [switch]$V2Only
)
$ErrorActionPreference = 'Stop'
$v2Root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$StableRoot = [IO.Path]::GetFullPath($StableRoot)
$HarmonyRoot = [IO.Path]::GetFullPath($HarmonyRoot)
if (!$OutputDirectory) { $OutputDirectory = Join-Path $v2Root ('out/validation/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff')) }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
if (!(Test-Path -LiteralPath (Join-Path $HarmonyRoot '0Harmony.dll'))) { throw "Harmony installation not found: $HarmonyRoot" }
$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
$results = [Collections.Generic.List[object]]::new()

function Invoke-Check([string]$Name, [string]$Executable, [string[]]$Arguments, [string]$WorkingDirectory) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $Executable
    $info.WorkingDirectory = $WorkingDirectory
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $info
    $exitCode = -1
    $timedOut = $false
    $output = ''
    try {
        [void]$process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
            $timedOut = $true
            $process.Kill($true)
            $process.WaitForExit()
        }
        $output = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        $exitCode = $process.ExitCode
    } catch { $output += "`n$_" }
    finally { $process.Dispose() }
    $log = Join-Path $OutputDirectory "$Name.log"
    [IO.File]::WriteAllText($log, $output)
    $ok = $exitCode -eq 0 -and !$timedOut
    $results.Add([pscustomobject]@{ Name=$Name; Passed=$ok; ExitCode=$exitCode; TimedOut=$timedOut; Seconds=$watch.Elapsed.TotalSeconds; Log=$log })
    Write-Host "$(if ($ok) { 'PASS' } else { 'FAIL' }) $Name"
    return $ok
}

$runnerOutput = Join-Path $OutputDirectory 'runner'
$runnerProject = Join-Path $PSScriptRoot 'HeadlessAudit/HeadlessAudit.csproj'
$runnerReady = $true
if (!$NoRestore) { $runnerReady = Invoke-Check 'runner-restore' $dotnet @('restore', $runnerProject, '--ignore-failed-sources', '-p:NuGetAudit=false', '-v:q') $v2Root }
if ($runnerReady) { $runnerReady = Invoke-Check 'runner-build' $dotnet @('build', $runnerProject, '--no-restore', '-t:Rebuild', '-c', 'Debug', '-p:BL=14', "-p:OutputPath=$runnerOutput", '-v:q') $v2Root }
$runner = Join-Path $runnerOutput 'HeadlessAudit.exe'
$branches = @([pscustomobject]@{ Name='v2'; Root=$v2Root; Versions=@('12','13','14','15') })
if (!$V2Only) { $branches += [pscustomobject]@{ Name='stable'; Root=$StableRoot; Versions=@('12','13','14') } }

foreach ($branch in $branches) {
    $project = Join-Path $branch.Root 'src/Retinues/Retinues.csproj'
    $ready = $true
    if (!$NoRestore) { $ready = Invoke-Check "$($branch.Name)-restore" $dotnet @('restore', $project, '--ignore-failed-sources', '-p:NuGetAudit=false', '-v:q') $branch.Root }
    if (!$ready) { continue }
    foreach ($version in $branch.Versions) {
        $references = Join-Path $branch.Root "dll/$version"
        $symbol = if ($version -eq '15') { '14' } else { $version }
        foreach ($configuration in @('Debug','Release')) {
            $label = "$($branch.Name)-$version-$configuration"
            $binaryDirectory = Join-Path $OutputDirectory "binaries/$label"
            $built = Invoke-Check "$label-build" $dotnet @('build', $project, '--no-restore', '-t:Rebuild', '-c', $configuration, "-p:BL=$symbol", "-p:ReferenceRoot=$references", "-p:OutputPath=$binaryDirectory", '-p:DeployToGame=false', '-v:q') $branch.Root
            # Never execute stale artifacts after a failed build.
            if (!$built -or !$runnerReady) { continue }
            $binary = Join-Path $binaryDirectory 'Retinues.dll'
            $arguments = @($binary, $references, $branch.Name, $HarmonyRoot)
            if ($configuration -eq 'Release') { $arguments += '--release-check' }
            else { $arguments += @("--seed=$Seed", "--repeat=$Repeat", "--junit=$(Join-Path $OutputDirectory "$label.xml")", "--inventory=$(Join-Path $OutputDirectory "$label-inventory.xml")") }
            [void](Invoke-Check "$label-tests" $runner $arguments $branch.Root)
            if ($configuration -eq 'Release') {
                $baselineNames = if ($branch.Name -eq 'stable') { @('stable-ff69d116') } else { @('v2-96a9dc90', 'stable-ff69d116') }
                foreach ($baselineName in $baselineNames) {
                    $baseline = Join-Path $PSScriptRoot "HeadlessAudit/Fixtures/$baselineName.xml"
                    $contract = Join-Path $OutputDirectory "contracts/$label-from-$baselineName.xml"
                    [void](Invoke-Check "$label-from-$baselineName" $runner @($binary, $references, $branch.Name, $HarmonyRoot, "--save-contract=$contract", "--baseline=$baseline") $branch.Root)
                }
            }
            if ($branch.Name -eq 'v2' -and $version -eq '14' -and $configuration -eq 'Debug') {
                # A shipped 1.4 binary must also load against the 1.5 engine assemblies.
                $arguments[1] = Join-Path $branch.Root 'dll/15'
                $arguments = $arguments | Where-Object { !($_.StartsWith('--junit=') -or $_.StartsWith('--inventory=')) }
                $arguments += "--junit=$(Join-Path $OutputDirectory 'v2-14-on-15.xml')"
                [void](Invoke-Check 'v2-14-on-15-tests' $runner $arguments $branch.Root)
            }
        }
    }
}

$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'summary.json') -Encoding utf8
$suite = [Xml.XmlDocument]::new()
$root = $suite.CreateElement('testsuite')
$root.SetAttribute('name', 'Retinues build and process matrix')
$root.SetAttribute('tests', [string]$results.Count)
$root.SetAttribute('failures', [string]@($results | Where-Object { !$_.Passed }).Count)
[void]$suite.AppendChild($root)
foreach ($result in $results) {
    $entry = $suite.CreateElement('testcase')
    $entry.SetAttribute('name', $result.Name)
    if (!$result.Passed) {
        $failure = $suite.CreateElement('failure')
        $failure.SetAttribute('message', "Exit=$($result.ExitCode); timeout=$($result.TimedOut); log=$($result.Log)")
        [void]$entry.AppendChild($failure)
    }
    [void]$root.AppendChild($entry)
}
$suite.Save((Join-Path $OutputDirectory 'matrix.xml'))
Write-Host "Reports: $OutputDirectory"
if (@($results | Where-Object { !$_.Passed }).Count -gt 0) { exit 1 }
exit 0
