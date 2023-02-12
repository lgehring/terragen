using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roads
{
    public enum RoadNodeType
    {
        Road,
        Tunnel,
        Bridge
    }

    public struct RoadNode
    {
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
        private readonly int size;
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
            for (var i = 0; i < lastIndex; i++)
                if (elements[i] == idx)
                {
                    if (dist[i] < _dist) throw new Exception("New distance is somehow bigger then previous distance");
                    dist[i] = _dist;
                    for (var j = i; j > 0; j--)
                        if (dist[j] < dist[j - 1])
                            swap(j, j - 1);
                        else
                            break;
                    return;
                }

            elements[lastIndex] = idx;
            dist[lastIndex++] = _dist;
            for (var i = lastIndex - 1; i > 0; i--)
                if (dist[i] < dist[i - 1])
                    swap(i, i - 1);
                else
                    break;
        }

        public int pop()
        {
            var firstElement = elements[0];
            for (var i = 0; i < lastIndex - 1; i++) swap(i, i + 1);
            lastIndex--;
            return firstElement;
        }

        public float allElementsBigger(float _dist)
        {
            for (var i = 0; i < lastIndex; i++)
                if (dist[i] < _dist)
                    return _dist - dist[i];
            return -1;
        }

        public void swap(int currentIndex, int nextIndex)
        {
            var tempIdx = elements[nextIndex];
            var tempDist = dist[nextIndex];
            elements[nextIndex] = elements[currentIndex];
            dist[nextIndex] = dist[currentIndex];
            elements[currentIndex] = tempIdx;
            dist[currentIndex] = tempDist;
        }

        public string distArrayAsString()
        {
            var result = "";

            for (var i = 0; i < lastIndex; i++)
            {
                result += dist[i];
                result += ", ";
            }

            return result;
        }
    }

    public struct RoadSegments
    {
        private readonly List<Vector3> nodes;
        public List<Vector3> normals;
        public List<Vector3> Vertices;
        public List<int> triangles;
        public List<Vector2> uvs;

        public RoadSegments(List<Vector3> _nodes, List<Vector3> _normals)
        {
            nodes = _nodes;

            Vertices = new List<Vector3>();
            triangles = new List<int>();
            normals = new List<Vector3>();
            uvs = new List<Vector2>();
            var firstNode = nodes[0];
            var secondNode = nodes[1];

            var middleNode = (nodes[1] - nodes[0]) / 2;

            var dirVec = middleNode.normalized;

            var perpen = Vector3.Cross(dirVec, _normals[0]);

            Vertices.Add(firstNode - middleNode + perpen * 2f);
            Vertices.Add(firstNode - middleNode + perpen * -2f);

            normals.Add(_normals[0]);
            normals.Add(_normals[0]);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));

            var alternateForUVs = false;
            for (var i = 0; i < nodes.Count - 1; i++)
            {
                firstNode = nodes[i];
                secondNode = nodes[i + 1];

                middleNode = (nodes[i + 1] - nodes[i]) / 2;

                dirVec = middleNode.normalized;

                perpen = Vector3.Cross(dirVec, _normals[i]);

                Vertices.Add(firstNode + middleNode + perpen * 2f);
                Vertices.Add(firstNode + middleNode + perpen * -2f);

                normals.Add((_normals[i] + _normals[i + 1]) / 2);
                normals.Add((_normals[i] + _normals[i + 1]) / 2);

                if (alternateForUVs)
                {
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    alternateForUVs = false;
                }
                else
                {
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));
                    alternateForUVs = true;
                }
            }

            for (var i = 0; i < Vertices.Count - 3; i++)
            {
                triangles.Add(i);
                triangles.Add(i + 1);
                triangles.Add(i + 2);

                triangles.Add(i + 1);
                triangles.Add(i + 3);
                triangles.Add(i + 2);
            }
        }

        public void addSegment(Vector3 newNode, Vector3 normal)
        {
            nodes.Add(newNode);

            var firstNode = nodes[nodes.Count - 2];
            var middleNode = (newNode - nodes[nodes.Count - 2]) / 2;

            Vertices.Add(firstNode + middleNode + new Vector3(0, 0, 0.5f));
            Vertices.Add(firstNode + middleNode + new Vector3(0, 0, -0.5f));

            triangles.Add(Vertices.Count - 4);
            triangles.Add(Vertices.Count - 3);
            triangles.Add(Vertices.Count - 2);

            triangles.Add(Vertices.Count - 3);
            triangles.Add(Vertices.Count - 2);
            triangles.Add(Vertices.Count - 1);

            normals.Add((normals[normals.Count - 1] + normal) / 2);
            normals.Add((normals[normals.Count - 1] + normal) / 2);
        }

        public int getSegmentCount()
        {
            return nodes.Count - 1;
        }
    }

    public class Road : MonoBehaviour
    {
        private readonly bool allowBridges;
        private readonly bool allowTunnels;
        private readonly GridBounds gridBounds;
        private readonly int gridSize;
        private readonly int innerRadius;
        private readonly TerrainCollider MyMeshCollider;
        private readonly Vector2 offset;
        private readonly int outerRadius;
        private readonly bool restrictCurvature;
        private readonly int stepLength;
        private Vector2Int endingPosition;
        public RoadNode[] nodes;
        private Vector2Int startingPosition;

        public Road(Vector2Int start, Vector2Int end, int _bounds, int _innerRadius, int _outerRadius, int _stepLength,
            Vector2 _offset, bool _allowBridges, bool _allowTunnels, bool _restrictCurvature)
        {
            gridSize = _bounds / _stepLength;
            stepLength = _stepLength;
            offset.x = _bounds / 2 - _offset.x;
            offset.y = _bounds / 2 - _offset.y;
            nodes = new RoadNode[gridSize * gridSize];
            startingPosition = start;
            endingPosition = end;
            innerRadius = _innerRadius;
            outerRadius = _outerRadius;
            allowBridges = _allowBridges;
            allowTunnels = _allowTunnels;
            restrictCurvature = _restrictCurvature;

            MyMeshCollider = FindObjectOfType<UnityEngine.Terrain>().GetComponent<TerrainCollider>();
            var gridExpansion = Math.Max(1, 300 / stepLength);
            var lowerX = Math.Max(0, Math.Min(startingPosition.x, endingPosition.x) - gridExpansion);
            var upperX = Math.Min(gridSize - 1, Math.Max(startingPosition.x, endingPosition.x) + gridExpansion);
            var lowerZ = Math.Max(0, Math.Min(startingPosition.y, endingPosition.y) - gridExpansion);
            var upperZ = Math.Min(gridSize - 1, Math.Max(startingPosition.y, endingPosition.y) + gridExpansion);
            gridBounds = new GridBounds(lowerX, upperX, lowerZ, upperZ);

            if (startingPosition.x < lowerX || startingPosition.x > upperX || startingPosition.y < lowerZ ||
                startingPosition.y > upperZ)
            {
                Debug.Log("The starting position with " + startingPosition.x + " and " + startingPosition.y +
                          " is not in the grid");
                Debug.Log(
                    "The grid has the dimensions " + lowerX + " to " + upperX + " and " + lowerZ + " to " + upperZ);
                throw new Exception("Starting position is not in bounds");
            }

            if (endingPosition.x < lowerX || endingPosition.x > upperX || endingPosition.y < lowerZ ||
                endingPosition.y > upperZ) throw new Exception("Ending position is not in bounds");

            for (var z = 0; z < gridSize; ++z)
            for (var x = 0; x < gridSize; ++x)
            {
                var index = z * gridSize + x;
                if (z < gridBounds.lowerZ || z > gridBounds.upperZ || x < gridBounds.lowerX || x > gridBounds.upperX)
                {
                    nodes[index].isValid = false;
                }
                else
                {
                    nodes[index].isValid = true;
                    nodes[index] = new RoadNode(index)
                    {
                        pos = new Vector3(x * stepLength - offset.x, 0.0f, z * stepLength - offset.y)
                    };
                    (nodes[index].mapHeight, nodes[index].normal) =
                        RaycastAtPosition(new Vector2(x * stepLength - offset.x, z * stepLength - offset.y));
                    nodes[index].idx = index;
                    nodes[index].predIdx = -1;
                    nodes[index].dist = float.PositiveInfinity;
                }
            }
        }

        private (float height, Vector3 normal) RaycastAtPosition(Vector2 position)
        {
            const int maxHeight = 1024; // Height "Zugspitze" in m
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
            var index = 0;

            // for the heuristic we are just going to calculate the square 3D distance from the end point to every point
            for (var z_val = 0; z_val < gridSize; ++z_val)
            for (var x_val = 0; x_val < gridSize; ++x_val)
            {
                index = z_val * gridSize + x_val;
                if (!nodes[index].isValid) continue;
                var dists = nodes[index].pos - nodes[endingPosition.y * gridSize + endingPosition.x].pos;
                nodes[index].heuristic = dists.x * dists.x + dists.z * dists.z;
            }

            // FIXME: This can be way smaller or even dynamic
            var notVisited = new NodeHeap(gridSize * gridSize);
            var visited = new bool[gridSize * gridSize];

            // we are going to start at the starting point
            notVisited.push(startingPosition.y * gridSize + startingPosition.x, 0f);
            nodes[startingPosition.y * gridSize + startingPosition.x].dist = 0.0f;

            var count = 0;

            while (notVisited.lastIndex > 0 && !visited[endingPosition.y * gridSize + endingPosition.x])
            {
                if (count > 100000) throw new Exception("Too many iterations");
                var oldIndex = notVisited.pop();
                if (!nodes[oldIndex].isValid) continue;
                for (var i = -outerRadius; i <= outerRadius; ++i)
                for (var j = -outerRadius; j <= outerRadius; ++j)
                {
                    var x = oldIndex % gridSize + i;
                    var z = oldIndex / gridSize + j;

                    if (x < 0 || x >= gridSize || z < 0 || z >= gridSize) continue;

                    index = z * gridSize + x;

                    if (visited[index]) continue;

                    if (Math.Abs(i) <= innerRadius && Math.Abs(j) <= innerRadius) continue;

                    var relativeDist = nodes[index].pos - nodes[oldIndex].pos;
                    var relativeHeight = nodes[index].mapHeight - nodes[oldIndex].mapHeight;

                    var direction = new Vector3(relativeDist.x, relativeHeight, relativeDist.z).normalized;

                    var roadDist = 0f;
                    var tunnelDist = 0f;
                    var bridgeDist = 0f;

                    tunnelDist = nodes[oldIndex].dist +
                                 evalTunnel(relativeHeight, nodes[oldIndex].roadType) * relativeDist.sqrMagnitude;
                    bridgeDist = nodes[oldIndex].dist +
                                 evalBridge(nodes[index].mapHeight, relativeHeight) * relativeDist.sqrMagnitude;

                    if (!restrictCurvature)
                    {
                        roadDist = nodes[oldIndex].dist + evalSlope(direction) *
                            EvalHeightRoad(nodes[index].mapHeight) *
                            relativeDist.sqrMagnitude;
                    }
                    else
                    {
                        if (nodes[oldIndex].predIdx != -1)
                        {
                            var oldDirection = nodes[oldIndex].pos - nodes[nodes[oldIndex].predIdx].pos;
                            roadDist = nodes[oldIndex].dist + evalSlope(direction) *
                                EvalHeightRoad(nodes[index].mapHeight) * EvalCurvature(oldDirection, direction) *
                                relativeDist.sqrMagnitude;
                        }
                        else
                        {
                            roadDist = nodes[oldIndex].dist + evalSlope(direction) *
                                EvalHeightRoad(nodes[index].mapHeight) *
                                relativeDist.sqrMagnitude;
                        }
                    }

                    if (roadDist < nodes[index].dist && roadDist < tunnelDist && roadDist < bridgeDist)
                    {
                        nodes[index].dist = roadDist;
                        nodes[index].predIdx = oldIndex;
                        notVisited.push(index, roadDist);
                        nodes[index].roadType = RoadNodeType.Road;
                    }
                    else
                    {
                        if (tunnelDist < nodes[index].dist && tunnelDist < bridgeDist)
                        {
                            nodes[index].dist = tunnelDist;
                            nodes[index].predIdx = oldIndex;
                            notVisited.push(index, tunnelDist);
                            nodes[index].roadType = RoadNodeType.Tunnel;
                        }
                        else if (bridgeDist < nodes[index].dist)
                        {
                            nodes[index].dist = bridgeDist;
                            nodes[index].predIdx = oldIndex;
                            notVisited.push(index, bridgeDist);
                            nodes[index].roadType = RoadNodeType.Bridge;
                        }
                    }
                }

                count++;
                visited[oldIndex] = true;
            }

            if (nodes[endingPosition.y * gridSize + endingPosition.x].predIdx == -1)
                throw new Exception("No path found");

            // Set starting and ending point to road
            nodes[startingPosition.y * gridSize + startingPosition.x].roadType = RoadNodeType.Road;
            nodes[endingPosition.y * gridSize + endingPosition.x].roadType = RoadNodeType.Road;
        }

        private float EvalHeightRoad(float height)
        {
            if (height <= 24 || height >= 200) return float.PositiveInfinity;
            return 1;
        }

        private float evalSlope(Vector3 dirVec)
        {
            dirVec.y = Mathf.Abs(dirVec.y);
            dirVec.Normalize();

            // If the slope is above 30 degrees we do not want a path going up there (or down there)
            if (dirVec.y > 0.35f)
                return float.PositiveInfinity;
            return 1 + dirVec.y * 10;
        }

        private float evalTunnel(float height, RoadNodeType roadTypePrevNode)
        {
            if (!allowTunnels) return float.PositiveInfinity;
            height = Mathf.Abs(height);
            if (height < 7f && roadTypePrevNode != RoadNodeType.Tunnel) return float.PositiveInfinity;
            if (height >= 10f)
                return 20 + height;
            if (height >= 20f)
                return float.PositiveInfinity;
            return float.PositiveInfinity;
        }

        private float evalBridge(float height, float relativeHeight)
        {
            if (!allowBridges) return float.PositiveInfinity;
            if (height > 24f) return float.PositiveInfinity;
            height = Mathf.Abs(relativeHeight);
            return Mathf.Abs(relativeHeight) * 0.5f;
        }

        private float EvalCurvature(Vector3 dirVec, Vector3 nextDirVec)
        {
            var angle = Vector3.Angle(dirVec, nextDirVec);
            if (angle > 30f)
                return float.PositiveInfinity;
            return 1 + angle * 0.01f;
        }
    }
}