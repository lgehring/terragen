using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(RoadGenerator))]
[InitializeOnLoad]
public class RoadGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RoadGenerator roadGen = (RoadGenerator)target;

        // If checks if any value was changed
        if (DrawDefaultInspector())
        {
        }

        // Creats a button that say generate and generates a noise map texture if the button is pressed
        if (GUILayout.Button("Generate"))
        {
            roadGen.drawRoadMesh();
        }
    }
}
