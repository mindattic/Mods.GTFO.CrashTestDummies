using System.Collections.Generic;
using AIGraph;
using LevelGeneration;
using SNetwork;

namespace GTFO.CrashTestDummies;

/// <summary>Scatters extra dummies on the far side of a security door the moment an alarm scan unlocks it.</summary>
internal static class DoorAmbush
{
    // Doors (by Unity instance ID) seen alarm-locked and not yet opened. An alarm door passes through
    // several states between locked and open (puzzle activated, unlocked, opening), so it's the
    // "was alarm-locked" fact that's remembered, not just the last status; a door that was never
    // alarm-locked never ambushes, and each one ambushes once.
    private static readonly HashSet<int> AlarmLocked = new();

    // Instance IDs are only meaningful within one generated level.
    public static void ResetForNewExpedition() => AlarmLocked.Clear();

    public static void OnSecurityDoorStatusChanged(LG_SecurityDoor? door, eDoorStatus status)
    {
        if (door == null || !Settings.AmbushOnAlarmDoors.Value || !SNet.IsMaster)
            return;

        var key = door.GetInstanceID();
        if (status == eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm)
        {
            AlarmLocked.Add(key);
            return;
        }

        if (status != eDoorStatus.Open || !AlarmLocked.Remove(key))
            return;

        var farNode = FindFarSideNode(door);
        if (farNode == null)
        {
            Plugin.Logger.LogWarning("CrashTestDummies: alarm door opened but no far-side node was found - nothing spawned.");
            return;
        }

        Plugin.Logger.LogInfo($"CrashTestDummies: alarm door opened - ambushing the far side near {farNode.Position}.");
        // Through the safety wrapper like every other spawn routine (G.CTD-T47): an exception from the
        // ambush scatter is logged and stops only this ambush.
        LevelRoutines.Start(
            CoroutineSafety.Run(
                EnemySpawner.SpawnAround(
                    farNode.Position,
                    Settings.AmbushSkipFirstMeters.Value,
                    Settings.AmbushMaxDistanceMeters.Value,
                    Settings.MinSpacingMeters.Value,
                    Settings.AmbushCount.Value),
                "door ambush"));
    }

    // A security door's gate is built from the area the players come from (m_linksFrom) into the first
    // area of the zone it unlocks (m_linksTo), and that zone records the gate as its m_sourceGate. So the
    // far side is the linked area whose zone was entered through this gate - m_linksTo unless the game
    // says otherwise. Where players happen to stand doesn't decide it: they can be anywhere when an
    // alarm door opens, including already past it.
    private static AIG_CourseNode? FindFarSideNode(LG_SecurityDoor door)
    {
        var gate = door.Gate;
        if (gate == null)
            return null;

        var to = gate.m_linksTo;
        var from = gate.m_linksFrom;
        var farArea = IsEnteredThrough(from, gate) && !IsEnteredThrough(to, gate) ? from : to;
        return farArea?.m_courseNode;
    }

    private static bool IsEnteredThrough(LG_Area? area, LG_Gate gate)
    {
        var sourceGate = area?.m_zone?.m_sourceGate;
        return sourceGate != null && sourceGate.Pointer == gate.Pointer;
    }
}
