using System.Collections.Generic;
using System.Linq;
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
    private List<Plant> _plantTemplates;
    public QuadTree<Plant> plantsQuadTree;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Redefine bounds TODO: like mesh, move somewhere else
        bounds = new Rect(-1205, -1205, 2410, 2410); // 2.41 km x 2.41 km ~ 5.81 km^2

        // Initialize quadtree with ecosystem bounds and maximum plants per node
        plantsQuadTree = new QuadTree<Plant>(50, bounds); // 50 was optimal for 20 iterations

        // Set instantiateLive to false for performance 
        instantiateLive = false;
        
        // Get mesh Collider
        var meshCollider = GameObject.Find("Mesh").GetComponent<MeshCollider>();

        // Populate plant templates
        _plantTemplates = new List<Plant>
        {
            // new(
            //     "Grass", 
            //     Vector2.zero, //TODO: calc height here instead of in plant
            //     1.0f,
            //     0.5f,
            //     1.5f,
            //     5.0f,
            //     5,
            //     3.0,
            //     Resources.Load("grass"),
            //     meshCollider,
            //     0.0,
            //     100.0,
            //     0.0,
            //     3000.0),

            new(
                "Tree",
                Vector2.zero,
                5.0f,
                3.0f,
                6.0f,
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

    // Draw debugging information for the quadtree
    // Called automatically by Unity when the object is selected in the editor
    private void OnDrawGizmosSelected()
    {
        plantsQuadTree.DrawDebug();
    }

    // Performs full single step of the ecosystem simulation
    public void EvolveEcosystem()
    {
        // Generate new plants from seeds and add them to the ecosystem if they are within the bounds
        foreach (var seed in plantsQuadTree.GetAllElements().Select(plant => plant.GenerateSeeds())
                     .SelectMany(seeds => seeds))
            if (bounds.Contains(seed.GetPosition()))
            {
                plantsQuadTree.Insert(seed);
                if (instantiateLive) seed.Instantiate();
            }
            else
            {
                seed.Die();
            }

        // Remove plants that collide with other plants
        RemoveCollisions();
        // Grow all plants and remove old ones and seeds that have unsuitable conditions
        foreach (var plant in from plant in plantsQuadTree.GetAllElements()
                 let dies = plant.Grow()
                 where dies
                 select plant)
            plantsQuadTree.Remove(plant);

        // Advance time
        time += DeltaT;
    }

    // Check for colliding plants and kill them
    private void RemoveCollisions()
    {
        // For each plant get the colliding plants
        foreach (var plant in plantsQuadTree.GetAllElements())
        {
            var collidingPlants = plantsQuadTree.RetrieveObjectsInArea(plant.MaxBounds);

            // Check for collisions with each colliding plant
            foreach (var collidingPlant in collidingPlants)
            {
                if (collidingPlant == plant) continue;

                var loser = plant.Collides(collidingPlant);
                switch (loser)
                {
                    case 1:
                        plant.Die();
                        plantsQuadTree.Remove(plant);
                        break;
                    case 2:
                        collidingPlant.Die();
                        plantsQuadTree.Remove(collidingPlant);
                        break;
                }
            }
        }
    }

    // Initially seed the ecosystem randomly with a few plants from the templates
    public void InitSeeds(int numPlants = 1000)
    {
        for (var i = 0; i < numPlants; i++)
        {
            var plantIndex = Random.Range(0, _plantTemplates.Count);
            // Deep copy the plant template
            var newPlant = _plantTemplates[plantIndex].Clone();
            // Randomize position
            var x = Random.Range(bounds.xMin, bounds.xMax);
            var y = Random.Range(bounds.yMin, bounds.yMax);
            newPlant.Position = new Vector2(x, y);
            plantsQuadTree.Insert(newPlant);
            if (instantiateLive) newPlant.Instantiate();
        }
    }

    // Instantiate plants
    public void Instantiate()
    {
        foreach (var plant in plantsQuadTree.GetAllElements()) plant.Instantiate();
    }
}