# Stable branch tests

Stable's Debug suite has strict failure/skip reporting, assertion counts, deterministic seeds,
repeated shuffled runs, JUnit and campaign leak detection. Headless checks include frozen save
contracts, utility examples, generated equipment similarity and runner self-tests. The shared
host also verifies installation of the militia Harmony patch against each supported engine.

The automated build/runtime matrix lives in the separate V2 checkout and covers both branches:

```powershell
# From V2; replace the path if your stable checkout is elsewhere:
./tests/Run-Validation.ps1 -StableRoot C:/Users/soufi/Code/bannerlord-retinues -Repeat 3
```

See V2's `tests/README.md` for prerequisites and `tests/LIVE_GAME_MATRIX.md` for save, battle,
UI and soak acceptance scenarios. These are distinct from headless coverage.

With a matching Debug module and a disposable campaign copy loaded:

```text
retinues.run_tests --seed=12345 --repeat=3 --junit=C:/Temp/retinues-stable.xml
retinues.run_tests --headless --seed=12345 --repeat=3
retinues.run_tests regression
```

Keep a separate original save and reload it after testing. Tests mutate game state; cleanup
and before/after checks are not a complete rollback. The runner monitors player resources,
main-party members/prisoners including wounds, XP and object identity, and active custom IDs.
Cleanup failures or detected leaks stop the remaining suite. Other world state can still leak.

Skipped DLC/config/fixture cases are untested. Every executed test must assert. Release builds
exclude tests. Never put `SafeClass`/`SafeMethod` on assertions: swallowed exceptions can turn
failure into a false pass.
