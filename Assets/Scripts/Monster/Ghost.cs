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
        DetectRange = 3; // 7x7 area
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
        // Possible moves for adjacency (up, down, left, right)
        Vector2Int[] directions = {
        new Vector2Int(0, 3),  // Up
        new Vector2Int(0, -3), // Down
        new Vector2Int(3, 0),  // Right
        new Vector2Int(-3, 0)  // Left
    };

        // Step 1: Collect all valid adjacent positions
        List<Vector2Int> adjacentPositions = new List<Vector2Int>();
        foreach (Vector2Int direction in directions)
        {
            Vector2Int adjacentPosition = targetGridPosition + direction;
            // Check if the position is valid (within bounds and unoccupied)
            if (gridManager.IsWithinGridBounds(adjacentPosition) &&
                !gridManager.IsCharacterPosition(adjacentPosition) &&
                !gridManager.IsEnemyPosition(adjacentPosition) &&
                !gridManager.IsSylphPosition(adjacentPosition) &&
                !gridManager.IsObstaclePosition(adjacentPosition))
            {
                adjacentPositions.Add(adjacentPosition);
            }
        }

        // Step 2: Sort by distance to ensure preference for closer cells
        adjacentPositions = adjacentPositions.OrderBy(pos => Vector2Int.Distance(CurrentGridPosition, pos)).ToList();

        // Step 3: Return the first valid unoccupied position
        if (adjacentPositions.Count > 0)
        {
            return adjacentPositions[0];
        }

        // No valid positions available, select a new target if needed or remain in place
        return CurrentGridPosition; // Stay in current position if no valid moves are found
    }
}
