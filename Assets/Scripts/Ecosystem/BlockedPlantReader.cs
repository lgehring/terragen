using System.IO;
using UnityEngine;

namespace Ecosystem
{
    public static class BlockedPlantReader
    {
        public static bool[,] BlockedArrayFromImage(string path, int edgeLengthInMeters, int centerCropLengthInMeters)
        {
            var blockedMap = new Texture2D(1, 1);
            blockedMap.LoadImage(File.ReadAllBytes(path));
            blockedMap = TextureUtilities.Rotate90Clockwise(blockedMap);
            blockedMap = TextureUtilities.CropCenter(blockedMap, edgeLengthInMeters, centerCropLengthInMeters);
            blockedMap = TextureUtilities.ScaleTexture(blockedMap, centerCropLengthInMeters);
            var blocks = TextureToBlocked(blockedMap);
            return blocks;
        }

        private static bool[,] TextureToBlocked(Texture2D heightMap)
        {
            var pixels = heightMap.GetPixels();
            var noiseMap = new bool[heightMap.width, heightMap.height];
            for (var x = 0; x < heightMap.width; x++)
            for (var y = 0; y < heightMap.height; y++)
            {
                var pixelColor = pixels[y * heightMap.width + x];
                noiseMap[x, y] = pixelColor.r > pixelColor.g && pixelColor.r > pixelColor.b;
            }

            return noiseMap;
        }
    }
}