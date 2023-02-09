using Roads;
using Terrain;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(RoadGenerator))]
    public class RoadGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var roadGen = (RoadGenerator)target;

            // If checks if any value was changed
            if (DrawDefaultInspector())
            {
            }

            // Creats a button that say generate and generates a noise map texture if the button is pressed
            if (GUILayout.Button("Generate")) roadGen.drawRoadMesh();
        }
    }
}