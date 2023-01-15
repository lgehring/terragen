using System.Collections.Generic;
using UnityEngine;

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
    private const double DeltaT = 0.5;
    public Rect bounds;
    public double time;
    public bool instantiateLive;
    public int count;
    private List<Plant> _plantTemplates;
    public Plant[,] plantsMatrix;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Redefine bounds TODO: like mesh, move somewhere else
        // get mapChunkSize from MapGenerator
        var ecoSize = MapGenerator.mapChunkSize * 10; // in meters

        bounds = new Rect(-ecoSize / 2, -ecoSize / 2, ecoSize, ecoSize);

        // Initialize plantMatrix with nulls
        plantsMatrix = new Plant[(int)bounds.width, (int)bounds.height]; // decimeter resolution

        // Set instantiateLive to false for performance 
        instantiateLive = false;

        // Get mesh Collider
        var meshCollider = GameObject.Find("Mesh").GetComponent<MeshCollider>();

        // Populate plant templates
        _plantTemplates = new List<Plant>
        {
            new(
                "Grass",
                Vector2.zero, //TODO: calc height here instead of in plant
                1,
                5.0f,
                5,
                3.0,
                Resources.Load("grass"),
                meshCollider,
                0.0,
                100.0,
                0.0,
                3000.0),

            new(
                "Tree",
                Vector2.zero,
                8,
                20.0f,
                1,
                5.0,
                Resources.Load("tree"),
                meshCollider,
                0.0,
                200.0,
                0.0,
                2000.0)
        };
    }

    // A function that translates a position in the world to a position in the plant matrix
    private Vector2Int WorldToMatrix(Vector2 worldPos)
    {
        return new Vector2Int(
            (int)(worldPos.x + bounds.width / 2),
            (int)(worldPos.y + bounds.height / 2));
    }

    // Performs full single step of the ecosystem simulation
    public void EvolveEcosystem()
    {
        var newSeeds = new List<Plant>();
        for (var i = 0; i < plantsMatrix.GetLength(0); i++)
        for (var j = 0; j < plantsMatrix.GetLength(1); j++)
        {
            // Skip cell if plant does not exist
            if (plantsMatrix[i, j] == null) continue;
            
            // Remove collisions
            var collisionPositions = GetCollisionsInRadius(new[] { i, j }, plantsMatrix[i, j].Radius);
            foreach (var collisionPosition in collisionPositions)
            {
                var winsFight = plantsMatrix[i, j].Fight(plantsMatrix[collisionPosition[0], collisionPosition[1]]);
                if (winsFight)
                {
                    plantsMatrix[collisionPosition[0], collisionPosition[1]] = null;
                }
                else
                {
                    plantsMatrix[i, j] = null;
                    break;
                }
            }
            
            // Check if plant still exists
            if (plantsMatrix[i, j] == null) continue;

            // Generate new seeds
            newSeeds.AddRange(plantsMatrix[i, j].GenerateSeeds());
            
            // Grow plant, remove old plants
            if (!plantsMatrix[i, j].Grow()) continue;
            plantsMatrix[i, j] = null;
        }

        // For each seed
        foreach (var seed in newSeeds)
        {
            // Check if seed is within bounds
            if (!bounds.Contains(seed.Position))
            {
                seed.Die();
                continue;
            }

            // If there is no plant at the position of the seed, add the seed to the ecosystem
            var seedMatrixPosition = WorldToMatrix(seed.Position);
            plantsMatrix[seedMatrixPosition.x, seedMatrixPosition.y] ??= seed;
            if (instantiateLive) seed.Instantiate();
        }

        // Advance time, update number of existing plants
        time += DeltaT;
        UpdateCount();
    }

    private IEnumerable<int[]> GetCollisionsInRadius(IReadOnlyList<int> pos, int radius)
    {
        var cells = new List<int[]>();
        var radiusSquared = radius * radius;
        var lowerBoundX = Mathf.Max(0, pos[0] - radius);
        var upperBoundX = Mathf.Min(plantsMatrix.GetLength(0) - 1, pos[0] + radius);
        var lowerBoundY = Mathf.Max(0, pos[1] - radius);
        var upperBoundY = Mathf.Min(plantsMatrix.GetLength(1) - 1, pos[1] + radius);
        for (var i = lowerBoundX; i <= upperBoundX; i++)
        for (var j = lowerBoundY; j <= upperBoundY; j++)
        {
            if (i == pos[0] && j == pos[1]) continue;
            if ((i - pos[0]) * (i - pos[0]) + (j - pos[1]) * (j - pos[1]) > radiusSquared) continue;
            if (plantsMatrix[i, j] == null) continue;
            cells.Add(new[] { i, j });
        }

        return cells;
    }

    // Initially seed the ecosystem randomly with a few plants from the templates
    public void InitSeeds(int numPlants = 1000)
    {
        for (var i = 0; i < numPlants; i++)
        {
            var plantIndex = Random.Range(0, _plantTemplates.Count);
            // Randomize position
            var x = (int)Random.Range(bounds.xMin, bounds.xMax);
            var y = (int)Random.Range(bounds.yMin, bounds.yMax);
            // Deep copy the plant template
            var newPlant = _plantTemplates[plantIndex].Clone(new Vector2(x, y));
            // Add the plant to the ecosystem
            var matrixPosition = WorldToMatrix(newPlant.GetPosition());
            plantsMatrix[matrixPosition.x, matrixPosition.y] = newPlant;
            if (instantiateLive) newPlant.Instantiate();
        }

        UpdateCount();
    }

    // Instantiate plants
    public void Instantiate()
    {
        for (var i = 0; i < plantsMatrix.GetLength(0); i++)
        for (var j = 0; j < plantsMatrix.GetLength(1); j++)
            if (plantsMatrix[i, j] != null)
                plantsMatrix[i, j].Instantiate();
    }

    public void UpdateCount()
    {
        count = 0;
        for (var i = 0; i < plantsMatrix.GetLength(0); i++)
        for (var j = 0; j < plantsMatrix.GetLength(1); j++)
            if (plantsMatrix[i, j] != null)
                count++;
    }
}