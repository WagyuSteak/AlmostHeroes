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
            // 기본 회복 효과 적용
            foreach (var ally in turnManager.characterTargetMap[this].Item1)
            {
                ally.Health = Mathf.Min(ally.Health + 1, ally.MaxHealth);
                Debug.Log($"{ally.name}의 체력 +1");
            }
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
                MoveCount++;
                ClearIndicators();
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

        // 1차 범위 내에서 대상 탐색 및 인디케이터 생성
        int effectiveRange = mana >= 6 ? 1 : attackRange; // 마나 6 이상일 때는 1로 설정
        for (int i = 1; i <= effectiveRange; i++)
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
            primaryTargetPosition = pathOrigin + pathDirection * effectiveRange;
        }

        // 양 옆 또는 뒤쪽 3x3 인디케이터 표시 (마나 값에 따라)
        List<Vector2Int> secondaryPositions;
        if (mana >= 6)
        {
            secondaryPositions = GetBehindPatternPositions(primaryTargetPosition.Value, pathDirection); // 9자 패턴
        }
        else
        {
            secondaryPositions = GetPiercingPositions(primaryTargetPosition.Value, pathDirection); // 전방
        }

        // 1차 범위의 끝 지점도 2차 범위로 간주하여 인디케이터 표시
        secondaryPositions.Add(primaryTargetPosition.Value);

        foreach (var pos in secondaryPositions)
        {
            if (gridManager.IsWithinGridBounds(pos))
            {
                Vector3 indicatorPos = gridOrigin + new Vector3(pos.x * cellWidth, pathIndicatorHeight, pos.y * cellHeight);
                GameObject secondaryIndicator = Instantiate(indicatorPrefab, indicatorPos, Quaternion.identity);

                // 마나 값에 따라 머터리얼 설정 분기 처리
                if (mana >= 6) // 버프 머터리얼 적용
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
                else if (mana <= 5) // 일반 공격 머터리얼 적용
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

        // 중심에서 뒤쪽 3x3 범위 계산
        Vector2Int left = new Vector2Int(-pathDirection.y, pathDirection.x);
        Vector2Int right = new Vector2Int(pathDirection.y, -pathDirection.x);

        secondaryPositions.Add(targetPosition + pathDirection); // 중심 지점
        secondaryPositions.Add(targetPosition + pathDirection + left); // 왼쪽
        secondaryPositions.Add(targetPosition + pathDirection + right); // 오른쪽

        secondaryPositions.Add(targetPosition + pathDirection * 2); // 뒤 1칸
        secondaryPositions.Add(targetPosition + pathDirection * 2 + left); // 왼쪽 뒤
        secondaryPositions.Add(targetPosition + pathDirection * 2 + right); // 오른쪽 뒤

        secondaryPositions.Add(targetPosition + left); // 왼쪽 뒤 2칸
        secondaryPositions.Add(targetPosition + right); // 오른쪽 뒤 2칸

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
                    Debug.Log($"캐릭터 감지됨: {character.name}, 위치: ({gridPosition.x}, {gridPosition.y})");
                }
            }
        }

        // 각 캐릭터의 타겟 리스트를 TurnManager에 저장
        turnManager.SetCharacterTargets(this, characterTargets, enemyTargets);
        ClearIndicators(); // 기존 인디케이터 제거
    }
}
