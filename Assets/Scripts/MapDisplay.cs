using UnityEngine;

/// <summary>
///     This script applies the texture to a mesh.
///     Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y a youtube series by Sebastian Lague
/// </summary>
public class MapDisplay : MonoBehaviour
{
    // This is the mesh that we want to apply the texture to 
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    /// <summary>
    ///     Creates a texture given a noise map
    /// </summary>
    /// <param name="noiseMap"> Generated noise map</param>
    public void DrawTexture(Texture2D texture)
    {
        // Using shared material because this can be changed during run time
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}