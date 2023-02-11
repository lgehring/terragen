using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Terrain
{
    [CreateAssetMenu]
    public class TerrainTextureData : UpdatableData
    {
        private const int TextureSize = 512;
        private const TextureFormat TextureFormat = UnityEngine.TextureFormat.RGB565;
        private static readonly int LayerCount = Shader.PropertyToID("layerCount");
        private static readonly int BaseColors = Shader.PropertyToID("baseColors");
        private static readonly int BaseStartHeights = Shader.PropertyToID("baseStartHeights");
        private static readonly int BaseBlends = Shader.PropertyToID("baseBlends");
        private static readonly int BaseColorStrength = Shader.PropertyToID("baseColorStrength");
        private static readonly int BaseTextureScales = Shader.PropertyToID("baseTextureScales");
        private static readonly int BaseTextures = Shader.PropertyToID("baseTextures");
        private static readonly int MinHeight = Shader.PropertyToID("minHeight");
        private static readonly int MaxHeight = Shader.PropertyToID("maxHeight");

        public Layer[] layers;

        public void ApplyToMaterial(Material material)
        {
            material.SetInt(LayerCount, layers.Length);
            material.SetColorArray(BaseColors, layers.Select(x => x.tint).ToArray());
            material.SetFloatArray(BaseStartHeights, layers.Select(x => x.startHeight / 1024f).ToArray());
            material.SetFloatArray(BaseBlends, layers.Select(x => x.blendStrength).ToArray());
            material.SetFloatArray(BaseColorStrength, layers.Select(x => x.tintStrength).ToArray());
            material.SetFloatArray(BaseTextureScales, layers.Select(x => x.textureScale).ToArray());
            var textureArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
            material.SetTexture(BaseTextures, textureArray);

            UpdateMeshHeights(material, 0, 1024); // max y terrain extent
        }

        public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
        {
            material.SetInt(LayerCount, layers.Length);
            material.SetColorArray(BaseColors, layers.Select(x => x.tint).ToArray());
            material.SetFloatArray(BaseStartHeights, layers.Select(x => x.startHeight / 1024f).ToArray());
            material.SetFloatArray(BaseBlends, layers.Select(x => x.blendStrength).ToArray());
            material.SetFloatArray(BaseColorStrength, layers.Select(x => x.tintStrength).ToArray());
            material.SetFloatArray(BaseTextureScales, layers.Select(x => x.textureScale).ToArray());

            material.SetFloat(MinHeight, minHeight);
            material.SetFloat(MaxHeight, maxHeight);

            // Set water height to first layer start
            var waterPlane = GameObject.Find("Water");
            var position = waterPlane.transform.position;
            position = new Vector3(position.x, layers[1].startHeight, position.z);
            waterPlane.transform.position = position;
        }

        private static Texture2DArray GenerateTextureArray(IReadOnlyList<Texture2D> textures)
        {
            var textureArray = new Texture2DArray(TextureSize, TextureSize, textures.Count, TextureFormat, true);
            for (var i = 0; i < textures.Count; i++) textureArray.SetPixels(textures[i].GetPixels(), i);
            textureArray.Apply();
            return textureArray;
        }

        [Serializable]
        public class Layer
        {
            public Texture2D texture;
            public Color tint;

            [Range(0, 1)] public float tintStrength;

            [Range(0, 1024)] public int startHeight;

            [Range(0, 1)] public float blendStrength;

            public float textureScale;
        }
    }
}