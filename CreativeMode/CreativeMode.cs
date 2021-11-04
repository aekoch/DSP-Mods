using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
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
        public static new BepInEx.Configuration.ConfigFile Config;

        #region Lifecycle

        private void Awake()
        {
            Logger = base.Logger;
            Config = base.Config;
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
            harmony.PatchAll(typeof(PatchNotifyStorageChange));
            harmony.PatchAll(typeof(PatchGameMainBegin));
            harmony.PatchAll(typeof(PatchStationComponentTakeItem));
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

        #endregion

        #region Config

        private static void ReloadConfig()
        {
            Logger.LogInfo("Reloading config");
            Config.Reload();
            foreach (ConfigDefinition key in Config.Keys)
            {
                ConfigEntryBase entry = Config[key];
                Logger.LogInfo($"{key.Key}={entry.BoxedValue.ToString()}");
            }
        }

        private void BindConfigs()
        {
            BindLogisticsConfigs();
            BindResearchConfigs();
        }

        #region Logistics
        public static ConfigEntry<bool> DemandedItemsAlwaysMax;
        public static ConfigEntry<bool> SuppliedItemsAlwaysZero;

        private void BindLogisticsConfigs()
        {
            string section = "Logistics Stations";
            
            Config.Bind(section, "DemandedItemsAlwaysMax", false, 
                "If true, any item demanded by a logistics station will be pinned to its maximum value. " +
                "This is useful when designing large blueprints, so your production will always run at a steady state.");
            
            Config.Bind(section, "SuppliedItemsAlwaysMin", false,
                "If true, any item supplied by a logistics station will be pinned to zero. " +
                "This is useful when designing large blueprints, so your production will always run at a steady state.");
        }

        #endregion

        #region Research

        public static ConfigEntry<ETechLevel> TechLevel;
        public static ConfigEntry<int> RepeatableTechCount;

        private void BindResearchConfigs()
        {
            string section = "Research";
            
            TechLevel = Config.Bind<ETechLevel>(section, "TechLevel", ETechLevel.WHITE,
                "Choose what techs you want researched. For example, if you choose yellow, " +
                "then all techs that require yellow cubes or less will be researched, " +
                "but no techs that require purple cubes or more.");

            RepeatableTechCount = Config.Bind<int>(section, "RepeatableTechCount", 8,
                "Choose how many of each repeatable tech you want researched. " +
                "Note: This will only take effect if TechLevel is WHITE, because " +
                "all repeatable techs require WHITE cubes.");
        }

        #endregion

        #endregion

        #region Utils

        public static void RefreshPlayerInventory()
        {
            GameMain.mainPlayer.package.NotifyStorageChange();
        }

        public static void FillPlayerEnergy()
        {
            GameMain.mainPlayer.mecha.coreEnergy = GameMain.mainPlayer.mecha.coreEnergyCap;
        }

        #endregion

        #region Patches

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Pause))]
        public static void ReloadConfigOnPause()
        {
            Logger.LogInfo("Game is paused");
            ReloadConfig();
            TechUtils.ResearchAllTechsOfLevel(Plugin.TechLevel.Value);
        }


        #endregion
    }

    [HarmonyPatch(typeof(StorageComponent), "NotifyStorageChange")]
    class PatchNotifyStorageChange
    {
        static void Postfix(ref StorageComponent __instance)
        {
            if (__instance == GameMain.mainPlayer.package) {
                Plugin.Logger.LogInfo("Refreshing player inventory");
                if (__instance.size != fullInventorySize) {
                    __instance.SetSize(fullInventorySize);
                }
                __instance.grids = fullInventory;
            }
        }

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
                    if (GameMain.history.ItemUnlocked(item.ID))
                    {
                        StorageComponent.GRID slot = result[storageIndex];
                        slot.itemId = item.ID;
                        slot.stackSize = item.StackSize;
                        slot.count = item.StackSize;
                        result[storageIndex] = slot;
                        storageIndex++;
                    }
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
    class PatchGameMainBegin
    {
        static void Postfix()
        {
            if (DSPGame.IsMenuDemo)
            {
                return;  // We are on the main menu
            }

            TechUtils.ResearchAllTechsOfLevel(Plugin.TechLevel.Value);
            initPlayerInventory();
            initPlayerEnergy();
        }

        static void initPlayerInventory()
        {
            GameMain.mainPlayer.package.NotifyStorageChange();
        }

        static void initPlayerEnergy()
        {
            GameMain.mainPlayer.mecha.coreEnergy = GameMain.mainPlayer.mecha.coreEnergyCap;
        }
    }

    [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.InternalTickLocal))]
    class PatchStationComponentTakeItem
    {
        static void Postfix(StationComponent __instance)
        {
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
                    else
                    {
                        // No adjustment necessary
                    }
                }
            }
        }
    }

    public enum ETechLevel
    {
        NONE,
        ITEM,
        BLUE,
        RED,
        YELLOW,
        PURPLE,
        GREEN,
        WHITE,
    }

    public class TechUtils
    {
        public const int WHITE_MATRIX_ID = 6006;
        public const int GREEN_MATRIX_ID = 6005;
        public const int PURPLE_MATRIX_ID = 6004;
        public const int YELLOW_MATRIX_ID = 6003;
        public const int RED_MATRIX_ID = 6002;
        public const int BLUE_MATRIX_ID = 6001;

        public static ETechLevel TechLevel(TechProto tech)
        {
            HashSet<int> requiredItemIds = new HashSet<int>(tech.unlockNeedItemArray.Select(x => x.id));

            if (requiredItemIds.Contains(WHITE_MATRIX_ID))
                return ETechLevel.WHITE;
            else if (requiredItemIds.Contains(GREEN_MATRIX_ID))
                return ETechLevel.GREEN;
            else if (requiredItemIds.Contains(PURPLE_MATRIX_ID))
                return ETechLevel.PURPLE;
            else if (requiredItemIds.Contains(YELLOW_MATRIX_ID))
                return ETechLevel.YELLOW;
            else if (requiredItemIds.Contains(RED_MATRIX_ID))
                return ETechLevel.RED;
            else if (requiredItemIds.Contains(BLUE_MATRIX_ID))
                return ETechLevel.BLUE;
            else if (requiredItemIds.Count > 0)
                return ETechLevel.ITEM;
            else
                return ETechLevel.NONE;
        }

        public static List<TechProto> TechsOfLevel(ETechLevel level)
        {
            return new List<TechProto>(LDB.techs.dataArray.Where(tech => TechLevel(tech) == level));
        }

        public static List<TechProto> TechsOfLevelOrBelow(ETechLevel targetLevel)
        {
            List<TechProto> result = new List<TechProto>();
            foreach (ETechLevel level in Enum.GetValues(typeof(ETechLevel)))
            {
                if (level <= targetLevel)
                {
                    result.AddRange(TechsOfLevel(level));
                }
            }
            return result;
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
            if (!GameMain.history.CanEnqueueTech(tech.ID)) {
                Plugin.Logger.LogWarning($"Tried to research tech that cannot be enqueued: ${tech.name}");
            }

            Plugin.Logger.LogInfo($"Researching {tech.name}");
            GameMain.history.UnlockTech(tech.ID);
        }

        public static void ResearchAllTechs()
        {
            foreach (TechProto tech in LDB.techs.dataArray)
                Research(tech);
        }

        public static void ResearchAllTechsOfLevel(ETechLevel level)
        {
            Plugin.Logger.LogInfo($"Set Research Tech Level to {level}");
            UnresearchAll();
            foreach (TechProto tech in TechsOfLevelOrBelow(level))
            {
                Research(tech);
            }
        }

        public static void UnresearchAll()
        {
            foreach (int key in new List<int>(GameMain.history.techStates.Keys))
            {
                TechState state = GameMain.history.techStates[key];
                state.unlocked = false;
                state.hashUploaded = 0;
                GameMain.history.techStates[key] = state;
            }
            ResetAllTechInfluencedValuestoDefaults();
        }

        private static void ResetAllTechInfluencedValuestoDefaults()
        {
            ModeConfig defaults = (ModeConfig) UnityEngine.ScriptableObject.CreateInstance("ModeConfig");

            // Mecha
            /* The order of these fields is the same as in the Mode Config class
             * Some of these are definitely modified by tech, some of them probably aren't,
             * but I don't know which aren't, so I reset them all.
             * This may affect compatibility with other mods.
             */ 
            Mecha mecha = GameMain.mainPlayer.mecha;
            mecha.coreLevel = defaults.mechaCoreLevel;
            mecha.thrusterLevel = defaults.mechaThrusterLevel;
            mecha.coreEnergyCap = defaults.mechaCoreEnergyCap;
            mecha.coreEnergy = mecha.coreEnergyCap;
            mecha.corePowerGen = defaults.mechaCorePowerGen;
            mecha.reactorPowerGen = defaults.mechaReactorPowerGen;
            mecha.walkPower = defaults.mechaWalkPower;
            mecha.jumpEnergy = defaults.mechaJumpEnergy;
            mecha.jumpSpeed = defaults.mechaJumpSpeed;
            mecha.thrustPowerPerAcc = defaults.mechaThrustPowerPerAcc;
            mecha.warpKeepingPowerPerSpeed = defaults.mechaWarpKeepingPowerPerSpeed;
            mecha.warpStartPowerPerSpeed = defaults.mechaWarpStartPowerPerSpeed;
            mecha.miningPower = defaults.mechaMiningPower;
            mecha.miningSpeed = defaults.mechaMiningSpeed;
            mecha.replicatePower = defaults.mechaReplicatePower;
            mecha.replicateSpeed = defaults.mechaReplicateSpeed;
            mecha.researchPower = defaults.mechaResearchPower;
            mecha.droneCount = defaults.mechaDroneCount;
            mecha.droneSpeed = defaults.mechaDroneSpeed;
            mecha.droneMovement = defaults.mechaDroneMovement;
            mecha.droneEjectEnergy = defaults.mechaDroneEjectEnergy;
            mecha.droneEnergyPerMeter = defaults.mechaDroneEnergyPerMeter;
            mecha.buildArea = defaults.mechaBuildArea;
            mecha.walkSpeed = defaults.mechaWalkSpeed;
            mecha.jumpSpeed = defaults.mechaJumpSpeed;
            mecha.maxSailSpeed = defaults.mechaSailSpeedMax;
            mecha.maxWarpSpeed = defaults.mechaWarpSpeedMax;

            // History
            GameHistoryData history = GameMain.history;
            history.blueprintLimit = defaults.blueprintLimit;
            history.universeMatrixPointUploaded = defaults.universeObserveLevel;
            history.universeObserveLevel = defaults.universeObserveLevel;
            history.techSpeed = defaults.techSpeed;
            
            // Logistic Drone
            history.logisticDroneCarries = defaults.logisticDroneCarries;
            history.logisticDroneSpeed = defaults.logisticDroneSpeed;

            // Logistic Ship
            history.logisticShipCarries = defaults.logisticShipCarries;
            history.logisticShipSailSpeed = defaults.logisticShipSailSpeed;
            history.logisticShipWarpDrive = defaults.logisticShipWarpDrive;
            history.logisticShipWarpSpeed = defaults.logisticShipWarpSpeed;

            // Mining
            history.miningCostRate = defaults.miningCostRate;
            history.miningSpeedScale = defaults.miningSpeedScale;

            // Solar sails
            history.solarSailLife = defaults.solarSailLife;
            history.solarEnergyLossRate = defaults.solarEnergyLossRate;

            // Inserters
            history.inserterStackCount = defaults.inserterStackCount;

            // Reset default recipes
            if (defaults.recipes != null)
            {
                foreach (int recipeId in defaults.recipes)
                    Plugin.Logger.LogInfo($"Defuault recipe: {recipeId}");

                history.recipeUnlocked.Clear();
                history.recipeUnlocked = new HashSet<int>(defaults.recipes);
            }

        }
    }
}
