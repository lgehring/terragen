using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Roads
{
    /// <summary>
    ///     Generates a spline with a road mesh
    /// </summary>
    public class RoadGenerator : MonoBehaviour
    {
        public Transform map;
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
            //print(road.nodes[endingIdx].predIdx);
            var index = road.nodes[prevIdx].predIdx;
            path.Add(new Vector3(road.nodes[prevIdx].pos.x, road.nodes[prevIdx].mapHeight, road.nodes[prevIdx].pos.z));
            normals.Add(road.nodes[prevIdx].normal);
            var count = 0;
            while (index != startIdx)
            {
                if (count > 1000) throw new ArgumentException("The road is too long");
                if (road.nodes[index].roadType == RoadNodeType.Tunnel)
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

            GameObject roadMesh = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/StraightRoad.prefab");

            for (var i = 0; i < points.Count - 1; i++)
            {
                GameObject roadSegment = new GameObject("RoadSegment");
                roadSegment.transform.position = points[i];
                roadSegment.transform.parent = this.transform;
                roadSegment.transform.rotation = Quaternion.LookRotation(tangents[i], normals[i]);
                roadSegment.transform.rotation *= Quaternion.Euler(-90, 0, 0);

                roadSegment.AddComponent<MeshFilter>();
                roadSegment.AddComponent<MeshRenderer>();
                
                roadSegment.AddComponent<MeshCollider>();
                roadSegment.GetComponent<MeshFilter>().sharedMesh = new Mesh();
                var segmentMesh = roadSegment.GetComponent<MeshFilter>().sharedMesh;
                //segmentMesh = new Mesh();
                
                cloneMesh(segmentMesh, roadMesh.GetComponent<MeshFilter>().sharedMesh);
                DeformMesh(roadSegment, points[i], points[i + 1], tangents[i], tangents[i+1], normals[i], normals[i+1]);
                
                roadSegment.GetComponent<MeshRenderer>().sharedMaterials = roadMesh.GetComponent<MeshRenderer>().sharedMaterials;
            }
        }

        private void DeformMesh(GameObject roadSegment, Vector3 start, Vector3 end, Vector3 tangentStart, Vector3 tangentEnd, Vector3 normalStart, Vector3 normalEnd)
        {
            var mesh = roadSegment.GetComponent<MeshFilter>().sharedMesh;
            var meshTransform = roadSegment.transform;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;

            Vector3 startCross = Vector3.Cross(tangentStart, normalStart).normalized;
            Vector3 endCross = Vector3.Cross(tangentEnd, normalEnd).normalized;
            Quaternion toLocal = Quaternion.FromToRotation(tangentStart, new Vector3(0, 0, 1));
            Quaternion toWorld = Quaternion.FromToRotation(new Vector3(0, 0, 1), tangentStart);
            tangentStart = toLocal * tangentStart;
            tangentEnd = toLocal * tangentEnd;
            Vector2 tangentStartXY = new Vector2(tangentStart.x, tangentStart.z).normalized;
            Vector2 tangentEndXY = new Vector2(tangentEnd.x, tangentEnd.z).normalized;
            float cosPhi = Vector2.Dot(tangentStartXY, tangentEndXY);
            float sinPhi = Mathf.Sqrt(1 - cosPhi * cosPhi);
            
            sinPhi =  tangentEnd.x > 0 ? sinPhi : -sinPhi;
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                if (vertex.y > 0f)
                {
                    vertices[i] = meshTransform.TransformPoint(vertex);
                    vertices[i] = start;
                    vertices[i] = meshTransform.InverseTransformPoint(vertices[i]);
                    vertices[i].x += vertex.x;
                    vertices[i].z += vertex.z;
                }
                else
                {
                    vertices[i] = meshTransform.TransformPoint(vertex);
                    vertices[i] = end;
                    vertices[i] = meshTransform.InverseTransformPoint(vertices[i]);
                    vertices[i].x += vertex.x;
                    vertices[i].z += vertex.z;
                    vertices[i].y += vertices[i].x * sinPhi;
                    vertices[i].x *= cosPhi;


                }

            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
        }

        private void cloneMesh(Mesh targetMesh, Mesh original)
        {
            targetMesh.vertices = original.vertices;
            targetMesh.normals = original.normals;
            targetMesh.uv = original.uv;
            targetMesh.triangles = original.triangles;
            targetMesh.tangents = original.tangents;
            targetMesh.bounds = original.bounds;
        }
    }
}