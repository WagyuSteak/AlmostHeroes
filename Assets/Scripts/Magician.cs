using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magician : CharacterBase
{
    public GameObject skill1ProjectilePrefab; // ��ų 1�� ����ü ������
    public GameObject skill2ProjectilePrefab; // ��ų 2�� ����ü ������

    private Animator Magiciananimator;

    protected override void Start()
    {
        base.Start();
        IsRanged = true;
        Magiciananimator = GetComponent<Animator>();
    }
    public override void UseSkill(int skillNumber, TurnManager turnManager)
    {
        if (skillNumber == 2)
        {
            Magiciananimator.SetTrigger("firebomb"); // Firebomb �ִϸ��̼� ���
            int damage = Damage + 1; // ��ų 2 ������
            FireProjectile(GetGridPosition(transform.position), GetForwardDirection(), 4, skill2ProjectilePrefab, turnManager, damage);
        }
        else if (skillNumber == 1)
        {
            Magiciananimator.SetTrigger("fireball"); // Fireball �ִϸ��̼� ���
            int damage = Damage; // ��ų 1 ������
            FireProjectile(GetGridPosition(transform.position), GetForwardDirection(), 4, skill1ProjectilePrefab, turnManager, damage);
        }
    }

    private void FireProjectile(Vector2Int pathOrigin, Vector2Int pathDirection, int attackRange, GameObject projectilePrefab, TurnManager turnManager, int damage)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab is null!");
            return;
        }

        // ����ü ���� ��ġ ����
        Vector3 spawnPosition = gridOrigin + new Vector3(pathOrigin.x * cellWidth, pathIndicatorHeight, pathOrigin.y * cellHeight);
        Vector3 directionOffset = new Vector3(pathDirection.x * 0.5f, 0, pathDirection.y * 0.5f); // �������� �ణ �� �̵�
        spawnPosition += directionOffset; // ������ ��ġ
        spawnPosition.y = 1.5f; // Y ��ǥ ������

        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Projectile created at {spawnPosition} with offset {directionOffset}");

        // ���� ��ġ ���
        Vector2Int targetGridPosition = CalculateProjectileTarget(pathOrigin, pathDirection, attackRange);
        Vector3 targetWorldPosition = gridOrigin + new Vector3(targetGridPosition.x * cellWidth, pathIndicatorHeight, targetGridPosition.y * cellHeight);

        // ����ü �̵� ����
        StartCoroutine(MoveProjectile(projectile, targetWorldPosition, turnManager, damage));
    }

    private Vector2Int CalculateProjectileTarget(Vector2Int pathOrigin, Vector2Int pathDirection, int attackRange)
    {
        Vector2Int? targetPosition = null;

        for (int i = 1; i <= attackRange; i++)
        {
            Vector2Int position = pathOrigin + pathDirection * i;

            if (!gridManager.IsWithinGridBounds(position))
            {
                targetPosition = pathOrigin + pathDirection * (i - 1);
                break;
            }

            if (gridManager.IsEnemyPosition(position) || gridManager.IsCharacterPosition(position) || gridManager.IsObstaclePosition(position))
            {
                targetPosition = position;
                break;
            }

            targetPosition = position; // ��� ������Ʈ
        }

        if (!targetPosition.HasValue)
        {
            targetPosition = pathOrigin + pathDirection * attackRange;
        }

        return targetPosition.Value;
    }

    private IEnumerator MoveProjectile(GameObject projectile, Vector3 targetPosition, TurnManager turnManager, int damage)
    {
        float speed = 5f; // ����ü �ӵ�

        // Y ��ǥ ����
        float fixedY = projectile.transform.position.y;
        targetPosition.y = fixedY; // ��ǥ ��ġ�� Y ��ǥ ����

        while (Vector3.Distance(projectile.transform.position, targetPosition) > 0.1f)
        {
            // ���� Y ��ǥ�� �����ϸ鼭 �̵�
            Vector3 currentPosition = projectile.transform.position;
            currentPosition = Vector3.MoveTowards(currentPosition, targetPosition, speed * Time.deltaTime);
            currentPosition.y = fixedY; // Y ��ǥ ����
            projectile.transform.position = currentPosition;

            yield return null;
        }

        // ����ü�� ��ǥ ��ġ�� ������ �� ������ ����
        ApplyDamageToTargets(damage, turnManager);

        // ����ü �ı�
        Destroy(projectile);
        Debug.Log($"Projectile reached target at {targetPosition} and was destroyed");
    }

    private Vector2Int GetForwardDirection()
    {
        // ĳ���Ͱ� �ٶ󺸴� ���� ���� ���
        Vector3 forward = transform.forward;
        int x = Mathf.RoundToInt(forward.x);
        int z = Mathf.RoundToInt(forward.z);
        return new Vector2Int(x, z);
    }

    protected override void ActivateCell(Vector2Int targetGridPosition)
    {
        if (MoveCount > 0 && gridManager != null)
        {
            if (gridManager.IsCellActivatedByCharacter(targetGridPosition))
            {
                Debug.Log("�ش� ���� �̹� �ٸ� ĳ���Ϳ� ���� Ȱ��ȭ�Ǿ����ϴ�. �̵��� �� �����ϴ�.");
                return;
            }

            activatedCells.Add(targetGridPosition);
            MoveCount--;
            gridManager.ActivateCellForCharacter(targetGridPosition);

            Vector3 circlePosition = gridOrigin + new Vector3(targetGridPosition.x * cellWidth, pathIndicatorHeight, targetGridPosition.y * cellHeight);

            // ���� ���� ���� ���̸� ����Ͽ� ��ȿ�� ��쿡�� �ε������� ������Ʈ
            Vector2Int pathDirection = activatedCells.Count > 1
                ? (activatedCells[activatedCells.Count - 1] - activatedCells[activatedCells.Count - 2])
                : Vector2Int.zero;


            // ������ 2~5�� ���� �ε������� ǥ��
            if (mana >= 2)
            {
                UpdateAttackIndicators(targetGridPosition, pathDirection);
            }
            else
            {
                ClearIndicators(); // ������ ���ǿ� ���� ������ �ε������� ����
            }

            // ù ���� ��� ĳ���� ��ġ�� ���׶� ������ ���� �� ���� ��ġ�� ���� ����
            if (activatedCells.Count == 1)
            {
                ClearIndicators();
                MoveCount++;
                //ĳ���� ��ġ�� y���� ��� ǥ�� ���̷� ����
                Vector3 startPosition = new Vector3(transform.position.x, pathIndicatorHeight, transform.position.z);
                GameObject startCircleIndicator = Instantiate(circlePrefab, startPosition, Quaternion.identity);
                pathCircles.Add(startCircleIndicator);

                // ù ��ġ�� ���� ��ġ�� ������ ��� ����
                if (targetGridPosition != GetGridPosition(startPosition))
                {
                    Vector3 direction = (circlePosition - startPosition).normalized;
                    float distance = Vector3.Distance(circlePosition, startPosition);

                    GameObject lineIndicator = Instantiate(linePrefab, (circlePosition + startPosition) / 2, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)));
                    lineIndicator.transform.localScale = new Vector3(lineIndicator.transform.localScale.x, lineIndicator.transform.localScale.y, distance);

                    pathLines.Add(lineIndicator);
                }
            }
            else
            {
                // ���� ����: ���� ��ġ�� ���� ��ġ�� �������� ����
                Vector2Int previousPosition = activatedCells[activatedCells.Count - 2];
                Vector3 previousCirclePosition = gridOrigin + new Vector3(previousPosition.x * cellWidth, pathIndicatorHeight, previousPosition.y * cellHeight);

                Vector3 direction = (circlePosition - previousCirclePosition).normalized;
                float distance = Vector3.Distance(circlePosition, previousCirclePosition);

                GameObject lineIndicator = Instantiate(linePrefab, (circlePosition + previousCirclePosition) / 2, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)));
                lineIndicator.transform.localScale = new Vector3(lineIndicator.transform.localScale.x, lineIndicator.transform.localScale.y, distance);

                pathLines.Add(lineIndicator);
            }

            // Ÿ�� ���� ���׶� ������ ����
            GameObject circleIndicator = Instantiate(circlePrefab, circlePosition, Quaternion.identity);
            pathCircles.Add(circleIndicator);

            currentGridPosition = targetGridPosition;
            gridManager.RemoveCharacterFromGrid(this);
            gridManager.AddCharacterToGrid(currentGridPosition, this); // ���� ��ġ�� ĳ���� ����
            Debug.Log($"MoveCount: {MoveCount}");
        }
    }

    public void UpdateAttackIndicators(Vector2Int pathOrigin, Vector2Int pathDirection)
    {
        ClearIndicators(); // ���� �ε������� ����
        Vector2Int? primaryTargetPosition = null;

        // 1�� ���� ������ ��� Ž�� �� �ε������� ����
        for (int i = 1; i <= attackRange; i++)
        {
            Vector2Int position = pathOrigin + pathDirection * i;

            if (!gridManager.IsWithinGridBounds(position))
            {
                // �׸��� ��踦 ����� �ݺ��� ����, ������ ��ġ�� primaryTargetPosition���� ����
                primaryTargetPosition = pathOrigin + pathDirection * (i - 1);
                break;
            }

            // ��� ���� �� ó��
            if (gridManager.IsEnemyPosition(position) || gridManager.IsCharacterPosition(position) || gridManager.IsSylphPosition(position) || gridManager.IsObstaclePosition(position))
            {
                primaryTargetPosition = position;
                break;
            }

            primaryTargetPosition = position; // 1�� ������ �� ���� ����
        }

        // ����� ������ �ִ� ��Ÿ� ��ġ�� ����
        if (!primaryTargetPosition.HasValue)
        {
            primaryTargetPosition = pathOrigin + pathDirection * attackRange;
        }

        // �� �� �Ǵ� X�� �ε������� ǥ�� (���� ���� ����)
        List<Vector2Int> secondaryPositions;
        if (mana >= 6)
        {
            secondaryPositions = GetCrossPatternPositions(primaryTargetPosition.Value); // X�� ����
        }
        else
        {
            secondaryPositions = GetSidePatternPositions(primaryTargetPosition.Value, pathDirection); // �� �� ����
        }

        // 1�� ������ �� ������ 2�� ������ �����Ͽ� �ε������� ǥ��
        secondaryPositions.Add(primaryTargetPosition.Value);

        foreach (var pos in secondaryPositions)
        {
            if (gridManager.IsWithinGridBounds(pos))
            {
                Vector3 indicatorPos = gridOrigin + new Vector3(pos.x * cellWidth, pathIndicatorHeight, pos.y * cellHeight);
                GameObject secondaryIndicator = Instantiate(indicatorPrefab, indicatorPos, Quaternion.identity);

                if (gridManager.IsEnemyPosition(pos) || gridManager.IsCharacterPosition(pos) || gridManager.IsSylphPosition(pos))
                {
                    secondaryIndicator.GetComponent<Renderer>().material = enemyMaterial;
                }
                else
                {
                    secondaryIndicator.GetComponent<Renderer>().material = emptyMaterial;
                }

                indicators.Add(secondaryIndicator);
            }
        }
    }

    // �ܼ��ϰ� �� �� Ÿ�� ��ġ ���
    private List<Vector2Int> GetSidePatternPositions(Vector2Int targetPosition, Vector2Int direction)
    {
        List<Vector2Int> secondaryPositions = new List<Vector2Int>();

        // �� �� ��ġ ���
        secondaryPositions.Add(targetPosition + new Vector2Int(-direction.y, direction.x)); // ����
        secondaryPositions.Add(targetPosition + new Vector2Int(direction.y, -direction.x)); // ������

        return secondaryPositions;
    }

    // X�� Ÿ�� ��ġ ���
    private List<Vector2Int> GetCrossPatternPositions(Vector2Int targetPosition)
    {
        List<Vector2Int> secondaryPositions = new List<Vector2Int>();

        // X�� ���� ��ġ ���
        secondaryPositions.Add(targetPosition + new Vector2Int(1, 1));    // ������ �� �밢��
        secondaryPositions.Add(targetPosition + new Vector2Int(-1, 1));   // ���� �� �밢��
        secondaryPositions.Add(targetPosition + new Vector2Int(1, -1));   // ������ �Ʒ� �밢��
        secondaryPositions.Add(targetPosition + new Vector2Int(-1, -1));  // ���� �Ʒ� �밢��

        return secondaryPositions;
    }

    public override IEnumerator StartMovement()
    {
        // ù ��° Ȱ��ȭ�� ���� �ǳʶٱ� ���� i�� 1�� ����
        for (int i = 1; i < activatedCells.Count; i++)
        {
            Vector2Int cellPosition = activatedCells[i];
            Vector3 targetPosition = gridOrigin + new Vector3(cellPosition.x * cellWidth, 0.55f, cellPosition.y * cellHeight);
            Vector3 direction = (targetPosition - transform.position).normalized;
            Quaternion targetRotation = Quaternion.Euler(0, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)).eulerAngles.y, 0);

            // �̵� �ִϸ��̼� ����
            Magiciananimator.SetBool("isMoving", true);
            yield return new WaitForSeconds(0.85f); // �ִϸ��̼��� ���۵ǵ��� ��� ���

            // ȸ��
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 30);
                yield return null;
            }

            // �̵�
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 7);
                yield return null;
            }

            yield return new WaitForSeconds(0.25f); // �� ��ġ���� ��� �ð�
        }
        Magiciananimator.SetBool("isMoving", false); // �̵� �ִϸ��̼� ����
        ClearIndicators(); // �̵��� ���� �� �ε������� �ʱ�ȭ
        ResetCharacterSet();
    }

    public override void CollectAttackTargets(TurnManager turnManager)
    {
        List<CharacterBase> characterTargets = new List<CharacterBase>();
        List<EnemyBase> enemyTargets = new List<EnemyBase>();

        foreach (var indicator in indicators)
        {
            Vector3 indicatorPos = indicator.transform.position;
            Vector2Int gridPosition = GetGridPosition(indicatorPos);

            if (gridManager.IsEnemyPosition(gridPosition))
            {
                EnemyBase enemy = gridManager.GetEnemyAtPosition(gridPosition);
                if (enemy != null && enemy.HP > 0)
                {
                    enemyTargets.Add(enemy);
                    Debug.Log($"�� ������: {enemy.name}, ��ġ: ({gridPosition.x}, {gridPosition.y})");
                }
            }
            else if (gridManager.IsCharacterPosition(gridPosition))
            {
                CharacterBase character = gridManager.GetCharacterAtPosition(gridPosition);
                if (character != null && character.Health > 0 && character != this)
                {
                    characterTargets.Add(character);
                    Debug.Log($"ĳ���� ������: {character.name}, ��ġ: ({gridPosition.x}, {gridPosition.y})");
                }
            }
        }

        // �� ĳ������ Ÿ�� ����Ʈ�� TurnManager�� ����
        turnManager.SetCharacterTargets(this, characterTargets, enemyTargets);
        ClearIndicators(); // ���� �ε������� ����
    }

    public override void TakeDamage(int damageAmount)
    {
        int damageToApply = damageAmount;

        if (Shield > 0)
        {
            int shieldAbsorbed = Mathf.Min(Shield, damageToApply);
            Shield -= shieldAbsorbed;
            damageToApply -= shieldAbsorbed;
            Debug.Log($"{name}�� ���尡 {shieldAbsorbed}��ŭ ���ظ� ����߽��ϴ�. ���� ����: {Shield}");
        }

        if (damageToApply > 0)
        {
            Magiciananimator.SetTrigger("hit"); // �ǰ� �ִϸ��̼� ���
            Health -= damageToApply;
            Debug.Log($"{name}��(��) {damageToApply}�� ���ظ� �Ծ����ϴ�. ���� ü��: {Health}");
        }

        if (Health <= 0)
        {
            Die();
        }
    }
    public override void Die()
    {
        Magiciananimator.SetTrigger("die"); // ��� �ִϸ��̼� ���
        base.Die(); // �⺻ ��� ���� ����
    }
}
