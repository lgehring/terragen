using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Roads;
using Terrain;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

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
        public UnityEngine.Terrain terrain;
        public PlantPool plantPool;
        private int[,][] _plantBlockedZones; //TODO: make it possible to add entries from outside
        private Plant[,] _plantsMatrix;
        private TerrainCollider _terrainCollider;
        private MeshCollider _roadCollider;

        // Awake is called when the script instance is being loaded
        private void Awake()
        {
            // Get the plantPool and terrain collider
            plantPool = GetComponent<PlantPool>();
            terrain = FindObjectOfType<UnityEngine.Terrain>();
            _terrainCollider = terrain.GetComponent<TerrainCollider>();
            
            // Get road collider TODO: access correct
            _roadCollider = GameObject.Find("Road").GetComponent<MeshCollider>();

            // Redefine bounds
            var ecoSize = terrain.terrainData.size.x; // in meters
            // ReSharper disable twice PossibleLossOfFraction
            bounds = new Rect(-ecoSize / 2, -ecoSize / 2, ecoSize, ecoSize);

            // Initialize plantMatrix
            _plantsMatrix = new Plant[(int)bounds.width, (int)bounds.height]; // decimeter resolution
            
            // Place details
            PlaceGrass();
            PlaceRandomDetails(100, 1); //rocks
            PlaceRandomDetails(250, 2); //branches
            PlaceRandomDetails(10, 3); //dead tree
        }

        // Performs full single step of the ecosystem simulation
        public void EvolveEcosystem()
        {
            const double deltaT = 0.5;
            var newSeedPositions = new List<Vector2Int>();
            var newSeedTypes = new List<string>();
            var plantTypes = PlantData.Data.Keys.ToList();
            var plantCounts = new int[plantTypes.Count];

            // Calculate per type area coverage
            for (var i = 0; i < _plantsMatrix.GetLength(0); i++)
            for (var j = 0; j < _plantsMatrix.GetLength(1); j++)
            {
                // Skip cell if plant does not exist
                if (_plantsMatrix[i, j] == null) continue;

                // Add plant to plantCounts
                plantCounts[plantTypes.IndexOf(_plantsMatrix[i, j].data.type)]++;
            }

            // Multiply each count by its types sizeRadius to get area coverage
            var coverages = new double[plantTypes.Count];
            for (var i = 0; i < plantCounts.Length; i++)
                coverages[i] = plantCounts[i] * PlantData.Data[plantTypes[i]].sizeRadius;
            // Calculate coverage fractions 
            //TODO: manipulate this to get different ecosystems: higher values (-> 1) = better growth
            var covFractions = GetOtherPlantsCovFractions(coverages);

            for (var i = 0; i < _plantsMatrix.GetLength(0); i++)
            for (var j = 0; j < _plantsMatrix.GetLength(1); j++)
            {
                // Skip cell if plant does not exist
                if (_plantsMatrix[i, j] == null) continue;

                // Remove collisions
                var collisionPositions = CheckArea(new[] { i, j }, _plantsMatrix[i, j].data.collisionRadius);
                foreach (var collisionPosition in collisionPositions)
                {
                    // Allow grass to grow on in the vicinity of other plants (not itself)
                    // var isGrass = _plantsMatrix[collisionPosition[0], collisionPosition[1]].data.type.Contains("grass");
                    // if (isGrass && !_plantsMatrix[i, j].data.type.Contains("grass")) continue;

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
                var typeIndex = plantTypes.IndexOf(_plantsMatrix[i, j].data.type);
                if (!_plantsMatrix[i, j].Grow(covFractions[typeIndex]))
                {
                    // Continues living
                    if (renderPlants)
                        AgeScalePlant(_plantsMatrix[i, j]);
                }
                else
                {
                    // Dies of old age
                    plantPool.ReturnPlant(_plantsMatrix[i, j]);
                    _plantsMatrix[i, j] = null;
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
            time += deltaT;
            UpdateCount();
        }

        private void PlaceGrass()
        {
            var terrainController = FindObjectOfType<TerrainController>();
            var heightmapFolder = terrainController.heightmapFolder;
            var realImageWidthInM = terrainController.realImageWidthInM;
            var path = heightmapFolder + "block_grass" + ".png";
            var grassBlockedZones = new bool[(int) bounds.width, (int) bounds.height];
            if (File.Exists(path))
            {
                grassBlockedZones = BlockedPlantReader.BlockedArrayFromImage(
                    path, realImageWidthInM, (int)bounds.width);
            }
            
            var grassPositions = new List<Vector2Int>();
            for (var i = 0; i < grassBlockedZones.GetLength(0); i++)
                for (var j = 0; j < grassBlockedZones.GetLength(1); j++)
                    if (!grassBlockedZones[i, j])
                    {
                        grassPositions.Add(new Vector2Int(i, j));
                        grassPositions.Add(new Vector2Int(i, j));
                    }
            
            PlaceDetails(grassPositions, 0);
        }

        private int[,][] GetBlockedZones()
        {
            // Array of indices of blocked plants at the position in the matrix
            var blockedZones = new int[(int)bounds.width, (int)bounds.height][];
            var plantTypes = PlantData.Data.Keys.Append("all").ToList(); // also check for a complete block map
            // Remove suffixes "_0", "_1", etc. and duplicates
            var plantGroups = plantTypes.Select(type => type.Split('_')[0]).Distinct().ToList();
            var terrainController = FindObjectOfType<TerrainController>();
            var heightmapFolder = terrainController.heightmapFolder;
            var realImageWidthInM = terrainController.realImageWidthInM;

            foreach (var group in plantGroups)
            {
                // Check if the path exists
                var path = heightmapFolder + "block_" + group + ".png";
                if (!File.Exists(path)) continue;
                // Get blocked map
                var blockedMap = BlockedPlantReader.BlockedArrayFromImage(path,
                    realImageWidthInM, (int)bounds.width);

                // For every entry in the map that is true, add the plant type index to the blockedZones array
                for (var i = 0; i < blockedMap.GetLength(0); i++)
                for (var j = 0; j < blockedMap.GetLength(1); j++)
                {
                    if (!blockedMap[i, j]) continue;
                    var plantGroupIndex = plantGroups.IndexOf(group);
                    if (blockedZones[i, j] == null)
                    {
                        blockedZones[i, j] = new[] { plantGroupIndex };
                    }
                    else
                    {
                        var newBlockedZones = new int[blockedZones[i, j].Length + 1];
                        blockedZones[i, j].CopyTo(newBlockedZones, 0);
                        newBlockedZones[^1] = plantGroupIndex;
                        blockedZones[i, j] = newBlockedZones;
                    }

                    // If current pos contains index plantTypes.Count, = all plants are blocked
                    // replace it with a new array containing all indices
                    if (!blockedZones[i, j].Contains(plantGroups.Count - 1)) continue;
                    blockedZones[i, j] = new int[plantGroups.Count - 1];
                    for (var k = 0; k < plantGroups.Count - 1; k++)
                        blockedZones[i, j][k] = k;
                }
            }

            return blockedZones;
        }

        private static double[] GetOtherPlantsCovFractions(IReadOnlyList<double> coverages)
        {
            // The otherPlantsCoverageFrac gives a_i/ a_n
            // where a_i is the area of the plant and a_n is the area of all plants
            var covFractions = new double[coverages.Count];
            var totalCoverage = coverages.Sum();
            for (var i = 0; i < coverages.Count; i++)
                covFractions[i] = coverages[i] / totalCoverage;

            return covFractions;
        }

        private void PlacePlant(Vector2Int pos, string type)
        {
            // Check if seed is within matrix bounds
            if (pos.x < 0 || pos.x >= _plantsMatrix.GetLength(0) ||
                pos.y < 0 || pos.y >= _plantsMatrix.GetLength(1)) return;

            // Check if position is empty
            if (_plantsMatrix[pos.x, pos.y] != null) return;

            // Check if position is blocked for type
            var plantGroupIndex = Array.IndexOf(PlantData.variations,
                PlantData.variations.FirstOrDefault(x => x.Contains(type)));
            if (_plantBlockedZones[pos.x, pos.y] != null &&
                _plantBlockedZones[pos.x, pos.y].Contains(plantGroupIndex)) return;

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
            var subType = PlantData.GetVariation(type);
            var newPlant = plantPool.GetPlant(subType, newPos, renderPlants);
            if (renderPlants) AgeScalePlant(newPlant);
            _plantsMatrix[pos.x, pos.y] = newPlant;
        }
        
        private void PlaceDetails(List<Vector2Int> positions, int detailLayerIndex)
        {
            var terrainData = terrain.terrainData;
            var detailMap = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, detailLayerIndex);
            var detailMapResolution = (float) terrainData.detailResolution;
            var detailMapScale = detailMapResolution / terrainData.size.x;

            foreach (var pos in positions)
            {
                detailMap[(int) (pos.y * detailMapScale), (int) (pos.x * detailMapScale)] = 1;
            }

            terrainData.SetDetailLayer(0, 0, detailLayerIndex, detailMap);
        }
        
        private void PlaceRandomDetails(int num, int index)
        {
            var positions = new List<Vector2Int>();
            for (var i = 0; i < num; i++)
            {
                var x = Random.Range(0, (int) bounds.width);
                var y = Random.Range(0, (int) bounds.height);
                positions.Add(new Vector2Int(x, y));
            }
            PlaceDetails(positions, index);
        }

        private IEnumerable<int[]> CheckArea(IReadOnlyList<int> pos, int radius)
        {
            var cells = new List<int[]>();
            var radiusSquared = radius * radius; // check for collisions only in direct neighborhood
            var lowerBoundX = Mathf.Max(0, pos[0] - radius);
            var upperBoundX = Mathf.Min(_plantsMatrix.GetLength(0) - 1, pos[0] + radius);
            var lowerBoundY = Mathf.Max(0, pos[1] - radius);
            var upperBoundY = Mathf.Min(_plantsMatrix.GetLength(1) - 1, pos[1] + radius);
            for (var i = lowerBoundX; i <= upperBoundX; i++)
            for (var j = lowerBoundY; j <= upperBoundY; j++)
            {
                // Check for collisions
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
            
            // Do not place plants on roads
            if (_roadCollider.Raycast(ray, out _, maxHeight))
                return (height, normal);

            if (!_terrainCollider.Raycast(ray, out var hit, maxHeight))
                return (height, normal);
            height = hit.point.y;
            normal = hit.normal;

            // VISUAL DEBUG RAYCAST
            var angleDegree = Vector3.Angle(Vector3.up, normal);
            var anglePercent = Mathf.Tan(angleDegree * Mathf.Deg2Rad) * 100;
            var color = Color.blue;
            if (anglePercent > 200)
            {
                color = Color.red;
            }
            else if (anglePercent > 100)
            {
                color = Color.yellow;
            }
            else if (anglePercent > 0)
            {
                color = Color.green;
            }
            Debug.DrawRay(hit.point, hit.normal, color, 100f);

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

        public void PrepareEcosystem()
        {
            _plantBlockedZones = FindObjectOfType<TerrainController>().useImage
                ? GetBlockedZones()
                : new int[(int)bounds.width, (int)bounds.height][]; // No blocked zones
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