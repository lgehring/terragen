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

public enum RoadNodeType
{
    Road,
    Tunnel,
    Bridge
}
public struct RoadNode {
    public Vector3 pos;
    public float heuristic;
    public int idx;
    public int predIdx;
    public Vector3 normal;
    public float dist;
    public bool isValid;
    public RoadNodeType roadType;
    public float mapHeight;
    public RoadNode(int _idx)
    {
        pos = new Vector3(0.0f, 0.0f, 0.0f);
        idx = _idx;
        heuristic = float.PositiveInfinity;
        predIdx = -1;
        normal = new Vector3(0.0f, 0.0f, 1.0f);
        dist = float.PositiveInfinity;
        isValid = true;
        roadType = RoadNodeType.Road;
        mapHeight = 0.0f;
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
    private int gridSize;
    private Vector2Int startingPosition;
    private Vector2Int endingPosition;
    public RoadNode[] nodes;
    private int innerRadius;
    private int outerRadius;
    private MeshCollider MyMeshCollider;
    private GridBounds gridBounds;
    private Vector2 offset;
    private int stepLength;

    public Road(Vector2Int start, Vector2Int end, int _bounds, int _innerRadius, int _outerRadius, int _stepLength, Vector2 _offset)
    {
        gridSize = _bounds/_stepLength;
        stepLength = _stepLength;
        offset.x = _bounds / 2 - _offset.x;
        offset.y = _bounds / 2 - _offset.y;
        nodes = new RoadNode[gridSize*gridSize];
        startingPosition = start;
        endingPosition = end;
        innerRadius = _innerRadius;
        outerRadius = _outerRadius;
        MyMeshCollider = GameObject.Find("Mesh").GetComponent<MeshCollider>();
        int gridExpansion = Math.Max(1, 100 / stepLength);
        int lowerX = Math.Max(0,Math.Min(startingPosition.x, endingPosition.x)- gridExpansion);
        int upperX = Math.Min(gridSize-1, Math.Max(startingPosition.x, endingPosition.x) + gridExpansion);
        int lowerZ = Math.Max(0, Math.Min(startingPosition.y, endingPosition.y) - gridExpansion);
        int upperZ = Math.Min(gridSize - 1, Math.Max(startingPosition.y, endingPosition.y) + gridExpansion);
        gridBounds = new GridBounds(lowerX, upperX, lowerZ, upperZ);

        if (startingPosition.x < lowerX || startingPosition.x > upperX || startingPosition.y < lowerZ || startingPosition.y > upperZ)
        {
            Debug.Log("The starting position with " + startingPosition.x + " and " + startingPosition.y + " is not in the grid");
            Debug.Log("The grid has the dimensions " + lowerX + " to " + upperX + " and " + lowerZ + " to " + upperZ);
            throw new Exception("Starting position is not in bounds");
        }
        if (endingPosition.x < lowerX || endingPosition.x > upperX || endingPosition.y < lowerZ || endingPosition.y > upperZ)
        {
            throw new Exception("Ending position is not in bounds");
        }

        for (int z = 0; z < gridSize; ++z)
        {
            for (int x = 0; x < gridSize; ++x)
            {
                int index = z * gridSize + x;
                if (z < gridBounds.lowerZ || z > gridBounds.upperZ || x < gridBounds.lowerX || x > gridBounds.upperX)
                {
                    nodes[index].isValid = false;
                }
                else
                {
                    nodes[index].isValid = true;
                    nodes[index] = new RoadNode(index);
                    nodes[index].pos = new Vector3((x*stepLength) - offset.x, 0.0f, (z*stepLength) - offset.y);
                    (nodes[index].mapHeight, nodes[index].normal) = RaycastAtPosition(new Vector2((x * stepLength) - offset.x, (z * stepLength) - offset.y));
                    nodes[index].idx = index;
                    nodes[index].predIdx = -1;
                }
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
        
        return (height, normal);
    }
    
    public void generateRoad(Vector2Int start, Vector2Int end)
    {
        // We are going to use an A*-Algorithm to calculate the shortest path
        startingPosition = start;
        endingPosition = end;
        int index = 0;
        
        // for the heuristic we are just going to calculate the square 3D distance from the end point to every point
        for (int z_val = 0; z_val < gridSize; ++z_val)
        {
            for (int x_val = 0; x_val < gridSize; ++x_val)
            {
                index = z_val * gridSize + x_val;
                if (!nodes[index].isValid)
                {
                    continue;
                }
                Vector3 dists = nodes[index].pos - nodes[endingPosition.y*gridSize+endingPosition.x].pos;
                nodes[index].heuristic = dists.x * dists.x + dists.z*dists.z;
            }
        }

        // FIXME: This can be way smaller or even dynamic
        NodeHeap notVisited = new NodeHeap(gridSize*gridSize);
        bool[] visited = new bool[gridSize * gridSize];

        // we are going to start at the starting point
        notVisited.push(startingPosition.y*gridSize+startingPosition.x, 0f);
        nodes[startingPosition.y * gridSize + startingPosition.x].dist = 0.0f;

        // Initializing variables for the loop to save memory
        int oldIndex;
        int count = 0;
        int x;
        int z;
        Vector3 relativeDist;
        float relativeHeight;
        float roadDist;
        float tunnelDist;
        
        while (notVisited.lastIndex > 0 && !visited[endingPosition.y * gridSize + endingPosition.x])
        {
            oldIndex = notVisited.pop();
            if (!nodes[oldIndex].isValid)
            {
                continue;
            }
            for (int i = -outerRadius; i <= outerRadius; ++i)
            {
                for (int j = -outerRadius; j <= outerRadius; ++j)
                {
                    x = oldIndex % gridSize + i;
                    z = oldIndex / gridSize + j;
                    if (x < 0 || x >= gridSize || z < 0 || z >= gridSize) continue;
                    index = z * gridSize + x;
                    if (visited[index]) continue;
                    //if (oldIndex == index) continue;
                    if (Math.Abs(i) <= innerRadius && Math.Abs(j) <= innerRadius) continue;
                    relativeDist = nodes[index].pos - nodes[oldIndex].pos;
                    relativeHeight = nodes[index].mapHeight - nodes[oldIndex].mapHeight;
                    Vector3 direction = new Vector3(relativeDist.x, relativeHeight, relativeDist.z).normalized;
                    roadDist = nodes[oldIndex].dist + evalSlope(direction)*relativeDist.sqrMagnitude;
                    tunnelDist = nodes[oldIndex].dist + evalTunnel(relativeHeight) * relativeDist.sqrMagnitude;
                    if (roadDist < nodes[index].dist && roadDist < tunnelDist)
                    {
                        nodes[index].dist = roadDist;
                        nodes[index].predIdx = oldIndex;
                        notVisited.push(index, roadDist);
                        nodes[index].roadType = RoadNodeType.Road;
                    }
                    else
                    {
                        if (tunnelDist < nodes[index].dist)
                        {
                            nodes[index].dist = tunnelDist;
                            nodes[index].predIdx = oldIndex;
                            notVisited.push(index, tunnelDist);
                            nodes[index].roadType = RoadNodeType.Tunnel;
                        }
                    }
                }
            }
            visited[oldIndex] = true;
            count++;
        }

        if (nodes[endingPosition.y * gridSize + endingPosition.x].predIdx == -1)
        {
            throw new Exception("No path found");
        }
    }

    private float evalSlope(Vector3 dirVec)
    {
        dirVec.y = Mathf.Abs(dirVec.y);
        dirVec.Normalize();

        // If the slope is above 30 degrees we do not want a path going up there (or down there)
        if(dirVec.y > 0.5f)
        {
            return float.PositiveInfinity;
        }
        else
        {   
            return 1+ dirVec.y*10;
        }
    }

    private float evalTunnel(float height)
    {
        height = Mathf.Abs(height);
        if (height < 3f) return float.PositiveInfinity;
        if (height < 8f)
            return height * 0.5f;
        if (height <= 15f)
            return 4f + (height - 8f) * 0.25f;
        return float.PositiveInfinity;
    }
}
