using System.Globalization;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (Ecosystem))]
public class EcosystemEditor : Editor
{
    /// <summary>
    /// Changes what the user sees
    /// </summary>
    public override void OnInspectorGUI()
    {
        var ecosystem = (Ecosystem)target;
        
        // Show info
        EditorGUILayout.LabelField("Ecosystem", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Number of plants", ecosystem.plants.Count.ToString());
        EditorGUILayout.LabelField("Time", ecosystem.time.ToString(CultureInfo.InvariantCulture));
        EditorGUILayout.LabelField("Bounds", ecosystem.bounds.ToString());

        if (GUILayout.Button("InitSeeds"))
        {
            ecosystem.InitSeeds();
        }

        if (GUILayout.Button("Advance"))
        {
            ecosystem.EvolveEcosystem();
        }
        
        if (GUILayout.Button("Advance 10"))
        {
            for (var i = 0; i < 10; i++)
            {
                ecosystem.EvolveEcosystem();
            }
        }
    }
}