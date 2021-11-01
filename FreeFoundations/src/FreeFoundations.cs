using BepInEx;
using HarmonyLib;
using UnityEngine;
using BepInEx.Logging;

namespace FreeFoundations
{
    [BepInPlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION)]
    public class FreeFoundations : BaseUnityPlugin
    {

        private static Harmony harmony;
        public static ManualLogSource logger;

        private void Awake()
        {
            logger = Logger;

            // Plugin startup logic
            logger.LogInfo($"Plugin {PluginMetadata.GUID} is loaded!");
            Debug.Log("Unity log message");

            // Load hooks
            harmony = new Harmony(PluginMetadata.GUID);
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
            harmony.PatchAll(typeof(PatchComputeFlattenTerrainReform));
            harmony.PatchAll(typeof(PatchGetItemCount));
            harmony.PatchAll(typeof(PatchGetSandCount));
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

    [HarmonyPatch(typeof(PlanetFactory), "ComputeFlattenTerrainReform")]
    class PatchComputeFlattenTerrainReform
    {
        static void Postfix(ref int __result)
        {
            __result = 0;
        }
    }

    [HarmonyPatch(typeof(StorageComponent), "GetItemCount")]
    class PatchGetItemCount
    {
        const int FOUNDATION_ITEM_ID = 1131;

        static void Postfix(int itemId, ref int __result)
        {
            if (itemId == FOUNDATION_ITEM_ID)
            {
                __result = 1000;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "sandCount", MethodType.Getter)]
    class PatchGetSandCount
    {
        static void Postfix(ref int __result)
        {
            __result = 1000000;
        }
    }
}
