// using System.Collections.Generic;
// using UnityEngine;
//
// public class PlantPool : MonoBehaviour {
//     private readonly Stack<Plant> _availablePlants = new();
//     public GameObject plantPrefab;
//     public int initialPoolSize = 100000;
//
//     private void Start() {
//         // Create the initial pool of Plant objects
//         for (var i = 0; i < initialPoolSize; i++) {
//             var newPlantGo = Instantiate(plantPrefab);
//             var newPlant = newPlantGo.GetComponent<Plant>();
//             newPlant.isActive = false;
//             _availablePlants.Push(newPlant);
//         }
//     }
//
//     public Plant GetPlant(string type) {
//         Plant plant;
//         if (_availablePlants.Count > 0) {
//             plant = _availablePlants.Pop();
//             plant.isActive = true;
//             plant.gameObject.SetActive(true);
//         } else {
//             var newPlantGo = Instantiate(plantPrefab);
//             plant = newPlantGo.GetComponent<Plant>();
//             plant.isActive = true;
//         }
//         plant.Initialize(type);
//         return plant;
//     }
//
//     public void ReturnPlant(Plant plant) {
//         plant.Reset();
//         plant.gameObject.SetActive(false);
//         _availablePlants.Push(plant);
//     }
// }
//
// public class Plant : MonoBehaviour, IQuadTreeObject {
//     public bool isActive;
//     public PlantInfo info;
//     public Vector2 position;
//
//     public void Initialize(string type) {
//         isActive = true;
//         info = PlantInfo.Get(type);
//     }
//
//     public void Reset()
//     {
//         isActive = false;
//         info = null;
//         position = Vector2.zero;
//     }
//     
//     public Vector2 GetPosition() {
//         return position;
//     }
// }