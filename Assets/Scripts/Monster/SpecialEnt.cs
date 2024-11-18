using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialEnt : EnemyBase
{
    public int Health; // 현재 적의 체력
    public int MaxHealth; // 적의 최대 체력
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        HP = 3; // Set health points
        MoveCount = 3; // Set number of tiles the enemy can move
        AttackDamage = 1; // You can adjust attack damage here if needed

        FindObjectOfType<TurnManager>().RegisterEnemy(this);
    }
}
