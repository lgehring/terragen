using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     A class defining a single plant object and its ecological properties.
/// </summary>
public class Plant
{
    private Mesh _heightMapMesh;
    private string Name { get; set; }
    protected internal Vector2 Position { get; set; }
    private float SizeRadiusInternal { get; set; } // Blocking zone for other plants of the same type
    private float SizeRadiusExternal { get; set; } // Blocking zone for other plants of different types
    private float SeedingRadius { get; set; }
    private int SeedCount { get; set; }
    private double Viability { get; set; }
    private double BirthDate { get; set; }
    private double MaxAge { get; set; }
    private double Age { get; set; }
    private bool IsDead { get; set; }
    private GameObject GameObject { get; set; }
    private string PrefabPath { get; set; }
    private Mesh HeightMapMesh { get; set; }

    // Constructor
    public Plant(string name, Vector2 position, float sizeRadiusInternal, float sizeRadiusExternal, float seedingRadius,
        int seedCount, double birthDate,
        double maxAge, string prefabPath, Mesh heightMapMesh, bool isTemplate = false)
    {
        Name = name;
        Position = position;
        SizeRadiusInternal = sizeRadiusInternal;
        SizeRadiusExternal = sizeRadiusExternal;
        SeedingRadius = seedingRadius;
        SeedCount = seedCount;
        BirthDate = birthDate;
        MaxAge = maxAge;
        Age = 0;
        PrefabPath = prefabPath;
        HeightMapMesh = heightMapMesh;
        if (!isTemplate)
        {
            GameObject = (GameObject)Object.Instantiate(Resources.Load(prefabPath));
            // TODO: Get height from the heightMapMesh
            // var height = heightMapMesh.vertices[(int)position.x * heightMapMesh.vertices.GetLength(0) + (int)position.y]
            //     .y;
            GameObject.transform.position = new Vector3(position.x, 0, position.y);
        }
    }

    // Age the plant and check if it is dead
    public bool Grow(double deltaT = 0.5)
    {
        Age += deltaT;
        UpdateViability();
        if (Age > MaxAge)
        {
            Die();
            return true;
        }

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
            var seed = new Plant(Name, seedPosition, SizeRadiusInternal, SizeRadiusExternal, SeedingRadius, SeedCount, BirthDate, MaxAge, PrefabPath, HeightMapMesh);
            seeds.Add(seed);
        }
        return seeds;
    }

    // Update Viability
    private void UpdateViability()
    {
        var normalizedAge = Age / MaxAge;
        if (normalizedAge < 0.5)
        {
            Viability = normalizedAge;
        }
        else
        {
            Viability = 1 - normalizedAge;
        }
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
            if (!(distance < SizeRadiusInternal + other.SizeRadiusInternal)) return 0;
        }
        else
        {
            // Different plant type
            if (!(distance < SizeRadiusExternal + other.SizeRadiusExternal)) return 0;
        }
        
        // Else: Kill the plant with lower viability
        if (Viability > other.Viability)
        {
            other.Die();
            return 2;
        }
        else
        {
            Die();
            return 1;
        }
    }
}