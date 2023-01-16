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

            if (GUILayout.Button("Create Plant Pool (FREEZES EDITOR)"))
                ecosystem.CreatePlantPool();

            if (GUILayout.Button("InitSeeds")) ecosystem.InitSeeds();

            if (GUILayout.Button("Advance")) ecosystem.EvolveEcosystem();

            if (GUILayout.Button("UpdatePlantPrefabs")) ecosystem.UpdatePlantPrefabFiles();
            
            // if (GUILayout.Button("Combine Active Plants")) ecosystem.plantPool.CombineActivePlants();
        }
    }
}