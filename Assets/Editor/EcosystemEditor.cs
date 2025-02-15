using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(Ecosystem.Ecosystem))]
    public class EcosystemEditor : UnityEditor.Editor
    {
        /// <summary>
        ///     Changes what the user sees
        /// </summary>
        public override void OnInspectorGUI()
        {
            var ecosystem = (Ecosystem.Ecosystem)target;

            // Show info
            EditorGUILayout.LabelField("Ecosystem", EditorStyles.boldLabel);
            // Non-null matrix entries
            EditorGUILayout.LabelField("Number of plants", ecosystem.count.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Time", ecosystem.time.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Bounds", ecosystem.bounds.ToString());

            var showPlants = EditorGUILayout.Toggle("Show plants", ecosystem.renderPlants);
            ecosystem.ShowPlants(showPlants);

            if (GUILayout.Button("Prepare Ecosystem (FREEZES EDITOR)"))
                ecosystem.PrepareEcosystem();

            if (GUILayout.Button("InitSeeds")) ecosystem.InitSeeds();

            if (GUILayout.Button("Advance")) ecosystem.EvolveEcosystem();

            if (GUILayout.Button("Advance 5"))
                for (var i = 0; i < 10; i++)
                    ecosystem.EvolveEcosystem();
        }
    }
}