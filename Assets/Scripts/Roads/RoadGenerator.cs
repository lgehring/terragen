using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

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
        private GameObject endPoint;
        private GameObject startingPoint;

        private void OnValidate()
        {
            if (gridSizeInMeters < 1)
                gridSizeInMeters = 1;
            if (gridSizeInMeters > 2410) gridSizeInMeters = 2410;
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
            generateSpline(path, normals, tangents);
            generateRoadMesh(path, normals, tangents);
            
        }

        private void generateSpline(List<Vector3> points, List<Vector3> normals, List<Vector3> tangents)
        {
            /*GameObject spline = new GameObject("Road");
        spline.transform.parent = this.transform;
        spline.GetOrAddComponent<SplineContainer>();
        var splineObj = spline.GetComponent<SplineContainer>().Spline;
        for (int i = 0; i < points.Count; i++)
        {
            splineObj.Add(new BezierKnot(points[i]));
        }

        spline.AddComponent<SplineExtrude>();
        var splineMesh = spline.GetComponent<SplineExtrude>();
        splineMesh.Radius = 2f;
        splineMesh.Rebuild();
        */
            for (int i = 0; i < points.Count; i++)
            {
                if (i == points.Count - 1)
                {
                    tangents.Add((points[i] - points[i - 1]).normalized);
                }
                else
                {
                    tangents.Add((points[i + 1] - points[i]).normalized);
                }
            }
        }

        private void generateRoadMesh(List<Vector3> points, List<Vector3> normals, List<Vector3> tangents)
        {
            var segments = new RoadSegments(points, normals);

            var roadMesh = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/StraightRoad.prefab");

            for (var i = 0; i < points.Count - 1; i++)
            {
                GameObject roadSegment = GameObject.Instantiate(roadMesh, this.transform, true);
                roadSegment.transform.position = points[i];
                roadSegment.transform.parent = this.transform;
                roadSegment.transform.rotation = Quaternion.LookRotation(tangents[i], normals[i]);
                roadSegment.transform.rotation *= Quaternion.Euler(-90, 0, 0);

                //roadSegment.AddComponent<MeshFilter>();
                //roadSegment.GetComponent<MeshFilter>().sharedMesh = roadMesh.GetComponent<MeshFilter>().sharedMesh;
                //roadSegment.AddComponent<MeshRenderer>();
                
                //var roadSegmentRenderer = roadSegment.GetComponent<MeshRenderer>();
                //roadSegmentRenderer = Instantiate(roadMesh.GetComponent<MeshRenderer>());
                //roadSegmentRenderer.sharedMaterials = roadMesh.GetComponent<MeshRenderer>().sharedMaterials;
                
                //roadSegment.AddComponent<MeshCollider>();
                //var roadSegmentMesh = roadSegment.GetComponent<MeshFilter>();
                //roadSegmentMesh.sharedMesh = new Mesh();
                
                //cloneMesh(roadSegmentMesh.sharedMesh, roadMesh.GetComponent<MeshFilter>().sharedMesh);
                DeformMesh(roadSegment, points[i], points[i + 1], tangents[i], tangents[i+1], normals[i], normals[i+1]);
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
            float Theta = Vector2.Angle(new Vector2(endCross.x, endCross.z), new Vector2(startCross.x, startCross.z));
            float Phi = Vector2.Angle(new Vector2(tangentStart.x, tangentStart.z), new Vector2(tangentEnd.x, tangentEnd.z));

            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                if (vertex.y > 0f)
                {
                    vertices[i] = meshTransform.TransformPoint(vertex);
                    vertices[i] = start;
                    vertices[i] = meshTransform.InverseTransformPoint(vertices[i]);
                    vertices[i].x += vertex.x * 20f;
                    vertices[i].z += vertex.z * 20f;
                }
                else
                {
                    vertices[i] = meshTransform.TransformPoint(vertex);
                    vertices[i] = end;
                    vertices[i] = meshTransform.InverseTransformPoint(vertices[i]);
                    vertices[i].x += vertex.x * 20f;
                    vertices[i].z += vertex.z * 20f;
                    vertices[i].y += vertices[i].x * Mathf.Sin(-Theta * Mathf.Deg2Rad);
                    vertices[i].x *= Mathf.Cos(Theta * Mathf.Deg2Rad);

                    
                }

            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Debug.DrawLine(start, start + normalStart, Color.red, 10, false);
        }

        private void cloneMesh(Mesh targetMesh, Mesh original)
        {
            targetMesh.subMeshCount = original.subMeshCount;
            targetMesh.vertices = original.vertices;
            targetMesh.normals = original.normals;
            targetMesh.uv = original.uv;
            targetMesh.triangles = original.triangles;
            targetMesh.tangents = original.tangents;
            targetMesh.bounds = original.bounds;
        }
    }
}