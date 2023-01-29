using System;
using UnityEngine;

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

        [Range(0, 1)] public float persistance;

        public float lacunarity;

        public int seed;

        public Vector2 offset;

        public float warpingStrength;

        // Heightmap import settings
        public bool useImage;

        public string heightmapFolder = "Assets/Heightmaps/tuebingen/";

        public int realImageWidthInM = 8000;

        public int imageSmoothingFactor = 3;

        private void OnValidate()
        {
            if (lacunarity < 1)
                lacunarity = 1;
            if (octaves < 0)
                octaves = 0;
        }

        public void DrawMapInEditor()
        {
            if (onlyNoiseMap)
            {
                var planeRenderer = GameObject.Find("NoiseRenderer").GetComponent<MeshRenderer>();
                var heightTexture = GenerateNoiseTexture();
                var texture = TextureUtilities.FlipTexture(heightTexture);
                planeRenderer.sharedMaterial.mainTexture = texture;
                planeRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
            }
            else
            {
                TerrainData terrainData;
                if (useImage)
                {
                    var (data, heightMap) = TerrainGenerator.TerrainFromImage(heightmapFolder + "heightmap.png",
                        realImageWidthInM, depthRange, imageSmoothingFactor, mapSize);
                    terrainData = data;
                    // Draw heightmap
                    var texture = TextureUtilities.FlipTexture(TextureUtilities.ScaleTexture(heightMap, mapSize / 10));
                    var planeRenderer = GameObject.Find("NoiseRenderer").GetComponent<MeshRenderer>();
                    planeRenderer.sharedMaterial.mainTexture = texture;
                    planeRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
                }
                else
                {
                    var heightTexture = GenerateNoiseTexture();
                    terrainData = TerrainGenerator.TerrainDataFromTexture(heightTexture, mapSize, depthRange);
                }

                var terrain = GameObject.Find("Terrain");
                terrain.GetComponent<UnityEngine.Terrain>().terrainData = terrainData;
                terrain.GetComponent<TerrainCollider>().terrainData = terrainData;
                terrain.transform.position = new Vector3(-terrainData.size.x / 2, 0, -terrainData.size.z / 2);
            }
        }

        private Texture2D GenerateNoiseTexture()
        {
            var heightArray = Noise.GenerateNoiseMap(mapSize, seed, noiseScale, octaves, persistance,
                lacunarity, offset, warpingStrength);
            var heightTexture = TerrainGenerator.HeightsToTexture(heightArray, mapSize / 10);

            return heightTexture;
        }
    }
}