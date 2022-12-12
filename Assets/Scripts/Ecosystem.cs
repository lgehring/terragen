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
        bounds = new Rect(-100, -100, 200, 200);

        // Initialize quadtree with ecosystem bounds and maximum plants per node
        plantsQuadTree = new QuadTree<Plant>(50, bounds); // 50 was optimal for 20 iterations

        // Set instantiateLive to false for performance 
        instantiateLive = false;

        // Populate plant templates
        _plantTemplates = new List<Plant>
        {
            new(
                "Grass",
                Vector2.zero, //TODO: calc height here instead of in plant
                1.0f,
                0.5f,
                1.5f,
                5.0f,
                5,
                3.0,
                Resources.Load("grass")),

            new(
                "Tree",
                Vector2.zero,
                5.0f,
                3.0f,
                6.0f,
                20.0f,
                1,
                5.0,
                Resources.Load("tree"))
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
        // Grow all plants and remove old ones
        foreach (var plant in from plant in plantsQuadTree.GetAllElements()
                 let old = plant.Grow()
                 where old
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
    public void InitSeeds(int numPlants = 10)
    {
        for (var i = 0; i < numPlants; i++)
        {
            var plantIndex = Random.Range(0, _plantTemplates.Count);
            var newPlant = _plantTemplates[plantIndex];
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