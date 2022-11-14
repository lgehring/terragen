using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script is used to generate a noise map that can be applied to another object.
/// Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y a youtube series by Sebastian Lague
/// </summary>
public class MapGenerator : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;
    public float noiseScale;

    public int octaves;
    public float persistance;
    public float lacunarity;

    public int seed;
    // offset that can be changed by the user
    public Vector2 offset;

    public bool autoUpdate;

    /// <summary>
    /// Calls the <c>GenerateNoiseMap</c> method with the given parameters and applys it to a display
    /// </summary>
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }
}
