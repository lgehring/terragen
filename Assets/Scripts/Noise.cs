using UnityEditor.PackageManager.UI;
using UnityEngine;
using Random = System.Random;

/// <summary>
///     A script that is going to generate a noise map. Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y
///     a youtube series by Sebastian Lague
/// </summary>
public static class Noise
{
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
        float persistance, float lacunartiy, Vector2 offset, bool addSystemTimeToSeed, float warpingStrenght)
    {
        var noiseMap = new float[mapWidth, mapHeight];

        // adding the seed to the map
        var prng = new Random(seed);
        var octaveOffsets = new Vector2[octaves];
        for (var i = 0; i < octaves; ++i)
        {
            // The magic numbers -100000 to 100000 seem to be the perfect interval
            var offsetX = prng.Next(-100000, 100000) + offset.x;
            var offsetY = prng.Next(-100000, 100000) + offset.y;

            if (addSystemTimeToSeed)
            {
                offsetX *= Mathf.Sin((float) Time.unscaledTimeAsDouble/100000.0f);
                offsetY /= Mathf.Cos((float) Time.unscaledTimeAsDouble/100000.0f);
            }
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        // Checks if the scale is zero and clamps it if yes
        if (scale <= 0) scale = 0.0001f;

        // used for normalization
        var maxNoiseHeight = float.MinValue;
        var minNoiseHeight = float.MaxValue;

        // Changes so that the map zooms inside of the middle of the map for large scale values
        var halfWidth = mapWidth / 2f;
        var halfHeight = mapHeight / 2f;

        // Loop over every vertex
        for (var y = 0; y < mapHeight; ++y)
        for (var x = 0; x < mapWidth; ++x)
        {
            // infuences the largest value possible
            float amplitued = 1;
            // influnces the variance of the samples
            float frequency = 1;
            float noiseHeight = 0;

                // domain warping
            for (var i = 0; i < octaves; ++i)
            {
                var sampleX = (x - halfWidth)/scale * frequency + octaveOffsets[i].x;
                var sampleY = (y - halfHeight)/scale * frequency + octaveOffsets[i].y;

                float[] q = new float[2] {Mathf.PerlinNoise(sampleX, sampleY),Mathf.PerlinNoise(sampleX+5.2f,sampleY+1.3f)};

                float[] r = new float[2] {Mathf.PerlinNoise(sampleX + warpingStrenght * q[0] + 1.7f, sampleY + warpingStrenght * q[0] + 9.2f),
                                          Mathf.PerlinNoise(sampleX + warpingStrenght * q[0] + 8.3f, sampleX + warpingStrenght * q[0] + 2.8f)};

                noiseHeight += Mathf.PerlinNoise(sampleX + warpingStrenght * r[0], sampleY + warpingStrenght * r[1]) * amplitued;

                amplitued *= persistance;

                frequency *= lacunartiy;
            }

            // Getting the max and min noise height for normalization later on
            if (noiseHeight > maxNoiseHeight)
                maxNoiseHeight = noiseHeight;
            else if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
            noiseMap[x, y] = noiseHeight;
        }

        // normilization 
        for (var y = 0; y < mapHeight; ++y)
        for (var x = 0; x < mapWidth; ++x)
            noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);

        return noiseMap;
    }
}