using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     A class defining a single plant object and its ecological properties.
/// </summary>
public class Plant : IQuadTreeObject
{
    // Constructor
    public Plant(string name, Vector2 position, float sizeRadius, float minDistIntern, float minDistExtern,
        float seedingRadius, int seedCount, double maxAge, Object prefab, MeshCollider myMeshCollider, double minSlope, double maxSlope,
        double minHeight, double maxHeight)
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
        MinSlope = minSlope;
        MaxSlope = maxSlope;
        MinHeight = minHeight;
        MaxHeight = maxHeight;
        MyMeshCollider = myMeshCollider;
        
        // Get height and slope(%) at position
        Vector3 normal;
        (Height, normal) = RaycastAtPosition(position);
        var slopeDegree = Vector3.Angle(Vector3.up, normal);
        SlopePercent = Mathf.Tan(slopeDegree * Mathf.Deg2Rad) * 100;
    }

    internal string Name { get; }
    protected internal Vector2 Position { get; set; }
    protected internal float Height { get; set; }
    protected internal double SlopePercent { get; set; }
    private float SizeRadius { get; }
    internal Rect MaxBounds { get; }
    private float MinDistIntern { get; } // Blocking zone for other plants of the same type
    private float MinDistExtern { get; } // Blocking zone for other plants of different types
    private float SeedingRadius { get; }
    private int SeedCount { get; }
    private double Viability { get; set; }
    private double MaxAge { get; }
    private double Age { get; set; }
    private bool IsDead { get; set; }
    private GameObject GameObject { get; set; }
    private MeshCollider MyMeshCollider { get; set; }
    private Object Prefab { get; }
    private double MinSlope { get; }
    private double MaxSlope { get; }
    private double MinHeight { get; }
    private double MaxHeight { get; }

    // Implement IQuadTreeObject interface
    public Vector2 GetPosition()
    {
        return Position;
    }
    
    // Return a deep copy of this plant
    public Plant Clone()
    {
        return new Plant(Name, Position, SizeRadius, MinDistIntern, MinDistExtern, SeedingRadius, SeedCount, MaxAge, Prefab, MyMeshCollider, MinSlope, MaxSlope, MinHeight, MaxHeight);
    }

    // Draw the plant on the map if conditions met, else kill it
    // Returns true if the plant was drawn (conditions met)
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
        GameObject.transform.position = new Vector3(Position.x, Height, Position.y);
    }

    private (float height, Vector3 normal) RaycastAtPosition(Vector2 position)
    {
        const int maxHeight = 3000; // Height "Zugspitze" in m
        var height = 0f;
        var normal = Vector3.zero;
        var ray = new Ray(new Vector3(position.x, maxHeight, position.y), Vector3.down);

        if (!MyMeshCollider.Raycast(ray, out var hit, maxHeight)) return (height, normal);
        height = hit.point.y;
        normal = hit.normal;
        
        // Convert normal to angle in degrees to XZ plane (0° = flat, 90° = vertical) TODO: remove
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

    // Age the plant and check if it dies
    public bool Grow(double deltaT = 0.5)
    { 
        Age += deltaT;
        if (Age > MaxAge)
        {
            Die();
            return true;
        }
        if (SlopePercent < MinSlope || SlopePercent > MaxSlope)
        {
            Die();
            return true;
        }
        if (Height < MinHeight || Height > MaxHeight)
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
                MaxAge, Prefab, MyMeshCollider, MinSlope, MaxSlope, MinHeight, MaxHeight);
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
    protected internal int Collides(Plant other, float viabilityModifier, float collidingViabilityModifier)
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
        if (Viability*viabilityModifier > other.Viability*collidingViabilityModifier)
        {
            other.Die();
            return 2;
        }

        Die();
        return 1;
    }
}