using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Priest : CharacterBase
{
    protected override void Start()
    {
        base.Start();
        IsRanged = true;
    }
    public override void UseSkill(int skillNumber, TurnManager turnManager)
    {
        if (skillNumber == 1)
        {
            int damage = Damage;
            ApplyDamageToTargets(damage, turnManager);
        }
        else
        {
            // �⺻ ȸ�� ȿ�� ����
            foreach (var ally in turnManager.characterTargetMap[this].Item1)
            {
                ally.Health = Mathf.Min(ally.Health + 1, ally.MaxHealth);
                Debug.Log($"{ally.name}�� ü�� +1");
            }
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

        // 1�� ���� ������ ��� Ž�� �� �ε������� ����
        int effectiveRange = mana >= 6 ? 1 : attackRange; // ���� 6 �̻��� ���� 1�� ����
        for (int i = 1; i <= effectiveRange; i++)
        {
            Vector2Int position = pathOrigin + pathDirection * i;

            if (!gridManager.IsWithinGridBounds(position))
            {
                // �׸��� ��踦 ����� �ݺ��� ����, ������ ��ġ�� primaryTargetPosition���� ����
                primaryTargetPosition = pathOrigin + pathDirection * (i - 1);
                break;
            }

            // ��� ���� �� ó��
            if (gridManager.IsEnemyPosition(position) || gridManager.IsCharacterPosition(position) || gridManager.IsSylphPosition(position))
            {
                primaryTargetPosition = position;
                break;
            }

            primaryTargetPosition = position; // 1�� ������ �� ���� ����
        }

        // ����� ������ �ִ� ��Ÿ� ��ġ�� ����
        if (!primaryTargetPosition.HasValue)
        {
            primaryTargetPosition = pathOrigin + pathDirection * effectiveRange;
        }

        // �� �� �Ǵ� ���� 3x3 �ε������� ǥ�� (���� ���� ����)
        List<Vector2Int> secondaryPositions;
        if (mana >= 6)
        {
            secondaryPositions = GetBehindPatternPositions(primaryTargetPosition.Value, pathDirection); // 9�� ����
        }
        else
        {
            secondaryPositions = GetPiercingPositions(primaryTargetPosition.Value, pathDirection); // ����
        }

        // 1�� ������ �� ������ 2�� ������ �����Ͽ� �ε������� ǥ��
        secondaryPositions.Add(primaryTargetPosition.Value);

        foreach (var pos in secondaryPositions)
        {
            if (gridManager.IsWithinGridBounds(pos))
            {
                Vector3 indicatorPos = gridOrigin + new Vector3(pos.x * cellWidth, pathIndicatorHeight, pos.y * cellHeight);
                GameObject secondaryIndicator = Instantiate(indicatorPrefab, indicatorPos, Quaternion.identity);

                // ���� ���� ���� ���͸��� ���� �б� ó��
                if (mana >= 6) // ���� ���͸��� ����
                {
                    Vector2Int gridPosition = GetGridPosition(indicatorPos);
                    CharacterBase character = gridManager.GetCharacterAtPosition(gridPosition);
                    if (gridManager.IsCharacterPosition(pos) && character != this)
                    {
                        secondaryIndicator.GetComponent<Renderer>().material = BuffMaterial;
                    }
                    else
                    {
                        secondaryIndicator.GetComponent<Renderer>().material = emptyMaterial;
                    }
                }
                else if (mana <= 5) // �Ϲ� ���� ���͸��� ����
                {
                    if (gridManager.IsEnemyPosition(pos) || gridManager.IsCharacterPosition(pos) || gridManager.IsSylphPosition(pos))
                    {
                        secondaryIndicator.GetComponent<Renderer>().material = enemyMaterial;
                    }
                    else
                    {
                        secondaryIndicator.GetComponent<Renderer>().material = emptyMaterial;
                    }
                }

                indicators.Add(secondaryIndicator);
            }
        }
    }

    private List<Vector2Int> GetPiercingPositions(Vector2Int targetPosition, Vector2Int pathDirection)
    {
        List<Vector2Int> secondaryPositions = new List<Vector2Int>();

        return secondaryPositions;
    }
    private List<Vector2Int> GetBehindPatternPositions(Vector2Int targetPosition, Vector2Int pathDirection)
    {
        List<Vector2Int> secondaryPositions = new List<Vector2Int>();

        // �߽ɿ��� ���� 3x3 ���� ���
        Vector2Int left = new Vector2Int(-pathDirection.y, pathDirection.x);
        Vector2Int right = new Vector2Int(pathDirection.y, -pathDirection.x);

        secondaryPositions.Add(targetPosition + pathDirection); // �߽� ����
        secondaryPositions.Add(targetPosition + pathDirection + left); // ����
        secondaryPositions.Add(targetPosition + pathDirection + right); // ������

        secondaryPositions.Add(targetPosition + pathDirection * 2); // �� 1ĭ
        secondaryPositions.Add(targetPosition + pathDirection * 2 + left); // ���� ��
        secondaryPositions.Add(targetPosition + pathDirection * 2 + right); // ������ ��

        secondaryPositions.Add(targetPosition + left); // ���� �� 2ĭ
        secondaryPositions.Add(targetPosition + right); // ������ �� 2ĭ

        return secondaryPositions;
    }

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
}
