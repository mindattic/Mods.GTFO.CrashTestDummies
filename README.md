# GTFO.CrashTestDummies

A BepInEx mod for GTFO that keeps up to 100 sleeping Strikers alive around you, always spawned and recycled out of sight, so you always have targets to sneak up on or shoot while testing other mods.

[![C#](https://img.shields.io/badge/language-C%23-239120)](src/GTFO.CrashTestDummies) [![.NET 6](https://img.shields.io/badge/.NET-6.0-512BD4)](src/GTFO.CrashTestDummies/GTFO.CrashTestDummies.csproj) [![BepInEx IL2CPP](https://img.shields.io/badge/BepInEx-IL2CPP-orange)](package/manifest.json) [![Version 19](https://img.shields.io/badge/version-19.0.0-blue)](package/CHANGELOG.md) [![License](https://img.shields.io/badge/license-all%20rights%20reserved-lightgrey)](#license)

```text
            drop point                 alarm door opens               every 60 s
                |                             |                            |
     100 Strikers, 10-100 m         20 Strikers, 10-100 m        50-100 Strikers, 10-100 m
       around where you land         past that door's far side     around where you are now
                \                             |                            /
                 +----------- out-of-sight check (every player) ----------+
                                              |
                          population cap: 100 alive at once
                                              |
               cleanup every 30 s: untouched, unseen, 5+ min old -> despawned
                                              |
                 performance guard every 5 s: FPS < 45 -> unseen despawned now
```

Try it: run `build.ps1` to build into a Thunderstore Mod Manager profile and package
`dist/GTFO.CrashTestDummies-18.0.0.zip` (Thunderstore package name `GtfoCrashTestDummies`).

## Why

- Land in any rundown and have a hundred sleeping targets waiting 10-100 m away, at the level's own enemy spawn spots.
- Keep practicing for the whole run: alarm doors and a steady trickle refill the population as you kill or leave dummies behind.
- Never watch an enemy pop in or out: every spawn and every recycle skips spots any player can see.
- Test other mods (weapons, movement, items) against live enemies without playing a whole expedition to find some.
- Switch the whole thing off from one config line, without removing it from your profile.

## Features

**Three spawn triggers, one population.** Each scatters in its own 10-100 m band around its own origin, not one band covering the whole level.

- On drop: 100 sleeping Strikers scattered 10-100 m from where you land, 2 seconds after you're in the level. The elevator drop area itself stays clear. This always runs first, so it claims its share of the cap before anything else.
- On each alarm door: when a security door locked behind an alarm scan finishes opening, 20 more are scattered 10-100 m past the far side of that doorway, wherever it is in the level. Each door ambushes once.
- Ambient trickle: every 60 seconds, a random 50-100 more are scattered 10-100 m from wherever you currently are.

**Never seen popping in or out.** A spot is hidden from the host if it's more than 70 degrees off the centre of their view, or something blocks line of sight to it. The host can't tell which way other players and bots are facing, so for them only a blocked line of sight hides a spot. A spot is used only when it's hidden from every player.

**A population cap, not a one-time budget.** Up to 100 of this mod's dummies may be alive at once, across all three triggers. Every 30 seconds, any dummy alive 5+ minutes that is still untouched (unhurt and not fighting) and out of sight is despawned through the game's network replicator, freeing its slot for fresh dummies.

**A framerate safety valve on top of that.** Every 5 seconds the host's actual average framerate is measured; if it's below 45, out-of-sight dummies are recycled immediately - oldest first, regardless of age or combat state - until framerate recovers or none are left unseen. Combined with the lower population cap, the mod self-manages so it can't become the reason your game chugs.

**Host only, level-scoped.** Only the host spawns or recycles enemies. Every routine stops when the level ends (success, fail, abort to lobby or checkpoint reload), and the ambient trickle and cleanup restart after a checkpoint reload; the drop-point scatter does not repeat.

## Quick start

You need GTFO with BepInExPack_GTFO 3.2.3 in a Thunderstore Mod Manager profile that has been launched once (so `BepInEx/interop` exists), and the .NET 6 SDK.

```powershell
git clone https://github.com/mindattic/GTFO.CrashTestDummies.git
cd GTFO.CrashTestDummies
.\build.ps1                      # builds into the "Easy Mode DEV" profile
.\build.ps1 -Profile MyProfile   # or into another profile
```

Launch any rundown from that profile. `BepInEx/LogOutput.log` should show
`GTFO.CrashTestDummies v18.0.0 loaded`, then on landing the chosen dummy type and
`spawning 100x 'Striker_Hibernate' (Hibernate).`

## Configuration

Settings live in `BepInEx/config/MindAttic.GTFO.CrashTestDummies.cfg`. Every setting, its default
and what it does is in [package/README.md](package/README.md#config). The first entry is the master switch:

| Setting | Default | What it does |
|---|---|---|
| `[0. Mod] Enabled` | `true` | Turn the whole mod on or off. When false, `Plugin.Load` logs one line and returns before binding anything else, adding the coroutine host or patching, so nothing of the mod runs. `0. Mod` sorts ahead of every other section, so it's the file's first entry. It replaces the old `[General] Enabled`, which only gated spawning: `Settings.BindMasterSwitch` carries a saved `false` over and removes the old key. |

The other sections, all bound in `src/GTFO.CrashTestDummies/Config.cs`:

| Section | Controls |
|---|---|
| `General` | `MaxTotalEnemies` (population cap, 100) and `OutOfSightHalfAngleDegrees` (view cone, 70) |
| `Spawning` | The on-drop scatter (count 100, 10-100 m, 2 s delay) plus `MinSpacingMeters`, `EnemyNameFilter` and `SpawnAsSleepers`, shared by every trigger |
| `Ambush` | The alarm-door ambush (on, 20, 10-100 m) |
| `Ambient` | The ambient trickle (on, every 60 s, 50-100, 10-100 m) |
| `Cleanup` | Recycling (on, checked every 30 s, after 300 s) |
| `Performance` | Framerate safety valve (on, checked every 5 s, below 45 FPS recycles up to 25 unseen dummies) |

## How it works

All three triggers share the same core routine (`EnemySpawner.SpawnAround`): gather candidate spots
from every room (`AIG_CourseNode.s_allNodes`) near an origin point: each room's designer-placed
`LG_Area.m_enemySpawnPoints` (with their authored facing) plus a few `GetRandomPositionInside()`
points. Drop any outside the `skipMeters`-`maxMeters` band or currently visible to a player (the
shared `Visibility` check), shuffle what's left, and let `SpawnPicker` keep up to `count` that are at
least `MinSpacingMeters` apart. Each is spawned with `EnemyAllocator.Current.SpawnEnemy` on its own
room's course node, in `Hibernate` mode unless `SpawnAsSleepers` is off. What differs between
triggers is only the origin and the distance/count settings.
`count` is also clamped so `EnemyCleanup.AliveCount` (how many of this mod's dummies are currently
alive) never exceeds `MaxTotalEnemies` - a steady-state population cap, not a one-time budget.

**Out of sight, for every trigger and for cleanup** (`Visibility.cs`, `OutOfSightHalfAngleDegrees`,
default 70 degrees): a candidate point is hidden from a player if it's outside this many degrees of the
centre of their `FPSCamera`'s forward direction (from `PlayerAgent.EyePosition`), or if it's inside
that cone but a `Physics.Linecast` between their eye and the point hits something first (round a
corner, behind a crate). It's hidden only if every alive player in `PlayerManager.PlayerAgentsInLevel`
fails to see it. Only the local player has an `FPSCamera`, so on the host every client and bot is
treated as looking every way at once: for them only the linecast can hide a point.

**Startup ordering**: `Post_EnterLevel` runs a `StartupSequence` coroutine that `yield return`s the
on-drop scatter (a nested `IEnumerator` runs to completion before its caller resumes), and only
then starts the ambient and cleanup loops. The scatter runs through `CoroutineSafety.Run`, which
also guards the routines it yields (the scatter's own `SpawnAround`), so an exception anywhere in it
is logged and the loops still start. So the on-drop 100 always gets first claim on the
population cap, regardless of how aggressively `Ambient.*` is tuned.

**On drop** (`SpawnCount`/`SkipFirstMeters`/`MaxDistanceMeters`, default 100 / 10 m / 100 m from the
drop point):

- A prefix on `LevelGeneration.Builder.Build` resets a once-per-expedition flag.
- A postfix on `GS_InLevel.Enter` fires once the cage has landed and you're standing in the first zone. (Not `LocalPlayerAgent.Setup`: the player agent already exists in the elevator while the level is still generating, so it has no course node yet.) On the host only (`SNet.IsMaster`), it starts a coroutine that waits `SpawnDelaySeconds`, then runs `SpawnAround` centered on the drop point.
- The dummy type comes from `GameDataBlockBase<EnemyDataBlock>`, limited to enabled blocks with a built prefab: of those whose name contains `EnemyNameFilter` (default `Striker_Hibernate`, the plain sleeping Striker), the one with the lowest max health is used. If nothing matches, it uses the weakest enemy in the game data. The chosen type is logged to `BepInEx/LogOutput.log`.

**On each alarm door** (`AmbushCount`/`AmbushSkipFirstMeters`/`AmbushMaxDistanceMeters`, default
20 / 10 m / 100 m from the far side of that door, wherever it is in the level):

- A postfix on `LevelGeneration.LG_SecurityDoor.OnSyncDoorStatusChange` watches every door's networked status. `DoorAmbush` doesn't keep each door's last `eDoorStatus` (an alarm door passes through puzzle-activated, unlocked and opening on its way to open): it only remembers, by Unity instance ID, which doors it has seen `Closed_LockedWithChainedPuzzle_Alarm` (an alarm-scan lock). When one of those reaches `Open` it's forgotten, so each door ambushes once. On that open (host only), "the other side" is the room the door leads into: the gate's `m_linksTo` area (the first area of the zone it unlocks; `m_linksFrom` is the side players come from), cross-checked against `LG_Zone.m_sourceGate`, and its `m_courseNode`. Where the players are standing doesn't matter.
- It then calls the same `EnemySpawner.SpawnAround` routine as the level-start scatter, but centered on that far node instead of the drop point, using the `Ambush.*` distance/count settings. `MinSpacingMeters`, `EnemyNameFilter` and `SpawnAsSleepers` are shared across all triggers.

**Ambient trickle** (`AmbientSpawner.cs`; `AmbientCountMin`/`AmbientCountMax`/`AmbientSkipFirstMeters`/
`AmbientMaxDistanceMeters`, default 50-100 / 10 m / 100 m from the local player):

- An infinite coroutine loops `yield return new WaitForSeconds(AmbientIntervalSeconds)` (default 60), then calls `SpawnAround` centered on the local player's current position with a random count in `[AmbientCountMin, AmbientCountMax]`.
- Started from `StartupSequence` only after the on-drop scatter finishes (and only if the level hasn't ended meanwhile).

**Cleanup** (`EnemyCleanup.cs`; `CleanupCheckIntervalSeconds`/`CleanupAfterSeconds`, default 30 s / 300 s):

- Every spawned `EnemyAgent` (from all three triggers) is registered with `EnemyCleanup.Track` the moment `EnemyAllocator.Current.SpawnEnemy` returns it. `EnemyCleanup.AliveCount` prunes dead/null entries as it counts, and is what `SpawnAround` checks against `MaxTotalEnemies` before spawning.
- A second infinite coroutine wakes every `CleanupCheckIntervalSeconds` and recycles any tracked dummy that's older than `CleanupAfterSeconds`, untouched (`AI.Mode` isn't `Agressive` and `Damage.Health` is still at `HealthMax`, so a hurt or fighting dummy - and, with `SpawnAsSleepers` off, every dummy - stays) and currently hidden (same `Visibility` check) by calling `agent.m_replicator.Despawn()` - the same `SNet_Replicator` method the game's own networked spawn/despawn lifecycle uses, so it's removed cleanly for every client, not just locally destroyed.

**Performance Guard** (`PerformanceGuard.cs`; `PerformanceCheckIntervalSeconds`/`MinAcceptableFPS`/
`PerformanceDespawnBatchSize`, default 5 s / 45 / 25):

- A third infinite coroutine samples `Time.frameCount` and `Time.unscaledTime` at the start of each
  wait and again after `WaitForSeconds(PerformanceCheckIntervalSeconds)`, so the frame delta divided
  by the elapsed time is the host's actual average framerate over that window, not a single noisy
  instantaneous reading.
- If that average is below `MinAcceptableFPS`, it calls `EnemyCleanup.DespawnHiddenEnemies`, which
  recycles up to `PerformanceDespawnBatchSize` tracked dummies that are currently hidden (the same
  `Visibility` check as cleanup), oldest first - unlike the cleanup sweep, it doesn't wait for
  `CleanupAfterSeconds` or require the dummy to be untouched, since the point is to claw back
  performance right away. A dummy a player might be looking at is never touched.
- Started from `StartLoops` alongside the ambient and cleanup loops, so it's on for the whole level
  and restarts after a checkpoint reload the same way they do.

**Level lifetime** (`LevelRoutines.cs`): `CoroutineHost` is a plugin-level component that outlives any
single expedition, so every coroutine started for a level - the startup scatter, the ambient and cleanup
loops, and each door ambush - is started through `LevelRoutines` and stopped together when the level
ends: a postfix on `GS_InLevel.Exit` (success, fail, abort to lobby, checkpoint), the success/fail
screens' `Enter`, and `Pre_Build`. The stop is `CoroutineHost.StopAllCoroutines()`, not
`StopCoroutine` on each started handle: a routine that `yield return`s another `IEnumerator` (the
startup scatter, each ambient scatter) hands it to Unity as a separate child coroutine, which stopping
the parent leaves running; nothing but `LevelRoutines` uses `CoroutineHost`. Each stop also bumps
`LevelRoutines.Run`; the loops and `StartupSequence` check the value they started with after every
wait, so nothing outlives its level. Re-entering `InLevel` in the same expedition (a checkpoint reload)
restarts the ambient and cleanup loops, but not the once-per-expedition drop-point scatter.

`LevelPatches` needs its class-level `[HarmonyPatch]`: `PatchAll` skips classes without one.

The spawn API and patch points were taken from ExtraEnemyCustomization's `SpawnEnemyAbility` and
TheArchive, and checked against the interop assemblies in the DEV profile.

## Project layout

```text
src/GTFO.CrashTestDummies/        the plugin project (Harmony patches, game calls)
src/GTFO.CrashTestDummies.Core/   pure spawn logic, no game dependency: spot picking, enemy-type
                                  selection, once-per-level gate; ships alongside the plugin DLL
tests/                            GTFO.CrashTestDummies.Core.Tests - NUnit, plain dotnet test, no game needed
package/                          what ships to Thunderstore / gets zipped (manifest name "GtfoCrashTestDummies")
  icon.png                        256x256, resized from reference/logo-1254x1254.png
reference/                        source art
build.ps1                         builds into the profile and packages dist/GTFO.CrashTestDummies-<version>.zip
```

## Building

Requires BepInExPack_GTFO in a Thunderstore Mod Manager profile that has been launched once, so
`BepInEx/interop` exists (this project compiles against the game's interop assemblies).

```powershell
.\build.ps1                      # Thunderstore Mod Manager profile "Easy Mode DEV"
.\build.ps1 -Profile MyProfile
```

`build.ps1` builds the plugin straight into `BepInEx/plugins/GTFO.CrashTestDummies/` of that profile,
copies the plugin DLL and `GTFO.CrashTestDummies.Core.dll` (a runtime dependency that must ship with it)
into `package/`, and zips `package/` into `dist/GTFO.CrashTestDummies-<version>.zip`.

## Testing

```powershell
dotnet test tests/GTFO.CrashTestDummies.Core.Tests/GTFO.CrashTestDummies.Core.Tests.csproj
```

No game install or BepInEx profile needed. This covers `GTFO.CrashTestDummies.Core`: spot
picking (distance band from an arbitrary origin, spacing, count cap - the same `SpawnPicker` logic
underlies all three spawn triggers, just with different origins and band/count settings), enemy-type
selection (weakest name match, Big Strikers skipped, disabled/no-prefab/zero-health blocks excluded,
fallback when nothing matches), and the once-per-level host-only gate. The visibility check
(`Visibility.cs`), population tracking (`EnemyCleanup.cs`) and ambient/cleanup coroutines live in the
game-dependent project since they need `PlayerAgent`, `FPSCamera` and `Physics.Linecast` - they're
untested by `dotnet test` and only verifiable in-game.

The Harmony patches and game calls in `src/GTFO.CrashTestDummies/` only run inside the game, so
check those in-game after `build.ps1`: launch any rundown in the profile. `LogOutput.log` should show
`GTFO.CrashTestDummies v18.0.0 loaded`, then on landing the chosen dummy type and
`spawning 100x 'Striker_Hibernate' (Hibernate).` Open an alarm-locked security door and it should log
`alarm door opened - ambushing the far side near ...`, then `spawning 20x 'Striker_Hibernate' (Hibernate).`
About a minute later it should log another `spawning Nx 'Striker_Hibernate' (Hibernate).` (N between 50
and 100) from the ambient trickle, and after dummies have sat unseen for 5+ minutes, occasional
`recycled N forgotten dummies (M still alive).` lines from cleanup. Worth confirming visually too: no
dummy should ever appear to pop in or out while you're looking at the spot it occupies.

## Versioning

Major-only bumps (v1, v2, ...), written as `N.0.0` in `manifest.json`. As of v17 this mod is on the
same `vN` scheme as the GTFO.EZ family and GTFO.EZ.Shared, rather than numbering its own releases
independently - a matching version number across every GTFO.* mod is easy to spot. What changed in
each version is in [package/CHANGELOG.md](package/CHANGELOG.md).

## Publishing to Thunderstore

`dist/GTFO.CrashTestDummies-<version>.zip` is ready to upload as-is.

## Limitations

- Only the host spawns or recycles dummies; on a client the mod spawns nothing.
- The host can't see which way other players and bots face, so for them only a blocked line of sight counts as hidden.
- With `SpawnAsSleepers` off, dummies spawn awake and aggressive, so cleanup never recycles any of them.
- The visibility check, population tracking, coroutines and Harmony patches have no automated tests; they are checked in-game.

## Documentation

- [package/README.md](package/README.md) - the player-facing page shipped to Thunderstore, with the full config table.
- [package/CHANGELOG.md](package/CHANGELOG.md) - what changed in each version.
- [docs/USER_STORIES.md](docs/USER_STORIES.md) - the ticket log, generated from the workflow database.
- [AGENTS.md](AGENTS.md) - instructions for coding agents working in this repo.

## License

This repo has no LICENSE file; all rights reserved. The spawn API and patch points were learned from
ExtraEnemyCustomization and TheArchive.

Part of [MindAttic](https://mindattic.com) — see more projects at [github.com/mindattic](https://github.com/mindattic). Related: [GTFO.EZ](https://github.com/mindattic/GTFO.EZ).
