using System;
using System.Collections.Generic;

namespace Ecosystem
{
    [Serializable]
    public class PlantData
    {
        public static readonly Dictionary<string, PlantData> Data = new()
        {
            {
                "tree", new PlantData(
                    "tree",
                    8,
                    9,
                    20,
                    2,
                    4,
                    new[] { 0, 1024 },
                    new[] { 0, 200 },
                    "Assets/polygonTrees/polygonTrees/prefabs/tree/trees/simpleTree Variant.prefab"
                )
            },
            {
                "grass", new PlantData(
                    "grass",
                    2,
                    3,
                    10,
                    4,
                    2,
                    new[] { 0, 768 }, // 75% of 1024
                    new[] { 0, 100 },
                    "Assets/polygonTrees/polygonTrees/prefabs/grass/grass Variant.prefab"
                )
            },
            {
                "bush", new PlantData(
                    "bush",
                    4,
                    5,
                    8,
                    3,
                    3,
                    new[] { 0, 1024 },
                    new[] { 0, 500 },
                    "Assets/polygonTrees/polygonTrees/prefabs/shrubs/littleLeavesShrub Variant.prefab"
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
    }
}