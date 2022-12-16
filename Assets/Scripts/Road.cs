using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using Unity.VisualScripting.ReorderableList.Element_Adder_Menu;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Analytics;

public struct RoadNode {
    public Vector3 pos;
    public float heuristic;
    public int idx;
    public int predIdx;
    public Vector3 normal;
    public float dist;
    public bool isValid;
    public RoadNode(int _idx)
    {
        pos = new Vector3(0.0f, 0.0f, 0.0f);
        idx = _idx;
        heuristic = float.PositiveInfinity;
        predIdx = _idx;
        normal = new Vector3(0.0f, 0.0f, 1.0f);
        dist = float.PositiveInfinity;
        isValid = true;
    }
}
public struct GridBounds
{
    public int lowerX;
    public int upperX;
    public int lowerZ;
    public int upperZ;

    public GridBounds(int _lowerX, int _upperX, int _lowerZ, int _upperZ)
    {
        lowerX = _lowerX;
        upperX = _upperX;
        lowerZ = _lowerZ;
        upperZ = _upperZ;
    }
}

public struct NodeHeap
{
    // Currently this is a priority queue
    public int[] elements;
    public float[] dist;
    private int size;
    public int lastIndex;
    public int popcount;

    public NodeHeap(int _size)
    {
        size = _size;
        elements = new int[size];
        dist = new float[size];
        lastIndex = 0;
        popcount = 0;
    }

    public void push(int idx, float _dist)
    {
        for (int i = 0; i < lastIndex; i++)
        {
            if (elements[i] == idx)
            {
                if (dist[i] < _dist)
                {
                    throw new Exception("New distance is somehow bigger then previous distance");
                }
                dist[i] = _dist;
                for(int j = i; j > 0; j--)
                {
                    if(dist[j] < dist[j - 1])
                    {
                        swap(j,j-1);
                    }
                    else
                    {
                        break;
                    }
                }
                return;
            }
        }
        elements[lastIndex] = idx;
        dist[lastIndex++] = _dist;
        for(int i = lastIndex-1; i > 0; i--)
        {
            if (dist[i] < dist[i - 1])
            {
                swap(i, i - 1);
            }
            else
            {
                break;
            }
        }

        /*
        int currentIndex = -1;
        bool isSmaller;
        int nextIndex = 0;
        bool isAlreadyInHeap = false;
        for (int i = 0; i < lastIndex; i++)
        {
            if (elements[i] == idx)
            {
                currentIndex = i;
                isAlreadyInHeap = true;
                dist[i] = _dist;
                break;
            }
        }
        if (!isAlreadyInHeap)
        {
            elements[lastIndex] = idx;
            dist[lastIndex] = _dist;
            currentIndex = lastIndex;
            if (lastIndex % 2 == 0 && currentIndex !=0)
            {
                nextIndex = currentIndex / 2 - 1;
                isSmaller = dist[nextIndex] > dist[currentIndex];
            }
            else
            {
                nextIndex = currentIndex / 2;
                isSmaller = dist[nextIndex] > dist[currentIndex];
            }
            lastIndex++;
        }
        else
        {
            if (currentIndex % 2 == 0 && currentIndex != 0)
            {
                nextIndex = currentIndex / 2 - 1;
                isSmaller = dist[nextIndex] > dist[currentIndex];
            }
            else
            {
                nextIndex = currentIndex / 2;
                isSmaller = dist[nextIndex] > dist[currentIndex];
            }

        }
        while (currentIndex != 0 && isSmaller)
        {
            // c# apperently has no swap method
            int tempIdx = elements[nextIndex];
            float tempDist = dist[nextIndex];
            elements[nextIndex] = elements[currentIndex];
            dist[nextIndex] = dist[currentIndex];
            elements[currentIndex] = tempIdx;
            dist[currentIndex] = tempDist;

            currentIndex = nextIndex;
            nextIndex = currentIndex / 2 - 1;
            if(nextIndex == -1)
            {
                break;
            }
            isSmaller = dist[nextIndex] > dist[currentIndex];
        }*/
    }

    public int pop()
    {
        int firstElement = elements[0];
        for(int i = 0; i < lastIndex-1; i++)
        {
            swap(i, i + 1);
        }
        lastIndex--;
        return firstElement;
        /*int firstElement =  elements[0];

        int currentIdx = 0;

        while (true)
        {
            if(currentIdx * 2 + 1 > lastIndex || currentIdx * 2 + 2 > lastIndex)
            {
                break;
            }
            if (dist[currentIdx * 2 + 1] < dist[currentIdx * 2 + 2])
            {
                dist[currentIdx] = dist[currentIdx * 2 + 1];
                elements[currentIdx] = elements[currentIdx * 2 + 1];
                currentIdx = currentIdx * 2 + 1;
            }
            else
            {
                dist[currentIdx] = dist[currentIdx * 2 + 2];
                elements[currentIdx] = elements[currentIdx * 2 + 2];
                currentIdx = currentIdx * 2 + 2;
            }
        }
        lastIndex--;
        popcount++;
        return firstElement;*/
    }

    public float allElementsBigger(float _dist)
    {
        for(int i = 0; i < lastIndex; i++)
        {
            if (dist[i] < _dist)
            {
                return _dist - dist[i];
            }
        }
        return -1;
    }

    public void swap(int currentIndex, int nextIndex)
    {
        int tempIdx = elements[nextIndex];
        float tempDist = dist[nextIndex];
        elements[nextIndex] = elements[currentIndex];
        dist[nextIndex] = dist[currentIndex];
        elements[currentIndex] = tempIdx;
        dist[currentIndex] = tempDist;
    }

    public String distArrayAsString()
    {
        String result = "";

        for(int i = 0; i < lastIndex; i++)
        {
            result += dist[i];
            result += ", ";
        }
        return result;
    }
}
public class Road : MonoBehaviour
{
    private int bounds;
    private Vector2Int startingPosition;
    private Vector2Int endingPosition;
    public RoadNode[] nodes;
    private int innerRadius;
    private int outerRadius;
    private MeshCollider MyMeshCollider;
    private GridBounds gridBounds;

    public Road(Vector2 start, Vector2 end, int _bounds, int _innerRadius, int _outerRadius)
    {
        bounds = _bounds;
        int halveBounds = bounds / 2;
        //FIXME: This assumes that bounds goes from -n to n
        startingPosition = new Vector2Int((int)start.x + halveBounds, (int)start.y + halveBounds);
        endingPosition = new Vector2Int((int)end.x + halveBounds, (int)end.y+ halveBounds);
        nodes = new RoadNode[bounds*bounds];
        innerRadius = _innerRadius;
        outerRadius = _outerRadius;
        MyMeshCollider = GameObject.Find("Mesh").GetComponent<MeshCollider>();
        int lowerX = Math.Max(0,Math.Min(startingPosition.x, endingPosition.x)-100);
        int upperX = Math.Min(bounds-1, Math.Max(startingPosition.x, endingPosition.x) + 100);
        int lowerZ = Math.Min(bounds - 1, Math.Max(startingPosition.y, endingPosition.y) + 100);
        int upperZ = Math.Min(bounds - 1, Math.Max(startingPosition.y, endingPosition.y) + 100);
        gridBounds = new GridBounds(lowerX, upperX, lowerZ, upperZ);

        for (int z = 0; z < bounds; ++z)
        {
            for (int x = 0; x < bounds; ++x)
            {
                int index = z * bounds + x;
                if (z < gridBounds.lowerZ || z > gridBounds.upperZ || x < gridBounds.lowerX || x > gridBounds.upperX)
                {
                    nodes[index].isValid = false;
                }
                nodes[index] = new RoadNode(index);
                // FIXME: This assumes that the plane is at 0,0
                nodes[index].pos = new Vector3(x-halveBounds,0.0f, z - halveBounds);
                (nodes[index].pos.y, nodes[index].normal) = RaycastAtPosition(new Vector2(x-halveBounds, z-halveBounds));
                nodes[index].idx = index;
                nodes[index].predIdx = index;
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
        int halveBounds = bounds / 2;
        // We are going to use an A*-Algorithm to calculate the shortest path
        startingPosition = new Vector2Int((int)start.x + halveBounds, (int)start.y + halveBounds);
        endingPosition = new Vector2Int((int)end.x + halveBounds, (int)end.y + halveBounds);

        
        // for the heuristic we are just going to calculate the square 3D distance from the end point to every point
        for (int z_val = 0; z_val < bounds; ++z_val)
        {
            for (int x_val = 0; x_val < bounds; ++x_val)
            {
                int index = z_val * bounds + x_val;
                if (!nodes[index].isValid)
                {
                    continue;
                }
                Vector3 dists = nodes[index].pos - nodes[endingPosition.y*bounds+endingPosition.x].pos;
                nodes[index].heuristic = dists.x * dists.x + dists.y * dists.y+dists.z*dists.z;
                nodes[startingPosition.y * bounds + startingPosition.x].dist = 0.0f;
            }
        }

        // FIXME: This can be way smaller or even dynamic
        NodeHeap notVisited = new NodeHeap(bounds*bounds);
        bool[] visited = new bool[bounds * bounds];

        // we are going to start at the starting point
        notVisited.push(startingPosition.y*bounds+startingPosition.x, 0f);
        int oldIndex = -1;
        visited[endingPosition.y * bounds + endingPosition.x] = false;
        int count = 0;
        while (notVisited.lastIndex > 0 && !visited[endingPosition.y * bounds + endingPosition.x])
        {
            oldIndex = notVisited.pop();
            float test = notVisited.allElementsBigger(nodes[oldIndex].dist);
            if (test > 0)
            {
                print(test);
                throw new Exception("Heap not working");
            }
            if (!nodes[oldIndex].isValid)
            {
                continue;
            }
            for (int i = -outerRadius; i <= outerRadius; ++i)
            {
                for (int j = -outerRadius; j <= outerRadius; ++j)
                {
                    int x = oldIndex % bounds + i;
                    int z = oldIndex / bounds + j;
                    if (x < 0 || x >= bounds || z < 0 || z >= bounds) continue;
                    int index = z * bounds + x;
                    if (visited[index]) continue;
                    if (oldIndex == index) continue;
                    if (i * i + j * j < innerRadius * innerRadius) continue;
                    if (i * i + j * j > outerRadius * outerRadius) continue;
                    Vector3 relativeDist = nodes[index].pos - nodes[oldIndex].pos;
                    float newDist = nodes[oldIndex].dist + relativeDist.x * relativeDist.x + relativeDist.y * relativeDist.y + relativeDist.z * relativeDist.z;
                    if (newDist < nodes[index].dist)
                    {
                        nodes[index].dist = newDist;
                        nodes[index].predIdx = oldIndex;
                        notVisited.push(index, newDist);
                    }
                }
            }
            visited[oldIndex] = true;
            count++;
        }
        print("This is the distance of the finale node: " + nodes[endingPosition.y*bounds+endingPosition.x].dist);
    }
}
