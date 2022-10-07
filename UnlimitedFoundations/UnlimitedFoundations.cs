using System;
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
        public const string VERSION = "1.0.2";

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
            // Plugin startup logic
            
            try
            {
                Logger.LogInfo("Patching GetItemCount");
                harmony.PatchAll(typeof(PatchGetItemCount));
            }
            catch(System.Exception ex)
            {
                Logger.LogInfo("Failed patching GetItemCount. Details: " + ex.ToString());
            }

            try
            {
                Logger.LogInfo("Patching GetSandCount");
                harmony.PatchAll(typeof(PatchGetSandCount));
            }
            catch (System.Exception ex)
            {
                Logger.LogInfo("Failed patching GetSandCount. Details: " + ex.ToString());
            }

            
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

    //[HarmonyPatch(typeof(PlanetFactory), "ComputeFlattenTerrainReform")]
    //class PatchComputeFlattenTerrainReform
    //{
    //    static void Postfix(ref int __result)
    //    {
    //        __result = 0;
    //    }
    //}

    [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.GetItemCount), new Type[] { typeof(int) })]
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