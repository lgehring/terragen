using System;
using System.Collections.Generic;
using UnityEngine;

public class PlantInfo : MonoBehaviour
{
    public PlantData plantData;
}

[Serializable]
public class PlantData
{
    public string type;
    public int radius;
    public int seedingRadius;
    public int seedCount;
    public int maxAge;
    public int[] heightConstraints;
    public int[] slopeConstraints;
    public string modelPath;

    public PlantData(string type, int radius, int seedingRadius, int seedCount, int maxAge, int[] heightConstraints,
        int[] slopeConstraints, string modelPath)
    {
        this.type = type;
        this.radius = radius;
        this.seedingRadius = seedingRadius;
        this.seedCount = seedCount;
        this.maxAge = maxAge;
        this.heightConstraints = heightConstraints;
        this.slopeConstraints = slopeConstraints;
        this.modelPath = modelPath;
    }
    
    public static readonly Dictionary<string, PlantData> Data = new()
    {
        {
            "tree", new PlantData(
                "tree",
                8,
                20,
                5,
                5,
                new[] { 0, 1024 },
                new[] { 0, 200 },
                "Assets/polygonTrees/polygonTrees/prefabs/tree/trees/simpleTree Variant.prefab"
            )
        },
        {
            "grass", new PlantData(
                "grass",
                2,
                5,
                5,
                3,
                new[] { 0, 768 }, // 75% of 1024
                new[] { 0, 100 },
                "Assets/polygonTrees/polygonTrees/prefabs/grass/grass Variant.prefab"
            )
        }
    };

    public static PlantData Get(string queryType)
    {
        return Data[queryType];
    }
}
