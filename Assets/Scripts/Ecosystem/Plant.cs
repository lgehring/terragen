using System.Collections.Generic;
using UnityEngine;

namespace Ecosystem
{
    public class Plant : MonoBehaviour
    {
        public PlantData data;
        public double age;
        public double viability;
        public double fullScale;
        public double maxAge;

        public void Reset()
        {
            age = 0;
            viability = 0;
        }

        public void Initialize(Vector3 pos)
        {
            gameObject.transform.position = pos;
        }

        public bool Grow(double deltaT = 0.5)
        {
            age += deltaT;
            if (age > maxAge) return true;
            UpdateViability();
            return false;
        }

        private void UpdateViability()
        {
            var normalizedAge = age / maxAge;
            if (normalizedAge < 0.5)
                viability = normalizedAge;
            else
                viability = 1 - normalizedAge;
        }

        public bool Fight(Plant other)
        {
            // true if this plant wins, false if other plant wins
            return !(viability < other.viability);
        }

        // Returns positions in matrix space
        public IEnumerable<Vector2Int> GenerateSeedPositions(Vector2Int pos)
        {
            var seedPositions = new List<Vector2Int>();
            for (var i = 0; i < data.seedCount; i++)
            {
                // Create a new plant with identical properties at a random position within the seeding radius
                var seedPosition = new Vector2Int(
                    Random.Range(pos.x - data.seedingRadius, pos.x + data.seedingRadius),
                    Random.Range(pos.y - data.seedingRadius, pos.y + data.seedingRadius)
                );
                // If seed position is within the size radius (would get killed), ignore it
                if (Vector2.Distance(seedPosition, pos) < data.sizeRadius) continue;

                seedPositions.Add(seedPosition);
            }

            return seedPositions;
        }
    }
}