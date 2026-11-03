using System.Collections;
using Player;
using SNetwork;
using UnityEngine;

namespace GTFO.CrashTestDummies;

/// <summary>
/// Every AmbientIntervalSeconds, scatters a random (AmbientCountMin-AmbientCountMax) number of more
/// dummies near the local player, skipping any spot currently inside a player's view cone, so they
/// never visibly pop in - they're just there next time you look.
/// </summary>
internal static class AmbientSpawner
{
    private const float MinIntervalSeconds = 1f;

    private static bool warnedInvertedRange;

    // G.CTD-T47: AmbientCountMin above AmbientCountMax (or a negative bound) is a config mistake, not a
    // request for a negative or inverted range: use the two values as the range's ends, never below 0,
    // and say so once.
    private static (int lo, int hi) CountRange(int min, int max)
    {
        if (min > max && !warnedInvertedRange)
        {
            warnedInvertedRange = true;
            Plugin.Logger.LogWarning(
                $"CrashTestDummies: AmbientCountMin ({min}) is above AmbientCountMax ({max}); using {max}-{min}.");
        }
        return (Math.Max(0, Math.Min(min, max)), Math.Max(0, Math.Max(min, max)));
    }

    /// <param name="run">The <see cref="LevelRoutines.Run"/> this loop belongs to; it ends once that level is over.</param>
    public static IEnumerator Routine(int run)
    {
        while (true)
        {
            // A zero or negative interval would run a full scatter every frame.
            yield return new WaitForSeconds(Math.Max(MinIntervalSeconds, Settings.AmbientIntervalSeconds.Value));

            if (!LevelRoutines.IsCurrent(run))
                yield break;

            if (!Settings.AmbientSpawnEnabled.Value || !SNet.IsMaster)
                continue;

            if (!PlayerManager.TryGetLocalPlayerAgent(out var localAgent) || localAgent == null)
                continue;

            var (lo, hi) = CountRange(Settings.AmbientCountMin.Value, Settings.AmbientCountMax.Value);
            var count = UnityEngine.Random.Range(lo, hi + 1);
            // Run through the safety wrapper: an exception from one ambient scatter must not end this
            // while(true) loop and silently stop ambient spawning for the rest of the level.
            yield return CoroutineSafety.Run(
                EnemySpawner.SpawnAround(
                    localAgent.transform.position,
                    Settings.AmbientSkipFirstMeters.Value,
                    Settings.AmbientMaxDistanceMeters.Value,
                    Settings.MinSpacingMeters.Value,
                    count),
                "ambient spawn");
        }
    }
}
