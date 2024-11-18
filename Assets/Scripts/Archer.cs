using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer : CharacterBase
{
    private Animator Archeranimator;
    public GameObject ProjectilePrefab;
    protected override void Start()
    {
        base.Start();
        IsRanged = true;
    }
    public override void UseSkill(int skillNumber, TurnManager turnManager)
    {
        if (skillNumber == 2)
        {
            int damage = Damage + 1;
            ApplyDamageToTargets(damage, turnManager);
            FireProjectile(GetGridPosition(transform.position), GetForwardDirection(), 4, ProjectilePrefab);
        }
        else if (skillNumber == 1)
        {
            int damage = Damage;
            ApplyDamageToTargets(damage, turnManager);
        }
    }

    private void FireProjectile(Vector2Int pathOrigin, Vector2Int pathDirection, int attackRange, GameObject projectilePrefab)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab is null!");
            return;
        }

        // ����ü ���� ��ġ ����: Y ��ǥ�� ���� ���� �߰�
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
        StartCoroutine(MoveProjectile(projectile, targetWorldPosition));
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

    private IEnumerator MoveProjectile(GameObject projectile, Vector3 targetPosition)
    {
        float speed = 7f; // ����ü �ӵ�

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
                MoveCount++;
                ClearIndicators();
                // ĳ���� ��ġ�� y���� ��� ǥ�� ���̷� ����
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

        int rangeToUse = mana >= 6 ? attackRange : 1; // ��ȭ�� ���뼦�� ������ ��Ÿ� 1

        // 1�� ���� ������ ��� Ž�� �� �ε������� ����
        for (int i = 1; i <= rangeToUse; i++)
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
            primaryTargetPosition = pathOrigin + pathDirection * rangeToUse;
        }

        // 2�� ���� ���
        List<Vector2Int> secondaryPositions;
        if (mana >= 6)
        {
            secondaryPositions = GetPiercingPositions(primaryTargetPosition.Value, pathDirection); // ���뼦
        }
        else
        {
            secondaryPositions = GetPiercingTwoPositions(primaryTargetPosition.Value, pathDirection); // 3ĭ ���뼦
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

    private List<Vector2Int> GetPiercingPositions(Vector2Int targetPosition, Vector2Int pathDirection)
    {
        List<Vector2Int> secondaryPositions = new List<Vector2Int>();

        secondaryPositions.Add(targetPosition + pathDirection);            // ���� 1ĭ

        return secondaryPositions;
    }

    // ���� �� ĭ�� ����ϴ� �޼��� (���뼦 ����)
    private List<Vector2Int> GetPiercingTwoPositions(Vector2Int targetPosition, Vector2Int pathDirection)
    {
        List<Vector2Int> secondaryPositions = new List<Vector2Int>();

        // Ÿ�� ������ �������� ���� 1ĭ�� 2ĭ ���
        secondaryPositions.Add(targetPosition + pathDirection);            // ���� 1ĭ
        secondaryPositions.Add(targetPosition + pathDirection * 2);        // ���� 2ĭ

        return secondaryPositions;
    }
}
