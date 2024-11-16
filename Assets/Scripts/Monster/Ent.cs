using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ent : EnemyBase
{
    public int Health; // ���� ���� ü��
    public int MaxHealth; // ���� �ִ� ü��
    // Start is called before the first frame update
    protected override void Start()
    {
        // Call the base class Start() to ensure common initialization logic is run
        base.Start();

        HP = 3; // Set health points
        MoveCount = 2; // Set number of tiles the enemy can move
        AttackDamage = 0; // You can adjust attack damage here if needed

        FindObjectOfType<TurnManager>().RegisterEnemy(this);
    }

    // ���� ���� �� ó���� ����
    protected override void Die()
    {
        // ���� ó�� ���� (��: �ı�, ȿ�� ��)
        Destroy(gameObject);
        Debug.Log($"{gameObject.name} has been destroyed!");
    }
}