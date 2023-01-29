using Terrain;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(TerrainController))]
    public class TerrainControllerEditor : UnityEditor.Editor
    {
        /// <summary>
        ///     Changes what the user sees
        /// </summary>
        public override void OnInspectorGUI()
        {
            var mapGen = (TerrainController)target;

            // If checks if any value was changed
            if (DrawDefaultInspector())
                // allows the Generator of the map to auto update
                if (mapGen.onlyNoiseMap)
                    mapGen.DrawMapInEditor();

            EditorGUILayout.Toggle("Only Noise Map", mapGen.onlyNoiseMap);
            GameObject.Find("Terrain").GetComponent<UnityEngine.Terrain>().enabled = !mapGen.onlyNoiseMap;

            // Creats a button that say generate and generates a noise map texture if the button is pressed
            if (GUILayout.Button("Generate")) mapGen.DrawMapInEditor();
        }
    }
}