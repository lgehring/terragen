using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (MapGenerator))]
[InitializeOnLoad]
/// <summary>
/// Used to edit the noise map during run time. 
/// Source: https://www.youtube.com/watch?v=WP-Bm65Q-1Y a youtube series by Sebastian Lague
/// </summary>
public class MapGeneratorEditor : Editor
{
    /// <summary>
    /// Changes what the user sees
    /// </summary>
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        // If checks if any value was changed
        if (DrawDefaultInspector())
        {
            // allows the Generator of the map to auto update
            if (mapGen.autoUpdate)
            {
                mapGen.DrawMapInEditor();
            }
        }

        // Creats a button that say generate and generates a noise map texture if the button is pressed
        if (GUILayout.Button("Generate"))
        {
            mapGen.DrawMapInEditor();
        }
    }
}
