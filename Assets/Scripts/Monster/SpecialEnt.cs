using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialEnt : EnemyBase
{
    public int Health; // 현재 적의 체력
    public int MaxHealth; // 적의 최대 체력

    Animator animator;
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        HP = 5; // Set health points
        MaxMoveCount = 3; // Set number of tiles the enemy can move
        AttackDamage = 0; // You can adjust attack damage here if needed
        moveSpeed = 3f;
        DetectRange = 2;
        AttackRange = 3;

        animator = GetComponent<Animator>();

        FindObjectOfType<TurnManager>().RegisterEnemy(this);
    }
<<<<<<< Updated upstream
}
=======

    public override List<CharacterBase> GetCharactersInAttackRange()
    {
        List<CharacterBase> charactersInRange = new List<CharacterBase>();

        // Iterate over a 5x5 grid centered around the character (excluding the character itself)
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue; // Skip the character's own position
                }

                Vector2Int checkPosition = CurrentGridPosition + new Vector2Int(x, y);
                if (gridManager.IsWithinGridBounds(checkPosition))
                {
                    CharacterBase character = gridManager.GetCharacterAtPosition(checkPosition);
                    if (character != null && Vector2Int.Distance(CurrentGridPosition, checkPosition) <= AttackRange)
                    {
                        charactersInRange.Add(character);
                    }
                }
            }
        }

        return charactersInRange;
    }

    public override IEnumerator HandleAttackIfInRange()
    {
        // Detect all characters within attack range
        List<CharacterBase> charactersInRange = GetCharactersInAttackRange();

        foreach (CharacterBase character in charactersInRange)
        {
            // Calculate the distance from the SpecialEnt to the target
            Vector2Int targetPosition = gridManager.GetGridPosition(character.transform.position);
            Vector2Int currentGridPosition = CurrentGridPosition;
            int distanceToTarget = Mathf.Abs(targetPosition.x - currentGridPosition.x) + Mathf.Abs(targetPosition.y - currentGridPosition.y);

            if (distanceToTarget == 1)
            {
                // Melee attack (adjacent)
                Debug.Log("SpecialEnt is performing a melee attack!");

                // Trigger melee attack animation and wait for completion
                if (animator != null)
                {
                    animator.SetBool("isAttackingMelee", true); // Assume you have a different animation trigger for melee
                }

                // Rotate to face the target before attacking
                Vector3 directionToTarget = gridManager.GetWorldPositionFromGrid(targetPosition) - transform.position;
                directionToTarget.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = targetRotation;

                // Apply damage (you can customize this as needed for melee damage)
                character.TakeDamage(AttackDamage);

                // Wait for the attack animation to finish
                yield return new WaitForSeconds(0.6f); // Adjust based on the length of the attack animation

                // Reset attack animation
                if (animator != null)
                {
                    animator.SetBool("isAttackingMelee", false);
                    Debug.Log("Melee attack animation complete.");
                }
            }
            else
            {
                // Ranged attack (non-adjacent but within attack range)
                Debug.Log("SpecialEnt is performing a ranged attack!");

                // Trigger ranged attack animation and wait for completion
                if (animator != null)
                {
                    animator.SetBool("isAttackingRanged", true); // Assume you have a different animation trigger for ranged
                }

                // Rotate to face the target before attacking
                Vector3 directionToTarget = gridManager.GetWorldPositionFromGrid(targetPosition) - transform.position;
                directionToTarget.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = targetRotation;

                // Apply ranged damage (customize this as needed)
                character.TakeDamage(AttackDamage); // You can modify or use different damage for ranged if needed

                // Wait for the attack animation to finish
                yield return new WaitForSeconds(0.6f); // Adjust based on the length of the attack animation

                // Reset attack animation
                if (animator != null)
                {
                    animator.SetBool("isAttackingRanged", false);
                    Debug.Log("Ranged attack animation complete.");
                }
            }

            // Optional: Delay between attacks on multiple targets
            yield return new WaitForSeconds(0.4f); // Adjust for desired pacing between attacks
        }
    }
}    
>>>>>>> Stashed changes
