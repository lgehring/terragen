using System;
using System.Collections.Generic;
using Terrain;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Roads
{
    /// <summary>
    ///     Generates a spline with a road mesh
    /// </summary>
    public class RoadGenerator : MonoBehaviour
    {
        public Transform map;

        [FormerlySerializedAs("TerrainController")]
        public TerrainController terrainController;

        public GameObject terrain;
        public int innerRadiusForAStar;
        public int outerRadiusForAStar;
        public int gridSizeInMeters;
        public int splineResolution;
        public bool allowBridges;
        public bool allowTunnels;
        public bool restrictCurvature;
        private float[,] _backupHeights;
        private List<GameObject> _endPoints;
        private List<GameObject> _startingPoints;
        private float[,] adjustedHeightMap;

        private void OnValidate()
        {
            if (gridSizeInMeters < 1)
                gridSizeInMeters = 1;
            if (gridSizeInMeters > 2410) gridSizeInMeters = 2410;
            if (innerRadiusForAStar < 1) innerRadiusForAStar = 1;
            if (innerRadiusForAStar > 2410) innerRadiusForAStar = 2410;
            if (outerRadiusForAStar < 1) outerRadiusForAStar = 1;
            if (outerRadiusForAStar > 2410) outerRadiusForAStar = 2410;
            if (splineResolution < 1) splineResolution = 1;
            if (splineResolution > 100) splineResolution = 100;
        } // ReSharper disable Unity.PerformanceAnalysis

        /// <summary>
        ///     Creates a spline extrapolates a road mesh onto it and adds it to the scene
        /// </summary>
        /// <exception cref="ArgumentException"> Throws an exception if the road has to many nodes </exception>
        public void DrawRoadMesh()
        {
            _startingPoints.Clear();
            _endPoints.Clear();
            if (map == null)
                throw new ArgumentException("The map is null");
            if (terrain == null)
                throw new ArgumentException("The terrain is null");
            _backupHeights = terrainController.GetBackupHeights();

            terrain.GetComponent<UnityEngine.Terrain>().terrainData.SetHeights(0, 0, _backupHeights);
            Transform[] children;
            children = GetComponentsInChildren<Transform>();
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].gameObject.tag == "starting point")
                    _startingPoints.Add(children[i].gameObject);
                if (children[i].gameObject.tag == "ending point")
                    _endPoints.Add(children[i].gameObject);
            }

            for (var i = GetComponentsInChildren<Transform>().Length - 1; i > 0; i--)
            {
                var toBeDestroyed = GetComponentsInChildren<Transform>()[i].gameObject;
                if (toBeDestroyed.tag == "starting point" || toBeDestroyed.tag == "ending point")
                    continue;
                DestroyImmediate(toBeDestroyed);
            }

            adjustedHeightMap = new float[_backupHeights.GetLength(0), _backupHeights.GetLength(1)];

            for (var i = 0; i < _backupHeights.GetLength(0); i++)
            for (var j = 0; j < _backupHeights.GetLength(1); j++)
                adjustedHeightMap[i, j] = _backupHeights[i, j];

            for (var roadIdx = 0; roadIdx < _startingPoints.Count; roadIdx++)
            {
                var bounds = (int)Mathf.Abs(map.localScale.x * 10);
                var mapPosition = map.position;
                var offset = new Vector2Int((int)mapPosition.x, (int)mapPosition.z);
                var startPosition = _startingPoints[roadIdx].transform.position;
                var start = new Vector2Int(
                    ((int)startPosition.x + bounds / 2 - offset.x) / gridSizeInMeters,
                    ((int)startPosition.z + bounds / 2 - offset.y) / gridSizeInMeters);
                var endPosition = _endPoints[roadIdx].transform.position;
                var end = new Vector2Int(((int)endPosition.x + bounds / 2 - offset.x) / gridSizeInMeters,
                    ((int)endPosition.z + bounds / 2 - offset.y) / gridSizeInMeters);

                if (Mathf.Min(end.x, end.y, start.x, start.y) < -bounds / 2 ||
                    Mathf.Max(end.x, end.y, start.x, start.y) > bounds / 2)
                    throw new ArgumentException("The starting or endpoint is out of bounds");

                var road = new Road(start, end, bounds, innerRadiusForAStar, outerRadiusForAStar, gridSizeInMeters,
                    offset,
                    allowBridges, allowTunnels, restrictCurvature);
                road.generateRoad(start, end);

                var startIdx = start.x + start.y * bounds / gridSizeInMeters;
                var endingIdx = end.x + end.y * bounds / gridSizeInMeters;

                var path = new List<Vector3>();
                var normals = new List<Vector3>();
                var roadTypes = new List<RoadNodeType>();

                var prevIdx = endingIdx;
                var index = road.nodes[prevIdx].predIdx;

                path.Add(new Vector3(road.nodes[prevIdx].pos.x, road.nodes[prevIdx].mapHeight,
                    road.nodes[prevIdx].pos.z));
                normals.Add(road.nodes[prevIdx].normal);
                roadTypes.Add(road.nodes[prevIdx].roadType);

                var count = 0;

                while (index != startIdx)
                {
                    if (count > 1000) throw new ArgumentException("The road is too long");

                    if (road.nodes[index].roadType == RoadNodeType.Tunnel)
                    {
                        var tunnelNodes = 1;
                        var tunnelStartIdx = index;
                        var tunnelEndIdx = index;
                        var tunnelIndices = new List<int>
                        {
                            index
                        };

                        prevIdx = index;
                        index = road.nodes[prevIdx].predIdx;

                        // Ensure that the tunnel is at least 2 nodes long
                        road.nodes[index].roadType = RoadNodeType.Tunnel;

                        while (road.nodes[index].roadType == RoadNodeType.Tunnel && index != startIdx)
                        {
                            tunnelNodes++;
                            tunnelEndIdx = index;
                            tunnelIndices.Add(tunnelEndIdx);
                            prevIdx = index;
                            index = road.nodes[prevIdx].predIdx;
                            count++;
                        }

                        var tunnelHeightDeltaPerNode =
                            road.nodes[tunnelEndIdx].mapHeight - road.nodes[tunnelStartIdx].mapHeight;
                        for (var i = 0; i < tunnelNodes; i++)
                        {
                            path.Add(new Vector3(road.nodes[tunnelIndices[i]].pos.x,
                                road.nodes[tunnelIndices[i]].mapHeight +
                                tunnelHeightDeltaPerNode * i / (tunnelNodes - 1),
                                road.nodes[tunnelIndices[i]].pos.z));
                            normals.Add(road.nodes[tunnelIndices[i]].normal);
                            roadTypes.Add(RoadNodeType.Tunnel);
                        }

                        if (prevIdx == index) break;
                    }
                    else if (road.nodes[index].roadType == RoadNodeType.Bridge)
                    {
                        var bridgeNodes = 1;
                        var bridgeStartIdx = index;
                        var bridgeEndIdx = index;
                        var bridgeIndices = new List<int>
                        {
                            index
                        };

                        prevIdx = index;
                        index = road.nodes[prevIdx].predIdx;

                        // Ensure that the Bridge is at least 2 nodes long
                        road.nodes[index].roadType = RoadNodeType.Bridge;

                        while (road.nodes[index].roadType == RoadNodeType.Bridge && index != startIdx)
                        {
                            bridgeNodes++;
                            bridgeEndIdx = index;
                            bridgeIndices.Add(bridgeEndIdx);
                            prevIdx = index;
                            index = road.nodes[prevIdx].predIdx;
                            count++;
                        }

                        var bridgeHeightDeltaPerNode =
                            road.nodes[bridgeEndIdx].mapHeight - road.nodes[bridgeStartIdx].mapHeight;
                        for (var i = 0; i < bridgeNodes; i++)
                        {
                            path.Add(new Vector3(road.nodes[bridgeIndices[i]].pos.x,
                                road.nodes[bridgeStartIdx].mapHeight + bridgeHeightDeltaPerNode * i / (bridgeNodes - 1),
                                road.nodes[bridgeIndices[i]].pos.z));
                            normals.Add(road.nodes[bridgeIndices[i]].normal);
                            roadTypes.Add(RoadNodeType.Bridge);
                        }

                        if (prevIdx == index) break;
                    }
                    else
                    {
                        path.Add(new Vector3(road.nodes[index].pos.x, road.nodes[index].mapHeight,
                            road.nodes[index].pos.z));
                        normals.Add(road.nodes[index].normal);
                        roadTypes.Add(RoadNodeType.Road);
                        prevIdx = index;
                        index = road.nodes[prevIdx].predIdx;
                        count++;
                        if (prevIdx == index && index != startIdx)
                            throw new ArgumentException("There is no path between the starting and endpoint");
                    }
                }

                var tangents = new List<Vector3>();

                (path, normals, tangents, roadTypes) = generateSpline(path, normals, tangents, roadTypes);
                generateRoadMesh(path, normals, tangents, roadTypes);
            }

            var roadChildren = new List<GameObject>();
            var allChildren = GetComponentsInChildren<Transform>();
            for (var i = 0; i < allChildren.Length; i++)
                if (allChildren[i].gameObject.tag == "Road")
                    roadChildren.Add(allChildren[i].GameObject());

            var allRoads = roadChildren[0];

            var numberOfSubMeshes = 0;
            for (var i = 0; i < roadChildren.Count; i++)
                numberOfSubMeshes += roadChildren[i].GetComponent<MeshFilter>().sharedMesh.subMeshCount;
            var combine = new CombineInstance[numberOfSubMeshes];
            var meshCount = 0;
            for (var i = 0; i < roadChildren.Count; i++)
            for (var j = 0; j < roadChildren[i].GetComponent<MeshFilter>().sharedMesh.subMeshCount; j++)
            {
                combine[meshCount].subMeshIndex = j;
                combine[meshCount].mesh = roadChildren[i].GetComponent<MeshFilter>().sharedMesh;
                combine[meshCount++].transform = roadChildren[i].transform.localToWorldMatrix;
            }

            allRoads.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            allRoads.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine, false);
        }

        private (List<Vector3>, List<Vector3>, List<Vector3>, List<RoadNodeType>) generateSpline(List<Vector3> points,
            List<Vector3> normals, List<Vector3> tangents, List<RoadNodeType> roadTypes)
        {
            if (points.Count < 3)
                throw new ArgumentException("The road has to have at least 3 nodes");
            tangents.Add(points[1] - points[0]);
            for (var i = 0; i < points.Count - 2; i++) tangents.Add(points[i + 2] - points[i]);

            tangents.Add(points[^1] - points[^2]);
            if (splineResolution == 1) return (points, normals, tangents, roadTypes);

            var newPoints = new List<Vector3>();
            var newNormals = new List<Vector3>();
            var newTangents = new List<Vector3>();
            var newRoadTypes = new List<RoadNodeType>();

            for (var i = 0; i < splineResolution; i++)
            {
                var t = (float)i / splineResolution;
                newPoints.Add((1 - t) * points[0] + t * points[1]);
                newNormals.Add((1 - t) * normals[0] + t * normals[1]);
                newTangents.Add((1 - t) * tangents[0] + t * tangents[1]);
                newRoadTypes.Add(roadTypes[0]);
            }

            for (var j = 1; j < points.Count - 2; j++)
                if (roadTypes[j] == RoadNodeType.Tunnel)
                {
                    var tunnelCount = 0;
                    var startIdx = j;

                    while (roadTypes[j] == RoadNodeType.Tunnel && j < points.Count - 1)
                    {
                        tunnelCount++;
                        j++;
                    }

                    var dist = Vector3.Distance(points[startIdx], points[j]);
                    // Magic number for the best distance between bridge nodes
                    var tunnelNodes = (int)(dist / 10);
                    for (var k = 0; k <= tunnelNodes; k++)
                    {
                        newPoints.Add(Vector3.Lerp(points[startIdx], points[j], (float)k / tunnelNodes));
                        newNormals.Add(Vector3.up);
                        if (k == 0)
                            newTangents.Add(newTangents[^1]);
                        else
                            newTangents.Add(points[j] - points[startIdx]);
                        newRoadTypes.Add(RoadNodeType.Tunnel);
                    }

                    j--;
                }
                else if (roadTypes[j] == RoadNodeType.Bridge)
                {
                    var bridgeCount = 0;
                    var startIdx = j;

                    while (roadTypes[j] == RoadNodeType.Bridge)
                    {
                        bridgeCount++;
                        j++;
                    }

                    var dist = Vector3.Distance(points[startIdx], points[j]);
                    // Magic number for the best distance between bridge nodes
                    var bridgeNodes = (int)(dist / 50);
                    for (var k = 0; k <= bridgeNodes; k++)
                    {
                        newPoints.Add(Vector3.Lerp(points[startIdx], points[j], (float)k / bridgeNodes));
                        newNormals.Add(Vector3.up);
                        newTangents.Add(points[j] - points[startIdx]);
                        newRoadTypes.Add(RoadNodeType.Bridge);
                    }

                    j--;
                }
                else
                {
                    if (roadTypes[j - 1] == RoadNodeType.Tunnel) continue;

                    var p0 = points[j];
                    var p1 = 0.5f * (-points[j - 1] + points[j + 1]);
                    var p2 = 0.5f * (2 * points[j - 1] - 5 * points[j] + 4 * points[j + 1] - points[j + 2]);
                    var p3 = 0.5f * (-points[j - 1] + 3 * points[j] - 3 * points[j + 1] + points[j + 2]);

                    var n0 = normals[j];
                    var n1 = 0.5f * (-normals[j - 1] + normals[j + 1]);
                    var n2 = 0.5f * (2 * normals[j - 1] - 5 * normals[j] + 4 * normals[j + 1] - normals[j + 2]);
                    var n3 = 0.5f * (-normals[j - 1] + 3 * normals[j] - 3 * normals[j + 1] + normals[j + 2]);

                    var t0 = tangents[j];
                    var t1 = 0.5f * (-tangents[j - 1] + tangents[j + 1]);
                    var t2 = 0.5f * (2 * tangents[j - 1] - 5 * tangents[j] + 4 * tangents[j + 1] - tangents[j + 2]);
                    var t3 = 0.5f * (-tangents[j - 1] + 3 * tangents[j] - 3 * tangents[j + 1] + tangents[j + 2]);

                    for (var i = 0; i < splineResolution; i++)
                    {
                        var t = (float)i / splineResolution;
                        newPoints.Add(p0 + t * p1 + t * t * p2 + t * t * t * p3);
                        newNormals.Add(n0 + t * n1 + t * t * n2 + t * t * t * n3);
                        newTangents.Add(t0 + t * t1 + t * t * t2 + t * t * t * t3);
                        newRoadTypes.Add(RoadNodeType.Road);
                    }
                }

            for (var i = 0; i < splineResolution; i++)
            {
                var t = (float)i / splineResolution;
                newPoints.Add((1 - t) * points[points.Count - 2] + t * points[points.Count - 1]);
                newNormals.Add((1 - t) * normals[points.Count - 2] + t * normals[points.Count - 1]);
                newTangents.Add((1 - t) * tangents[points.Count - 2] + t * tangents[points.Count - 1]);
                newRoadTypes.Add(roadTypes[points.Count - 1]);
            }

            return (newPoints, newNormals, newTangents, newRoadTypes);
        }

        private void generateRoadMesh(List<Vector3> points, List<Vector3> normals, List<Vector3> tangents,
            List<RoadNodeType> roadNodeTypes)
        {
            var roadMesh = PrefabUtility.LoadPrefabContents("Assets/Resources/StraightRoad.prefab");
            var tunnelMesh = PrefabUtility.LoadPrefabContents("Assets/Resources/Tunnel.prefab");
            var bridgeMesh = PrefabUtility.LoadPrefabContents("Assets/Resources/Bridge.prefab");

            var roadSegments = new GameObject[points.Count - 1];
            var path = new Vector3[(points.Count - 1) * 2];

            for (var i = 0; i < points.Count - 1; i++)
            {
                if (roadNodeTypes[i] == RoadNodeType.Road)
                    roadSegments[i] = Instantiate(roadMesh);
                else if (roadNodeTypes[i] == RoadNodeType.Bridge)
                    roadSegments[i] = Instantiate(bridgeMesh);
                else
                    roadSegments[i] = Instantiate(tunnelMesh);

                roadSegments[i].transform.position = points[i];
                roadSegments[i].transform.parent = transform;

                roadSegments[i].GetComponent<MeshFilter>().sharedMesh = new Mesh();

                if (roadNodeTypes[i] == RoadNodeType.Road)
                    roadSegments[i].GetComponent<MeshFilter>().sharedMesh =
                        Instantiate(roadMesh.GetComponent<MeshFilter>().sharedMesh);
                else if (roadNodeTypes[i] == RoadNodeType.Bridge)
                    roadSegments[i].GetComponent<MeshFilter>().sharedMesh =
                        Instantiate(bridgeMesh.GetComponent<MeshFilter>().sharedMesh);
                else
                    roadSegments[i].GetComponent<MeshFilter>().sharedMesh =
                        Instantiate(tunnelMesh.GetComponent<MeshFilter>().sharedMesh);

                (path[i * 2], path[i * 2 + 1]) = DeformMesh(roadSegments[i], points[i], points[i + 1], tangents[i],
                    tangents[i + 1], normals[i], normals[i + 1], roadNodeTypes[i]);
            }

            var hasBeenAdjusted = new bool[_backupHeights.GetLength(0), _backupHeights.GetLength(1)];
            var terrainData = terrain.GetComponent<UnityEngine.Terrain>().terrainData;

            for (var i = 0; i < points.Count - 2; i++)
                if (roadNodeTypes[i] == RoadNodeType.Road)
                    (adjustedHeightMap, hasBeenAdjusted) = mapTerrainToRoad(terrainData, adjustedHeightMap,
                        hasBeenAdjusted, path[i * 2], path[i * 2 + 1], path[(i + 1) * 2],
                        path[(i + 1) * 2 + 1]);

            terrain.GetComponent<UnityEngine.Terrain>().terrainData.SetHeights(0, 0, adjustedHeightMap);

            combineMeshes(roadSegments, roadNodeTypes);

            var endRoad = GetComponentsInChildren<Transform>()[3].GameObject();

            var uvs = endRoad.GetComponent<MeshFilter>().sharedMesh.uv;

            for (var j = 0; j < uvs.Length; j++)
                uvs[j] = new Vector2(uvs[j].x, uvs[j].y * Vector3.Distance(points[0], points[points.Count - 1]) / 100);

            endRoad.GetComponent<MeshFilter>().sharedMesh.uv = uvs;

            endRoad.AddComponent<MeshCollider>();
            endRoad.GetComponent<MeshCollider>().sharedMesh = endRoad.GetComponent<MeshFilter>().sharedMesh;
        }

        private (Vector3, Vector3) DeformMesh(GameObject roadSegment, Vector3 start, Vector3 end, Vector3 tangentStart,
            Vector3 tangentEnd, Vector3 normalStart, Vector3 normalEnd, RoadNodeType roadNodeType)
        {
            var mesh = roadSegment.GetComponent<MeshFilter>().sharedMesh;
            var meshTransform = roadSegment.transform;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;

            var leftMax = float.MinValue;
            var rightMax = float.MaxValue;
            var zMax = float.MinValue;
            var zMin = float.MaxValue;
            var leftIndex = 0;
            var rightIndex = 0;

            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                if (vertex.x > leftMax)
                {
                    leftMax = vertex.x;
                    leftIndex = i;
                }

                if (vertex.x < rightMax)
                {
                    rightMax = vertex.x;
                    rightIndex = i;
                }

                if (vertex.z > zMax)
                    zMax = vertex.z;

                if (vertex.z < zMin)
                    zMin = vertex.z;
            }


            var startRotation = Quaternion.LookRotation(tangentStart, Vector3.up);
            var endRotation = Quaternion.LookRotation(tangentEnd, Vector3.up);

            for (var i = 0; i < vertices.Length; i++)
            {
                if (roadNodeType == RoadNodeType.Tunnel)
                {
                    vertices[i].x *= 0.6f;
                    vertices[i].y *= 0.6f;
                }

                if (roadNodeType == RoadNodeType.Bridge)
                {
                    vertices[i].x *= 1.75f;
                    vertices[i].y *= 1.75f;
                }

                var vertex = vertices[i];
                vertices[i].z = 0;
                vertices[i] = Quaternion.Lerp(startRotation, endRotation, (vertex.z - zMin) / (zMax - zMin))
                              * vertices[i];
                vertices[i] += Vector3.Lerp(Vector3.zero, end - start, (vertex.z - zMin) / (zMax - zMin));
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;

            var leftMostVector = meshTransform.TransformPoint(vertices[leftIndex]);
            var rightMostVector = meshTransform.TransformPoint(vertices[rightIndex]);

            return (leftMostVector, rightMostVector);
        }

        private void combineMeshes(GameObject[] targets, List<RoadNodeType> nodeTypes)
        {
            var meshCount = 3;
            var hasBridge = false;
            var hasTunnel = false;
            var numberOfRoads = 0;
            var numberOfBridges = 0;
            var numberOfTunnels = 0;

            for (var i = 0; i < targets.Length; i++)
                if (nodeTypes[i] == RoadNodeType.Tunnel)
                {
                    if (!hasTunnel)
                    {
                        meshCount += 1;
                        hasTunnel = true;
                    }

                    numberOfTunnels += 1;
                }
                else if (nodeTypes[i] == RoadNodeType.Bridge)
                {
                    if (!hasBridge)
                    {
                        meshCount += 1;
                        hasBridge = true;
                    }

                    numberOfBridges += 1;
                }
                else
                {
                    numberOfRoads += 1;
                }

            // number of roads need to be decreased by 1, because we don't need to combine the last road

            var resultingMeshes = new Mesh[meshCount];
            for (var i = 0; i < meshCount; i++)
                resultingMeshes[i] = combineMeshesHelper(targets, i, nodeTypes, numberOfRoads, numberOfBridges,
                    numberOfTunnels);

            var finalSegments = new GameObject[meshCount];
            for (var i = 0; i < meshCount; i++)
            {
                finalSegments[i] = Instantiate(targets[i]);
                finalSegments[i].transform.parent = transform;
                finalSegments[i].GetComponent<MeshFilter>().sharedMesh = new Mesh();
                finalSegments[i].transform.position = new Vector3(0, 0, 0);
                finalSegments[i].GetComponent<MeshFilter>().sharedMesh.subMeshCount = meshCount;

                finalSegments[i].GetComponent<MeshFilter>().sharedMesh = resultingMeshes[i];
                finalSegments[i].GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
                finalSegments[i].GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
                finalSegments[i].GetComponent<MeshFilter>().sharedMesh.RecalculateTangents();
                finalSegments[i].GetComponent<MeshRenderer>().sharedMaterials =
                    targets[0].GetComponent<MeshRenderer>().sharedMaterials;
            }

            for (var i = 0; i < targets.Length; i++) DestroyImmediate(targets[i]);

            var combine = new CombineInstance[meshCount];

            for (var i = 0; i < meshCount; i++)
            {
                combine[i].mesh = finalSegments[i].GetComponent<MeshFilter>().sharedMesh;
                combine[i].transform = finalSegments[i].transform.localToWorldMatrix;
            }

            var endRoad = new GameObject();
            endRoad.AddComponent<MeshFilter>();
            endRoad.AddComponent<MeshRenderer>();

            endRoad.transform.parent = transform;
            endRoad.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            endRoad.GetComponent<MeshFilter>().sharedMesh.indexFormat = IndexFormat.UInt32;
            endRoad.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine, false);

            endRoad.GetComponent<MeshRenderer>().sharedMaterials =
                finalSegments[0].GetComponent<MeshRenderer>().sharedMaterials;

            if (hasBridge)
            {
                var materials = new Material[endRoad.GetComponent<MeshRenderer>().sharedMaterials.Length + 1];
                endRoad.GetComponent<MeshRenderer>().sharedMaterials.CopyTo(materials, 0);
                materials[^1] = endRoad.GetComponent<MeshRenderer>().sharedMaterials[0];
                endRoad.GetComponent<MeshRenderer>().sharedMaterials = materials;
            }

            if (hasTunnel)
            {
                var materials = new Material[endRoad.GetComponent<MeshRenderer>().sharedMaterials.Length + 1];
                endRoad.GetComponent<MeshRenderer>().sharedMaterials.CopyTo(materials, 0);
                materials[^1] = endRoad.GetComponent<MeshRenderer>().sharedMaterials[1];
                endRoad.GetComponent<MeshRenderer>().sharedMaterials = materials;
            }

            endRoad.transform.name = "Road";

            endRoad.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
            endRoad.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
            endRoad.GetComponent<MeshFilter>().sharedMesh.RecalculateTangents();

            endRoad.tag = "Road";

            for (var i = 0; i < meshCount; i++) DestroyImmediate(finalSegments[i]);
        }

        private Mesh combineMeshesHelper(GameObject[] targets, int index, List<RoadNodeType> nodeTypes,
            int numberOfRoads, int numberOfBridges, int numberOfTunnels)
        {
            var combineLength = 0;
            var roadType = RoadNodeType.Road;
            switch (index)
            {
                case 0:
                    combineLength = numberOfRoads;
                    break;
                case 1:
                    combineLength = numberOfRoads;
                    break;
                case 2:
                    combineLength = numberOfRoads;
                    break;
                case 3:
                    if (numberOfBridges == 0)
                    {
                        combineLength = numberOfTunnels;
                        roadType = RoadNodeType.Tunnel;
                        break;
                    }

                    combineLength = numberOfBridges;
                    roadType = RoadNodeType.Bridge;
                    break;
                case 4:
                    combineLength = numberOfTunnels;
                    roadType = RoadNodeType.Tunnel;
                    break;
            }

            var combine = new CombineInstance[combineLength];

            var idx = 0;

            var roadCount = 0;
            for (var i = 0; i < targets.Length - 1; i++)
            {
                if (nodeTypes[i] != roadType) continue;
                roadCount++;
            }

            for (var i = 0; i < targets.Length - 1; i++)
            {
                if (nodeTypes[i] != roadType) continue;
                if (roadType == RoadNodeType.Road)
                    combine[idx].subMeshIndex = index;
                combine[idx].mesh = targets[i].GetComponent<MeshFilter>().sharedMesh;
                combine[idx++].transform = targets[i].transform.localToWorldMatrix;
            }

            var returnMesh = new Mesh();
            returnMesh.indexFormat = IndexFormat.UInt32;
            returnMesh.CombineMeshes(combine);
            return returnMesh;
        }

        private (float[,], bool[,]) mapTerrainToRoad(TerrainData terrainData, float[,] oldHeightMap,
            bool[,] hasBeenAdjusted, Vector3 worldPrevLeft, Vector3 worldPrevRight, Vector3 worldLeft,
            Vector3 worldRight)
        {
            var terrainSize = terrainData.size;
            var terrainHeight = terrainData.heightmapResolution;
            var terrainWidth = terrainData.heightmapResolution;
            var radius = 10;
            var epsilon = 2f;
            var terrainHeightData = oldHeightMap;

            // Transform the world coordinates to local coordinates
            var prevLeft = terrain.transform.InverseTransformPoint(worldPrevLeft);
            var prevRight = terrain.transform.InverseTransformPoint(worldPrevRight);
            var left = terrain.transform.InverseTransformPoint(worldLeft);
            var right = terrain.transform.InverseTransformPoint(worldRight);

            // Create a bool array that checks if terrain was already adjusted
            var terrainAdjusted = hasBeenAdjusted;

            var maxX = (int)Mathf.Ceil(Mathf.Max(left.x, right.x, prevLeft.x, prevRight.x)) + radius;
            var minX = (int)Mathf.Floor(Mathf.Min(left.x, right.x, prevLeft.x, prevRight.x)) - radius;
            var maxZ = (int)Mathf.Ceil(Mathf.Max(left.z, right.z, prevLeft.z, prevRight.z)) + radius;
            var minZ = (int)Mathf.Floor(Mathf.Min(left.z, right.z, prevLeft.z, prevRight.z)) - radius;

            // Transform to world coordinates
            maxX = (int)(maxX / terrainSize.x * terrainWidth);
            minX = (int)(minX / terrainSize.x * terrainWidth);
            maxZ = (int)(maxZ / terrainSize.z * terrainHeight);
            minZ = (int)(minZ / terrainSize.z * terrainHeight);

            // Creating a Triangle from most leftX to prevLeftX to prevRightX and from leftX to prevRightX to rightX
            // and then adjusting the terrain height for each point in the triangle
            var areaOne = CalcDetTriangle2D(left, prevLeft, prevRight);
            var areaTwo = CalcDetTriangle2D(left, prevRight, right);

            var averageHeight = (left.y + prevLeft.y + right.y + prevRight.y) / 4;
            var maxDist = new Vector2(maxX - minX, maxZ - minZ).magnitude;

            for (var j = minX; j <= maxX; j++)
            for (var k = minZ; k <= maxZ; k++)
            {
                var worldX = (int)((float)j / terrainWidth * terrainSize.x);
                var worldZ = (int)((float)k / terrainHeight * terrainSize.z);
                for (var wigX = -1; wigX <= 1; wigX++)
                for (var wigZ = -1; wigZ <= 1; wigZ++)
                {
                    var point = new Vector2(worldX + wigX, worldZ + wigZ);

                    var bary1 = CalcBary1(prevLeft, prevRight, point) / areaOne;
                    if (bary1 <= 1f && bary1 >= 0f)
                    {
                        var bary2 = CalcBary2(left, prevRight, point) / areaOne;
                        if (bary2 <= 1f && bary1 + bary2 <= 1f && bary2 >= 0f)
                        {
                            var bary3 = 1 - bary1 - bary2;
                            var y = bary1 * left.y + bary2 * prevLeft.y + bary3 * prevRight.y;

                            terrainHeightData[k, j] = (y - epsilon) / terrainSize.y;
                            terrainAdjusted[k, j] = true;
                            break;
                        }
                    }
                    else
                    {
                        var bary4 = CalcBary1(prevRight, right, point) / areaTwo;
                        if (bary4 <= 1f && bary4 >= 0f)
                        {
                            var bary5 = CalcBary2(left, right, point) / areaTwo;
                            if (bary5 <= 1f && bary4 + bary5 <= 1f && bary5 >= 0f)
                            {
                                var bary6 = 1 - bary5 - bary4;
                                var y = bary4 * left.y + bary5 * prevRight.y + bary6 * right.y;

                                terrainHeightData[k, j] = (y - epsilon) / terrainSize.y;
                                terrainAdjusted[k, j] = true;
                                break;
                            }
                        }
                    }
                }

                if (!terrainAdjusted[k, j])
                {
                    var curHeight = terrainHeightData[k, j];

                    var distX = Mathf.Min(j - minX, maxX - j);
                    var distZ = Mathf.Min(k - minZ, maxZ - k);
                    var dist = Mathf.Min(distX, distZ);
                    var weight = dist / maxDist;

                    terrainHeightData[k, j] = Mathf.Lerp(curHeight, (averageHeight - epsilon) / terrainSize.y,
                        dist / maxDist);
                }
            }

            return (terrainHeightData, terrainAdjusted);
        }

        private float CalcDetTriangle2D(Vector3 firstVec, Vector3 secondVec, Vector3 thirdVec)
        {
            return (secondVec.z - thirdVec.z) * (firstVec.x - thirdVec.x) +
                   (thirdVec.x - secondVec.x) * (firstVec.z - thirdVec.z);
        }

        private float CalcBary1(Vector3 secondVec, Vector3 thirdVec, Vector2 point)
        {
            return (secondVec.z - thirdVec.z) * (point.x - thirdVec.x) +
                   (thirdVec.x - secondVec.x) * (point.y - thirdVec.z);
        }

        private float CalcBary2(Vector3 firstVec, Vector3 thirdVec, Vector2 point)
        {
            return (thirdVec.z - firstVec.z) * (point.x - thirdVec.x) +
                   (firstVec.x - thirdVec.x) * (point.y - thirdVec.z);
        }
    }
}