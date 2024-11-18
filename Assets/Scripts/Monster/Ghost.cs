using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ghost : EnemyBase
{
    // Override detection range and attack range settings

    protected override void Start()
    {
        // Call the base class Start() to ensure common initialization logic is run
        base.Start();

        // Set unique values for Ghost
        HP = 3;
        MaxMoveCount = 1;
        AttackDamage = 0;
        DetectRange = 7; // 7x7 area
        AttackRange = 3; // Attack range of 3 tiles away (no diagonal)
        moveSpeed = 2f;

        FindObjectOfType<TurnManager>().RegisterEnemy(this);
    }

    // Override target selection priority logic
    protected override CharacterBase SelectTargetByPriority(List<CharacterBase> detectedTargets)
    {
        List<CharacterBase> rangedTargets = detectedTargets.Where(t => t.IsRanged).ToList();
        List<CharacterBase> meleeTargets = detectedTargets.Where(t => !t.IsRanged).ToList();

        if (rangedTargets.Count > 0)
        {
            if (rangedTargets.Count == 1) return rangedTargets[0];
            return rangedTargets.OrderBy(t => Vector2Int.Distance(CurrentGridPosition, gridManager.GetGridPosition(t.transform.position))).First();
        }

        return meleeTargets.OrderBy(t => Vector2Int.Distance(CurrentGridPosition, gridManager.GetGridPosition(t.transform.position))).First();
    }

    // Custom logic for determining movement targets based on specific conditions
    protected override Vector2Int GetAdjacentPositionNearTarget(Vector2Int targetGridPosition)
    {
        // Define the four cardinal directions for movement
        Vector2Int[] directions = {
            new Vector2Int(3, 0),  // 3 tiles right
            new Vector2Int(-3, 0), // 3 tiles left
            new Vector2Int(0, 3),  // 3 tiles up
            new Vector2Int(0, -3)  // 3 tiles down
        };

        // Calculate potential positions
        List<Vector2Int> potentialPositions = new List<Vector2Int>();
        foreach (Vector2Int direction in directions)
        {
            Vector2Int newPosition = targetGridPosition + direction;
            if (gridManager.IsWithinGridBounds(newPosition))
            {
                potentialPositions.Add(newPosition);
                Debug.Log($"Calculated ghost potential position set to: {newPosition}");
            }
        }

        // Sort positions by x value in descending order (larger x comes first)
        potentialPositions = potentialPositions.OrderByDescending(pos => pos.x).ToList();

        // Check each position for the presence of other objects and return the first valid one
        foreach (Vector2Int position in potentialPositions)
        {
            if (!gridManager.IsCharacterPosition(position) && !gridManager.IsEnemyPosition(position))
            {
                return position; // Return the first valid position
            }
        }

        // Fallback: return the closest available valid position
        return potentialPositions.OrderBy(pos => Vector2Int.Distance(CurrentGridPosition, pos)).FirstOrDefault();
    }
}
