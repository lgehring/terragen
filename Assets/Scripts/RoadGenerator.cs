using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using System.Linq;

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

        print(this.GetComponentsInChildren<Transform>().Length);

        for (int i = this.GetComponentsInChildren<Transform>().Length - 1; i > 2; i--)
        {
            DestroyImmediate(this.GetComponentsInChildren<Transform>()[i].gameObject);
        }

        //FIXME: this assumes that the plane is at 0,0
        int bounds = (int)Mathf.Abs((map.localScale).x*10);
        
        // FIXME: This might assumes that the roadGenerator is at 0,0, could also give the correct coordinate
        Vector2 start = new Vector2(startingPoint.transform.position.x, startingPoint.transform.position.z);
        Vector2 end = new Vector2(endPoint.transform.position.x, endPoint.transform.position.z);

        print(start);
        print(end);

        if(Mathf.Min(end.x,end.y,start.x,start.y) < -bounds/2 || Mathf.Max(end.x,end.y,start.x,start.y) > bounds / 2)
        {
            throw new ArgumentException("The starting or endpoint is out of bounds");
        }

        Road road = new Road(start, end, bounds, innerRadiusForAStar, outerRadiusForAStar);
        road.generateRoad(start, end);

        
        

        int startIdx = (int)(start.y + bounds / 2) * bounds + (int)(start.x + bounds / 2);
        int endingIdx = (int)(end.y + bounds / 2) * bounds + (int)(end.x + bounds / 2);

        print(road.nodes[endingIdx].pos);
        
        int index = endingIdx;
        int prevIdx = endingIdx;
        int count = 0;
        while (index != startIdx && count < 100)
        {
            index = road.nodes[prevIdx].predIdx;
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = this.transform;
            cube.transform.position = road.nodes[index].pos;

            prevIdx = index;
            count++;
        }
        
    }
}
