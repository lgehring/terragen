using System.IO;
using UnityEngine;

/// <summary>
///     A class that provides tool to generate a "NoiseMap" (float[,]) from a given input image.
/// </summary>
public static class NoiseMapReader
{
    // TODO: support heightscaling
    // A function that reads in a black and white image and returns a float array of the grayscale values
    // Image must have the size of the map (1km = 10px)
    public static float[,] ReadNoiseMap(string path, int mapChunkSize)
    {
        // Load the image
        var fileData = File.ReadAllBytes(path);
        var texture = new Texture2D(1, 1); // size gets overwritten by LoadImage
        texture.LoadImage(fileData);
        // Crop the image to the size of the map
        var croppedTexture = CropTextureCenter(texture, mapChunkSize);
        // Scale the image to the size of the map
        var scaledTexture = ScaleTexture(croppedTexture, mapChunkSize); // 1px = 10m
        // Create a new float array
        var noiseMap = new float[scaledTexture.width, scaledTexture.height];
        // Loop through the image
        for (var x = 0; x < scaledTexture.width; x++)
        for (var y = 0; y < scaledTexture.height; y++)
        {
            // Get the pixel color
            var pixelColor = scaledTexture.GetPixel(x, y);
            // Set the noise map value to the grayscale value of the pixel
            noiseMap[x, y] = pixelColor.grayscale; // mirror the image to obtain the correct orientation
        }

        // Smooth the noise map
        var smoothMap = SmoothHeightmap(noiseMap, 1);
        // Return the noise map
        return smoothMap;
    }


    // Adapted from: http://answers.unity.com/answers/890986/view.html
    private static Texture2D ScaleTexture(Texture2D source, int mapChunkSize)
    {
        var result = new Texture2D(mapChunkSize, mapChunkSize, source.format, false);
        for (var i = 0; i < result.height; ++i)
        for (var j = 0; j < result.width; ++j)
        {
            var newColor = source.GetPixelBilinear(j / (float)result.width, i / (float)result.height);
            result.SetPixel(j, i, newColor);
        }

        result.Apply();
        return result;
    }

    private static Texture2D CropTextureCenter(Texture2D texture2D, int mapChunkSize)
    {
        // The image is always square and 8km in size
        // The mapChunkSize/targetWidth is given as 1km = 100
        var pixelsPerKm = texture2D.width / 8;
        var targetSizeKm = mapChunkSize / 100;
        var targetSizePx = targetSizeKm * pixelsPerKm;

        var croppedTexture = new Texture2D(targetSizePx, targetSizePx);
        croppedTexture.SetPixels(texture2D.GetPixels((texture2D.width - targetSizePx) / 2,
            (texture2D.height - targetSizePx) / 2, targetSizePx, targetSizePx));
        croppedTexture.Apply();
        return croppedTexture;
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