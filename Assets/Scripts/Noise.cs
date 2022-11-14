using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
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
    /// <param name="seed"> Variable to generate the same map again</param>
    /// <param name="scale"> The scale of the noise</param>
    /// <param name="octaves"> Determins how much impact the persistance and lacunarity have: 1 for no influence</param>
    /// <param name="persistance"> Number between 0 and 1 which influnces the amplitued</param>
    /// <param name="lacunartiy"> Number greater than 1 which influnces the frequency</param>
    /// <param name="offset"> An extra offset applied by the user</param>
    /// <returns> A noise map using perlin noise</returns>
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves,
                                            float persistance, float lacunartiy, Vector2 offset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // adding the seed to the map
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; ++i)
        {
            // The magic numbers -100000 to 100000 seem to be the perfect interval
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        // Checks if the scale is zero and clamps it if yes
        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        // used for normalization
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // Changes so that the map zooms inside of the middle of the map for large scale values
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        // Loop over every vertex
        for (int y = 0; y < mapHeight; ++y)
        {
            for (int x = 0; x < mapWidth; ++x)
            {
                // infuences the largest value possible
                float amplitued = 1;
                // influnces the variance of the samples
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; ++i)
                {
                    // Create a sample for perlin noise
                    float sampleX = (x-halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y-halfHeight) / scale * frequency + octaveOffsets[i].y;

                    // Perlin noise generation
                    // Generating values between -1 and 1
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

                    noiseHeight += perlinValue * amplitued;

                    amplitued *= persistance;
                    frequency *= lacunartiy;
                }

                // Getting the max and min noise height for normalization later on
                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }

        // normilization 
        for (int y = 0; y < mapHeight; ++y)
        {
            for (int x = 0; x < mapWidth; ++x)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }
}
