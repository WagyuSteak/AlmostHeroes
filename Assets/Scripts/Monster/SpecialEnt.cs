using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialEnt : EnemyBase
{
    public int Health; // ���� ���� ü��
    public int MaxHealth; // ���� �ִ� ü��
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
