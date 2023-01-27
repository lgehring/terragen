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
        private GameObject _plantsParent;

        public void CreatePlantPool()
        {
            // Update the plant prefabs
            var ecosystem = FindObjectOfType<Ecosystem>();
            ecosystem.UpdatePlantPrefabFiles();
            // Get the plant parent
            _plantsParent = GameObject.Find("Plants");
            // Create the initial pool of plants
            var numTypes = PlantTypes.Count;
            _plantPrefabs = new GameObject[numTypes];
            for (var typeIndex = 0; typeIndex < numTypes; typeIndex++)
            {
                var typeInfo = PlantData.Get(PlantTypes[typeIndex]);
                var typePrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlantPrefabs/" + typeInfo.type + ".prefab");
                typePrefab.name = typeInfo.type;
                // Rescale the plant prefab to the correct size
                var bounds = typePrefab.GetComponent<MeshFilter>().sharedMesh.bounds;
                var maxExtent = Mathf.Max(bounds.size.x, bounds.size.z);
                var scale = typeInfo.sizeRadius / maxExtent;
                typePrefab.transform.localScale = new Vector3(scale, scale, scale);
                _plantPrefabs[typeIndex] = typePrefab;
                // Populate the pool
                _availablePlants[typeIndex] = new Stack<Plant>();
                var numPlants = initialPoolSize;
                if (typeInfo.type.Contains("grass"))
                    numPlants = initialPoolSize * 100; // Grass is very common
                for (var j = 0; j < numPlants; j++)
                {
                    var newPlantGo = Instantiate(_plantPrefabs[typeIndex], _plantsParent.transform, true);
                    // Give each plant a random rotation
                    newPlantGo.transform.Rotate(0, Random.Range(0, 360), 0);
                    newPlantGo.isStatic = true;
                    var plant = newPlantGo.GetComponent<Plant>();
                    // Give each plant a slightly different max scale
                    plant.fullScale = Random.Range(0.8f, 1.2f) * scale;
                    // Give each plant a slightly different max age
                    plant.maxAge = Random.Range(0.8f, 1.2f) * typeInfo.maxAge;
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
                Debug.LogWarning("Creating new plant of type " + type);
                var newPlantGo = Instantiate(_plantPrefabs[typeIndex], _plantsParent.transform, true);
                var typeInfo = PlantData.Get(PlantTypes[typeIndex]);
                // Give each plant a random rotation
                newPlantGo.transform.Rotate(0, Random.Range(0, 360), 0);
                newPlantGo.isStatic = true;
                plant = newPlantGo.GetComponent<Plant>();
                // Rescale the plant prefab to the correct size
                var typePrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlantPrefabs/" + typeInfo.type + ".prefab");
                var bounds = typePrefab.GetComponent<MeshFilter>().sharedMesh.bounds;
                var maxExtent = Mathf.Max(bounds.size.x, bounds.size.z);
                var scale = typeInfo.sizeRadius / maxExtent;
                // Give each plant a slightly different max scale
                plant.fullScale = Random.Range(0.8f, 1.2f) * scale;
                // Give each plant a slightly different max age
                plant.maxAge = Random.Range(0.8f, 1.2f) * typeInfo.maxAge;
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