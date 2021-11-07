using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace AutoBlueprint
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string NAME = "AutoBlueprint";
        public const string GUID = "com.aekoch.mods.dsp.AutoBlueprint";
        public const string VERSION = "0.1.0";

        private static Harmony harmony;
        public static ManualLogSource logger;

        private void Awake()
        {
            logger = Logger;

            // Plugin startup logic
            Logger.LogInfo($"Plugin {GUID} is loaded!");

            // Load hooks
            harmony = new Harmony(GUID);
            Patch();
            LoadYourResources();
        }

        private void OnDestroy()
        {
            Unpatch();
            UnloadYourResources();
        }

        private void Patch()
        {
            // TODO
        }

        private void Unpatch()
        {
            harmony?.UnpatchSelf();
        }

        private void LoadYourResources()
        {
            // TODO
        }

        private void UnloadYourResources()
        {
            // TODO
        }
    }
}
