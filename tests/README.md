# Retinues validation

Validation has three layers: headless checks against real game assemblies, tests in a loaded
campaign, and live save/mission/UI scenarios. A passing headless matrix is a prerequisite for
release, not proof that a campaign is safe.

## Run both branches

From the V2 checkout in PowerShell 7:

```powershell
./tests/Run-Validation.ps1
./tests/Run-Validation.ps1 -NoRestore -Seed 20260909 -Repeat 5
./tests/Run-Validation.ps1 -StableRoot C:/Code/retinues-stable -HarmonyRoot C:/Game/Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client
```

Requires Windows, the .NET SDK with net472 targeting support, each checkout's existing `dll/`
caches, and the complete installed Bannerlord.Harmony dependency directory. Proprietary game
files are not distributed with the tests. The script restores dependencies by default;
`-NoRestore` uses existing assets files and packages.

The matrix rebuilds stable for BL12/13/14 and V2 for BL12/13/14 plus BL14 against 1.5 references,
in Debug and Release. It also runs the BL14 V2 binary against 1.5 assemblies. Every runtime
combination gets a fresh process. It does not deploy, launch the game or access campaign saves.
Each build has its own output directory; a failed build cannot run stale binaries. Each
process has a 180-second timeout, adjustable with `-TimeoutSeconds`.

Release binaries also run compatibility probes against fixtures exported by the prior stable
and V2 binaries. These execute the real save-type registration methods in an isolated engine
definition context, compare every registered class/field/container, and load/resave a prior V2
model/shared-XP payload. See [SAVE_COMPATIBILITY.md](SAVE_COMPATIBILITY.md) for baseline provenance
and the limits of this check. Contract exports are stored under the run's `contracts/` directory.

Artifacts under `out/validation/<timestamp>/`:

- `summary.json` and `matrix.xml`: build/process outcomes, including crashes and timeouts.
- `<branch>-<version>-Debug.xml`: JUnit outcomes, repetitions, assertion counts, suite/case
  seeds, build target, engine version and binary SHA-256 fingerprints.
- `*-inventory.xml`: all discovered tests, including campaign-only tests.
- `*.log` and `binaries/`: full output and the exact tested modules.

A failed build, timeout, initialization failure, assertion failure, unexpected skip or empty
selection returns a nonzero exit code. Missing dependencies fail discovery instead of hiding
tests. The runner's own configuration permits loading the supplied caches when a downloaded
DLL retains its Windows zone marker; installed game and machine settings are unchanged.
CI can call this script on a Windows worker with locally provisioned game caches. No public
CI job containing game DLLs is configured.

## Coverage

| Area | Stable | V2 |
| --- | --- | --- |
| Runner | Failure/skip/empty-test/cleanup self-tests | Same |
| Save contracts | Frozen legacy field IDs/types and namespace | Same, plus extra-tree descendants/captains |
| Serialization | Existing campaign save tests | Frozen XML; four successive loads across en-US/fr-FR/tr-TR; malformed/unknown payloads; transient exclusion; large gzip/chunk round trips |
| Generated inputs per repeat | 500 equipment similarity cases | 200 mixed-XP sequences, 200 crafted populations, 200 retinue-command sequences |
| Engine compatibility | Militia helper and actual Harmony installation | Managed reward fixtures, party bindings and three cap patch installations |
| Saved progression | Existing campaign XP tests | Real shared-pool SyncData load/save/continued awards through an in-memory IDataStore |
| Campaign | Existing feature suite with strict results | Existing feature suite plus 30 clone/edit/restore/remove cycles, equipment economy matrix and staged queue time-slicing/reload |
| Release | Runner and fixtures excluded | Same |

Generated checks call production code and compare with independent invariants or small
oracles. Managed engine fixtures do not simulate missions. The in-memory IDataStore does not
exercise the engine's full save-file serializer; the frozen XML is a model-format fixture,
not a `.sav`. Review frozen contracts explicitly when formats change; never regenerate them
from the implementation merely to clear a failing test.

## Campaign runs

Use a matching Debug module. Load a **disposable copy** of a test campaign and pause on the
campaign map. Feature tests mutate game objects; cleanup is not a complete world rollback.
Keep an archived original save and reload it after testing.

```text
retinues.run_tests --seed=12345 --repeat=3 --junit=C:/Temp/retinues-campaign.xml
retinues.run_tests sequences --seed=12345 --repeat=3
retinues.run_tests audit
retinues.run_tests --headless --seed=12345 --repeat=3
```

Positional filters are an exact group then a test-name substring; `-` omits a filter. `--stop`
stops on the first failure. Repetitions shuffle order deterministically. Reproduce a generated
failure with the reported **suite seed and repeat count**, identical binaries and fixtures.
Case seed, iteration and generated-case index are also recorded for debugging.

Missing DLC/configuration/fixtures are `Skipped`, never passed. Review every skip against the
fixture profile; a run with skips is not fully passing. Every executed test must assert.
Discovery errors and duplicate IDs fail. Never add exception-swallowing `SafeClass` or
`SafeMethod` wrappers to assertion helpers.

Before/after campaign checks monitor player gold, renown, influence, main-party members and
prisoners (including wounds, XP and object identity), and active custom troop IDs. A mismatch
or reported cleanup failure stops the suite and skips the remaining cases. This neither
repairs leaks nor covers every NPC, settlement, stock, setting, doctrine or event subscription.
New tests should use `ctx.Defer` or cleanup scopes that report failures, and verify any other
global state they touch. The in-game runner cannot safely abort a hung engine call; external
process timeouts apply to the headless host only.

Use [LIVE_GAME_MATRIX.md](LIVE_GAME_MATRIX.md) for disk-save, mission, UI and soak acceptance.
Keep versions, load order, reports, logs and before/after observations with each run. Missing
runs remain untested. See [VALIDATION.md](VALIDATION.md) for current evidence and
[HeadlessAudit/README.md](HeadlessAudit/README.md) for individual runner usage.
