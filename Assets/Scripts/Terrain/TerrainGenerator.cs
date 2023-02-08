using System;
using System.IO;
using UnityEngine;

namespace Terrain
{
    public static class TerrainGenerator
    {
        public static Tuple<float[,], Texture2D> TerrainFromImage(string path, int edgeLengthInMeters, int terrainResolution,
            int smoothingFactor = 0, int centerCropLengthInMeters = 0)
        {
            var heightMap = new Texture2D(1, 1);
            heightMap.LoadImage(File.ReadAllBytes(path));
            heightMap = TextureUtilities.FlipTexture(heightMap);
            if (centerCropLengthInMeters > 0)
            {
                heightMap = TextureUtilities.CropCenter(heightMap, edgeLengthInMeters, centerCropLengthInMeters);
            }

            heightMap = TextureUtilities.ScaleTexture(heightMap, terrainResolution);

            var heights = TextureToHeights(heightMap);
            if (smoothingFactor > 0) heights = SmoothHeightmap(heights, smoothingFactor);

            return new Tuple<float[,], Texture2D>(heights, heightMap);
        }

        private static float[,] TextureToHeights(Texture2D heightMap)
        {
            var pixels = heightMap.GetPixels();
            var noiseMap = new float[heightMap.width, heightMap.height];
            for (var x = 0; x < heightMap.width; x++)
            for (var y = 0; y < heightMap.height; y++)
            {
                var pixelColor = pixels[y * heightMap.width + x];
                noiseMap[x, y] = pixelColor.grayscale;
            }

            return noiseMap;
        }
        
        public static Texture2D HeightsToTexture(float[,] heights)
        {
            var texture = new Texture2D(heights.GetLength(0), heights.GetLength(1));
            for (var x = 0; x < heights.GetLength(0); x++)
            for (var y = 0; y < heights.GetLength(1); y++)
            {
                var heightValue = heights[x, y];
                texture.SetPixel(x, y, new Color(heightValue, heightValue, heightValue));
            }

            texture.Apply();

            return texture;
        }

        private static float[,] SmoothHeightmap(float[,] heightmap, int smoothness)
        {
            var smoothHeightmap = new float[heightmap.GetLength(0), heightmap.GetLength(1)];
            for (var x = 0; x < heightmap.GetLength(0); x++)
            for (var y = 0; y < heightmap.GetLength(1); y++)
            {
                var sum = 0f;
                var count = 0;
                for (var i = -smoothness; i <= smoothness; i++)
                for (var j = -smoothness; j <= smoothness; j++)
                {
                    if (x + i < 0 || x + i >= heightmap.GetLength(0) || y + j < 0 ||
                        y + j >= heightmap.GetLength(1)) continue;
                    sum += heightmap[x + i, y + j];
                    count++;
                }

                smoothHeightmap[x, y] = sum / count;
            }

            return smoothHeightmap;
        }
    }
}