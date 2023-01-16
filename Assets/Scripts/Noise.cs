using UnityEngine;
using Random = System.Random;

/// <summary>
///     A script that is going to generate a noise map. Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y
///     a youtube series by Sebastian Lague
/// </summary>
public static class Noise
{
    public enum NormalizeMode
    {
        Local,
        Global
    }

    /// <summary>
    ///     Generates a noise map using perlin noise
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
        float persistance, float lacunartiy, Vector2 offset, bool addSystemTimeToSeed, float warpingStrenght,
        NormalizeMode normalizeMode)
    {
        var noiseMap = new float[mapWidth, mapHeight];

        // adding the seed to the map
        var prng = new Random(seed);
        var octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        // infuences the largest value possible
        float amplitude = 1;
        // influnces the variance of the samples
        float frequency = 1;

        for (var i = 0; i < octaves; ++i)
        {
            // The magic numbers -100000 to 100000 seem to be the perfect interval
            var offsetX = prng.Next(-100000, 100000) + offset.x;
            var offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        // Checks if the scale is zero and clamps it if yes
        if (scale <= 0) scale = 0.0001f;

        // used for normalization
        var maxLocalNoiseHeight = float.MinValue;
        var minLocalNoiseHeight = float.MaxValue;

        // Changes so that the map zooms inside of the middle of the map for large scale values
        var halfWidth = mapWidth / 2f;
        var halfHeight = mapHeight / 2f;

        // Loop over every vertex
        for (var y = 0; y < mapHeight; ++y)
        for (var x = 0; x < mapWidth; ++x)
        {
            // infuences the largest value possible
            amplitude = 1;
            // influnces the variance of the samples
            frequency = 1;
            float noiseHeight = 0;

            // domain warping
            for (var i = 0; i < octaves; ++i)
            {
                var sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                var sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                var q = new float[2]
                {
                    Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1,
                    Mathf.PerlinNoise(sampleX + 5.2f, sampleY + 1.3f) * 2 - 1
                };

                var r = new float[2]
                {
                    Mathf.PerlinNoise(sampleX + warpingStrenght * q[0] + 1.7f,
                        sampleY + warpingStrenght * q[0] + 9.2f) * 2 - 1,
                    Mathf.PerlinNoise(sampleX + warpingStrenght * q[0] + 8.3f,
                        sampleX + warpingStrenght * q[0] + 2.8f) * 2 - 1
                };

                noiseHeight +=
                    (Mathf.PerlinNoise(sampleX + warpingStrenght * r[0], sampleY + warpingStrenght * r[1]) * 2 - 1) *
                    amplitude;

                amplitude *= persistance;

                frequency *= lacunartiy;
            }

            // Getting the max and min noise height for normalization later on
            if (noiseHeight > maxLocalNoiseHeight)
                maxLocalNoiseHeight = noiseHeight;
            else if (noiseHeight < minLocalNoiseHeight)
                minLocalNoiseHeight = noiseHeight;
            noiseMap[x, y] = noiseHeight;
        }

        // normilization 
        for (var y = 0; y < mapHeight; ++y)
        for (var x = 0; x < mapWidth; ++x)
            if (normalizeMode == NormalizeMode.Local)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
            }
            else
            {
                var normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
                noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
            }

        return noiseMap;
    }
}