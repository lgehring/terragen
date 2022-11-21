using UnityEngine;

/// <summary>
///     This script applies the texture to a mesh.
///     Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y a youtube series by Sebastian Lague
/// </summary>
public class MapDisplay : MonoBehaviour
{
    // This is the mesh that we want to apply the texture to 
    public Renderer textureRender;

    /// <summary>
    ///     Creates a texture given a noise map
    /// </summary>
    /// <param name="noiseMap"> Generated noise map</param>
    public void DrawNoiseMap(float[,] noiseMap)
    {
        // get dimensions
        var width = noiseMap.GetLength(0);
        var height = noiseMap.GetLength(1);

        // create Texture
        var texture = new Texture2D(width, height);

        // Set colors of texture according to the noise map
        var colourMap = new Color[width * height];
        for (var y = 0; y < height; ++y)
        for (var x = 0; x < width; ++x)
            // get grayscale value for the noise value inside of the noise map
            colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);

        // Setting the colour of the texture of the map 
        texture.SetPixels(colourMap);
        texture.Apply();

        // Using shared material because this can be changed during run time
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(width, 1, height);
    }
}