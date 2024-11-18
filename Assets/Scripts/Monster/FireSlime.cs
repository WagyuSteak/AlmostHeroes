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
        MoveCount = 2; // Set number of tiles the enemy can move
        AttackDamage = 1; // You can adjust attack damage here if needed

        FindObjectOfType<TurnManager>().RegisterEnemy(this);
    }
}
