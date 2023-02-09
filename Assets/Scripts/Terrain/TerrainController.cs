using System;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain
{
    /// <summary>
    ///     This script is used to generate a noise map that can be applied to another object.
    ///     Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y a youtube series by Sebastian Lague
    /// </summary>
    public class TerrainController : MonoBehaviour
    {
        // Terrain dimension settings
        public int mapSize = 2000; // edge length in meters

        public int depthRange = 1024;

        // Noise settings
        public bool onlyNoiseMap;

        public float noiseScale;

        public int octaves;

        public float lacunarity;

        public int seed;

        public Vector2 offset;

        public float warpingStrength;

        public float heightSquish = 1.0f;

        // Heightmap import settings
        public bool useImage;

        public string heightmapFolder = "Assets/Heightmaps/tuebingen/";

        public int realImageWidthInM = 8000;

        public int imageSmoothingFactor = 6;

        public TerrainTextureData terrainTextureData;
        
        private float[,] backupHeights;

        private void OnValidate()
        {
            if (terrainTextureData != null)
            {
                terrainTextureData.OnValuesUpdated -= onValuesUpdated;
                terrainTextureData.OnValuesUpdated += onValuesUpdated;
            }
            if (lacunarity < 1)
                lacunarity = 1;
            if (octaves < 0)
                octaves = 0;
        }
        
        void onValuesUpdated()
        {
            var terrain = GameObject.Find("Terrain");
            var material = terrain.GetComponent<UnityEngine.Terrain>().materialTemplate;
            if (material != null && terrainTextureData != null)
                terrainTextureData.ApplyToMaterial(material);
            terrain.GetComponent<UnityEngine.Terrain>().materialTemplate = null;
            terrain.GetComponent<UnityEngine.Terrain>().materialTemplate = material;
        }

        public void DrawMapInEditor()
        {
            var terrainResolution = mapSize + 1;
            if (onlyNoiseMap)
            {
                var planeRenderer = GameObject.Find("NoiseRenderer").GetComponent<MeshRenderer>();
                var noiseTexture = TerrainGenerator.HeightsToTexture(GenerateNoise(terrainResolution));
                var texture = TextureUtilities.ScaleTexture(noiseTexture, mapSize / 10); // match terrain
                texture = TextureUtilities.FlipTexture(texture);
                planeRenderer.sharedMaterial.mainTexture = texture;
                planeRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
            }
            else
            {
                float[,] heights;
                if (useImage)
                {
                    var (heightsArray, heightMap) = TerrainGenerator.TerrainFromImage(heightmapFolder + "heightmap.png",
                        realImageWidthInM, terrainResolution, imageSmoothingFactor, mapSize);
                    heights = heightsArray;
                    // Draw heightmap
                    var texture = TextureUtilities.FlipTexture(TextureUtilities.ScaleTexture(heightMap, mapSize / 10));
                    var planeRenderer = GameObject.Find("NoiseRenderer").GetComponent<MeshRenderer>();
                    planeRenderer.sharedMaterial.mainTexture = texture;
                    planeRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
                }
                else
                {
                    heights = GenerateNoise(terrainResolution);
                }

                var terrain = GameObject.Find("Terrain");
                var terrainData = terrain.GetComponent<UnityEngine.Terrain>().terrainData;
                terrainData.SetDetailResolution(terrainResolution-1, 16);
                terrainData.heightmapResolution = terrainResolution;
                terrainData.size = new Vector3(mapSize, depthRange, mapSize);
                terrainData.SetHeights(0, 0, heights);
                terrain.GetComponent<TerrainCollider>().terrainData = terrainData;

                var minHeight = float.MaxValue;
                var maxHeight = float.MinValue;
                
                for (int i = 0; i < heights.GetLength(0); i++)
                {
                    for (int j = 0; j < heights.GetLength(1); j++)
                    {
                        var val = heights[i, j];
                        if (val < minHeight)
                        {
                            minHeight = val;
                        }
                        if (val > maxHeight)
                        {
                            maxHeight = val;
                        }
                    }
                }

                minHeight *= terrainData.size.y;
                maxHeight *= terrainData.size.y;

                Material material; 
                if (terrain.GetComponent<UnityEngine.Terrain>().materialTemplate == null)
                {
                    material = Resources.Load<Material>("Materials/TerrainMaterial");
                    material.name = "_TerrainMaterial";
                }
                else
                {
                    material = terrain.GetComponent<UnityEngine.Terrain>().materialTemplate;
                    material.name = "_TerrainMaterial";
                    terrain.GetComponent<UnityEngine.Terrain>().materialTemplate = null;
                }

                terrainTextureData.UpdateMeshHeights(material, minHeight, maxHeight);
                // material.SetFloat("minHeight", minHeight);
                // material.SetFloat("maxHeight", maxHeight);
                terrain.GetComponent<UnityEngine.Terrain>().materialTemplate = material;
                
                terrain.transform.position = new Vector3(-terrainData.size.x / 2, 0, -terrainData.size.z / 2);
                backupHeights = heights;
            }
        }

        private float[,] GenerateNoise(int resolution)
        {
            var heightArray = Noise.GenerateNoiseMap(resolution, seed, noiseScale, octaves,
                lacunarity, offset, warpingStrength, heightSquish);

            return heightArray;
        }

        public float[,] GetBackupHeights()
        {
            return backupHeights;
        }
    }
}