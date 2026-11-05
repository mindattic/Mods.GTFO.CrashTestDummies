using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace GTFO.CrashTestDummies;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInProcess("GTFO.exe")]
public class Plugin : BasePlugin
{
    public const string GUID = "MindAttic.GTFO.CrashTestDummies";
    public const string NAME = "GTFO.CrashTestDummies";
    public const string VERSION = "20.0.0";

    internal static ManualLogSource Logger { get; private set; } = null!;
    internal static CoroutineHost CoroutineHostInstance { get; private set; } = null!;

    public override void Load()
    {
        Logger = Log;

        if (!Settings.BindMasterSwitch(Config))
        {
            Logger.LogInfo($"{NAME} is switched off in its config ([0. Mod] Enabled) - not loading.");
            return;
        }

        Settings.Bind(Config);

        CoroutineHostInstance = AddComponent<CoroutineHost>();

        new Harmony(GUID).PatchAll();

        Logger.LogInfo($"{NAME} v{VERSION} loaded - low-level enemy flood armed for the next expedition entry.");
    }
}
