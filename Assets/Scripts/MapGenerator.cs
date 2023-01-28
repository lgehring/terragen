using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
///     This script is used to generate a noise map that can be applied to another object.
///     Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y a youtube series by Sebastian Lague
/// </summary>
public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        ColorMap,
        Mesh
    }

    public static int mapChunkSize = 200;

    public static string mapFolder = "Assets/Heightmaps/langenargen/";

    public DrawMode drawMode;

    public Noise.NormalizeMode normalizeMode;

    [Range(0, 6)] public int editorPreviewLOD;

    public float noiseScale;

    public int octaves;

    [Range(0, 1)] public float persistance;

    public float lacunarity;

    public int seed;

    public bool addSystemTimeToSeed;

    // offset that can be changed by the user
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    public float warpingStrength;

    private readonly Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new();
    private readonly Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new();

    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
            for (var i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                var threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }

        if (meshDataThreadInfoQueue.Count > 0)
            for (var i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                var threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
    }

    private void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;
        if (octaves < 0)
            octaves = 0;
    }

    public void DrawMapInEditor()
    {
        var mapData = GenerateMapData(Vector2.zero);

        var display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.ColorMap)
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Mesh)
            display.DrawMesh(
                MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve,
                    editorPreviewLOD),
                TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate { MapDataThread(centre, callback); };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        var mapData = GenerateMapData(centre);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(mapData, lod, callback); };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        var meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    /// <summary>
    ///     Calls the <c>GenerateNoiseMap</c> method with the given parameters and applys it to a display
    /// </summary>
    private MapData GenerateMapData(Vector2 centre)
    {
        var noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance,
            lacunarity,
            centre + offset, addSystemTimeToSeed, warpingStrength, normalizeMode);
        noiseMap = NoiseMapReader.ReadNoiseMap(mapFolder + "heightmap.png", mapChunkSize, true);

        var colorMap = new Color[mapChunkSize * mapChunkSize];
        for (var y = 0; y < mapChunkSize; y++)
        for (var x = 0; x < mapChunkSize; x++)
        {
            var currentHeight = noiseMap[x, y];
            for (var i = 0; i < regions.Length; i++)
                if (currentHeight >= regions[i].height)
                    colorMap[y * mapChunkSize + x] = regions[i].color;
                else
                    break;
        }

        return new MapData(noiseMap, colorMap);
    }

    private struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}