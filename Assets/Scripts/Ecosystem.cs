using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
///     A class keeping track of and modelling all plants.
///     This follows a paper by Bedrich Benes: "A STABLE MODELING OF LARGE PLANT ECOSYSTEMS"
///     The basic algorithm is as follows:
///         1. plants that should go to seed generate new seeds,
///         2. plants out of the ecosystem are eliminated,
///         3. colliding plants are detected,
///         4. old and colliding plants are eliminated,
///         5. all plants grow, and
///         6. the time step is increased
/// </summary>
public class Ecosystem : MonoBehaviour
{
    public readonly List<Plant> plants = new();
    public readonly Tuple<Vector2, Vector2> bounds = new(new Vector2(-100, -100), new Vector2(100, 100));
    public double time;
    private const double DeltaT = 0.5;
    private List<Plant> _plantTemplates;
    private Mesh _mesh;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Populate plant templates
        _plantTemplates = new List<Plant>
        {
            new Plant(
                "Grass",
                Vector2.zero, //TODO: calc height here instead of in plant
                1.0f,
                0.5f,
                1.5f,
                5.0f,
                5,
                0.0,
                3.0,
                "grass",
                _mesh,
                true),

            new Plant(
                "Tree",
                Vector2.zero,
                5.0f,
                3.0f,
                6.0f,
                20.0f,
                1,
                0.0,
                5.0,
                "tree",
                _mesh,
                true)
        };
    }

    public void SetMeshData(Mesh mesh)
    {
        _mesh = mesh;
        Debug.Log("Ecosystem mesh set");
    }

    // Performs full single step of the ecosystem simulation
    public void EvolveEcosystem()
    {
        // Generate new plants from seeds and add them to the ecosystem
        var newPlants = new List<Plant>();
        foreach (var seeds in plants.Select(plant => plant.GenerateSeeds()))
        {
            newPlants.AddRange(seeds);
        }
        plants.AddRange(newPlants);
        // Add plants out of the ecosystem to the list of dead plants
        var deadPlants = plants.Where(plant => plant.Position.x < bounds.Item1.x || plant.Position.x > bounds.Item2.x || plant.Position.y < bounds.Item1.y || plant.Position.y > bounds.Item2.y).ToList();
        plants.RemoveAll(plant => deadPlants.Contains(plant));
        foreach (var plant in deadPlants) plant.Die();
        // Remove plants that collide with other plants
        RemoveCollisions();
        // Grow all plants and remove old ones
        plants.RemoveAll(plant => plant.Grow());
        // Advance time
        time += DeltaT;
    }

    // Check for colliding plants and kill them
    private void RemoveCollisions()
    {
        var plantsToRemove = new List<int>();
        // Detect collisions
        for (var i = 0; i < plants.Count; i++)
        {
            for (var j = 0; j < plants.Count; j++)
            {
                if (i == j) continue;
                
                var loser = plants[i].Collides(plants[j]); // changes losing plants internal property
                switch (loser)
                {
                    case 1:
                        plants[i].Die();
                        plantsToRemove.Add(i);
                        break;
                    case 2:
                        plants[j].Die();
                        plantsToRemove.Add(j);
                        break;
                }
            }
        }

        // Remove dead plants
        plantsToRemove.Sort();
        plantsToRemove.Reverse(); // Sort descending to avoid index errors
        foreach (var index in plantsToRemove)
        {
            plants.RemoveAt(index);
        }
    }

    // Initially seed the ecosystem with a few plants from the templates
    public void InitSeeds(int numPlants = 10)
    {
        for (var i = 0; i < numPlants; i++)
        {
            var plantIndex = Random.Range(0, _plantTemplates.Count);
            var newPlant = _plantTemplates[plantIndex];
            // Randomize position
            var x = Random.Range(bounds.Item1.x, bounds.Item2.x);
            var y = Random.Range(bounds.Item1.y, bounds.Item2.y);
            newPlant.Position = new Vector2(x, y);
            plants.Add(newPlant);
        }
    }
}