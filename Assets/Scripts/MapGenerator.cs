using UnityEngine;

/// <summary>
///     This script is used to generate a noise map that can be applied to another object.
///     Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y a youtube series by Sebastian Lague
/// </summary>
public class MapGenerator : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;
    public float noiseScale;

    public int octaves;

    [Range(0, 1)] public float persistance;

    public float lacunarity;

    public int seed;

    // offset that can be changed by the user
    public Vector2 offset;

    public bool autoUpdate;

    private void OnValidate()
    {
        if (mapWidth < 1)
            mapWidth = 1;
        if (mapHeight < 1)
            mapHeight = 1;
        if (lacunarity < 1)
            lacunarity = 1;
        if (octaves < 0)
            octaves = 0;
    }

    /// <summary>
    ///     Calls the <c>GenerateNoiseMap</c> method with the given parameters and applys it to a display
    /// </summary>
    public void GenerateMap()
    {
        var noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity,
            offset);

        var display = FindObjectOfType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }
}