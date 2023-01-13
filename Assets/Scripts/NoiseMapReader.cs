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
    public static float[,] ReadNoiseMap(string path)
    {
        // Load the image
        var fileData = System.IO.File.ReadAllBytes(path);
        var texture = new Texture2D(1, 1); // size gets overwritten by LoadImage
        texture.LoadImage(fileData);
        // Scale the image to the size of the map
        var scaledTexture = ScaleTexture(texture, 800, 800); // TODO: get map size from somewhere
        
        // Create a new float array
        var noiseMap = new float[scaledTexture.width, scaledTexture.height];
        // Loop through the image
        for (var x = 0; x < scaledTexture.width; x++)
        {
            for (var y = 0; y < scaledTexture.height; y++)
            {
                // Get the pixel color
                var pixelColor = scaledTexture.GetPixel(x, y);
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
}
