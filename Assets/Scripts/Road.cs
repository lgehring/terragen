using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

public struct RoadNode {
    public Vector3 pos;
    public float heuristic;
    public int idx;
    public int predIdx;
    public Vector3 normal;
    public RoadNode(int _idx)
    {
        pos = new Vector3(0.0f, 0.0f, 0.0f);
        idx = _idx;
        heuristic = float.PositiveInfinity;
        predIdx = _idx;
        normal = new Vector3(0.0f, 0.0f, 1.0f);
    }
}
public class Road : MonoBehaviour
{
    public int bounds;
    public Vector2Int startingPosition;
    public Vector2Int endingPosition;
    private RoadNode[] nodes;
    private MeshCollider MyMeshCollider { get; set; }

    public Road(Vector2 start, Vector2 end, MeshCollider mesh, int _bounds)
    {
        bounds = _bounds;
        startingPosition = new Vector2Int((int)start.x, (int)start.y);
        endingPosition = new Vector2Int((int)end.x, (int)end.y);
        nodes = new RoadNode[bounds*bounds];

        # pragma omp parallel for
        for (int y = 0; y < bounds; ++y)
        {
            for(int x = 0; x < bounds; ++x)
            {
                int index = y * bounds + x;
                nodes[index] = new RoadNode(index);
                nodes[index].pos = new Vector3(x,y,0.0f);
                (nodes[index].pos.z, nodes[index].normal) = RaycastAtPosition(new Vector2(x, y));
            }
        }
    }

    private (float height, Vector3 normal) RaycastAtPosition(Vector2 position)
    {
        const int maxHeight = 3000; // Height "Zugspitze" in m
        var height = 0f;
        var normal = Vector3.zero;
        var ray = new Ray(new Vector3(position.x, maxHeight, position.y), Vector3.down);

        if (!MyMeshCollider.Raycast(ray, out var hit, maxHeight)) return (height, normal);
        height = hit.point.y;
        normal = hit.normal;

        // // VISUAL DEBUG RAYCAST
        // var angleDegree = Vector3.Angle(Vector3.up, normal);
        // var anglePercent = Mathf.Tan(angleDegree * Mathf.Deg2Rad) * 100;
        // var color = Color.blue;
        // if (anglePercent > 200)
        // {
        //     color = Color.red;
        // }
        // else if (anglePercent > 100)
        // {
        //     color = Color.yellow;
        // }
        // else if (anglePercent > 0)
        // {
        //     color = Color.green;
        // }
        // Debug.DrawRay(hit.point, hit.normal, color, 100f);

        return (height, normal);
    }

    public void generateRoad(Vector2 start, Vector2 end)
    {
        // We are going to use an A*-Algorithm to calculate the shortest path
        startingPosition = new Vector2Int((int)start.x, (int)start.y);
        endingPosition = new Vector2Int((int)end.x, (int)end.y);

        // for the heuristic we are just going to calculate the square 3D distance from the end point to every point
        for (int y = 0; y < bounds; ++y)
        {
            for (int x = 0; x < bounds; ++x)
            {
                int index = y * bounds + x; 
                Vector3 dists = nodes[index].pos - nodes[endingPosition.y*bounds+endingPosition.x].pos;
                nodes[index].heuristic = dists.x*dists.x+dists.y*dists.y+dists.z*dists.z;
            }
        }
        

    }
}
