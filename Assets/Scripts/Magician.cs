using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magician : CharacterBase
{
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
        }
        else if (skillNumber == 1)
        {
            int damage = Damage;
            ApplyDamageToTargets(damage, turnManager);
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

            // 이전 셀과 방향 차이를 계산하여 유효한 경우에만 인디케이터 업데이트
            Vector2Int pathDirection = activatedCells.Count > 1
                ? (activatedCells[activatedCells.Count - 1] - activatedCells[activatedCells.Count - 2])
                : Vector2Int.zero;


            // 마나가 2~5일 때만 인디케이터 표시
            if (mana >= 2)
            {
                UpdateAttackIndicators(targetGridPosition, pathDirection);
            }
            else
            {
                ClearIndicators(); // 마나가 조건에 맞지 않으면 인디케이터 제거
            }

            // 첫 셀의 경우 캐릭터 위치에 동그란 프리팹 생성 및 다음 위치와 직선 연결
            if (activatedCells.Count == 1)
            {
                ClearIndicators();
                MoveCount++;
                //캐릭터 위치의 y값을 경로 표시 높이로 맞춤
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

        // 1차 범위 내에서 대상 탐색 및 인디케이터 생성
        for (int i = 1; i <= attackRange; i++)
        {
            Vector2Int position = pathOrigin + pathDirection * i;

            if (!gridManager.IsWithinGridBounds(position))
            {
                // 그리드 경계를 벗어나면 반복문 종료, 마지막 위치를 primaryTargetPosition으로 설정
                primaryTargetPosition = pathOrigin + pathDirection * (i - 1);
                break;
            }

            // 대상 감지 후 처리
            if (gridManager.IsEnemyPosition(position) || gridManager.IsCharacterPosition(position) || gridManager.IsSylphPosition(position))
            {
                primaryTargetPosition = position;
                break;
            }

            primaryTargetPosition = position; // 1차 범위의 끝 지점 갱신
        }

        // 대상이 없으면 최대 사거리 위치로 설정
        if (!primaryTargetPosition.HasValue)
        {
            primaryTargetPosition = pathOrigin + pathDirection * attackRange;
        }

        // 양 옆 또는 X자 인디케이터 표시 (마나 값에 따라)
        List<Vector2Int> secondaryPositions;
        if (mana >= 6)
        {
            secondaryPositions = GetCrossPatternPositions(primaryTargetPosition.Value); // X자 패턴
        }
        else
        {
            secondaryPositions = GetSidePatternPositions(primaryTargetPosition.Value, pathDirection); // 양 옆 패턴
        }

        // 1차 범위의 끝 지점도 2차 범위로 간주하여 인디케이터 표시
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

    // 단순하게 양 옆 타격 위치 계산
    private List<Vector2Int> GetSidePatternPositions(Vector2Int targetPosition, Vector2Int direction)
    {
        List<Vector2Int> secondaryPositions = new List<Vector2Int>();

        // 양 옆 위치 계산
        secondaryPositions.Add(targetPosition + new Vector2Int(-direction.y, direction.x)); // 왼쪽
        secondaryPositions.Add(targetPosition + new Vector2Int(direction.y, -direction.x)); // 오른쪽

        return secondaryPositions;
    }

    // X자 타격 위치 계산
    private List<Vector2Int> GetCrossPatternPositions(Vector2Int targetPosition)
    {
        List<Vector2Int> secondaryPositions = new List<Vector2Int>();

        // X자 형태 위치 계산
        secondaryPositions.Add(targetPosition + new Vector2Int(1, 1));    // 오른쪽 위 대각선
        secondaryPositions.Add(targetPosition + new Vector2Int(-1, 1));   // 왼쪽 위 대각선
        secondaryPositions.Add(targetPosition + new Vector2Int(1, -1));   // 오른쪽 아래 대각선
        secondaryPositions.Add(targetPosition + new Vector2Int(-1, -1));  // 왼쪽 아래 대각선

        return secondaryPositions;
    }
}
