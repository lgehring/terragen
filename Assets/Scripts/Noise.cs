using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A script that is going to generate a noise map. Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y
/// a youtube series by Sebastian Lague
/// </summary>
public static class Noise
{
    /// <summary>
    /// Generates a noise map using perlin noise
    /// </summary>
    /// <param name="mapWidth"> The width of the map</param>
    /// <param name="mapHeight"> The height of the map</param>
    /// <param name="scale"> The scale of the noise</param>
    /// <returns> A noise map using perlin noise</returns>
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Checks if the scale is zero and clamps it if yes
        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        // Loop over every vertex
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // Create a sample for perlin noise
                float sampleX = x / scale;
                float sampleY = y / scale;

                // Perlin noise generation and save
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                noiseMap[x, y] = perlinValue;
            }
        }

        return noiseMap;
    }
}
