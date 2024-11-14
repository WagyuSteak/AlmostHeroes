using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Warrior : CharacterBase
{
    protected override void Start()
    {
        base.Start();
        IsRanged = false;
    }
    public override void UseSkill(int skillNumber, TurnManager turnManager)
    {
        if (skillNumber == 2)
        {
            int damage = Damage + 1;
            ApplyDamageToTargets(damage, turnManager);
        }
        else if (skillNumber == 1)
        {
            // �⺻ ��� ȿ�� ����
            Shield += 1;
            Debug.Log("�������� ���� +1");
        }
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

            // ������ 6 �̻��� ���� ���� �ε�������, 6 �̸��� ���� ���� �ε������� ǥ��
            if (mana >= 6)
            {
                UpdateAttackIndicators(targetGridPosition, pathDirection);
            }
            else if (mana <= 2)
            {
                ClearIndicators(); // ������ ���ǿ� ���� ������ �ε������� ����
            }
            else
            {
                ShowBuffIndicatorAtCurrentPosition(circlePosition); // ���� �ε������� ǥ�� �Լ� ȣ��
            }

            // ù ���� ��� ĳ���� ��ġ�� ���׶� ������ ���� �� ���� ��ġ�� ���� ����
            if (activatedCells.Count == 1)
            {
                MoveCount++;
                if(mana >= 6)
                {
                    ClearIndicators();
                }
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

        // 1�� ���� ������ ��� Ž�� �� �ε������� ǥ�� ���� ��ġ�� ���
        for (int i = 1; i <= attackRange; i++)
        {
            Vector2Int position = pathOrigin + pathDirection * i;

            if (!gridManager.IsWithinGridBounds(position))
            {
                primaryTargetPosition = pathOrigin + pathDirection * (i - 1);
                break;
            }

            if (gridManager.IsEnemyPosition(position) || gridManager.IsCharacterPosition(position) || gridManager.IsSylphPosition(position))
            {
                primaryTargetPosition = position;
                break;
            }

            primaryTargetPosition = position; // 1�� ������ �� ���� ����
        }

        if (!primaryTargetPosition.HasValue)
        {
            primaryTargetPosition = pathOrigin + pathDirection * attackRange;
        }

        List<Vector2Int> secondaryPositions = GetPiercingPositions(primaryTargetPosition.Value, pathDirection);
        secondaryPositions.Add(primaryTargetPosition.Value); // 1�� ������ �� ������ �ε������� ǥ�� ��� ����

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

        secondaryPositions.Add(targetPosition + pathDirection); // ���� 1ĭ
        return secondaryPositions;
    }

    private void ShowBuffIndicatorAtCurrentPosition(Vector3 position)
    {
        ClearIndicators(); // ���� �ε������� ����

        GameObject indicator = Instantiate(indicatorPrefab, position, Quaternion.identity);

        // ���� ���͸��� ����
        if (BuffMaterial != null)
        {
            indicator.GetComponent<Renderer>().material = BuffMaterial;
        }

        indicators.Add(indicator); // �ε������� ����Ʈ�� �߰��Ͽ� ���߿� ���ŵ� �� �ֵ��� ��
    }

    // ���� ���� ���� ĳ���Ϳ� ���� üũ�ϰ� �� �Ŵ����� ����Ʈ ����
    public override void CollectAttackTargets(TurnManager turnManager)
    {
        List<CharacterBase> characterTargets = new List<CharacterBase>();
        List<EnemyBase> enemyTargets = new List<EnemyBase>();

        foreach (var indicator in indicators)
        {
            Vector3 indicatorPos = indicator.transform.position;
            Vector2Int gridPosition = GetGridPosition(indicatorPos);

            if (gridManager.IsCharacterPosition(gridPosition))
            {
                CharacterBase character = gridManager.GetCharacterAtPosition(gridPosition);
                if (character != null && character.Health > 0)
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
}
