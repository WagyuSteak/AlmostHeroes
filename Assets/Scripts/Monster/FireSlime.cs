using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FireSlime : EnemyBase
{
    public int Health; // 현재 적의 체력
    public int MaxHealth; // 적의 최대 체력
    public GameObject vfxPrefab; // Reference to the VFX prefab to be spawned

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

                // Spawn VFX at the affected position
                SpawnVFXAtGridPosition(targetPosition);
            }
        }
    }

    private void SpawnVFXAtGridPosition(Vector2Int gridPosition)
    {
        // Get the world position from the grid position
        Vector3 worldPosition = gridManager.GetWorldPositionFromGrid(gridPosition);
        worldPosition.y = 0; // Set Z-axis to 0 for 2D VFX positioning

        if (vfxPrefab != null)
        {
            // Instantiate the VFX prefab at the specified position
            Instantiate(vfxPrefab, worldPosition, Quaternion.identity);
        }
    }
}
