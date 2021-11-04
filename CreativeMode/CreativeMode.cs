using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;

namespace CreativeMode
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string NAME = "CreativeMode";
        public const string GUID = "com.aekoch.mods.dsp.CreativeMode";
        public const string VERSION = "0.1.0";

        private static Harmony harmony;
        public static new ManualLogSource Logger;
        public static new ConfigFile Config;

        #region Lifecycle
        private void Awake()
        {
            Logger = base.Logger;
            Config = base.Config;

            // Plugin startup logic
            Logger.LogInfo($"Plugin {GUID} is loaded!");
            BindConfigs();

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
            harmony.PatchAll(typeof(Plugin));
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
        #endregion Lifecycle

        #region Config

        private void BindConfigs()
        {
            BindInventoryConfigs();
            BindResearchConfigs();
            BindLogisticsConfigs();
            BindEnergyConfigs();
        }

        private static void OnConfigReload()
        {
            if (Config_ResearchAllTechs.Value)
                TechUtils.ResearchAllTechs();

            if (Config_InventoryAlwaysContainsAllUnlockedItems.Value)
                InventoryUtils.FillPlayerInventory();

            if (Config_InfiniteWarpers.Value)
                InventoryUtils.FillPlayerWarpers();
        }

        #region Inventory

        public static ConfigEntry<bool> Config_InventoryAlwaysContainsAllUnlockedItems;
        public static ConfigEntry<bool> Config_InfiniteWarpers;

        private void BindInventoryConfigs()
        {
            string section = "Inventory";

            Config_InventoryAlwaysContainsAllUnlockedItems = Config.Bind<bool>(
                section, "Inventory always contains all unlocked items", true,
                "If true, the players inventory will always contain a full stack of each unlocked item. " +
                "Note: There is a maximum limit of 120 items in the inventory, so if using mods that add items " +
                "some may not be available. ");

            Config_InfiniteWarpers = Config.Bind<bool>(section, "Infinite Warpers", true);
        }

        #endregion

        #region Research

        public static ConfigEntry<bool> Config_ResearchAllTechs;
        public static ConfigEntry<bool> Config_InstantResearch;

        private void BindResearchConfigs()
        {
            string section = "Research";
            Config_ResearchAllTechs = Config.Bind<bool>(section, "Research All Techs", true,
                "If true, all techs will be researched when loading a save");

            Config_InstantResearch = Config.Bind<bool>(section, "Instant Research", true,
                "If true, all techs will be researched instantly upon being enqueued. " +
                "Research popups are disabled while this is true.");
        }

        #endregion

        #region Logistics

        public static ConfigEntry<bool> Config_EnsureLogisticStationSteadyState;

        private void BindLogisticsConfigs()
        {
            string section = "Logistics";
            Config_EnsureLogisticStationSteadyState = Config.Bind(section, "Ensure Logistic Station Steady State", true,
                "If true, any item demanded by a logistics station will be pinned to the max, " +
                "and any item supplied by a logistics station will be pinned to zero. " +
                "This ensures that belts will always have an infinite source and sink. " +
                "This is good for designing and optimizing large blueprints, but is not ideal " +
                "for testing planet, solar, or galatic transportation networks");
        }

        #endregion

        #region Energy

        public static ConfigEntry<bool> Config_InfinitePlanetEnergy;
        public static ConfigEntry<bool> Config_InfiniteMechEnergy;

        private void BindEnergyConfigs()
        {
            string section = "Energy";
            Config_InfiniteMechEnergy = Config.Bind<bool>(section, "Infinite Mech Energy", true,
                "If true, the mech's power will be pinned to its maximum value.");

            Config_InfinitePlanetEnergy = Config.Bind<bool>(section, "Infinite Planet Energy", true,
                "If true, buildings will always have 100% energy, and all energy storage will be pinned to its maximum value");
        }

        #endregion

        #endregion

        #region Patches

        #region Reload Config

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void PatchGameBegin()
        {
            if (DSPGame.IsMenuDemo)
            {
                return;  // We are on the main menu
            }

            Config.Reload();
            OnConfigReload();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Pause))]
        public static void PatchGamePause()
        {
            Config.Reload();
            OnConfigReload();
        }

        #endregion

        #region Research

        [HarmonyPrefix, HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.EnqueueTech))]
        public static bool PatchCompleteResearchInstantly(int techId)
        {
            if (!Config_InstantResearch.Value)
                return true; // Allow the normal behavior to proceed
            
            TechUtils.Research(LDB.techs.Select(techId));
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIResearchResultWindow), nameof(UIResearchResultWindow.SetTechId))]
        public static bool PatchRemoveResearchResultUIWindow()
        {
            if (Config_InstantResearch.Value || Config_ResearchAllTechs.Value)
                return false; // Don't run this function

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIGeneralTips), "OnTechUnlocked")]
        public static bool PatchRemoveResearchResultUIBanner()
        {
            if (Config_InstantResearch.Value || Config_ResearchAllTechs.Value)
                return false; // Don't run this function

            return true;
        }

        #endregion

        #region Inventory

        [HarmonyPostfix, HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.NotifyStorageChange))]
        public static void EnsurePlayerInventoryHasAFullStackOfEachUnlockedItem(StorageComponent __instance)
        {
            if (!Config_InventoryAlwaysContainsAllUnlockedItems.Value)
                return;
            
            if (__instance != GameMain.mainPlayer.package)
                return;

            InventoryUtils.FillPlayerInventory();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameScenarioLogic), nameof(GameScenarioLogic.NotifyOnWarpModeEnter))]
        public static void EnsureInfiniteWarpers()
        {
            if (Config_InfiniteWarpers.Value)
                InventoryUtils.FillPlayerWarpers();
        }

        #endregion

        #region Logistics

        [HarmonyPostfix, HarmonyPatch(typeof(StationComponent), nameof(StationComponent.InternalTickLocal))]
        static void PatchEnsureLogisticStationSteadyState(StationComponent __instance)
        {
            if (!Plugin.Config_EnsureLogisticStationSteadyState.Value)
                return;

            lock (__instance.storage)
            {
                for (int i = 0; i < __instance.storage.Length; i++)
                {
                    StationStore storage = __instance.storage[i];

                    if (storage.localLogic == ELogisticStorage.Demand || storage.remoteLogic == ELogisticStorage.Demand)
                    {
                        storage.count = storage.max;
                        __instance.storage[i] = storage;
                    }
                    else if (storage.localLogic == ELogisticStorage.Supply || storage.remoteLogic == ELogisticStorage.Supply)
                    {
                        storage.count = 0;
                        __instance.storage[i] = storage;
                    }
                }
            }
        }

        #endregion

        #region Energy

        [HarmonyPostfix, HarmonyPatch(typeof(Mecha), nameof(Mecha.GameTick))]
        static void PatchInfiniteMechaEnergy(Mecha __instance)
        {
            if (!Plugin.Config_InfiniteMechEnergy.Value)
                return;
            __instance.coreEnergy = __instance.coreEnergyCap;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.GameTick))]
        static void PatchInfinitePlanetEnergy(PowerSystem __instance)
        {
            if (!Plugin.Config_InfinitePlanetEnergy.Value)
                return;
            PowerSystem powerSystem = __instance;

            // The PowerNetworks are used to to determine power pole UI, and are also
            // used to determine the power state of buildings.
            int networkId = 0;
            for (int i = 0; i < powerSystem.netPool.Length; i++)
            {
                PowerNetwork network = powerSystem.netPool[i];
                if (network == null)
                    continue;
                networkId = network.id;

                network.energyCapacity = network.energyRequired;
                network.energyServed = network.energyRequired;
                network.consumerRatio = 1.0;
                network.generaterRatio = 1.0;
                powerSystem.netPool[i] = network;
            }

            // This is what actually informs buildings about the power available
            // See AssemblerComponent.InternalUpdate, which is called from FactorySystem.GameTick
            for (int i = 0; i < powerSystem.networkServes.Length; i++)
            {
                powerSystem.networkServes[i] = 1.0f;
            }

            // The entitySignPool determines which worldspace icon to display on a building
            // This is calculated as part of the PowerSystem GameTick
            // SignData.signType = 1U means no power connection (red X icon)
            // SignData.signType = 2U means no power (red flashing lightning bolt)
            // SignData.signType = 3U means low power (yellow flashing lightning bolt)
            // We don't want these displayed at all, so we set them to 0, which means no icon
            // This should allow other icons to be displayed as intended, see SignData for details about other icons
            for (int i = 0; i < powerSystem.factory.entitySignPool.Length; i++)
            {
                SignData signData = __instance.factory.entitySignPool[i];
                if (signData.signType == 1U || signData.signType == 2U || signData.signType == 3U)
                    signData.signType = 0U;
                powerSystem.factory.entitySignPool[i] = signData;
            }
        }

        #endregion

        #endregion
    }


    class InventoryUtils {
        public static List<ItemProto> UnlockedItems
        {
            get
            {
                return new List<ItemProto>(LDB.items.dataArray.Where(item => GameMain.history.ItemUnlocked(item.ID)));
            }
        }

        public static StorageComponent.GRID FullStack(ItemProto item)
        {
            StorageComponent.GRID result = new StorageComponent.GRID();
            result.itemId = item.ID;
            result.count = item.StackSize;
            result.stackSize = item.StackSize;
            return result;
        }

        public static StorageComponent.GRID[] FullInventory
        {
            get{
                StorageComponent.GRID[] result = new StorageComponent.GRID[MaxInventorySize];
                List<ItemProto> items = UnlockedItems;
                
                for (int i = 0; i < Math.Min(result.Length, items.Count); i++)
                {
                    result[i] = FullStack(items[i]);
                }

                return result;
            }
        }

        public static int MaxInventorySize
        {
            get
            {
                return 120;
            }
        }

        public static void FillPlayerInventory()
        {
            StorageComponent inventory = GameMain.mainPlayer.package;
            if (inventory.size != MaxInventorySize)
                inventory.SetSize(MaxInventorySize);
            inventory.grids = FullInventory;
        }

        public static void FillPlayerWarpers()
        {
            StorageComponent warperInventory = GameMain.mainPlayer.mecha.warpStorage;
            ItemProto warper = LDB.items.Select(1210);
            warperInventory.grids[0] = FullStack(warper);
        }
    }

    class TechUtils
    {
        public static void ResearchAllTechs()
        {
            foreach (TechProto tech in LDB.techs.dataArray)
            {
                Research(tech);
            }
        }

        public static void Research(TechProto tech)
        {
            // Don't research unpublished techs
            if (!tech.Published)
                return;

            // No need to research an already researched tech
            if (GameMain.history.techStates[tech.ID].unlocked)
                return;

            // Research each pre tech
            foreach (int id in tech.PreTechs)
                Research(LDB.techs.Select(id));

            // Make sure that the tech has been researched in the proper order
            if (!GameMain.history.CanEnqueueTech(tech.ID))
                Plugin.Logger.LogWarning($"Tried to research tech that cannot be enqueued: ${tech.name}");

            Plugin.Logger.LogInfo($"Researching {tech.name}");
            GameMain.history.UnlockTech(tech.ID);
        }
    }
}
