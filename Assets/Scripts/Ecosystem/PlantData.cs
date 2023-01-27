using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace Ecosystem
{
    [Serializable]
    public class PlantData
    {
        public static string[][] variations =
        {
            new[] { "tree_0", "tree_1", "tree_2" },
            new[] { "evergreen_0", "evergreen_1", "evergreen_2" },
            new[] { "grass_0", "grass_1" },
            new[] { "bush_0", "bush_1" }
        };

        public static readonly Dictionary<string, PlantData> Data = new()
        {
            {
                "tree_0", new PlantData(
                    "tree_0",
                    8,
                    7,
                    20,
                    2,
                    6,
                    new[] { 0, 1024 },
                    new[] { 0, 200 },
                    "Assets/BrokenVector/LowPolyTreePack/Prefabs/Tree Type0 05.prefab"
                )
            },
            {
                "tree_1", new PlantData(
                    "tree_1",
                    8,
                    7,
                    20,
                    2,
                    6,
                    new[] { 0, 1024 },
                    new[] { 0, 200 },
                    "Assets/BrokenVector/LowPolyTreePack/Prefabs/Tree Type0 06.prefab"
                )
            },
            {
                "tree_2", new PlantData(
                    "tree_2",
                    6,
                    7,
                    25,
                    2,
                    4,
                    new[] { 0, 1000 },
                    new[] { 0, 150 },
                    "Assets/BrokenVector/LowPolyTreePack/Prefabs/Tree Type4 03.prefab"
                )
            },
            {
                "evergreen_0", new PlantData(
                    "evergreen_0",
                    6,
                    6,
                    15,
                    2,
                    10,
                    new[] { 0, 1024 },
                    new[] { 0, 500 },
                    "Assets/BrokenVector/LowPolyTreePack/Prefabs/Tree Type1 04.prefab"
                )
            },
            {
                "evergreen_1", new PlantData(
                    "evergreen_1",
                    7,
                    7,
                    15,
                    2,
                    10,
                    new[] { 0, 1024 },
                    new[] { 0, 500 },
                    "Assets/BrokenVector/LowPolyTreePack/Prefabs/Tree Type2 03.prefab"
                )
            },
            {
                "evergreen_2", new PlantData(
                    "evergreen_2",
                    6,
                    6,
                    15,
                    2,
                    10,
                    new[] { 0, 1024 },
                    new[] { 0, 500 },
                    "Assets/BrokenVector/LowPolyTreePack/Prefabs/Tree Type2 05.prefab"
                )
            },
            {
                "grass_0", new PlantData(
                    "grass_0",
                    3,
                    3,
                    50,
                    4,
                    3,
                    new[] { 0, 768 }, // 75% of 1024
                    new[] { 0, 100 },
                    "Assets/polygonTrees/polygonTrees/prefabs/grass/grass Variant.prefab"
                )
            },
            {
                "grass_1", new PlantData(
                    "grass_1",
                    3,
                    3,
                    50,
                    3,
                    4,
                    new[] { 0, 768 }, // 75% of 1024
                    new[] { 0, 100 },
                    "Assets/polygonTrees/polygonTrees/prefabs/grass/littleGrass Variant.prefab"
                )
            },
            {
                "bush_0", new PlantData(
                    "bush_0",
                    4,
                    4,
                    8,
                    3,
                    5,
                    new[] { 0, 1024 },
                    new[] { 0, 500 },
                    "Assets/BrokenVector/LowPolyTreePack/Prefabs/Tree Type0 01.prefab"
                )
            },
            {
                "bush_1", new PlantData(
                    "bush_1",
                    4,
                    4,
                    8,
                    3,
                    5,
                    new[] { 0, 1024 },
                    new[] { 0, 500 },
                    "Assets/BrokenVector/LowPolyTreePack/Prefabs/Tree Type0 04.prefab"
                )
            }
        };

        public string type;
        public int sizeRadius;
        public int collisionRadius;
        public int seedingRadius;
        public int seedCount;
        public int maxAge;
        public int[] heightConstraints;
        public int[] slopeConstraints;
        public string modelPath;

        public PlantData(string type, int sizeRadius, int collisionRadius, int seedingRadius, int seedCount, int maxAge,
            int[] heightConstraints,
            int[] slopeConstraints, string modelPath)
        {
            this.type = type;
            this.sizeRadius = sizeRadius;
            this.collisionRadius = collisionRadius;
            this.seedingRadius = seedingRadius;
            this.seedCount = seedCount;
            this.maxAge = maxAge;
            this.heightConstraints = heightConstraints;
            this.slopeConstraints = slopeConstraints;
            this.modelPath = modelPath;
        }

        public static PlantData Get(string queryType)
        {
            return Data[queryType];
        }

        public static string GetVariation(string input)
        {
            var index = Array.IndexOf(variations, variations.FirstOrDefault(x => x.Contains(input)));
            if (index == -1)
                // not found
                return input;

            var randomIndex = Random.Range(0, variations[index].Length);
            return variations[index][randomIndex];
        }
    }
}