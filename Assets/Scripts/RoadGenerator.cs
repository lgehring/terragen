using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using System.Linq;
using SplineMesh;

public class RoadGenerator : MonoBehaviour
{
    GameObject startingPoint;
    GameObject endPoint;
    public Transform map;
    public int innerRadiusForAStar;
    public int outerRadiusForAStar;
    public void drawRoadMesh()
    {
        startingPoint = this.GetComponentsInChildren<Transform>()[1].gameObject;
        endPoint = this.GetComponentsInChildren<Transform>()[2].gameObject;
        
        for (int i = this.GetComponentsInChildren<Transform>().Length - 1; i > 2; i--)
        {
            DestroyImmediate(this.GetComponentsInChildren<Transform>()[i].gameObject);
        }

        //FIXME: this assumes that the plane is at 0,0
        int bounds = (int)Mathf.Abs((map.localScale).x*10);
        
        // FIXME: This might assumes that the roadGenerator is at 0,0, could also give the correct coordinate
        Vector2 start = new Vector2(startingPoint.transform.position.x, startingPoint.transform.position.z);
        Vector2 end = new Vector2(endPoint.transform.position.x, endPoint.transform.position.z);

        if(Mathf.Min(end.x,end.y,start.x,start.y) < -bounds/2 || Mathf.Max(end.x,end.y,start.x,start.y) > bounds / 2)
        {
            throw new ArgumentException("The starting or endpoint is out of bounds");
        }

        Road road = new Road(start, end, bounds, innerRadiusForAStar, outerRadiusForAStar);
        road.generateRoad(start, end);

        
        

        int startIdx = (int)(start.y + bounds / 2) * bounds + (int)(start.x + bounds / 2);
        int endingIdx = (int)(end.y + bounds / 2) * bounds + (int)(end.x + bounds / 2);

        List<Vector3> path = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        int prevIdx = endingIdx;
        int index = road.nodes[prevIdx].predIdx;
        path.Add(road.nodes[prevIdx].pos);
        normals.Add(road.nodes[prevIdx].normal);
        int count = 0;
        while (true)
        {
            if (count > 1000)
            {
                throw new ArgumentException("The road is too long");
            }
            path.Add(road.nodes[index].pos);
            normals.Add(road.nodes[index].normal);
            prevIdx = index;
            index = road.nodes[prevIdx].predIdx;
            if (prevIdx == index) { 
                break;
            }
            count++;
        }

        generateSpline(path, normals);
    }

    private void generateSpline(List<Vector3> points, List<Vector3> normals)
    {
        /*GameObject spline = new GameObject("Road");
        spline.transform.parent = this.transform;
        spline.GetOrAddComponent<SplineContainer>();
        for (int i = 0; i < points.Count; i++)
        {
            spline.GetComponent<SplineContainer>().Spline.Add(new BezierKnot(points[i]));
        }

        spline.GetComponent<SplineContainer>().Spline.SetTangentMode(TangentMode.AutoSmooth);*/
        /*GameObject splineGameObj = new GameObject("Road");
        splineGameObj.transform.parent = this.transform;
        splineGameObj.AddComponent<Spline>();
        splineGameObj.GetComponent<Spline>().nodes.Clear();
        for (int i = 0; i < points.Count-1; i++)
        {
            splineGameObj.GetComponent<Spline>().AddNode(new SplineMesh.SplineNode(points[i], (points[points.Count-1]- points[i]).normalized));
            splineGameObj.GetComponent<Spline>().nodes[i].Up = normals[i];
        }*/
        for (int i = 0; i < points.Count - 1; i++)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.transform.parent = this.transform;
            road.transform.position = points[i];
            road.transform.up = normals[i];
            road.transform.localScale = new Vector3(0.1f, 0.1f, Vector3.Distance(points[i], points[i + 1]));
            var Renderer = road.GetComponent<Renderer>();
            Renderer.sharedMaterial.SetColor("_Color", Color.red);
        }
    }
}
