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
    public int HP = 3; // Health points
    public int MoveCount = 2; // Number of tiles enemy can move
    public int AttackDamage = 1; // Damage dealt to player
    public int DetectRange = 2; // Default detection range for detecting targets
    public int AttackRange = 1; // Range for attacking (adjacent tiles)
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    public Vector2Int CurrentGridPosition;
    protected GridManager gridManager;
    public CharacterBase Target; // The current target

    private List<Vector2Int> previousHighlightedCells = new List<Vector2Int>(); // Track previously highlighted cells
    private List<Vector2Int> permanentlyHighlightedCells = new List<Vector2Int>(); // Track cells highlighted permanently
    private Vector2Int calculatedIntermediatePosition; // New variable for storing intermediate position

    Animator animator;

    protected virtual void Start()
    {
        // Obtain reference to GridManager
        gridManager = FindObjectOfType<GridManager>();

        // Initialize the enemy's position on the grid
        CurrentGridPosition = gridManager.GetGridPosition(transform.position);
        gridManager.AddEnemyPosition(CurrentGridPosition, gameObject);

        animator = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        // Check if the 'G' key is pressed
        if (Input.GetKeyDown(KeyCode.G))
        {
            // Logic to execute when 'G' key is pressed
            OnGKeyPressed();
        }
    }

    private void OnGKeyPressed()
    {


        Debug.Log("G key was pressed!");
    }

        public void SetCalculatedIntermediatePosition(Vector2Int position)
    {
        calculatedIntermediatePosition = position;
        Debug.Log($"Calculated intermediate position set to: {calculatedIntermediatePosition}");
    }

    public Vector2Int GetCalculatedIntermediatePosition()
    {
        return calculatedIntermediatePosition;
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
            if (gridManager.IsWithinGridBounds(adjacentPosition) &&
                !gridManager.IsCharacterPosition(adjacentPosition) &&
                !gridManager.IsEnemyPosition(adjacentPosition))
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


        // Step 5: If no valid adjacent positions are found, select a new target
        Target = SelectNewTarget(); // Assuming SelectNewTarget() is a method to select a new target
        return gridManager.GetGridPosition(Target.transform.position); // Return new target's position
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
        if (Target != null) return Target; // If a target has already been selected, keep it

        List<CharacterBase> detectedTargets = DetectTargetsInRange();
        if (detectedTargets.Count == 0)
        {
            // If no targets are detected, select the nearest player character on the grid
            Target = SelectNearestCharacter(gridManager.characters);
        }
        else if (detectedTargets.Count == 1)
        {
            // If one target is detected, select it as the target
            Target = detectedTargets[0];
        }
        else
        {
            // If multiple targets are detected, apply target priority rules
            Target = SelectTargetByPriority(detectedTargets);
        }

        // Highlight the target's position in black if a target is selected
        if (Target != null)
        {
            Debug.Log($"Target selected: {Target.name}");
            Vector2Int actualTargetPosition = gridManager.GetGridPosition(Target.transform.position);
            if (gridManager.gridCells.ContainsKey(actualTargetPosition))
            {
                Renderer cellRenderer = gridManager.gridCells[actualTargetPosition].GetComponent<Renderer>();
                if (cellRenderer != null)
                {
                    cellRenderer.material.color = Color.white; // Highlight the target's exact position
                }

                // Add the target's position to the list of permanently highlighted cells
                if (!permanentlyHighlightedCells.Contains(actualTargetPosition))
                {
                    permanentlyHighlightedCells.Add(actualTargetPosition);
                }
            }
        }
        else
        {
            Debug.Log("No target selected.");
        }

        return Target;
    }
    protected CharacterBase SelectNearestCharacter(List<CharacterBase> characters)
    {
        CharacterBase nearestCharacter = null;
        float shortestDistance = float.MaxValue;

        foreach (CharacterBase character in characters)
        {
            float distance = Vector2Int.Distance(CurrentGridPosition, gridManager.GetGridPosition(character.transform.position));
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestCharacter = character;
            }
        }

        return nearestCharacter;
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
        MoveCount = 2;

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int currentPosition = start;

        while (currentPosition != targetGridPosition)
        {
            Vector2Int direction = targetGridPosition - currentPosition;

            // Restrict movement to cardinal directions
            Vector2Int step = new Vector2Int(
                direction.x != 0 ? Mathf.Clamp(direction.x, -1, 1) : 0,
                direction.x == 0 ? Mathf.Clamp(direction.y, -1, 1) : 0
            );

            Vector2Int nextPosition = currentPosition + step;

            // Break if the next cell is blocked by an object or another enemy
            if (gridManager.IsCharacterPosition(nextPosition) ||
                gridManager.IsEnemyPosition(nextPosition) ||
                gridManager.IsSylphPosition(nextPosition))
            {
                break;
            }

            path.Add(nextPosition);
            currentPosition = nextPosition;
        }

        return path;
    }

    public Vector2Int GetIntermediatePoint(List<Vector2Int> path)
    {
        ClearHighlightedCells();

        if (path.Count == 0) return CurrentGridPosition; // No valid path

        int maxSteps = Mathf.Min(MoveCount, path.Count);
        return path[maxSteps - 1]; // Get the point after MoveCount steps or the end of the path
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

    public Vector2Int FindValidIntermediatePosition(List<EnemyBase> enemies, List<Vector2Int> path, Vector2Int originalIntermediate)
    {
        Vector2Int newIntermediate = originalIntermediate;

        foreach (EnemyBase enemy in enemies)
        {
            if (enemy == this) continue; // Skip self

            Vector2Int otherIntermediate = enemy.GetFinalDestination();
            if (newIntermediate == otherIntermediate || gridManager.IsCharacterPosition(newIntermediate) || gridManager.IsEnemyPosition(newIntermediate))
            {
                // Find a new valid intermediate point
                for (int i = path.Count - 1; i >= 0; i--)
                {
                    Vector2Int potentialPosition = path[i];
                    if (!gridManager.IsCharacterPosition(potentialPosition) &&
                        !gridManager.IsEnemyPosition(potentialPosition) &&
                        !enemies.Any(e => e != this && e.GetFinalDestination() == potentialPosition))
                    {
                        newIntermediate = potentialPosition;
                        break;
                    }
                }
            }
        }

        return newIntermediate;
    }

    public Vector2Int GetFinalDestination()
    {
        // Return the previously computed final destination or calculate it as needed
        return CurrentGridPosition; // This should be replaced with logic to fetch actual destination
    }

    public Vector2Int CalculateIntermediatePosition(List<EnemyBase> enemies)
    {
        // Check if any characters are in attack range before moving
        if (IsCharacterInAttackRange())
        {
            Debug.Log("Character detected in attack range, preventing movement.");
            MoveCount = 0; // Prevent any movement
            return CurrentGridPosition; // Return current position as the intermediate position
        }

        // Continue with normal path calculation if no characters are in range
        Vector2Int targetGridPosition = GetAdjacentPositionNearTarget(gridManager.GetGridPosition(Target.transform.position));
        List<Vector2Int> path = CalculatePathToTarget(CurrentGridPosition, targetGridPosition);
        Vector2Int intermediatePoint = GetIntermediatePoint(path);

        // Handle conflicts with other enemies
        Vector2Int finalIntermediatePoint = FindValidIntermediatePosition(enemies, path, intermediatePoint);

        // Highlight the path to the final intermediate point
        HighlightPath(path, finalIntermediatePoint);

        // Return the calculated intermediate position
        return finalIntermediatePoint;
    }

    public IEnumerator MoveToCalculatedPosition(Vector2Int targetPosition)
    {
        // Ensure you have an Animator component attached to your GameObject
        Animator animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("No Animator component found on this GameObject. Movement animation will not be triggered.");
        }

        // Trigger move animation before starting movement
        if (MoveCount > 0 && animator != null)
        {
            animator.SetBool("isMoving", true); // Use a bool parameter for movement
            yield return null; // Allow animator state to update before moving
        }

        Vector2Int currentGridPosition = CurrentGridPosition;
        List<Vector2Int> path = CalculatePathToTarget(CurrentGridPosition, targetPosition); // Get path to the target

        foreach (Vector2Int nextGrid in path)
        {
            // If MoveCount is 0, stop moving immediately
            if (MoveCount <= 0)
            {
                break;
            }

            // Check for characters in attack range during movement
            if (IsCharacterInAttackRange())
            {
                Debug.Log("Character detected in attack range during movement, stopping.");
                MoveCount = 0; // Stop movement
                if (animator != null)
                {
                    animator.SetBool("isMoving", false); // Reset animation to idle
                }
                yield break; // Exit the coroutine
            }

            // Check if there's an obstacle (character, enemy, or any other relevant object)
            if (gridManager.IsCharacterPosition(nextGrid) || gridManager.IsEnemyPosition(nextGrid))
            {
                Debug.Log($"Movement stopped due to an obstacle at {nextGrid}");
                MoveCount = 0; // Stop further movement
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

            // Smoothly rotate towards the target rotation
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed); // Adjust rotation speed as needed
                yield return null;
            }

            Vector3 targetWorldPosition = gridManager.GetWorldPositionFromGrid(nextGrid);
            targetWorldPosition.y = transform.position.y; // Keep the y-coordinate constant

            // Move one grid cell at a time
            while (Vector3.Distance(transform.position, targetWorldPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, Time.deltaTime * moveSpeed);
                yield return null;
            }

            // Snap to target position to ensure perfect alignment
            transform.position = targetWorldPosition;

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

        // Stop move animation when done moving
        if (animator != null)
        {
            animator.SetBool("isMoving", false); // Reset movement animation state
        }
    }


    // Method to check if any characters are within attack range
    private bool IsCharacterInAttackRange()
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


    public void AttackIfInRange()
    {
        if (Target == null) return;

        Vector2Int targetPosition = gridManager.GetGridPosition(Target.transform.position);
        float distanceToTarget = Vector2Int.Distance(CurrentGridPosition, targetPosition);

        if (distanceToTarget <= AttackRange)
        {
            // Calculate direction to face the target
            Vector3 directionToTarget = gridManager.GetWorldPositionFromGrid(targetPosition) - transform.position;
            directionToTarget.y = 0; // Keep rotation on the horizontal plane
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // Instantly face the target
            transform.rotation = targetRotation;

            // Play attack animation
            if (animator != null)
            {
                animator.SetBool("isAttacking", true);
            }

            // Apply damage to the target
            Target.TakeDamage(AttackDamage);

            // Optionally reset attack animation (if needed)
            if (animator != null)
            {
                StartCoroutine(ResetAttackAnimation());
            }
        }
    }

    private IEnumerator ResetAttackAnimation()
    {
        // Wait for a short duration to simulate attack animation duration (adjust as necessary)
        yield return new WaitForSeconds(0.5f); // Adjust this time based on your animation length

        // Reset attack animation state
        animator.SetBool("isAttacking", false);
    }

    // 데미지를 받는 메서드
    public void TakeDamage(int damageAmount)
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
