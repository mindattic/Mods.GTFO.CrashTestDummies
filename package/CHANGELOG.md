# Changelog

## v19

- Moves in step with the GTFO.EZ family's shared version number (GTFO.EZ, GTFO.EZ.AbsurdWeapons,
  GTFO.EZ.AbsurdMoves, GTFO.EZ.AbsurdItems, GTFO.CrashTestDummies and GTFO.SkipDrop all share one).
  No change in this mod's own behavior.

## v18

- **Lower default population cap: 300 -> 100.** `General.MaxTotalEnemies` now defaults to 100, a
  size that stays comfortably out of the way of normal play instead of flooding the level.
- **New: Performance Guard, a framerate-based safety valve.** Every `Performance.PerformanceCheckIntervalSeconds`
  (default 5s), the host's actual average framerate since the last check is measured; if it's below
  `Performance.MinAcceptableFPS` (default 45), up to `Performance.PerformanceDespawnBatchSize` (default 25)
  currently out-of-sight dummies are recycled immediately, oldest first - regardless of age or combat
  state, unlike the existing time-gated cleanup sweep. Never touches a dummy a player might be looking
  at. Toggle with `Performance.PerformanceGuardEnabled`. Together with the lower population cap, this
  makes the mod self-manage so it doesn't become a hindrance to gameplay even on weaker hardware.

## v17

- **Joins the GTFO.EZ family's `vN` versioning scheme** (previously numbered independently; last
  independent version was v4), so a matching version number across every GTFO.* mod is easy to spot.
- **Removed: Insert/Delete infinite ammo/health.** Out of scope for a mod whose whole job is
  spawning test targets - GTFO.EZ and its Absurd companions already carry it. The
  `GTFO.EZ.Shared` submodule dependency is gone entirely along with it, since nothing else in this
  mod used it.
- **Bug fix:** the cleanup loop that recycles forgotten dummies had no exception safety, unlike
  every other long-running routine in this mod - a bad agent (a destroyed replicator, a torn-down
  camera) could throw and silently stop cleanup for the rest of the level, with the population cap
  never refilling again. Wrapped the same way as the rest.

## v4

- **Bug fix:** a host who entered a level without a local player got no ambient scatter and no
  cleanup for that whole expedition, not even after a checkpoint reload. Now only the drop-point
  scatter is skipped and both loops still start.

## v3

- Master switch: `0. Mod.Enabled` (default `true`) is now the config file's first entry, and turning
  it off turns the whole mod off - nothing is patched, no dummies, no Insert/Delete. It replaces
  `General.Enabled`, which only stopped spawning; if you'd set that to `false`, the new switch starts
  `false` too and the old key is removed.
- Ambient scatter: every `AmbientIntervalSeconds` (default 60s), the host scatters a random
  `AmbientCountMin`-`AmbientCountMax` (default 50-100) more dummies between `AmbientSkipFirstMeters`
  and `AmbientMaxDistanceMeters` (default 10-100 m) of wherever the local player currently is. Toggle
  with `Ambient.AmbientSpawnEnabled`.
- Population cap, not a lifetime budget: `General.MaxTotalEnemies` (default 300) now caps how many of
  this mod's dummies may be alive *at once*, shared across all three spawn triggers. As dummies die or
  get recycled, room opens back up for more - it never permanently maxes out and goes quiet for the
  rest of the expedition.
- Cleanup: every `CleanupCheckIntervalSeconds` (default 30s), any dummy that's been alive longer than
  `CleanupAfterSeconds` (default 300s), is untouched (unhurt and not in aggressive mode) and is
  currently out of sight gets quietly despawned (via the
  network replicator, same as a normal enemy removal), freeing its slot in the population cap. A dummy
  is never touched while a player might be looking at it. Toggle with `Cleanup.CleanupEnabled`.
- Never-seen guarantee, now everywhere: a shared out-of-sight check (`General.OutOfSightHalfAngleDegrees`,
  default 70°) - outside every player's view cone, or blocked by a line-of-sight check even if inside
  it - now gates all three spawn triggers (previously only the ambient scatter had it) and the cleanup
  despawn. Combined with cleanup, nothing this mod does should ever visibly pop in or out. The host
  can't tell where other players and bots are looking, so for them anything in their line of sight
  counts as seen, whichever way they face.
- The level-start scatter is guaranteed to run, and claim its share of the population cap, before the
  ambient scatter or cleanup loop start - so the initial 100 is never crowded out regardless of how
  aggressively `Ambient.*` is tuned.
- Nothing outlives its level: the ambient scatter, cleanup and any scatter still spawning (level-start,
  ambient or door ambush) all stop
  the moment you leave the level (success, fail, abort to lobby or a checkpoint reload), and the
  ambient scatter and cleanup start again when you're back in after a checkpoint.

## v2

- Alarm door ambush: when a security door locked behind an alarm scan (chained puzzle) finishes
  opening, the host scatters `AmbushCount` (default 20) more sleeping dummies between
  `AmbushSkipFirstMeters` (10 m) and `AmbushMaxDistanceMeters` (100 m) of the far side of that
  doorway - same spot-picking as the level-start scatter, centered on the door instead of the drop
  point. Uses the existing spacing, enemy-type and sleep/aggressive settings. Toggle with
  `Ambush.AmbushOnAlarmDoors`.

## v1

- Initial release: on entering an expedition, the host scatters `SpawnCount` (default 100) sleeping
  Strikers between 10 m and 100 m from the drop point, at the level's own enemy spawn
  spots. Count, distance band, spacing, enemy type, delay and sleep/aggressive mode
  are configurable.
