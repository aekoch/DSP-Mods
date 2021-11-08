using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;


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

        #region Lifecycle
        private void Awake()
        {
            logger = Logger;

            // Plugin startup logic
            Logger.LogInfo($"Plugin {GUID} is loaded!");

            // Load hooks
            harmony = new Harmony(GUID);
            Patch();
            LoadYourResources();

            Logger.LogInfo(SphericalCoordinate.FromDegrees(1, 0, 0));
            Logger.LogInfo(SphericalCoordinate.FromCartesian(new Vector3(1, 1, 0)));
        }
        
        private void Update()
        {
            if (DSPGame.IsMenuDemo)
                return;

            if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                logger.LogInfo("Keypad1");
                PlanetFactory factory = GameMain.mainPlayer.factory;
                foreach (EntityData data in factory.entityPool)
                {
                    if (data.beltId != 0)
                    {
                        logger.LogInfo(data.beltId);
                    }
                }
            }
        }

        private void Start()
        {
            // RecipeUtils.PrintDescription(RecipeUtils.AssemblerComponentRecipes);
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
        #endregion

        #region Patches

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Pause))]
        public static void PatchGamePause()
        {
            RemoveAllBuildingsFromFactory(GameMain.mainPlayer.factory);
            
            BuildingUtils.PlaceBuilding(
                GameMain.mainPlayer.factory,
                building: Buildings.ASSEMBLER_1,
                coordinate: new Vector2Int(0, 0)
                );

            BuildingUtils.PlaceBeltLine(
                GameMain.mainPlayer.factory,
                belt: Buildings.BELT_1,
                from: new Vector2Int(0, 5),
                to: new Vector2Int(10, 5)
                );
        }

        public static void PatchGameTick()
        {

        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.AddPrebuildData))]
        public static void ListenToPrebuildData(PrebuildData prebuild)
        {
            PrintDescription(prebuild);
        }

        #endregion

        private static void RemoveAllBuildingsFromFactory(PlanetFactory factory)
        {
            if (factory == null)
                return;

            logger.LogInfo($"Planet Segments={factory.planet.aux.mainGrid.segment}");
            for (int i = 0; i < factory.entityPool.Length; i++)
            {
                factory.RemoveEntityWithComponents(i);
            }
        }

        private static void PrintDescription(PrebuildData data) {
            Plugin.logger.LogInfo("--------------------------------------------------------------");
            SphericalCoordinate spherical = SphericalCoordinate.FromCartesian(data.pos);
            Vector2Int discrete = GridUtils.SphericalToDiscrete(GameMain.mainPlayer.planetData, spherical);
            // Plugin.logger.LogInfo($"Position: {data.pos} - Spherical: {spherical} - Discrete: {discrete}");
            
            SphericalCoordinate spherical2 = GridUtils.DiscreteToSpherical(GameMain.mainPlayer.planetData, discrete);
            Vector3 cartesian = spherical2.ToCartesian();
            // Plugin.logger.LogInfo($"Position: {cartesian} - Spherical: {spherical2} - Discrete: {discrete}");

            // Plugin.logger.LogInfo($"Rotation: {data.rot.eulerAngles} - Rotation2: {data.rot.eulerAngles}");

            if (data.protoId != 0)
            {
                ItemProto building = LDB.items.Select(data.protoId);
                Plugin.logger.LogInfo($"Building: {building.name}");
            }
            if (data.itemRequired != 0) 
            {
                ItemProto item = LDB.items.Select(data.itemRequired);
                Plugin.logger.LogInfo($"Item: {item.name}");
            }
            if (data.recipeId != 0)
            {
                RecipeProto recipe = LDB.recipes.Select(data.recipeId);
                Plugin.logger.LogInfo($"Recipe: {recipe.name}");
            }
            if (data.filterId != 0)
            {
                ItemProto item = LDB.items.Select(data.filterId);
                Plugin.logger.LogInfo($"Filter: {item.name}");
            }
            if (data.parameters != null && data.parameters.Length > 1)
            {
                Plugin.logger.LogInfo($"Parameters: {data.parameters}");
            }
            if (data.pickOffset != 0)
            {
                Plugin.logger.LogInfo($"Pick Offset: {data.pickOffset}");
            }
            if (data.insertOffset != 0)
            {
                Plugin.logger.LogInfo($"Insert Offset: {data.insertOffset}");
            }

        }
    }


    struct SphericalCoordinate
    {
        public float r;
        public float theta; // -180 to +180 // Also known as longitude or inclination
        public float phi;   // -90 to +90   // Also known as latitude or azimuth

        public SphericalCoordinate(float r, float theta, float phi)
        {
            this.r = r;
            this.theta = theta;
            this.phi = phi;
        }

        public float latitude
        {
            get
            {
                return phi;
            }
        }

        public float longitude
        {
            get
            {
                return theta;
            }
        }

        override public string ToString()
        {
            return $"({r}, {Math.Round(Mathf.Rad2Deg * theta, 1)}, {Math.Round(Mathf.Rad2Deg * phi, 1)})";
        }

        public Vector3 ToCartesian()
        {
            float a = r * Mathf.Cos(phi);
            
            float x = a * Mathf.Cos(theta);
            float y = r * Mathf.Sin(phi);
            float z = a * Mathf.Sin(theta);

            return new Vector3(x, y, z);
        }

        public static SphericalCoordinate FromCartesian(Vector3 pos)
        {

            if (pos.x == 0)
                pos.x = Mathf.Epsilon;
            
            float r = Mathf.Sqrt((pos.x * pos.x) + (pos.y * pos.y) + (pos.z * pos.z));
            
            float theta = Mathf.Atan(pos.z / pos.x);
            if (pos.x < 0)
                theta += Mathf.PI;
            
            float phi = Mathf.Asin(pos.y / r);
            
            return new SphericalCoordinate(r, theta, phi);
        }

        public static SphericalCoordinate FromDegrees(float r, float thetaDegrees, float phiDegrees)
        {
            return new SphericalCoordinate(r, Mathf.Deg2Rad * thetaDegrees, Mathf.Deg2Rad * phiDegrees);
        }
    }


    class GridUtils
    {
        public static Vector2Int SphericalToDiscrete(PlanetData planet, SphericalCoordinate coordinate)
        {
            return LatLongToDiscrete(planet, coordinate.latitude, coordinate.longitude);
        }

        public static Vector2Int LatLongToDiscrete(PlanetData planet, float latitude, float longitude)
        {
            // This is the "percentage" between the equator and the poles.
            // -1 = at south pole | 0 = at equator | 1 = at north pole
            float f_lat = latitude / (2 * Mathf.PI);
            
            int y = Mathf.RoundToInt(f_lat * planet.aux.mainGrid.segment * 5f);

            // Note: This is not exactly correct at the boundaries between tropic lines.  I'm not sure why
            int segmentCount = PlanetGrid.DetermineLongitudeSegmentCount(y/5, planet.aux.mainGrid.segment);

            float f_long = longitude / (2 * Mathf.PI);

            int x = Mathf.RoundToInt(f_long * segmentCount * 5f);

            return new Vector2Int(x, y);
        }

        public static SphericalCoordinate DiscreteToSpherical(PlanetData planet, Vector2Int discrete)
        {
            int x = discrete.x;
            int y = discrete.y;

            int totalSteps = planet.aux.mainGrid.segment * 5;

            float phi = ((float)y / totalSteps) * (2 * Mathf.PI);

            int stepsAtPhi = PlanetGrid.DetermineLongitudeSegmentCount(y / 5, planet.aux.mainGrid.segment) * 5;

            float theta = ((float)x / stepsAtPhi) * (2 * Mathf.PI);

            return new SphericalCoordinate(planet.radius, theta, phi);
        }

        public static Vector3 DiscreteToVector3(PlanetData planet, Vector2Int discrete)
        {
            SphericalCoordinate spherical = DiscreteToSpherical(planet, discrete);
            return planet.aux.mainGrid.SnapTo(spherical.ToCartesian()) * (1.001f * planet.radius);
        }
    }

    class Buildings
    {
        public static ItemProto ASSEMBLER_1 = LDB.items.Select(2303);
        public static ItemProto ASSEMBLER_2 = LDB.items.Select(2304);
        public static ItemProto ASSEMBLER_3 = LDB.items.Select(2305);

        public static ItemProto BELT_1 = LDB.items.Select(2001);
        public static ItemProto BELT_2 = LDB.items.Select(2002);
        public static ItemProto BELT_3 = LDB.items.Select(2003);

        public static ItemProto SORTER_1 = LDB.items.Select(2011);
        public static ItemProto SORTER_2 = LDB.items.Select(2012);
        public static ItemProto SORTER_3 = LDB.items.Select(2013);

        public static ItemProto TESLA_TOWER = LDB.items.Select(2201);
    }

    class BuildingUtils
    {
        public static void PlaceBuilding(PlanetFactory factory, ItemProto building, Vector2Int coordinate, RecipeProto recipe = null)
        {
            Plugin.logger.LogInfo($"Placing {building.name} at {coordinate} or {GridUtils.DiscreteToVector3(factory.planet, coordinate)}");
            PrebuildData prebuildData = new PrebuildData();
            prebuildData.protoId = (short) building.ID;
            prebuildData.modelIndex = (short) building.ModelIndex;
            prebuildData.pos = GridUtils.DiscreteToVector3(factory.planet, coordinate);
            prebuildData.pos2 = GridUtils.DiscreteToVector3(factory.planet, coordinate);
            prebuildData.rot = Maths.SphericalRotation(prebuildData.pos, 0.0f);
            prebuildData.recipeId = recipe != null ? recipe.ID : 0;

            int prebuildId = factory.AddPrebuildDataWithComponents(prebuildData);

            factory.BuildFinally(GameMain.mainPlayer, prebuildId);
        }

        public static void PlaceBeltLine(PlanetFactory factory, ItemProto belt, Vector2Int from, Vector2Int to)
        {
            if (!((from.x == to.x) || (from.y == to.y)))
            {
                Debug.LogWarning($"From {from} to {to} is not a straight line.");
            }
            else
            {
                List<Vector2Int> coordinates = new List<Vector2Int>();
                if (from.x == to.x) { 
                    for (int y = from.y; y <= to.y; y++)
                    {
                        coordinates.Add(new Vector2Int(from.x, y));
                    }
                }
                else
                {
                    for (int x = from.x; x <= to.x; x++)
                    {
                        coordinates.Add(new Vector2Int(x, from.y));
                    }
                }

                foreach (Vector2Int coordinate in coordinates)
                {
                    PlaceBuilding(factory, belt, coordinate);
                }
            }

        }

        public static void PlaceSorter(PlanetFactory factory, ItemProto sorter, Vector2Int from, Vector2Int to, ItemProto filterItem = null)
        {

        }
    }

    class RecipeUtils
    {
        public static List<RecipeProto> AssemblerAllRecipes
        {
            get
            {
                return new List<RecipeProto>(LDB.recipes.dataArray.Where(recipe => recipe.Type == ERecipeType.Assemble));
            }
        }

        public static List<RecipeProto> AssemblerBuildingRecipes
        {
            get
            {
                List<RecipeProto> result = new List<RecipeProto>();
                foreach (RecipeProto recipe in AssemblerAllRecipes)
                {
                    foreach (int itemId in recipe.Results)
                    {
                        ItemProto item = LDB.items.Select(itemId);
                        if (item.Type == EItemType.Logistics || item.Type == EItemType.Production)
                        {
                            result.Add(recipe);
                        }
                    }
                }
                return result;
            }
        }

        public static List<RecipeProto> AssemblerComponentRecipes
        {
            get
            {
                List<RecipeProto> result = new List<RecipeProto>();
                foreach (RecipeProto recipe in AssemblerAllRecipes)
                {
                    foreach (int itemId in recipe.Results)
                    {
                        ItemProto item = LDB.items.Select(itemId);
                        if (item.Type == EItemType.Component || item.Type == EItemType.Product)
                        {
                            result.Add(recipe);
                        }
                    }
                }
                return result;
            }
        }

        public static List<RecipeProto> SmelterRecipes
        {
            get
            {
                return new List<RecipeProto>(LDB.recipes.dataArray.Where(recipe => recipe.Type == ERecipeType.Smelt));
            }
        }

        public static void PrintDescription(RecipeProto recipe)
        {
            Plugin.logger.LogInfo($"Recipe: {recipe.name}");
            
            Plugin.logger.LogInfo($"  Inputs:");
            for(int i = 0; i < recipe.Items.Length; i++)
            {
                ItemProto item = LDB.items.Select(recipe.Items[i]);
                int count = recipe.ItemCounts[i];

                Plugin.logger.LogInfo($"    {count}x - {item.name}");
            }
            
            Plugin.logger.LogInfo($"  Outputs:");
            for (int i = 0; i < recipe.Results.Length; i++)
            {
                ItemProto item = LDB.items.Select(recipe.Results[i]);
                int count = recipe.ResultCounts[i];

                Plugin.logger.LogInfo($"    {count}x - {item.name}");
            }
        }

        public static void PrintDescription(List<RecipeProto> recipes)
        {
            foreach (RecipeProto recipe in recipes)
            {
                Plugin.logger.LogInfo("----------------------------------------------------------");
                PrintDescription(recipe);
               
            }
        }
    }
}
