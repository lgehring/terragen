using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ecosystem
{
    /// <summary>
    ///     A class keeping track of and modelling all plants.
    ///     This follows a paper by Bedrich Benes: "A STABLE MODELING OF LARGE PLANT ECOSYSTEMS"
    ///     The basic algorithm is as follows:
    ///     1. plants that should go to seed generate new seeds
    ///     2. plants out of the ecosystem are eliminated
    ///     3. colliding plants are detected and eliminated
    ///     4. all plants grow and old plants are eliminated
    ///     5. the time step is increased
    /// </summary>
    public class Ecosystem : MonoBehaviour
    {
        public Rect bounds;
        public double time;
        public bool renderPlants;
        public int count;
        public PlantPool plantPool;
        private Plant[,] _plantsMatrix;
        private MeshCollider _terrainCollider;

        // Awake is called when the script instance is being loaded
        private void Awake()
        {
            // Get the plantPool and terrain collider
            plantPool = GetComponent<PlantPool>();
            _terrainCollider = GameObject.Find("Mesh").GetComponent<MeshCollider>();

            // Redefine bounds
            var ecoSize = MapGenerator.mapChunkSize * 10; // in meters

            // ReSharper disable twice PossibleLossOfFraction
            bounds = new Rect(-ecoSize / 2, -ecoSize / 2, ecoSize, ecoSize);

            // Initialize plantMatrix
            _plantsMatrix = new Plant[(int)bounds.width, (int)bounds.height]; // decimeter resolution

            renderPlants = false;
        }

        // Performs full single step of the ecosystem simulation
        public void EvolveEcosystem()
        {
            var newSeedPositions = new List<Vector2Int>();
            var newSeedTypes = new List<string>();
            for (var i = 0; i < _plantsMatrix.GetLength(0); i++)
            for (var j = 0; j < _plantsMatrix.GetLength(1); j++)
            {
                // Skip cell if plant does not exist
                if (_plantsMatrix[i, j] == null) continue;

                // Remove collisions
                var collisionPositions =
                    GetCollisionsInRadius(new[] { i, j }, _plantsMatrix[i, j].data.collisionRadius);
                foreach (var collisionPosition in collisionPositions)
                {
                    // Allow grass to grow on in the vicinity of other plants (not itself)
                    var isGrass = _plantsMatrix[collisionPosition[0], collisionPosition[1]].data.type.Contains("grass");
                    if (isGrass && !_plantsMatrix[i, j].data.type.Contains("grass")) continue;

                    var winsFight = _plantsMatrix[i, j]
                        .Fight(_plantsMatrix[collisionPosition[0], collisionPosition[1]]);
                    if (winsFight)
                    {
                        plantPool.ReturnPlant(_plantsMatrix[collisionPosition[0], collisionPosition[1]]);
                        _plantsMatrix[collisionPosition[0], collisionPosition[1]] = null;
                    }
                    else
                    {
                        plantPool.ReturnPlant(_plantsMatrix[i, j]);
                        _plantsMatrix[i, j] = null;
                        break;
                    }
                }

                // Check if plant still exists
                if (_plantsMatrix[i, j] == null) continue;

                // Generate new seeds
                var newPos = _plantsMatrix[i, j].GenerateSeedPositions(new Vector2Int(i, j));
                // ReSharper disable twice PossibleMultipleEnumeration
                newSeedPositions.AddRange(newPos);
                newSeedTypes.AddRange(Enumerable.Repeat(_plantsMatrix[i, j].data.type, newPos.Count()));
                // Grow plant, remove old plants
                //TODO: Add environmental feedback
                if (_plantsMatrix[i, j].Grow())
                {
                    // Plant dies
                    plantPool.ReturnPlant(_plantsMatrix[i, j]);
                    _plantsMatrix[i, j] = null;
                }
                else
                {
                    if (renderPlants)
                        AgeScalePlant(_plantsMatrix[i, j]);
                }
            }

            // Iterate over new seeds and instantiate them if valid
            for (var i = 0; i < newSeedPositions.Count; i++)
            {
                var seedPos = newSeedPositions[i];
                var seedType = newSeedTypes[i];
                PlacePlant(seedPos, seedType);
            }

            // Advance time, update number of existing plants
            const double deltaT = 0.5;
            time += deltaT;
            UpdateCount();
        }

        private void PlacePlant(Vector2Int pos, string type)
        {
            // Check if seed is within matrix bounds
            if (pos.x < 0 || pos.x >= _plantsMatrix.GetLength(0) ||
                pos.y < 0 || pos.y >= _plantsMatrix.GetLength(1)) return;

            // Check if position is empty
            if (_plantsMatrix[pos.x, pos.y] != null) return;

            // Check if position is valid for type
            var worldPos = MatrixToWorld(pos);
            var raycastResult = RaycastAtPosition(worldPos);
            var slopeDegree = Vector3.Angle(Vector3.up, raycastResult.normal);
            var slopePercent = Mathf.Tan(slopeDegree * Mathf.Deg2Rad) * 100;
            var heightConstraints = PlantData.Get(type).heightConstraints;
            var slopeConstraints = PlantData.Get(type).slopeConstraints;
            if (!(heightConstraints[0] <= raycastResult.height) ||
                !(heightConstraints[1] > raycastResult.height) ||
                !(slopeConstraints[0] <= slopePercent) ||
                !(slopeConstraints[1] > slopePercent)) return;

            // Instantiate plant
            var newPos = new Vector3(worldPos.x, raycastResult.height, worldPos.y);
            // Randomly choose a type variation
            // var subType = PlantData.GetVariation(type);
            var newPlant = plantPool.GetPlant(type, newPos, renderPlants);
            _plantsMatrix[pos.x, pos.y] = newPlant;
        }

        private IEnumerable<int[]> GetCollisionsInRadius(IReadOnlyList<int> pos, int radius)
        {
            var cells = new List<int[]>();
            var radiusSquared = radius * radius;
            var lowerBoundX = Mathf.Max(0, pos[0] - radius);
            var upperBoundX = Mathf.Min(_plantsMatrix.GetLength(0) - 1, pos[0] + radius);
            var lowerBoundY = Mathf.Max(0, pos[1] - radius);
            var upperBoundY = Mathf.Min(_plantsMatrix.GetLength(1) - 1, pos[1] + radius);
            for (var i = lowerBoundX; i <= upperBoundX; i++)
            for (var j = lowerBoundY; j <= upperBoundY; j++)
            {
                if (i == pos[0] && j == pos[1]) continue;
                if ((i - pos[0]) * (i - pos[0]) + (j - pos[1]) * (j - pos[1]) > radiusSquared) continue;
                if (_plantsMatrix[i, j] == null) continue;
                cells.Add(new[] { i, j });
            }

            return cells;
        }

        // Initially seed the ecosystem randomly with a few plants from the templates
        public void InitSeeds(int numPlants = 1000)
        {
            for (var i = 0; i < numPlants; i++)
            {
                var type = PlantData.Data.ElementAt(Random.Range(0, PlantData.Data.Count)).Key;
                // Randomize position
                var x = (int)Random.Range(bounds.xMin, bounds.xMax);
                var y = (int)Random.Range(bounds.yMin, bounds.yMax);
                var pos = WorldToMatrix(new Vector2(x, y));

                PlacePlant(pos, type);
            }

            UpdateCount();
        }

        private void UpdateCount()
        {
            count = 0;
            for (var i = 0; i < _plantsMatrix.GetLength(0); i++)
            for (var j = 0; j < _plantsMatrix.GetLength(1); j++)
                if (_plantsMatrix[i, j] != null)
                    count++;
        }

        private (float height, Vector3 normal) RaycastAtPosition(Vector2 position)
        {
            const int maxHeight = 1025; // Limited by the heightmap resolution
            var height = 0f;
            var normal = Vector3.zero;
            var ray = new Ray(new Vector3(position.x, maxHeight, position.y), Vector3.down);

            if (!_terrainCollider.Raycast(ray, out var hit, maxHeight)) return (height, normal);
            height = hit.point.y;
            normal = hit.normal;

            // // VISUAL DEBUG RAYCAST
            // var angleDegree = Vector3.Angle(Vector3.up, normal);
            // var anglePercent = Mathf.Tan(angleDegree * Mathf.Deg2Rad) * 100;
            // var color = Color.blue;
            // if (anglePercent > 200)
            // {
            //     color = Color.red;
            // }
            // else if (anglePercent > 100)
            // {
            //     color = Color.yellow;
            // }
            // else if (anglePercent > 0)
            // {
            //     color = Color.green;
            // }
            // Debug.DrawRay(hit.point, hit.normal, color, 100f);

            return (height, normal);
        }

        public void ShowPlants(bool show)
        {
            if (renderPlants == show) return;
            for (var i = 0; i < _plantsMatrix.GetLength(0); i++)
            for (var j = 0; j < _plantsMatrix.GetLength(1); j++)
                if (_plantsMatrix[i, j] != null)
                {
                    if (show) AgeScalePlant(_plantsMatrix[i, j]);
                    _plantsMatrix[i, j].gameObject.SetActive(show);
                }

            renderPlants = show;
        }

        private static void AgeScalePlant(Plant plant)
        {
            var relAge = (float)(plant.age / plant.maxAge);
            var plantFullScale = (float)plant.fullScale;
            // Scale plant based on age up to 0.5 relAge (fullScale), then keep it at full scale
            plant.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, plantFullScale, relAge * 2);
        }

        public void CreatePlantPool()
        {
            plantPool.CreatePlantPool();
        }

        public void UpdatePlantPrefabFiles()
        {
            // Delete all existing plant prefab files
            var plantPrefabsPath = Path.Combine(Application.dataPath, "PlantPrefabs");
            var plantPrefabs = Directory.GetFiles(plantPrefabsPath);
            foreach (var plantPrefab in plantPrefabs)
                File.Delete(plantPrefab);

            // Create new plant prefab files
            foreach (var type in PlantData.Data.Keys)
            {
                var plantInfo = PlantData.Get(type);
                var plantModel = AssetDatabase.LoadAssetAtPath<GameObject>(plantInfo.modelPath);
                var prefab = Instantiate(plantModel, transform);
                var plant = prefab.AddComponent<Plant>();
                var path = "Assets/PlantPrefabs/" + plantInfo.type + ".prefab";
                plant.data = PlantData.Get(type);
                plant.data.modelPath = path;
                prefab.name = plantInfo.type;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                DestroyImmediate(prefab);
            }
        }

        // A function that translates a position in the world to a position in the plant matrix
        private Vector2Int WorldToMatrix(Vector2 worldPos)
        {
            return new Vector2Int(
                (int)(worldPos.x + bounds.width / 2),
                (int)(worldPos.y + bounds.height / 2));
        }

        // A function that translates a position in the plant matrix to a position in the world
        private Vector2 MatrixToWorld(Vector2Int matrixPos)
        {
            return new Vector2(
                matrixPos.x - bounds.width / 2,
                matrixPos.y - bounds.height / 2);
        }
    }
}