using UnityEngine;

public static class TextureUtilities
{
    public static Texture2D FlipTexture(Texture2D heightMap)
    {
        var flippedTexture = new Texture2D(heightMap.width, heightMap.height);
        for (var x = 0; x < heightMap.width; x++)
        for (var y = 0; y < heightMap.height; y++)
            flippedTexture.SetPixel(x, y, heightMap.GetPixel(heightMap.width - x - 1, y));
        flippedTexture.Apply();
        return flippedTexture;
    }

    // Adapted from: http://answers.unity.com/answers/890986/view.html
    public static Texture2D ScaleTexture(Texture2D source, int resolution)
    {
        var result = new Texture2D(resolution, resolution, source.format, false);
        for (var i = 0; i < result.height; ++i)
        for (var j = 0; j < result.width; ++j)
        {
            var newColor = source.GetPixelBilinear(j / (float)result.width, i / (float)result.height);
            result.SetPixel(j, i, newColor);
        }

        result.Apply();
        return result;
    }

    public static Texture2D CropCenter(Texture2D texture2D, int edgeLengthInMeters, int centerCropLengthInMeters)
    {
        var centerCropLengthInPixels =
            (int)(centerCropLengthInMeters / (float)edgeLengthInMeters * texture2D.width);
        var centerCropStartX = (texture2D.width - centerCropLengthInPixels) / 2;
        var centerCropStartY = (texture2D.height - centerCropLengthInPixels) / 2;

        var croppedTexture = new Texture2D(centerCropLengthInPixels, centerCropLengthInPixels);
        for (var x = 0; x < centerCropLengthInPixels; x++)
        for (var y = 0; y < centerCropLengthInPixels; y++)
            croppedTexture.SetPixel(x, y, texture2D.GetPixel(centerCropStartX + x, centerCropStartY + y));

        croppedTexture.Apply();
        return croppedTexture;
    }

    public static Texture2D Rotate90Clockwise(Texture2D blockedMap)
    {
        var rotatedBlockedMap = new Texture2D(blockedMap.height, blockedMap.width);
        for (var x = 0; x < blockedMap.width; x++)
        for (var y = 0; y < blockedMap.height; y++)
            rotatedBlockedMap.SetPixel(y, blockedMap.width - x - 1, blockedMap.GetPixel(x, y));
        rotatedBlockedMap.Apply();

        return rotatedBlockedMap;
    }
}