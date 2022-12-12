using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     A class defining a single plant object and its ecological properties.
/// </summary>
public class Plant : IQuadTreeObject
{
    // Constructor
    public Plant(string name, Vector2 position, float sizeRadius, float minDistIntern, float minDistExtern,
        float seedingRadius, int seedCount, double maxAge, Object prefab)
    {
        Name = name;
        Position = position;
        SizeRadius = sizeRadius;
        MinDistIntern = minDistIntern;
        MinDistExtern = minDistExtern;
        SeedingRadius = seedingRadius;
        SeedCount = seedCount;
        MaxAge = maxAge;
        Age = 0;
        Prefab = prefab;
        MaxBounds = new Rect(position.x - minDistExtern,
            position.y - minDistExtern,
            minDistExtern * 2,
            minDistExtern * 2);
    }

    private string Name { get; }
    protected internal Vector2 Position { get; set; }
    private float SizeRadius { get; }
    internal Rect MaxBounds { get; set; }
    private float MinDistIntern { get; } // Blocking zone for other plants of the same type
    private float MinDistExtern { get; } // Blocking zone for other plants of different types
    private float SeedingRadius { get; }
    private int SeedCount { get; }
    private double Viability { get; set; }
    private double MaxAge { get; }
    private double Age { get; set; }
    private bool IsDead { get; set; }
    private GameObject GameObject { get; set; }
    private Object Prefab { get; }

    // Implement IQuadTreeObject interface
    public Vector2 GetPosition()
    {
        return Position;
    }

    public void Instantiate()
    {
        GameObject = (GameObject)Object.Instantiate(Prefab);
        GameObject.isStatic = true;
        // Scale game object to fit within the internal size radius of the plant TODO: optimize
        var meshFilter = GameObject.GetComponent<MeshFilter>();
        var bounds = meshFilter.mesh.bounds;
        var maxExtent = Mathf.Max(bounds.size.x, bounds.size.z);
        var scale = SizeRadius / maxExtent;
        GameObject.transform.localScale = new Vector3(scale, scale, scale);
        // Set position
        var height = GetHeightAtPosition(Position);
        GameObject.transform.position = new Vector3(Position.x, height, Position.y);
    }

    private static float GetHeightAtPosition(Vector2 position)
    {
        var height = 0f;
        var ray = new Ray(new Vector3(position.x, 1000, position.y), Vector3.down);
        if (Physics.Raycast(ray, out var hit)) height = hit.point.y;

        return height;
    }

    // Age the plant and check if it is dead
    public bool Grow(double deltaT = 0.5)
    {
        Age += deltaT;
        if (Age > MaxAge)
        {
            Die();
            return true;
        }

        UpdateViability();
        return false;
    }

    // Seed offspring, return newly seeded plants
    protected internal List<Plant> GenerateSeeds()
    {
        var seeds = new List<Plant>();
        for (var i = 0; i < SeedCount; i++)
        {
            // Create a new plant with identical properties at a random position within the seeding radius
            var seedPosition = new Vector2(
                Random.Range(Position.x - SeedingRadius, Position.x + SeedingRadius),
                Random.Range(Position.y - SeedingRadius, Position.y + SeedingRadius)
            );
            var seed = new Plant(Name, seedPosition, SizeRadius, MinDistIntern, MinDistExtern, SeedingRadius, SeedCount,
                MaxAge, Prefab);
            seeds.Add(seed);
        }

        return seeds;
    }

    // Update Viability
    private void UpdateViability()
    {
        var normalizedAge = Age / MaxAge;
        if (normalizedAge < 0.5)
            Viability = normalizedAge;
        else
            Viability = 1 - normalizedAge;
    }

    // Kills the plant
    protected internal void Die()
    {
        IsDead = true;
        Object.Destroy(GameObject);
    }

    // Detect if two plants collide and kill the one with lower viability
    // Returns 1 if the plant was killed, 2 if the other plant was killed and 0 if neither was killed
    protected internal int Collides(Plant other)
    {
        // If one plant is already dead, do nothing
        if (IsDead || other.IsDead) return 0;

        // If the plants are too far apart, do nothing
        var distance = Vector2.Distance(Position, other.Position);
        if (Name == other.Name)
        {
            // Same plant type
            if (!(distance < MinDistIntern)) return 0;
        }
        else
        {
            // Different plant type
            if (!(distance < Mathf.Max(MinDistExtern + other.MinDistExtern))) return 0;
        }

        // Else: Kill the plant with lower viability
        if (Viability > other.Viability)
        {
            other.Die();
            return 2;
        }

        Die();
        return 1;
    }
}