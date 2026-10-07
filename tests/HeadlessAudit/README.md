# Headless runner

Prefer the complete matrix documented in `../README.md`. This host runs every explicitly
marked `RequiresCampaign = false` test from the actual Debug mod using real game assemblies,
plus isolated managed engine integration checks. It never starts Bannerlord or accesses saves.

```powershell
dotnet build src/Retinues/Retinues.csproj -t:Rebuild -c Debug -p:BL=14 -p:DeployToGame=false
dotnet build tests/HeadlessAudit/HeadlessAudit.csproj -c Debug -p:BL=14 -p:DeployToGame=false
& ./out/bin/HeadlessAudit/Debug/HeadlessAudit.exe ./out/bin/Retinues/Debug/Retinues.dll ./dll/14 v2 '<Harmony module>/bin/Win64_Shipping_Client' --seed=12345 --repeat=3 --junit=out/headless.xml --inventory=out/tests.xml
```

For stable, supply that checkout's Debug binary/game cache and change `v2` to `stable`.
Use a fresh process for each game version. Supply Harmony's complete MonoMod/Mono.Cecil
folder, not only 0Harmony.dll. Keep HeadlessAudit.exe.config alongside the executable.

`--inventory` lists campaign cases too. `--release-check` instead verifies that Release excludes
the runner and embedded fixtures. Failed, skipped and zero-test suites return 1; invalid basic
usage returns 2. The matrix script adds timeouts, process logs and build prerequisites.

For offline restore, use cached Microsoft.NETFramework.ReferenceAssemblies and
Microsoft.NETFramework.ReferenceAssemblies.net472 package directories as local `--source`
arguments, with `--packages` under `out/`, then build with `--no-restore`.
No extra test-framework package is required.

Save compatibility is also checked from Release binaries, without the in-game test framework:

```powershell
& ./out/bin/HeadlessAudit/Debug/HeadlessAudit.exe '<current Retinues.dll>' ./dll/14 v2 '<Harmony directory>' --save-contract=out/current-contract.xml --baseline=tests/HeadlessAudit/Fixtures/v2-96a9dc90.xml
```

Omit `--baseline` to export a contract from a deliberately selected older binary. The checked-in
fixtures came from rebuilt prior commits; do not overwrite them with current output to clear
a compatibility failure. The matrix checks prior stable -> stable, prior V2 -> V2, and stable
-> V2. These managed fixtures are not full Bannerlord saves.
