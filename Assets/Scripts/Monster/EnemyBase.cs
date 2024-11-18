using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Collections.AllocatorManager;

public enum EnemyType
{
    SpecialEnt,
    Ent,
    FireSlime,
    Slime,
    Ghost
}

public abstract class EnemyBase : MonoBehaviour
{
    // Core attributes
    public int HP; // Health points
    public int MoveCount; // Number of tiles enemy can move
    public int MaxMoveCount { get; protected set; } // Allow derived classes to set it
    public int AttackDamage;// Damage dealt to player
    public int DetectRange; // Default detection range for detecting targets
    public int AttackRange; // Range for attacking (adjacent tiles)
    public float moveSpeed;
    public float rotationSpeed = 20f;

    public Vector2Int CurrentGridPosition;
    protected GridManager gridManager;
    public CharacterBase Target; // The current target


    private bool isDead = false;
    public List<Vector2Int> previousHighlightedCells = new List<Vector2Int>(); // Track previously highlighted cells
    private Vector2Int calculatedIntermediatePosition; // New variable for storing intermediate position
    private List<Vector2Int> storedPath;

    public Animator animator;

    protected virtual void Start()
    {
        // Obtain reference to GridManager
        gridManager = FindObjectOfType<GridManager>();

        // Initialize the enemy's position on the grid
        CurrentGridPosition = gridManager.GetGridPosition(transform.position);
        gridManager.AddEnemyPosition(CurrentGridPosition, gameObject);

        animator = GetComponent<Animator>();

        // Initialize MaxMoveCount to the same value as MoveCount at the start
        MaxMoveCount = MoveCount;

        HighlightDetectRange();
    }

    public void HighlightDetectRange()
    {
        // Iterate over all cells within the detection range
        for (int x = -DetectRange; x <= DetectRange; x++)
        {
            for (int y = -DetectRange; y <= DetectRange; y++)
            {
                Vector2Int checkPosition = new Vector2Int(CurrentGridPosition.x + x, CurrentGridPosition.y + y);

                // Check if the position is within grid bounds and not occupied by obstacles
                if (gridManager.IsWithinGridBounds(checkPosition) &&
                    !gridManager.IsObstaclePosition(checkPosition))
                {
                    // Check if the cell exists in the grid
                    if (gridManager.gridCells.ContainsKey(checkPosition))
                    {
                        Renderer cellRenderer = gridManager.gridCells[checkPosition].GetComponent<Renderer>();
                        if (cellRenderer != null)
                        {
                            cellRenderer.material.color = Color.blue; // Set cell color to blue
                        }
                    }
                }
            }
        }
    }

    public void SetCalculatedIntermediatePosition(Vector2Int position)
    {
        calculatedIntermediatePosition = position;
    }

    protected virtual Vector2Int GetAdjacentPositionNearTarget(Vector2Int targetGridPosition)
    {
        // Possible moves for adjacency (up, down, left, right)
        Vector2Int[] directions = {
        new Vector2Int(0, 1),  // Up
        new Vector2Int(0, -1), // Down
        new Vector2Int(1, 0),  // Right
        new Vector2Int(-1, 0)  // Left
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
        Target = SelectNewTarget(); // Assuming SelectNewTarget() selects a new target if needed
        return CurrentGridPosition; // Stay in current position if no valid moves are found
    }

    private CharacterBase SelectNewTarget()
    {
        // Filter out the previously selected target
        List<CharacterBase> availableTargets = gridManager.characters.Where(t => t != Target).ToList();

        // If no other targets are available, return null
        if (availableTargets.Count == 0)
        {
            return null;
        }

        // Select the nearest target among the available targets
        CharacterBase newTarget = SelectNearestCharacter(availableTargets);

        // Log the new target selection for debugging
        if (newTarget != null)
        {
            Debug.Log($"New target selected: {newTarget.name}");
        }

        return newTarget;
    }

    public CharacterBase SelectTarget()
    {
        // Detect targets within range
        List<CharacterBase> detectedTargets = DetectTargetsInRange();

        // If no targets are detected, select the target that is nearest to self
        if (detectedTargets.Count == 0)
        {
            Target = SelectNearestCharacter(gridManager.characters); // Select the nearest character if no one is in range
            Debug.Log($"{name} selected the nearest target: {Target?.name}");
            return Target;
        }

        // If only one target is detected, set it as the Target
        if (detectedTargets.Count == 1)
        {
            // Check if it's a new target or keep the existing one if it's already the same
            if (Target == null || Target != detectedTargets[0])
            {
                Target = detectedTargets[0];
                Debug.Log($"{name} detected one target and selected: {Target?.name}");
            }
            return Target;
        }

        if (detectedTargets.Count == 2)
        {
            // If multiple targets are detected, apply target priority rules
            CharacterBase newTarget = SelectTargetByPriority(detectedTargets);
            
            // Update Target if it is different or if we have a new higher-priority target
            if (Target == null || Target != newTarget)
            {
                Target = newTarget;
                Debug.Log($"{name} selected a new target: {Target?.name}");
            }
            // Update Target if it is different or if we have a new higher-priority target
        }

        return Target;
    }


    protected CharacterBase SelectNearestCharacter(List<CharacterBase> characters)
    {
        CharacterBase nearestCharacter = null;
        int shortestPathLength = int.MaxValue; // Use an integer for path length comparison

        foreach (CharacterBase character in characters)
        {
            Vector2Int targetPosition = gridManager.GetGridPosition(character.transform.position);
            Vector2Int validTargetPosition = GetValidAdjacentPosition(targetPosition); // Get a valid adjacent position
            List<Vector2Int> path = FindPath(CurrentGridPosition, validTargetPosition);

            if (path != null && path.Count < shortestPathLength)
            {
                shortestPathLength = path.Count;
                nearestCharacter = character;
            }
        }
        return nearestCharacter;
    }

    protected Vector2Int GetValidAdjacentPosition(Vector2Int targetPosition)
    {
        Debug.Log("GetValidAdjacentPosition called");

        // Possible moves for adjacency (up, down, left, right)
        Vector2Int[] directions = {
        new Vector2Int(0, 1),  // Up
        new Vector2Int(0, -1), // Down
        new Vector2Int(1, 0),  // Right
        new Vector2Int(-1, 0)  // Left
    };

        // Step 1: Collect all valid adjacent positions
        List<Vector2Int> adjacentPositions = new List<Vector2Int>();
        foreach (Vector2Int direction in directions)
        {
            Vector2Int adjacentPosition = targetPosition + direction;
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

        return CurrentGridPosition; // Stay in current position if no valid moves are found
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
    {
        // Use a queue for BFS
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int?> cameFrom = new Dictionary<Vector2Int, Vector2Int?>();
        queue.Enqueue(start);
        cameFrom[start] = null;

        Vector2Int[] directions = {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(0, -1),  // Down
        new Vector2Int(1, 0),   // Right
        new Vector2Int(-1, 0)   // Left
    };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // If we reach the target position, reconstruct the path
            if (current == target)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                Vector2Int? step = current;
                while (step.HasValue)
                {
                    path.Add(step.Value);
                    step = cameFrom[step.Value];
                }
                path.Reverse(); // Reverse to get the path from start to target
                return path;
            }

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                // Check if the next position is valid (not blocked and within grid bounds)
                if (gridManager.IsWithinGridBounds(next) &&
                    !cameFrom.ContainsKey(next) && // Check that we haven't already processed this position
                    !gridManager.IsCharacterPosition(next) &&
                    !gridManager.IsEnemyPosition(next) &&
                    !gridManager.IsObstaclePosition(next) &&
                    !gridManager.IsSylphPosition(next))
                {
                    queue.Enqueue(next);
                    cameFrom[next] = current;
                }
                else
                {
                }
            }
        }

        Debug.Log("No valid path found to the target.");

        // Return an empty path if no valid path is found
        return new List<Vector2Int>();
    }

    protected virtual CharacterBase SelectTargetByPriority(List<CharacterBase> detectedTargets)
    {
        // Priority logic: Prefer ranged targets first, then closest by distance
        List<CharacterBase> rangedTargets = detectedTargets.Where(t => t.IsRanged).ToList();
        List<CharacterBase> meleeTargets = detectedTargets.Where(t => !t.IsRanged).ToList();

        if (rangedTargets.Count > 0)
        {
            // Prefer ranged targets
            if (rangedTargets.Count == 1) return rangedTargets[0];

            // If multiple ranged targets, select the closest
            return rangedTargets.OrderBy(t => Vector2Int.Distance(CurrentGridPosition, gridManager.GetGridPosition(t.transform.position))).First();
        }

        // If no ranged targets, select the closest melee target
        return meleeTargets.OrderBy(t => Vector2Int.Distance(CurrentGridPosition, gridManager.GetGridPosition(t.transform.position))).First();
    }

    protected List<CharacterBase> DetectTargetsInRange()
    {
        List<CharacterBase> detectedTargets = new List<CharacterBase>();

        for (int x = -DetectRange; x <= DetectRange; x++)
        {
            for (int y = -DetectRange; y <= DetectRange; y++)
            {
                Vector2Int checkPosition = new Vector2Int(CurrentGridPosition.x + x, CurrentGridPosition.y + y);
                if (gridManager.IsWithinGridBounds(checkPosition) && gridManager.IsCharacterPosition(checkPosition))
                {
                    detectedTargets.Add(gridManager.GetCharacterAtPosition(checkPosition));
                }
            }
        }

        return detectedTargets;
    }

    public List<Vector2Int> CalculatePathToTarget(Vector2Int start, Vector2Int targetGridPosition)
    {
        MoveCount = MaxMoveCount; // Reset MoveCount to MaxMoveCount

        // BFS setup
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int?> cameFrom = new Dictionary<Vector2Int, Vector2Int?>();
        queue.Enqueue(start);
        cameFrom[start] = null;

        Vector2Int[] directions = {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(0, -1),  // Down
        new Vector2Int(1, 0),   // Right
        new Vector2Int(-1, 0)   // Left
    };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // If we reach the target position, reconstruct the path
            if (current == targetGridPosition)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                Vector2Int? step = current;
                while (step.HasValue)
                {
                    path.Add(step.Value);
                    step = cameFrom[step.Value];
                }
                path.Reverse(); // Reverse to get the path from start to target
                return path;
            }

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                // Check if the next position is valid (not blocked and within grid bounds)
                if (!cameFrom.ContainsKey(next) && gridManager.IsWithinGridBounds(next) &&
                    !gridManager.IsCharacterPosition(next) &&
                    !gridManager.IsEnemyPosition(next) &&
                    !gridManager.IsObstaclePosition(next) &&
                    !gridManager.IsSylphPosition(next))
                {
                    queue.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }

        // Return an empty path if no valid path is found
        return new List<Vector2Int>();
    }

    public Vector2Int GetIntermediatePoint(List<Vector2Int> path)
    {
        if (path.Count == 0) return CurrentGridPosition; // No valid path

        int maxSteps = Mathf.Min(MoveCount, path.Count);
        return path[maxSteps -1]; // Get the point after MoveCount steps or the end of the path
    }

    public void HighlightPath(List<Vector2Int> path, Vector2Int middlePoint)
    {
        foreach (Vector2Int position in path)
        {
            if (gridManager.gridCells.ContainsKey(position))
            {
                Renderer cellRenderer = gridManager.gridCells[position].GetComponent<Renderer>();
                if (cellRenderer != null)
                {
                    cellRenderer.material.color = Color.red;
                    previousHighlightedCells.Add(position);
                }
            }

            if (position == middlePoint)
            {
                break; // Stop highlighting after the middle point
            }
        }
    }

    private void HighlightAlternativePath(List<Vector2Int> path, Vector2Int intermediatePoint)
    {
        foreach (Vector2Int position in path)
        {
            if (gridManager.gridCells.ContainsKey(position))
            {
                Renderer cellRenderer = gridManager.gridCells[position].GetComponent<Renderer>();
                if (cellRenderer != null)
                {
                    cellRenderer.material.color = Color.green; // Set a different color for alternative path
                    previousHighlightedCells.Add(position);
                }
            }

            if (position == intermediatePoint)
            {
                break; // Stop highlighting after the intermediate point
            }
        }
    }

    public Vector2Int CalculateIntermediatePosition(List<EnemyBase> enemies)
    {
        if (IsCharacterInAttackRange())
        {
            Debug.Log($"{name} detected a character in attack range, preventing movement.");
            return CurrentGridPosition; // Return current position as the intermediate position
        }

        // Initial path calculation
        Vector2Int targetGridPosition = GetAdjacentPositionNearTarget(gridManager.GetGridPosition(Target.transform.position));
        List<Vector2Int> path = CalculatePathToTarget(CurrentGridPosition, targetGridPosition);

        // Ensure to skip own current position
        if (path.Count > 0 && path[0] == CurrentGridPosition)
        {
            path.RemoveAt(0); // Remove the first element if it's the current position
        }

        // Calculate the initial intermediate point
        Vector2Int intermediatePoint = GetIntermediatePoint(path);

        // Check if intermediatePoint is occupied by another enemy
        bool pointOccupied = enemies.Any(e => e != this && e.GetCalculatedIntermediatePosition() == intermediatePoint);

        if (pointOccupied)
        {
            // Recalculate path to avoid occupied points
            List<Vector2Int> alternativePath = FindAlternativePath(targetGridPosition, enemies);

            // Ensure to skip own current position
            if (alternativePath.Count > 0 && alternativePath[0] == CurrentGridPosition)
            {
                alternativePath.RemoveAt(0); // Remove the first element if it's the current position
            }

            // If a valid alternative path is found, use it
            if (alternativePath.Count > 0)
            {
                intermediatePoint = GetIntermediatePoint(alternativePath);
                HighlightAlternativePath(alternativePath, intermediatePoint); // Custom highlight for alternative path
                Debug.Log("AlternativePath IntermediatePoint");

                return intermediatePoint;
            }
            storedPath = CalculatePathToTarget(CurrentGridPosition, intermediatePoint);
        }

        // Highlight the path to the final intermediate point
        HighlightPath(path, intermediatePoint);
        calculatedIntermediatePosition = intermediatePoint;
        storedPath = CalculatePathToTarget(CurrentGridPosition, intermediatePoint);
        Debug.Log("OriginalPath IntermediatePoint");

        // Return the calculated intermediate position
        return intermediatePoint;
    }


    private List<Vector2Int> FindAlternativePath(Vector2Int targetGridPosition, List<EnemyBase> enemies)
{
    // Use a modified pathfinding logic to exclude positions occupied by other enemies
    Queue<Vector2Int> queue = new Queue<Vector2Int>();
    Dictionary<Vector2Int, Vector2Int?> cameFrom = new Dictionary<Vector2Int, Vector2Int?>();
    queue.Enqueue(CurrentGridPosition);
    cameFrom[CurrentGridPosition] = null;

    Vector2Int[] directions = {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(0, -1),  // Down
        new Vector2Int(1, 0),   // Right
        new Vector2Int(-1, 0)   // Left
    };

    while (queue.Count > 0)
    {
        Vector2Int current = queue.Dequeue();

        // If we reach the target position, reconstruct the path
        if (current == targetGridPosition)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int? step = current;
            while (step.HasValue)
            {
                path.Add(step.Value);
                step = cameFrom[step.Value];
            }
            path.Reverse(); // Reverse to get the path from start to target
            return path;
        }

        foreach (Vector2Int direction in directions)
        {
            Vector2Int next = current + direction;

            // Check if the next position is valid (not blocked, within grid bounds, and not occupied by another enemy)
            if (gridManager.IsWithinGridBounds(next) &&
                !cameFrom.ContainsKey(next) && // Check that we haven't already processed this position
                !gridManager.IsCharacterPosition(next) &&
                !gridManager.IsEnemyPosition(next) &&
                !gridManager.IsObstaclePosition(next) &&
                !gridManager.IsSylphPosition(next) &&
                !enemies.Any(e => e != this && e.GetCalculatedIntermediatePosition() == next)) // Avoid occupied positions
            {
                queue.Enqueue(next);
                cameFrom[next] = current;
            }
        }
    }

    // Return an empty path if no valid path is found
    return new List<Vector2Int>();
}

    public Vector2Int GetCalculatedIntermediatePosition()
    {
        return calculatedIntermediatePosition;
    }

    public IEnumerator MoveToCalculatedPosition(Vector2Int intermediatePoint)
    {
        if (storedPath == null || storedPath.Count == 0)
        {
            Debug.Log("No valid path found to the intermediate point.");
            yield break; // Exit if no path is found
        }

        Vector2Int currentGridPosition = CurrentGridPosition;

        // Skip the first grid (current position) and start moving from the next grid
        if (storedPath.Count > 1)
        {
            storedPath.RemoveAt(0); // Remove the first element if it's the current position
        }
        
        foreach (Vector2Int nextGrid in storedPath)
        {
            // If MoveCount is 0, stop moving immediately
            if (MoveCount <= 0)
            {
                break;
            }

            // Trigger move animation before starting to move towards next grid
            if (animator != null)
            {
                animator.SetBool("isMoving", true); // Turn on the move animation
            }

            // Check if there's an obstacle (character, enemy, or any other relevant object)
            if (gridManager.IsCharacterPosition(nextGrid) || gridManager.IsEnemyPosition(nextGrid) || gridManager.IsSylphPosition(nextGrid) || gridManager.IsObstaclePosition(nextGrid))
            {
                Debug.Log($"Movement stopped due to an obstacle at {nextGrid}");
                if (animator != null)
                {
                    animator.SetBool("isMoving", false); // Reset animation to idle
                }
                yield break; // Exit the coroutine
            }

            // Calculate the direction vector to the next grid
            Vector3 direction = gridManager.GetWorldPositionFromGrid(nextGrid) - transform.position;
            direction.y = 0; // Keep the y-axis locked to prevent unwanted tilting

            // Calculate the target rotation to face the next grid
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Transition to idle animation while rotating
            if (animator != null)
            {
                animator.SetBool("isMoving", false); // Temporarily set to idle during rotation
            }

            // Smoothly rotate towards the target rotation
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed); // Adjust rotation speed as needed
                yield return null;
            }

            // Resume move animation after rotation is complete
            if (animator != null)
            {
                animator.SetBool("isMoving", true);
            }

            Vector3 targetWorldPosition = gridManager.GetWorldPositionFromGrid(nextGrid);
            targetWorldPosition.y = transform.position.y; // Keep the y-coordinate constant

            // Move one grid cell at a time
            while (Vector3.Distance(transform.position, targetWorldPosition) > 0.01f)
            {
                Vector3 moveDirection = (targetWorldPosition - transform.position).normalized;
                Vector3 step = new Vector3(
                    Mathf.Round(moveDirection.x),
                    0,
                    Mathf.Round(moveDirection.z)
                ); // Only move in cardinal directions

                transform.position = Vector3.MoveTowards(transform.position, transform.position + step, Time.deltaTime * moveSpeed);
                yield return null;
            }

            // Snap to target position to ensure perfect alignment
            transform.position = targetWorldPosition;

            // Turn off the move animation after each step
            if (animator != null)
            {
                animator.SetBool("isMoving", false);
            }

            // Optional: Add a brief pause between moves
            yield return new WaitForSeconds(0.25f); // Adjust the duration as desired (e.g., 0.1f seconds)

            // Update grid position after moving
            gridManager.RemoveEnemyPosition(currentGridPosition);
            CurrentGridPosition = nextGrid;
            gridManager.AddEnemyPosition(CurrentGridPosition, gameObject);
            currentGridPosition = nextGrid;

            // Reduce move count for each step
            MoveCount--;

            // Stop if no moves remain
            if (MoveCount <= 0)
            {
                break;
            }
        }

        // Ensure move animation is stopped at the end
        if (animator != null)
        {
            animator.SetBool("isMoving", false); // Reset movement animation state
        }
    }

    // Method to check if any characters are within attack range
    public virtual bool IsCharacterInAttackRange()
    {
        // Define the four cardinal directions for adjacency check
        Vector2Int[] directions = {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(0, -1),  // Down
        new Vector2Int(1, 0),   // Right
        new Vector2Int(-1, 0)   // Left
    };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int adjacentGridPosition = CurrentGridPosition + direction;

            // Check if the position is within the grid bounds
            if (gridManager.IsWithinGridBounds(adjacentGridPosition))
            {
                // Check if there's a character at this position
                CharacterBase character = gridManager.GetCharacterAtPosition(adjacentGridPosition);
                if (character != null)
                {
                    // Calculate distance to target
                    float distanceToTarget = Vector2Int.Distance(CurrentGridPosition, adjacentGridPosition);

                    // If within attack range, return true
                    if (distanceToTarget <= AttackRange)
                    {
                        return true;
                    }
                }
            }
        }

        return false; // No characters in attack range
    }

    public virtual IEnumerator HandleAttackIfInRange()
    {
        // Detect all characters within attack range
        List<CharacterBase> charactersInRange = GetCharactersInAttackRange();

        foreach (CharacterBase character in charactersInRange)
        {
            // Trigger attack animation and wait for completion
            if (animator != null)
            {
                animator.SetBool("isAttacking", true);
            }

            // Rotate to face the target before attacking
            Vector2Int targetPosition = gridManager.GetGridPosition(character.transform.position);
            Vector3 directionToTarget = gridManager.GetWorldPositionFromGrid(targetPosition) - transform.position;
            directionToTarget.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = targetRotation;

            // Apply damage
            character.TakeDamage(AttackDamage);

            // Wait for the attack animation to finish
            yield return new WaitForSeconds(0.6f); // Adjust based on the length of the attack animation


            // Reset attack animation
            if (animator != null)
            {
                animator.SetBool("isAttacking", false);
                Debug.Log("Animation Reset Compelete");
            }

            // Optional: Delay between attacks on multiple targets
            yield return new WaitForSeconds(0.4f); // Adjust for desired pacing between attacks
        }
    }

    public virtual List<CharacterBase> GetCharactersInAttackRange()
    {
        List<CharacterBase> charactersInRange = new List<CharacterBase>();
        Vector2Int[] directions = {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(0, -1),  // Down
        new Vector2Int(1, 0),   // Right
        new Vector2Int(-1, 0)   // Left
    };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int adjacentGridPosition = CurrentGridPosition + direction;
            if (gridManager.IsWithinGridBounds(adjacentGridPosition))
            {
                CharacterBase character = gridManager.GetCharacterAtPosition(adjacentGridPosition);
                if (character != null && Vector2Int.Distance(CurrentGridPosition, adjacentGridPosition) <= AttackRange)
                {
                    charactersInRange.Add(character);
                }
            }
        }

        return charactersInRange;
    }

    public virtual IEnumerator ResetAttackAnimation()
    {
        // Wait for a short duration to simulate attack animation duration (adjust as necessary)
        yield return new WaitForSeconds(0.5f); // Adjust this time based on your animation length

        // Reset attack animation state
        animator.SetBool("isAttacking", false);
    }


    public virtual void TakeDamage(int damageAmount)
    {
        HP -= damageAmount;
        if (HP <= 0)
        {
            HP = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        gridManager.RemoveEnemyPosition(CurrentGridPosition);
        Destroy(gameObject);
    }

    public void ClearHighlightedCells()
    {
        foreach (Vector2Int position in previousHighlightedCells)
        {
            if (gridManager.gridCells.ContainsKey(position))
            {
                Renderer cellRenderer = gridManager.gridCells[position].GetComponent<Renderer>();
                if (cellRenderer != null)
                {
                    cellRenderer.material.color = Color.white; // Reset to default color (adjust if needed)
                }
            }
        }

        previousHighlightedCells.Clear(); // Clear the list after resetting colors
    }
}
