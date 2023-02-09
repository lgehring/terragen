using System;
using System.Collections.Generic;
using Terrain;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace Roads
{
    /// <summary>
    ///     Generates a spline with a road mesh
    /// </summary>
    public class RoadGenerator : MonoBehaviour
    {
        public Transform map;
        [FormerlySerializedAs("TerrainController")] public TerrainController terrainController;
        public GameObject terrain;
        public int innerRadiusForAStar;
        public int outerRadiusForAStar;
        public int gridSizeInMeters;
        public int splineResolution;
        private GameObject _endPoint;
        private GameObject _startingPoint;

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
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        ///     Creates a spline extrapolates a road mesh onto it and adds it to the scene
        /// </summary>
        /// <exception cref="ArgumentException"> Throws an exception if the road has to many nodes </exception>
        public void DrawRoadMesh()
        {
            if (map == null) 
                throw new ArgumentException("The map is null");
            if (terrain == null) 
                throw new ArgumentException("The terrain is null");
            terrain.GetComponent<UnityEngine.Terrain>().terrainData.SetHeights(0, 0, terrainController.GetBackupHeights());

            _startingPoint = GetComponentsInChildren<Transform>()[1].gameObject;
            _endPoint = GetComponentsInChildren<Transform>()[2].gameObject;

            for (var i = GetComponentsInChildren<Transform>().Length - 1; i > 2; i--)
            {
                var toBeDestroyed = GetComponentsInChildren<Transform>()[i].gameObject;
                DestroyImmediate(toBeDestroyed);
            }

            var bounds = (int)Mathf.Abs(map.localScale.x * 10);
            var mapPosition = map.position;
            var offset = new Vector2Int((int)mapPosition.x, (int)mapPosition.z);
            var startPosition = _startingPoint.transform.position;
            var start = new Vector2Int(
                ((int)startPosition.x + bounds / 2 - offset.x) / gridSizeInMeters,
                ((int)startPosition.z + bounds / 2 - offset.y) / gridSizeInMeters);
            var endPosition = _endPoint.transform.position;
            var end = new Vector2Int(((int)endPosition.x + bounds / 2 - offset.x) / gridSizeInMeters,
                ((int)endPosition.z + bounds / 2 - offset.y) / gridSizeInMeters);

            if (Mathf.Min(end.x, end.y, start.x, start.y) < -bounds / 2 ||
                Mathf.Max(end.x, end.y, start.x, start.y) > bounds / 2)
                throw new ArgumentException("The starting or endpoint is out of bounds");

            var road = new Road(start, end, bounds, innerRadiusForAStar, outerRadiusForAStar, gridSizeInMeters, offset);
            road.generateRoad(start, end);

            var startIdx = start.x + start.y * bounds / gridSizeInMeters;
            var endingIdx = end.x + end.y * bounds / gridSizeInMeters;

            var path = new List<Vector3>();
            var normals = new List<Vector3>();
            var prevIdx = endingIdx;
            var index = road.nodes[prevIdx].predIdx;
            path.Add(new Vector3(road.nodes[prevIdx].pos.x, road.nodes[prevIdx].mapHeight, road.nodes[prevIdx].pos.z));
            normals.Add(road.nodes[prevIdx].normal);
            var count = 0;
            while (index != startIdx)
            {
                if (count > 1000) throw new ArgumentException("The road is too long");
                //TODO: Fix tunnels
                if (road.nodes[index].roadType == RoadNodeType.Tunnel && false)
                {
                    var tunnelNodes = 2;
                    var tunnelStartIdx = prevIdx;
                    var tunnelEndIdx = index;
                    while (road.nodes[index].roadType == RoadNodeType.Tunnel && index != startIdx)
                    {
                        tunnelNodes++;
                        tunnelEndIdx = road.nodes[index].predIdx;
                        prevIdx = index;
                        index = road.nodes[prevIdx].predIdx;
                        count++;
                    }

                    var tunnelHeightDeltaPerNode =
                        road.nodes[tunnelEndIdx].mapHeight - road.nodes[tunnelStartIdx].mapHeight;
                    for (var i = 0; i < tunnelNodes; i++)
                    {
                        path.Add(new Vector3(road.nodes[tunnelStartIdx].pos.x,
                            road.nodes[tunnelStartIdx].mapHeight + tunnelHeightDeltaPerNode * i / tunnelNodes,
                            road.nodes[tunnelStartIdx].pos.z));
                        normals.Add(road.nodes[tunnelStartIdx].normal);
                    }

                    if (prevIdx == index) break;
                }
                else
                {
                    path.Add(new Vector3(road.nodes[index].pos.x, road.nodes[index].mapHeight,
                        road.nodes[index].pos.z));
                    normals.Add(road.nodes[index].normal);
                    prevIdx = index;
                    index = road.nodes[prevIdx].predIdx;
                    count++;
                    if (prevIdx == index && index != startIdx)
                        throw new ArgumentException("There is no path between the starting and endpoint");
                }
            }
            List<Vector3> tangents = new List<Vector3>();
            (path, normals, tangents) = generateSpline(path, normals, tangents);
            generateRoadMesh(path, normals, tangents);
            
        }

        private (List<Vector3>, List<Vector3>, List<Vector3>) generateSpline(List<Vector3> points, List<Vector3> normals, List<Vector3> tangents)
        {
            if (points.Count < 3)
                throw new ArgumentException("The road has to have at least 3 nodes");
            tangents.Add(points[1] - points[0]);
            for (int i = 0; i < points.Count - 2; i++)
            {
                tangents.Add(points[i + 2] - points[i]);
            }
            tangents.Add(points[^1] - points[^2]);
            if (splineResolution == 1)
            {
                return (points, normals, tangents);
            }
            List<Vector3> newPoints = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector3> newTangents = new List<Vector3>();


            for (int i = 0; i < splineResolution; i++)
            {
                float t = (float)i / splineResolution;
                newPoints.Add((1-t) * points[0] + t * points[1]);
                newNormals.Add((1 - t) * normals[0] + t * normals[1]);
                newTangents.Add((1 - t) * tangents[0] + t * tangents[1]);
            }
            
            for (int j = 1; j < points.Count - 2; j++)
            {
                Vector3 p0 = points[j];
                Vector3 p1 = 0.5f * (-points[j - 1] + points[j + 1]);
                Vector3 p2 = 0.5f * (2 * points[j-1] - 5* points[j] + 4 * points[j+1] - points[j+2]);
                Vector3 p3 = 0.5f * (-points[j-1] + 3 * points[j] - 3 * points[j+1] + points[j+2]);

                Vector3 n0 = normals[j];
                Vector3 n1 = 0.5f * (-normals[j - 1] + normals[j + 1]);
                Vector3 n2 = 0.5f * (2 * normals[j - 1] - 5 * normals[j] + 4 * normals[j + 1] - normals[j+2]);
                Vector3 n3 = 0.5f * (-normals[j - 1] + 3 * normals[j] - 3 * normals[j + 1] + normals[j + 2]);

                Vector3 t0 = tangents[j];
                Vector3 t1 = 0.5f * (-tangents[j - 1] + tangents[j + 1]);
                Vector3 t2 = 0.5f * (2 * tangents[j - 1] - 5 * tangents[j] + 4 * tangents[j + 1] - tangents[j+2]);
                Vector3 t3 = 0.5f * (-tangents[j - 1] + 3 * tangents[j] - 3 * tangents[j + 1] + tangents[j + 2]);

                for (int i = 0; i < splineResolution; i++)
                {
                    float t = (float)i / splineResolution;
                    newPoints.Add(p0 + t * p1 + t * t * p2 + t * t * t * p3);
                    newNormals.Add(n0 + t * n1 + t * t * n2 + t * t * t * n3);
                    newTangents.Add(t0 + t * t1 + t * t * t2 + t * t * t * t3);
                }
            }

            for (int i = 0; i < splineResolution; i++)
            {
                float t = (float)i / splineResolution;
                newPoints.Add((1 - t) * points[points.Count-2] + t * points[points.Count-1]);
                newNormals.Add((1 - t) * normals[points.Count - 2] + t * normals[points.Count - 1]);
                newTangents.Add((1 - t) * tangents[points.Count - 2] + t * tangents[points.Count - 1]);
            }

            return (newPoints, newNormals, newTangents);
        }

        private void generateRoadMesh(List<Vector3> points, List<Vector3> normals, List<Vector3> tangents)
        {
            var segments = new RoadSegments(points, normals);
            
            GameObject roadMesh = PrefabUtility.LoadPrefabContents("Assets/Resources/StraightRoad.prefab");
            GameObject[] roadSegments = new GameObject[points.Count - 1];
            Vector3[] path = new Vector3[(points.Count - 1) * 2];

            for (var i = 0; i < points.Count - 1; i++)
            {
                roadSegments[i] = Instantiate(roadMesh);
                roadSegments[i].transform.position = points[i];
                roadSegments[i].transform.parent = this.transform;

                roadSegments[i].GetComponent<MeshFilter>().sharedMesh = new Mesh();
                roadSegments[i].GetComponent<MeshFilter>().sharedMesh = (Mesh)Instantiate(roadMesh.GetComponent<MeshFilter>().sharedMesh);
                (path[i*2], path[i*2 + 1]) = DeformMesh(roadSegments[i], points[i], points[i + 1], tangents[i], tangents[i+1], normals[i], normals[i+1]);
            }

            combineMeshes(roadSegments);

            GameObject endRoad = GetComponentsInChildren<Transform>()[3].GameObject();

            var uvs = endRoad.GetComponent<MeshFilter>().sharedMesh.uv;

            for (var j = 0; j < uvs.Length; j++)
            {
                uvs[j] = new Vector2(uvs[j].x, uvs[j].y * Vector3.Distance(points[0], points[points.Count - 1]) / 100);
            }

            endRoad.GetComponent<MeshFilter>().sharedMesh.uv = uvs;
            
            //TODO: do this right/better?
            endRoad.AddComponent<MeshCollider>().sharedMesh = endRoad.GetComponent<Mesh>();

            mapTerrainToRoad(path, endRoad.GetComponent<MeshFilter>().sharedMesh);
        }

        private (Vector3, Vector3) DeformMesh(GameObject roadSegment, Vector3 start, Vector3 end, Vector3 tangentStart, Vector3 tangentEnd, Vector3 normalStart, Vector3 normalEnd)
        {
            var mesh = roadSegment.GetComponent<MeshFilter>().sharedMesh;
            var meshTransform = roadSegment.transform;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;

            float leftMostPoint = 0;
            float rightMostPoint = 0;
            int leftIndex = 0;
            int rightIndex = 0;

            Quaternion startRotation = Quaternion.LookRotation(tangentStart, normalStart);
            Quaternion endRotation = Quaternion.LookRotation(tangentEnd, normalEnd);
 
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                if (vertex.z < 0f)
                {
                    if (vertex.x < leftMostPoint)
                    {
                        leftMostPoint = vertex.x;
                        leftIndex = i;
                    }
                    if (vertex.x > rightMostPoint)
                    {
                        rightMostPoint = vertex.x;
                        rightIndex = i;
                    }
                    
                    vertices[i] = meshTransform.TransformPoint(vertex);
                    vertices[i] = start;
                    vertices[i] = meshTransform.InverseTransformPoint(vertices[i]);
                    vertices[i].x += vertex.x;
                    vertices[i].y += vertex.y;
                    vertices[i] = startRotation * vertices[i];
                }
                else
                {
                    vertices[i] = meshTransform.TransformPoint(vertex);
                    vertices[i] = start;
                    vertices[i] = meshTransform.InverseTransformPoint(vertices[i]);
                    vertices[i].x += vertex.x;
                    vertices[i].y += vertex.y;
                    vertices[i] = endRotation * vertices[i];
                    vertices[i] += end - start;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;

            Vector3 leftMostVector = meshTransform.TransformPoint(vertices[leftIndex]);
            Vector3 rightMostVector = meshTransform.TransformPoint(vertices[rightIndex]);

            return (leftMostVector, rightMostVector);
        }

        private void combineMeshes(GameObject[] targets)
        {
            int meshCount = targets[0].GetComponent<MeshFilter>().sharedMesh.subMeshCount;
            Mesh[] resultingMeshes = new Mesh[meshCount];
            for (int i = 0; i < meshCount; i++)
            {
                resultingMeshes[i] = combineMeshesHelper(targets, i);
            }

            GameObject[] finalSegments = new GameObject[meshCount];
            for (int i = 0; i < meshCount; i++)
            {
                finalSegments[i] = Instantiate(targets[0]);
                finalSegments[i].transform.parent = this.transform;
                finalSegments[i].GetComponent<MeshFilter>().sharedMesh = new Mesh();
                finalSegments[i].transform.position = new Vector3(0, 0, 0);
                finalSegments[i].GetComponent<MeshFilter>().sharedMesh.subMeshCount = 3;

                finalSegments[i].GetComponent<MeshFilter>().sharedMesh = resultingMeshes[i];
                finalSegments[i].GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
                finalSegments[i].GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
                finalSegments[i].GetComponent<MeshFilter>().sharedMesh.RecalculateTangents();
                finalSegments[i].GetComponent<MeshRenderer>().sharedMaterials = targets[0].GetComponent<MeshRenderer>().sharedMaterials;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                DestroyImmediate(targets[i]);
            }

            CombineInstance[] combine = new CombineInstance[meshCount];

            for (int i = 0; i < meshCount; i++)
            {
                combine[i].mesh = finalSegments[i].GetComponent<MeshFilter>().sharedMesh;
                combine[i].transform = finalSegments[i].transform.localToWorldMatrix;
            }

            GameObject endRoad = new GameObject();
            endRoad.AddComponent<MeshFilter>();
            endRoad.AddComponent<MeshRenderer>();

            endRoad.transform.parent = this.transform;
            endRoad.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            endRoad.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine, false);

            endRoad.GetComponent<MeshRenderer>().sharedMaterials = finalSegments[0].GetComponent<MeshRenderer>().sharedMaterials;
            endRoad.transform.name = "Road";

            endRoad.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
            endRoad.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
            endRoad.GetComponent<MeshFilter>().sharedMesh.RecalculateTangents();

            for (int i = 0; i < meshCount; i++)
            {
                DestroyImmediate(finalSegments[i]);
            }
        }

        private Mesh combineMeshesHelper(GameObject[] targets, int index)
        {
            CombineInstance[] combine = new CombineInstance[targets.Length];
            for (int i = targets.Length - 1; i >= 0; i--)
            {
                combine[i].mesh = targets[i].GetComponent<MeshFilter>().sharedMesh;
                combine[i].transform = targets[i].transform.localToWorldMatrix;
                combine[i].subMeshIndex = index;
            }

            Mesh returnMesh = new Mesh();
            returnMesh.CombineMeshes(combine);
            return returnMesh;
        }

        private void mapTerrainToRoad(Vector3[] path, Mesh roadMesh)
        {
            var ter = terrain.GetComponent<UnityEngine.Terrain>();
            var terrainData = ter.terrainData;
            var terrainSize = terrainData.size;
            var terrainHeight = terrainData.heightmapResolution;
            var terrainWidth = terrainData.heightmapResolution;
            var terrainHeightData = terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);
            int radius = 10;
            var epsilon = 2f;

            // Adjust the terrain for the first points in the path
            var prevLeft = terrain.transform.InverseTransformPoint(path[0]);
            var prevRight = terrain.transform.InverseTransformPoint(path[1]);

            var prevLeft2D = new Vector2(prevLeft.x, prevLeft.z);
            var prevRight2D = new Vector2(prevRight.x, prevRight.z);

            // Create a float array that checks if terrain was already adjusted
            var terrainAdjusted = new bool[terrainWidth, terrainHeight];

            for (var i = 1; i < path.Length/2; i++)
            {
                var left = terrain.transform.InverseTransformPoint(path[i * 2]);
                var right = terrain.transform.InverseTransformPoint(path[i * 2 + 1]);

                var maxX = (int)Mathf.Ceil(Mathf.Max(left.x, right.x, prevLeft.x, prevRight.x)) + radius;
                var minX = (int)Mathf.Floor(Mathf.Min(left.x, right.x, prevLeft.x, prevRight.x)) - radius;
                var maxZ = (int)Mathf.Ceil(Mathf.Max(left.z, right.z, prevLeft.z, prevRight.z)) + radius;
                var minZ = (int)Mathf.Floor(Mathf.Min(left.z, right.z, prevLeft.z, prevRight.z)) - radius;

                // Transform to terrain coordinates
                maxX = (int)((maxX / terrainSize.x) * terrainWidth);
                minX = (int)((minX / terrainSize.x) * terrainWidth);
                maxZ = (int)((maxZ / terrainSize.z) * terrainHeight);
                minZ = (int)((minZ / terrainSize.z) * terrainHeight);

                var left2D = new Vector2(left.x, left.z);
                var right2D = new Vector2(right.x, right.z);

                // Creating a Triangle from most leftX to prevLeftX to prevRightX and from leftX to prevRightX to rightX
                // and then adjusting the terrain height for each point in the triangle
                var areaOne = CalcDetTriangle2D(left, prevLeft, prevRight);
                var areaTwo = CalcDetTriangle2D(left, prevRight, right);

                var averageHeight = (left.y + prevLeft.y + right.y + prevRight.y) / 4;
                var maxDist = new Vector2(maxX - minX, maxZ - minZ).magnitude;

                for (int j = minX; j <= maxX; j++)
                {
                    for(int k = minZ; k <= maxZ; k++)
                    {
                        var worldX = (int)(((float)j / terrainWidth) * terrainSize.x);
                        var worldZ = (int)(((float)k / terrainHeight) * terrainSize.z);
                        for (int wigX = -1; wigX <= 1; wigX++)
                        {
                            for (int wigZ = -1; wigZ <= 1; wigZ++)
                            {
                                var point = new Vector2(worldX + wigX, worldZ + wigZ);

                                var Bary1 = CalcBary1(prevLeft, prevRight, point)/ areaOne;
                                if (Bary1 <= 1f && Bary1 >= 0f)
                                {
                                    var Bary2 = CalcBary2(left, prevRight, point) / areaOne;
                                    if (Bary2 <= 1f && Bary1 + Bary2 <= 1f && Bary2 >= 0f)
                                    {
                                        var Bary3 = 1 - Bary1 - Bary2;
                                        var y = Bary1 * left.y + Bary2 * prevLeft.y + Bary3 * prevRight.y;

                                        terrainHeightData[k, j] = (y - epsilon) / terrainSize.y;
                                        terrainAdjusted[k, j] = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    var Bary4 = CalcBary1(prevRight, right, point) / areaTwo;
                                    if (Bary4 <= 1f && Bary4 >= 0f)
                                    {
                                        var Bary5 = CalcBary2(left, right, point) / areaTwo;
                                        if (Bary5 <= 1f && Bary4 + Bary5 <= 1f && Bary5 >= 0f)
                                        {
                                            var Bary6 = 1 - Bary5 - Bary4;
                                            var y = Bary4 * left.y + Bary5 * prevRight.y + Bary6 * right.y;

                                            terrainHeightData[k, j] = (y - epsilon) / terrainSize.y;
                                            terrainAdjusted[k, j] = true;
                                            break;
                                        }
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

                            terrainHeightData[k, j] = Mathf.Lerp(curHeight, (averageHeight - epsilon) / terrainSize.y, dist / maxDist);
                        }
                    }
                }
                prevLeft = left;
                prevRight = right;
                prevLeft2D = left2D;
                prevRight2D = right2D;
            }
            ter.terrainData.SetHeights(0, 0, terrainHeightData);
        }
            
        private float CalcDetTriangle2D(Vector3 firstVec, Vector3 secondVec, Vector3 thirdVec)
        {
            return (secondVec.z - thirdVec.z) * (firstVec.x - thirdVec.x) + (thirdVec.x - secondVec.x) * (firstVec.z - thirdVec.z);
        }

        private float CalcBary1(Vector3 secondVec, Vector3 thirdVec, Vector2 point)
        {
            return (secondVec.z - thirdVec.z) * (point.x - thirdVec.x) + (thirdVec.x - secondVec.x) * (point.y - thirdVec.z);
        }

        private float CalcBary2(Vector3 firstVec, Vector3 thirdVec, Vector2 point)
        {
            return (thirdVec.z - firstVec.z) * (point.x - thirdVec.x) + (firstVec.x - thirdVec.x) * (point.y - thirdVec.z);
        }
    }
}