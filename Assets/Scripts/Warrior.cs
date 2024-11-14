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
            // 기본 방어 효과 적용
            Shield += 1;
            Debug.Log("워리어의 쉴드 +1");
        }
    }

    protected override void ActivateCell(Vector2Int targetGridPosition)
    {
        if (MoveCount > 0 && gridManager != null)
        {
            if (gridManager.IsCellActivatedByCharacter(targetGridPosition))
            {
                Debug.Log("해당 셀은 이미 다른 캐릭터에 의해 활성화되었습니다. 이동할 수 없습니다.");
                return;
            }

            activatedCells.Add(targetGridPosition);
            MoveCount--;
            gridManager.ActivateCellForCharacter(targetGridPosition);

            Vector3 circlePosition = gridOrigin + new Vector3(targetGridPosition.x * cellWidth, pathIndicatorHeight, targetGridPosition.y * cellHeight);

            Vector2Int pathDirection = activatedCells.Count > 1
                ? (activatedCells[activatedCells.Count - 1] - activatedCells[activatedCells.Count - 2])
                : Vector2Int.zero;

            // 마나가 6 이상일 때는 공격 인디케이터, 6 미만일 때는 버프 인디케이터 표시
            if (mana >= 6)
            {
                UpdateAttackIndicators(targetGridPosition, pathDirection);
            }
            else if (mana <= 2)
            {
                ClearIndicators(); // 마나가 조건에 맞지 않으면 인디케이터 제거
            }
            else
            {
                ShowBuffIndicatorAtCurrentPosition(circlePosition); // 버프 인디케이터 표시 함수 호출
            }

            // 첫 셀의 경우 캐릭터 위치에 동그란 프리팹 생성 및 다음 위치와 직선 연결
            if (activatedCells.Count == 1)
            {
                MoveCount++;
                if(mana >= 6)
                {
                    ClearIndicators();
                }
                // 캐릭터 위치의 y값을 경로 표시 높이로 맞춤
                Vector3 startPosition = new Vector3(transform.position.x, pathIndicatorHeight, transform.position.z);
                GameObject startCircleIndicator = Instantiate(circlePrefab, startPosition, Quaternion.identity);
                pathCircles.Add(startCircleIndicator);

                // 첫 위치와 다음 위치가 존재할 경우 연결
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
                // 기존 로직: 이전 위치와 현재 위치를 직선으로 연결
                Vector2Int previousPosition = activatedCells[activatedCells.Count - 2];
                Vector3 previousCirclePosition = gridOrigin + new Vector3(previousPosition.x * cellWidth, pathIndicatorHeight, previousPosition.y * cellHeight);

                Vector3 direction = (circlePosition - previousCirclePosition).normalized;
                float distance = Vector3.Distance(circlePosition, previousCirclePosition);

                GameObject lineIndicator = Instantiate(linePrefab, (circlePosition + previousCirclePosition) / 2, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)));
                lineIndicator.transform.localScale = new Vector3(lineIndicator.transform.localScale.x, lineIndicator.transform.localScale.y, distance);

                pathLines.Add(lineIndicator);
            }

            // 타겟 셀에 동그란 프리팹 생성
            GameObject circleIndicator = Instantiate(circlePrefab, circlePosition, Quaternion.identity);
            pathCircles.Add(circleIndicator);

            currentGridPosition = targetGridPosition;
            gridManager.RemoveCharacterFromGrid(this);
            gridManager.AddCharacterToGrid(currentGridPosition, this); // 현재 위치로 캐릭터 갱신
            Debug.Log($"MoveCount: {MoveCount}");
        }
    }
    public void UpdateAttackIndicators(Vector2Int pathOrigin, Vector2Int pathDirection)
    {
        ClearIndicators(); // 기존 인디케이터 제거
        Vector2Int? primaryTargetPosition = null;

        // 1차 범위 내에서 대상 탐색 및 인디케이터 표시 없이 위치만 기록
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

            primaryTargetPosition = position; // 1차 범위의 끝 지점 갱신
        }

        if (!primaryTargetPosition.HasValue)
        {
            primaryTargetPosition = pathOrigin + pathDirection * attackRange;
        }

        List<Vector2Int> secondaryPositions = GetPiercingPositions(primaryTargetPosition.Value, pathDirection);
        secondaryPositions.Add(primaryTargetPosition.Value); // 1차 범위의 끝 지점도 인디케이터 표시 대상에 포함

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

        secondaryPositions.Add(targetPosition + pathDirection); // 뒤쪽 1칸
        return secondaryPositions;
    }

    private void ShowBuffIndicatorAtCurrentPosition(Vector3 position)
    {
        ClearIndicators(); // 기존 인디케이터 제거

        GameObject indicator = Instantiate(indicatorPrefab, position, Quaternion.identity);

        // 버프 머터리얼 적용
        if (BuffMaterial != null)
        {
            indicator.GetComponent<Renderer>().material = BuffMaterial;
        }

        indicators.Add(indicator); // 인디케이터 리스트에 추가하여 나중에 제거될 수 있도록 함
    }

    // 공격 범위 내의 캐릭터와 적을 체크하고 턴 매니저에 리스트 전달
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
                    Debug.Log($"캐릭터 감지됨: {character.name}, 위치: ({gridPosition.x}, {gridPosition.y})");
                }
            }
        }

        // 각 캐릭터의 타겟 리스트를 TurnManager에 저장
        turnManager.SetCharacterTargets(this, characterTargets, enemyTargets);
        ClearIndicators(); // 기존 인디케이터 제거
    }
}
