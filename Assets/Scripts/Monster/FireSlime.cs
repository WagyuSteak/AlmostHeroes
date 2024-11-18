using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSlime : EnemyBase
{
    public int Health; // 현재 적의 체력
    public int MaxHealth; // 적의 최대 체력
    // Start is called before the first frame update
    protected override void Start()
    {
        // Call the base class Start() to ensure common initialization logic is run
        base.Start();

        HP = 3; // Set health points
        MaxMoveCount = 2; // Set number of tiles the enemy can move
        AttackDamage = 0; // You can adjust attack damage here if needed
        moveSpeed = 3f;
        DetectRange = 2;
        AttackRange = 1;

        FindObjectOfType<TurnManager>().RegisterEnemy(this);
    }
<<<<<<< Updated upstream
=======

    public override void TakeDamage(int damageAmount)
    {
        // Apply damage to the FireSlime
        HP -= damageAmount;

        // Check if the FireSlime would survive the damage
        if (HP > 0)
        {
            // If still alive, deal damage to surrounding 3x3 area (excluding itself)
            DealAreaDamage();
        }
        else
        {
            // Ensure HP doesn't go below zero
            HP = 0;

            // Handle death if needed
            Die();
        }
    }

    private void DealAreaDamage()
    {
        // Define the 3x3 area around the FireSlime's current position
        Vector2Int[] directions = {
        new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
        new Vector2Int(-1, 0),                /* Self */ new Vector2Int(1, 0),
        new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1)
    };

        List<Vector2Int> affectedCells = new List<Vector2Int>();

        foreach (Vector2Int direction in directions)
        {
            Vector2Int targetPosition = CurrentGridPosition + direction;

            // Check if the position is within grid bounds
            if (gridManager.IsWithinGridBounds(targetPosition))
            {
                // Add the position to the list of affected cells
                affectedCells.Add(targetPosition);

                // Check if there is a character at this position
                CharacterBase character = gridManager.GetCharacterAtPosition(targetPosition);
                if (character != null)
                {
                    character.TakeDamage(1); // Adjust damage value as needed
                }
            }
        }

        // Start the coroutine to visually highlight the affected cells
        StartCoroutine(HighlightCellsTemporarily(affectedCells));
    }

    private IEnumerator HighlightCellsTemporarily(List<Vector2Int> cells)
    {
        // Set the cells to green
        foreach (Vector2Int cell in cells)
        {
            if (gridManager.gridCells.ContainsKey(cell))
            {
                Renderer cellRenderer = gridManager.gridCells[cell].GetComponent<Renderer>();
                if (cellRenderer != null)
                {
                    cellRenderer.material.color = Color.green;
                }
            }
        }

        // Wait for 0.5 seconds
        yield return new WaitForSeconds(0.5f);

        // Reset the cells back to their original color (assuming white here)
        foreach (Vector2Int cell in cells)
        {
            if (gridManager.gridCells.ContainsKey(cell))
            {
                Renderer cellRenderer = gridManager.gridCells[cell].GetComponent<Renderer>();
                if (cellRenderer != null)
                {
                    cellRenderer.material.color = Color.white; // Reset to default color (adjust as needed)
                }
            }
        }
    }
>>>>>>> Stashed changes
}
