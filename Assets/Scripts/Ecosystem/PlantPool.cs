using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ecosystem
{
    public class PlantPool : MonoBehaviour
    {
        private static readonly List<string> PlantTypes = PlantData.Data.Keys.ToList();
        public int initialPoolSize;
        private readonly Stack<Plant>[] _availablePlants = new Stack<Plant>[PlantTypes.Count];
        private GameObject[] _plantPrefabs;

        public void CreatePlantPool()
        {
            // Update the plant prefabs
            var ecosystem = FindObjectOfType<Ecosystem>();
            ecosystem.UpdatePlantPrefabFiles();
            // Create the initial pool of plants
            var numTypes = PlantTypes.Count;
            var numPerType = initialPoolSize / numTypes;
            _plantPrefabs = new GameObject[numTypes];
            for (var typeIndex = 0; typeIndex < numTypes; typeIndex++)
            {
                var typeInfo = PlantData.Get(PlantTypes[typeIndex]);
                var typePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlantPrefabs/" + typeInfo.type + ".prefab");
                typePrefab.name = typeInfo.type;
                // Rescale the plant prefab to the correct size
                var bounds = typePrefab.GetComponent<MeshFilter>().sharedMesh.bounds;
                var maxExtent = Mathf.Max(bounds.size.x, bounds.size.z);
                var scale = typeInfo.sizeRadius / maxExtent;
                typePrefab.transform.localScale = new Vector3(scale, scale, scale);
                _plantPrefabs[typeIndex] = typePrefab;
                // Populate the pool
                _availablePlants[typeIndex] = new Stack<Plant>();
                for (var j = 0; j < numPerType; j++)
                {
                    var newPlantGo = Instantiate(_plantPrefabs[typeIndex]);
                    var plant = newPlantGo.GetComponent<Plant>();
                    plant.gameObject.SetActive(false);
                    _availablePlants[typeIndex].Push(plant);
                }
            }
        }

        public Plant GetPlant(string type, Vector3 pos, bool render)
        {
            var typeIndex = PlantTypes.IndexOf(type);
            Plant plant;
            if (_availablePlants[typeIndex].Count > 0)
            {
                plant = _availablePlants[typeIndex].Pop();
            }
            else
            {
                var newPlantGo = Instantiate(_plantPrefabs[typeIndex]);
                plant = newPlantGo.GetComponent<Plant>();
            }

            plant.gameObject.SetActive(render);
            plant.Initialize(pos);
            return plant;
        }

        public void ReturnPlant(Plant plant)
        {
            var typeIndex = PlantData.Data.Keys.ToList().IndexOf(plant.data.type);

            plant.Reset();
            plant.gameObject.SetActive(false);
            _availablePlants[typeIndex].Push(plant);
        }
    }
}