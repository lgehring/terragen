using System.Globalization;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Ecosystem))]
public class EcosystemEditor : Editor
{
    /// <summary>
    ///     Changes what the user sees
    /// </summary>
    public override void OnInspectorGUI()
    {
        var ecosystem = (Ecosystem)target;

        // Show info
        EditorGUILayout.LabelField("Ecosystem", EditorStyles.boldLabel);
        // Non-null martrix entries
        EditorGUILayout.LabelField("Number of plants", ecosystem.count.ToString(CultureInfo.InvariantCulture));
        EditorGUILayout.LabelField("Time", ecosystem.time.ToString(CultureInfo.InvariantCulture));
        EditorGUILayout.LabelField("Bounds", ecosystem.bounds.ToString());

        ecosystem.instantiateLive = EditorGUILayout.Toggle("Instantiate Live", ecosystem.instantiateLive);

        if (GUILayout.Button("InitSeeds")) ecosystem.InitSeeds();

        if (GUILayout.Button("Advance")) ecosystem.EvolveEcosystem();

        if (GUILayout.Button("Advance 10"))
            for (var i = 0; i < 10; i++)
                ecosystem.EvolveEcosystem();

        if (GUILayout.Button("Instantiate")) ecosystem.Instantiate();
    }
}