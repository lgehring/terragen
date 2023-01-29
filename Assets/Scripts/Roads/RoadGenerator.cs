using System;
using System.Collections.Generic;
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

            // This is not working
            var pathOldRoadMesh = AssetDatabase.FindAssets("Road.asset");
            if (pathOldRoadMesh.Length != 0)
            {
                var oldRoadMesh = AssetDatabase.GUIDToAssetPath(pathOldRoadMesh[0]);
                AssetDatabase.DeleteAsset(pathOldRoadMesh[0]);
            }

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

            generateSpline(path, normals);
            generateRoadMesh(path, normals);
        }

        private void generateSpline(List<Vector3> points, List<Vector3> normals)
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
        }

        private void generateRoadMesh(List<Vector3> points, List<Vector3> normals)
        {
            var segments = new RoadSegments(points, normals);

            var roadMesh = new GameObject("Road")
            {
                transform =
                {
                    parent = transform
                }
            };

            roadMesh.AddComponent<MeshFilter>();

            var mf = roadMesh.GetComponent<MeshFilter>();
            if (mf.sharedMesh == null) mf.sharedMesh = new Mesh();
            var mesh = mf.sharedMesh;

            mesh.Clear();

            mesh.vertices = segments.Vertices.ToArray();
            mesh.normals = segments.normals.ToArray();
            mesh.uv = segments.uvs.ToArray();
            mesh.triangles = segments.triangles.ToArray();

            mf.sharedMesh = mesh;

            roadMesh.AddComponent<MeshRenderer>();

            var render = roadMesh.GetComponent<MeshRenderer>();
            var mat = (Material)Resources.Load("Materials/Road Material");
            var roadTex = (Texture2D)Resources.Load("Stolen_Road_texture");
            render.sharedMaterial = mat;
            render.sharedMaterial.mainTexture = roadTex;
        }
    }
}