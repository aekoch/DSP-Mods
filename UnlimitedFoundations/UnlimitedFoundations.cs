using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace UnlimitedFoundations
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string NAME = "UnlimitedFoundations";
        public const string GUID = "com.aekoch.mods.dsp.UnlimitedFoundations";
        public const string VERSION = "1.0.1";

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
