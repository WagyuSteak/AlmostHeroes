using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slime : EnemyBase
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

    // 적이 죽을 때 처리할 로직
    protected override void Die()
    {
        // 죽음 처리 로직 (예: 파괴, 효과 등)
        Destroy(gameObject);
        Debug.Log($"{gameObject.name} has been destroyed!");
    }
}
