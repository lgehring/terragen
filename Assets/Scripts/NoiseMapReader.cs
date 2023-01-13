using UnityEngine;

/// <summary>
///     A class that provides tool to generate a "NoiseMap" (float[,]) from a given input image.
/// </summary>
/// 
public static class NoiseMapReader
{   
    // TODO: support heightscaling
    // A function that reads in a black and white image and returns a float array of the grayscale values
    // Image must have the size of the map (1km = 10px)
    public static float[,] ReadNoiseMap(string path, int mapChunkSize)
    {
        // Load the image
        var fileData = System.IO.File.ReadAllBytes(path);
        var texture = new Texture2D(1, 1); // size gets overwritten by LoadImage
        texture.LoadImage(fileData);
        // Scale the image to the size of the map
        var scaledTexture = ScaleTexture(texture, 800, 800); // 1px = 10m
        // Crop the image to the size of the map
        var croppedTexture = CropTextureCenter(scaledTexture, mapChunkSize, mapChunkSize);
        // Create a new float array
        var noiseMap = new float[croppedTexture.width, croppedTexture.height];
        // Loop through the image
        for (var x = 0; x < croppedTexture.width; x++)
        {
            for (var y = 0; y < croppedTexture.height; y++)
            {
                // Get the pixel color
                var pixelColor = croppedTexture.GetPixel(x, y);
                // Set the noise map value to the grayscale value of the pixel
                noiseMap[x, y] = pixelColor.grayscale; // mirror the image to obtain the correct orientation
            }
        }
        // Return the noise map
        return noiseMap;
    }
    
    // Adapted from: http://answers.unity.com/answers/890986/view.html
    private static Texture2D ScaleTexture(Texture2D source,int targetWidth,int targetHeight) {
        var result=new Texture2D(targetWidth,targetHeight,source.format,false);
        for (var i = 0; i < result.height; ++i) {
            for (var j = 0; j < result.width; ++j) {
                var newColor = source.GetPixelBilinear(j / (float)result.width, i / (float)result.height);
                result.SetPixel(j, i, newColor);
            }
        }
        result.Apply();
        return result;
    }
    
    // Adapted from: http://answers.unity.com/answers/1935556/view.html
    private static Texture2D CropTextureCenter(Texture2D texture2D, int targetWidth, int targetHeight)
    {
        var croppedTexture = new Texture2D(targetWidth, targetHeight);
        croppedTexture.SetPixels(texture2D.GetPixels((texture2D.width - targetWidth) / 2, (texture2D.height - targetHeight) / 2, targetWidth, targetHeight));
        croppedTexture.Apply();
        return croppedTexture;
    }
}
