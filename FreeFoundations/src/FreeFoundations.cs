using BepInEx;
using HarmonyLib;

namespace FreeFoundations
{
    [BepInPlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION)]
    public class FreeFoundations : BaseUnityPlugin
    {

        private static Harmony harmony;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginMetadata.GUID} is loaded!");

            // Load hooks
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
