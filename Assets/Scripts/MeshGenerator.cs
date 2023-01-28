using UnityEngine;
using UnityEngine.Rendering;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve,
        int levelOfDetail)
    {
        var heightCurve = new AnimationCurve(_heightCurve.keys);

        var width = heightMap.GetLength(0);
        var height = heightMap.GetLength(1);
        var topLeftX = (width - 1) / -2f;
        var topLeftZ = (height - 1) / 2f;

        var meshSimplificationIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        var verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        var meshData = new MeshData(verticesPerLine, verticesPerLine);
        var vertexIndex = 0;

        for (var y = 0; y < height; y += meshSimplificationIncrement)
        for (var x = 0; x < width; x += meshSimplificationIncrement)
        {
            meshData.vertices[vertexIndex] = new Vector3(topLeftX + width - x - 1,
                heightCurve.Evaluate(heightMap[y, x]) * heightMultiplier, topLeftZ + y);
            meshData.uvs[vertexIndex] = new Vector2((width - x) / (float)width, (height - y) / (float)height);


            if (x < width - 1 && y < height - 1)
            {
                meshData.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + verticesPerLine + 1);
                meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine, vertexIndex);
            }


            vertexIndex++;
        }

        return meshData;
    }
}

public class MeshData
{
    private int _triangleIndex;
    public int[] triangles;
    public Vector2[] uvs;
    public Vector3[] vertices;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[_triangleIndex] = a;
        triangles[_triangleIndex + 1] = b;
        triangles[_triangleIndex + 2] = c;
        _triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        var mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32; // allows for up to 4 billion vertices
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}