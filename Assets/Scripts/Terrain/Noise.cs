using UnityEngine;
using Random = System.Random;

namespace Terrain
{
    /// <summary>
    ///     A script that generates a noise map.
    ///     Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y, a youtube series by Sebastian Lague
    /// </summary>
    public static class Noise
    {
        /// <summary>
        ///     Generates a noise map using perlin noise
        /// </summary>
        /// <param name="resolution"> The width and height of the map</param>
        /// <param name="seed"> Variable to generate the same map again</param>
        /// <param name="scale"> The scale of the noise</param>
        /// <param name="octaves"> Determines how much impact the persistence and lacunarity have: 1 for no influence</param>
        /// <param name="lacunarity"> Number greater than 1 which influences the frequency</param>
        /// <param name="offset"> An extra offset applied by the user</param>
        /// <param name="warping"> Domain warping strength </param>
        /// <param name="heightSquish"> Squishes height by a factor to prevent "large curves" </param>
        /// <returns> A float[,] noise map using perlin noise</returns>
        public static float[,] GenerateNoiseMap(int resolution, int seed, float scale, int octaves, float lacunarity,
            Vector2 offset, float warping, float heightSquish)
        {
            var noiseMap = new float[resolution, resolution];
            var prng = new Random(seed);
            var octaveOffsets = new Vector2[octaves];

            for (var i = 0; i < octaves; ++i)
            {
                var offsetX = prng.Next(-resolution * 10, resolution * 10) + offset.x;
                var offsetY = prng.Next(-resolution * 10, resolution * 10) - offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            if (scale <= 0) scale = 0.0001f; // Ensure scale > 0
            var halfMapSize = resolution / 2f; // Zoom into middle
            var maxLocalNoiseHeight = float.MinValue;
            var minLocalNoiseHeight = float.MaxValue;

            // Loop over every vertex
            for (var y = 0; y < resolution; ++y)
            for (var x = 0; x < resolution; ++x)
            {
                float frequency = 1;
                float noiseHeight = 0;

                // Domain warping
                for (var i = 0; i < octaves; ++i)
                {
                    var sampleX = (x - halfMapSize + octaveOffsets[i].x) / scale * frequency;
                    var sampleY = (y - halfMapSize + octaveOffsets[i].y) / scale * frequency;

                    var q = new[]
                    {
                        Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1,
                        Mathf.PerlinNoise(sampleX + 5.2f, sampleY + 1.3f) * 2 - 1
                    };
                    var r = new[]
                    {
                        Mathf.PerlinNoise(sampleX + warping * q[0] + 1.7f, sampleY + warping * q[0] + 9.2f) * 2 - 1,
                        Mathf.PerlinNoise(sampleX + warping * q[0] + 8.3f, sampleX + warping * q[0] + 2.8f) * 2 - 1
                    };

                    noiseHeight += Mathf.PerlinNoise(sampleX + warping * r[0], sampleY + warping * r[1]) * 2 - 1;
                    frequency *= lacunarity;
                }

                // Getting the max and min noise height for normalization
                if (noiseHeight > maxLocalNoiseHeight)
                    maxLocalNoiseHeight = noiseHeight;
                else if (noiseHeight < minLocalNoiseHeight)
                    minLocalNoiseHeight = noiseHeight;
                noiseMap[x, y] = noiseHeight;
            }

            maxLocalNoiseHeight *= heightSquish;

            // Local normalization 
            for (var y = 0; y < resolution; ++y)
            for (var x = 0; x < resolution; ++x)
                noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);

            return noiseMap;
        }
    }
}