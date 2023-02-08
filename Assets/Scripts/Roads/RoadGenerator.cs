using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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
        public GameObject terrain;
        private TerrainData backupData;
        public int innerRadiusForAStar;
        public int outerRadiusForAStar;
        public int gridSizeInMeters;
        public int splineResolution;
        private GameObject endPoint;
        private GameObject startingPoint;

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

        /// <summary>
        ///     Creates a spline extrapolates a road mesh onto it and adds it to the scene
        /// </summary>
        /// <exception cref="ArgumentException"> Throws an exception if the road has to many nodes </exception>
        public void drawRoadMesh()
        {
            if (map == null) 
                throw new ArgumentException("The map is null");
            if (terrain == null) 
                throw new ArgumentException("The terrain is null");

            startingPoint = GetComponentsInChildren<Transform>()[1].gameObject;
            endPoint = GetComponentsInChildren<Transform>()[2].gameObject;

            for (var i = GetComponentsInChildren<Transform>().Length - 1; i > 2; i--)
            {
                var toBeDestroyed = GetComponentsInChildren<Transform>()[i].gameObject;
                DestroyImmediate(toBeDestroyed);
            }

            var bounds = (int)Mathf.Abs(map.localScale.x * 10);
            var offset = new Vector2Int((int)map.position.x, (int)map.position.z);
            var start = new Vector2Int(
                ((int)startingPoint.transform.position.x + bounds / 2 - offset.x) / gridSizeInMeters,
                ((int)startingPoint.transform.position.z + bounds / 2 - offset.y) / gridSizeInMeters);
            var end = new Vector2Int(((int)endPoint.transform.position.x + bounds / 2 - offset.x) / gridSizeInMeters,
                ((int)endPoint.transform.position.z + bounds / 2 - offset.y) / gridSizeInMeters);

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
            tangents.Add(points[points.Count - 1] - points[points.Count - 2]);
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

            for (var i = 0; i < points.Count - 1; i++)
            {
                roadSegments[i] = Instantiate(roadMesh);
                roadSegments[i].transform.position = points[i];
                roadSegments[i].transform.parent = this.transform;

                roadSegments[i].GetComponent<MeshFilter>().sharedMesh = new Mesh();
                roadSegments[i].GetComponent<MeshFilter>().sharedMesh = (Mesh)Instantiate(roadMesh.GetComponent<MeshFilter>().sharedMesh);
                DeformMesh(roadSegments[i], points[i], points[i + 1], tangents[i], tangents[i+1], normals[i], normals[i+1]);
            }

            combineMeshes(roadSegments);

            GameObject endRoad = GetComponentsInChildren<Transform>()[3].GameObject();

            var uvs = endRoad.GetComponent<MeshFilter>().sharedMesh.uv;

            for (var j = 0; j < uvs.Length; j++)
            {
                uvs[j] = new Vector2(uvs[j].x, uvs[j].y * Vector3.Distance(points[0], points[points.Count - 1]) / 100);
            }

            endRoad.GetComponent<MeshFilter>().sharedMesh.uv = uvs;

            mapTerrainToRoad(endRoad);
        }

        private void DeformMesh(GameObject roadSegment, Vector3 start, Vector3 end, Vector3 tangentStart, Vector3 tangentEnd, Vector3 normalStart, Vector3 normalEnd)
        {
            var mesh = roadSegment.GetComponent<MeshFilter>().sharedMesh;
            var meshTransform = roadSegment.transform;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;

            Quaternion startRotation = Quaternion.LookRotation(tangentStart, normalStart);
            Quaternion endRotation = Quaternion.LookRotation(tangentEnd, normalEnd);
 
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                if (vertex.z < 0f)
                {
                    
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

        private void mapTerrainToRoad(GameObject Road)
        {
            var ter = terrain.GetComponent<UnityEngine.Terrain>();
            if (backupData == null)
            {
                backupData = ter.terrainData;
            }

            var terrainData = backupData;
            var terrainSize = terrainData.size;
            var terrainHeight = terrainData.heightmapResolution;
            var terrainWidth = terrainData.heightmapResolution;
            var terrainHeightData = terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);

            var mesh = Road.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;

            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var localVertex = Road.transform.TransformPoint(vertex);
                var terrainLoc = terrain.transform.InverseTransformPoint(localVertex);
                var x = (int)((terrainLoc.x / terrainSize.x) * terrainWidth);
                var z = (int)((terrainLoc.z / terrainSize.z) * terrainHeight);
                if (x >= 0 && x < terrainWidth && z >= 0 && z < terrainHeight)
                {
                    terrainHeightData[z, x] = terrainLoc.y / terrainSize.y;
                }
            }
            ter.terrainData.SetHeights(0, 0, terrainHeightData);
        }
    }
}