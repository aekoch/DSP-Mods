using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CreativeMode
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string NAME = "CreativeMode";
        public const string GUID = "com.aekoch.mods.dsp.CreativeMode";
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

            logger.LogInfo($"Inventory size = {PatchNotifyStorageChange.fullInventorySize}");

            //Plugin.logger.LogInfo("All items");
            //foreach (ItemProto item in LDB.items.dataArray)
            //{
            //    Plugin.logger.LogInfo($"{item.ID} - {item.name} - {item.Type}");
            //}

            Plugin.logger.LogInfo(GameMain.isRunning);
            Plugin.logger.LogInfo(GameMain.gameName);

            if (UIRoot.instance.isActiveAndEnabled)
            {
                Plugin.logger.LogInfo("MainMenu is open");
            }
            else
            {
                Plugin.logger.LogInfo("MainMenu is not open");
            }
        }

        private void OnDestroy()
        {
            Unpatch();
            UnloadYourResources();
        }

        private void Patch()
        {
            harmony.PatchAll(typeof(PatchCategoryUnlocked));
            harmony.PatchAll(typeof(PatchItemUnlocked));
            harmony.PatchAll(typeof(PatchNotifyStorageChange));
            harmony.PatchAll(typeof(PatchResearchedTechs));
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

    [HarmonyPatch(typeof(UIBuildMenu), "CategoryUnlocked")]
    class PatchCategoryUnlocked
    {
        static void Postfix(int category, ref bool __result)
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(GameHistoryData), "ItemUnlocked")]
    class PatchItemUnlocked
    {
        static void Postfix(int itemId, ref bool __result)
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(StorageComponent), "NotifyStorageChange")]
    class PatchNotifyStorageChange
    {
        static void Postfix(ref StorageComponent __instance)
        {
            if (__instance == GameMain.mainPlayer.package) {
                Plugin.logger.LogInfo("Refreshing player inventory");
                if (__instance.size != fullInventorySize) {
                    __instance.SetSize(fullInventorySize);
                }
                __instance.grids = fullInventory;
            }
        }

        // These are items that have not been published in the game yet
        // I don't think the devs would appreciate me adding them to the game
        public static List<int> disallowedItems = new List<int> { 
            1141, 1142, 1143, 2030, 2313
        };

        public static StorageComponent.GRID[] fullInventory
        {
            get
            {
                StorageComponent.GRID[] result = new StorageComponent.GRID[fullInventorySize];
                int storageIndex = 0;
                foreach(ItemProto item in LDB.items.dataArray)
                {
                    if (storageIndex >= fullInventorySize)
                    {
                        break;
                    }
                    if (disallowedItems.Contains(item.ID))
                    {
                        continue;
                    }
                    StorageComponent.GRID slot = result[storageIndex];
                    slot.itemId = item.ID;
                    slot.stackSize = item.StackSize;
                    slot.count = item.StackSize;
                    result[storageIndex] = slot;
                    storageIndex++;
                }
                return result;
            }
        }

        public static int fullInventorySize
        {
            get {
                return 120;
            }
        }
    }

    
    [HarmonyPatch(typeof(GameMain), "Begin")]
    class PatchResearchedTechs
    {
        static void Postfix()
        {
            if (DSPGame.IsMenuDemo)
            {
                return;  // We are on the main menu
            }

            GameHistoryData history = GameMain.history;
            foreach(TechProto tech in LDB.techs.dataArray)
            {
                TechState state = history.techStates[tech.ID];
                if (!state.unlocked) {
                    history.UnlockTech(tech.ID);
                }
            }
        }
    }
}
