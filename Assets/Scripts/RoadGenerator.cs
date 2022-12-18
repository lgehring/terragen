using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using System.Linq;
using UnityEngine.Splines;

public class RoadGenerator : MonoBehaviour
{
    GameObject startingPoint;
    GameObject endPoint;
    public Transform map;
    public int innerRadiusForAStar;
    public int outerRadiusForAStar;
    public int gridSizeInMeters;
    private void OnValidate()
    {
        if (gridSizeInMeters < 1)
            gridSizeInMeters = 1;
        if(gridSizeInMeters > 2410) {
            gridSizeInMeters = 2410;
        }
    }
    public void drawRoadMesh()
    {
        startingPoint = this.GetComponentsInChildren<Transform>()[1].gameObject;
        endPoint = this.GetComponentsInChildren<Transform>()[2].gameObject;
        
        for (int i = this.GetComponentsInChildren<Transform>().Length - 1; i > 2; i--)
        {
            DestroyImmediate(this.GetComponentsInChildren<Transform>()[i].gameObject);
        }

        int bounds = (int)Mathf.Abs((map.localScale).x*10);
        Vector2Int offset = new Vector2Int((int)map.position.x, (int)map.position.z);
        Vector2Int start = new Vector2Int((int)startingPoint.transform.position.x, (int)startingPoint.transform.position.z);
        Vector2Int end = new Vector2Int((int)endPoint.transform.position.x, (int)endPoint.transform.position.z);
        start -= offset;
        end -= offset;

        if (Mathf.Min(end.x,end.y,start.x,start.y) < -bounds/2 || Mathf.Max(end.x,end.y,start.x,start.y) > bounds / 2)
        {
            throw new ArgumentException("The starting or endpoint is out of bounds");
        }

        Road road = new Road(start, end, bounds, innerRadiusForAStar, outerRadiusForAStar, gridSizeInMeters, offset);
        road.generateRoad(start, end);

        //int startIdx = ((start.x + bounds/2 - (int)offset.x) * bounds + (start.y + bounds/2 - (int)offset.y))/gridSizeInMeters;
        int endingIdx = (end.x + bounds/2 - (int)offset.x)/gridSizeInMeters * bounds/gridSizeInMeters + (end.y + bounds/2 - (int)offset.y)/gridSizeInMeters;
        print("This is the ending index in the generator " + endingIdx);

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
        GameObject spline = new GameObject("Road");
        spline.transform.parent = this.transform;
        spline.GetOrAddComponent<SplineContainer>();
        for (int i = 0; i < points.Count; i++)
        {
            spline.GetComponent<SplineContainer>().Spline.Add(new BezierKnot(points[i]));
        }

        spline.GetComponent<SplineContainer>().Spline.SetTangentMode(TangentMode.AutoSmooth);
    }
}
