using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Warrior : CharacterBase
{
    private Animator Warrioranimator;

    private List<object> collisionTargets = new List<object>(); // 캐릭터와 적을 모두 저장

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

            // 넉백 전에 시각적으로 워리어 이동
            if (turnManager.characterTargetMap.TryGetValue(this, out var targets))
            {
                var (characterTargets, enemyTargets) = targets;

                // 캐릭터 타겟 처리
                foreach (var target in characterTargets)
                {
                    // 워리어 이동 시각적 처리
                    Vector3 warriorWorldPosition = gridManager.GetWorldPositionFromGrid(this.GetCurrentGridPosition());
                    StartCoroutine(MoveWarriorToPosition(warriorWorldPosition, 0.3f)); // 0.3초 동안 이동 애니메이션

                    // 캐릭터 넉백 처리
                    Vector3 knockbackWorldPosition = gridManager.GetWorldPositionFromGrid(target.GetCurrentGridPosition());
                    StartCoroutine(DelayedKnockback(target, knockbackWorldPosition, 0.3f, 0.5f)); // 0.3초 후 넉백 시작
                }

                // 적 타겟 처리
                foreach (var target in enemyTargets)
                {
                    // 워리어 이동 시각적 처리
                    Vector3 warriorWorldPosition = gridManager.GetWorldPositionFromGrid(this.GetCurrentGridPosition());
                    StartCoroutine(MoveWarriorToPosition(warriorWorldPosition, 0.3f)); // 0.3초 동안 이동 애니메이션

                    // 적 넉백 처리
                    Vector3 knockbackWorldPosition = gridManager.GetWorldPositionFromGrid(target.CurrentGridPosition);
                    StartCoroutine(DelayedKnockback(target, knockbackWorldPosition, 0.3f, 0.5f)); // 0.3초 후 넉백 시작
                }
            }
        }
        else if (skillNumber == 1)
        {
            // 기본 방어 효과 적용
            Shield += 1;
            Debug.Log("워리어의 쉴드 +1");
        }
    }
    private IEnumerator MoveWarriorToPosition(Vector3 targetPosition, float duration)
    {
        float elapsedTime = 0;
        Vector3 startingPosition = transform.position; // 워리어의 현재 위치 저장
        float fixedY = startingPosition.y; // Y 좌표를 고정

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(
                startingPosition,
                new Vector3(targetPosition.x, fixedY, targetPosition.z), // Y 좌표 고정
                elapsedTime / duration
            );
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = new Vector3(targetPosition.x, fixedY, targetPosition.z); // 최종 위치에도 Y 좌표 고정
    }

    private IEnumerator DelayedKnockback(object target, Vector3 knockbackPosition, float delay, float duration)
    {
        yield return new WaitForSeconds(delay); // 워리어 이동 시간 대기

        if (target is CharacterBase characterTarget)
        {
            // CharacterBase에 대한 넉백 처리
            StartCoroutine(KnockbackCoroutine(characterTarget, knockbackPosition, duration));
        }
        else if (target is EnemyBase enemyTarget)
        {
            // EnemyBase에 대한 넉백 처리
            StartCoroutine(KnockbackCoroutine(enemyTarget, knockbackPosition, duration));
        }
        else
        {
            Debug.LogError("Invalid target type for DelayedKnockback.");
        }
    }

    private void ApplyKnockback(object target)
    {
        Vector2Int direction, knockbackDirection, knockbackPosition;

        if (target is CharacterBase characterTarget)
        {
            // 캐릭터 넉백 처리
            direction = characterTarget.GetCurrentGridPosition() - this.GetCurrentGridPosition();
            knockbackDirection = new Vector2Int(Mathf.Clamp(direction.x, -1, 1), Mathf.Clamp(direction.y, -1, 1));
            knockbackPosition = characterTarget.GetCurrentGridPosition() + knockbackDirection;

            if (gridManager.IsWithinGridBounds(knockbackPosition) &&
                !gridManager.IsObstaclePosition(knockbackPosition) &&
                !gridManager.IsCharacterPosition(knockbackPosition) &&
                !gridManager.IsEnemyPosition(knockbackPosition) &&
                !gridManager.IsSylphPosition(knockbackPosition))
            {
                gridManager.RemoveCharacterFromGrid(characterTarget);
                characterTarget.SetGridPosition(knockbackPosition);
                gridManager.AddCharacterToGrid(knockbackPosition, characterTarget);
                Debug.Log($"{characterTarget.name}이(가) {knockbackDirection} 방향으로 한 칸 넉백되었습니다. 현재 위치: {characterTarget.GetCurrentGridPosition()}");
            }
            else
            {
                // 넉백 대상 추가
                collisionTargets.Add(target);

                // 충돌 대상 처리
                if (gridManager.IsCharacterPosition(knockbackPosition))
                {
                    var collidedCharacter = gridManager.GetCharacterAtPosition(knockbackPosition);
                    if (collidedCharacter != null)
                    {
                        collisionTargets.Add(collidedCharacter);
                    }
                }

                if (gridManager.IsEnemyPosition(knockbackPosition))
                {
                    var collidedEnemy = gridManager.GetEnemyAtPosition(knockbackPosition);
                    if (collidedEnemy != null)
                    {
                        collisionTargets.Add(collidedEnemy);
                    }
                }

                if (gridManager.IsObstaclePosition(knockbackPosition))
                {
                    Debug.Log($"장애물에 충돌: {target}");
                }
            }
        }
        else if (target is EnemyBase enemyTarget)
        {
            // 적 넉백 처리
            direction = enemyTarget.CurrentGridPosition - this.GetCurrentGridPosition();
            knockbackDirection = new Vector2Int(Mathf.Clamp(direction.x, -1, 1), Mathf.Clamp(direction.y, -1, 1));
            knockbackPosition = enemyTarget.CurrentGridPosition + knockbackDirection;

            if (gridManager.IsWithinGridBounds(knockbackPosition) &&
                !gridManager.IsObstaclePosition(knockbackPosition) &&
                !gridManager.IsCharacterPosition(knockbackPosition) &&
                !gridManager.IsEnemyPosition(knockbackPosition) &&
                !gridManager.IsSylphPosition(knockbackPosition))
            {
                gridManager.RemoveEnemyPosition(enemyTarget.CurrentGridPosition);
                enemyTarget.CurrentGridPosition = knockbackPosition;
                gridManager.AddEnemyPosition(knockbackPosition, enemyTarget.gameObject);
                Debug.Log($"{enemyTarget.name}이(가) {knockbackDirection} 방향으로 한 칸 넉백되었습니다. 현재 위치: {enemyTarget.CurrentGridPosition}");
            }
            else
            {
                // 넉백 대상 추가
                collisionTargets.Add(target);

                // 충돌 대상 처리
                if (gridManager.IsCharacterPosition(knockbackPosition))
                {
                    var collidedCharacter = gridManager.GetCharacterAtPosition(knockbackPosition);
                    if (collidedCharacter != null)
                    {
                        collisionTargets.Add(collidedCharacter);
                    }
                }

                if (gridManager.IsEnemyPosition(knockbackPosition))
                {
                    var collidedEnemy = gridManager.GetEnemyAtPosition(knockbackPosition);
                    if (collidedEnemy != null)
                    {
                        collisionTargets.Add(collidedEnemy);
                    }
                }

                if (gridManager.IsObstaclePosition(knockbackPosition))
                {
                    Debug.Log($"장애물에 충돌: {target}");
                }
            }
        }
    }
    private IEnumerator KnockbackCoroutine(object target, Vector3 targetPosition, float duration)
    {
        float elapsedTime = 0;
        Vector3 startingPosition;

        // 타겟의 시작 위치를 가져오기
        if (target is CharacterBase characterTarget)
        {
            startingPosition = characterTarget.transform.position;
        }
        else if (target is EnemyBase enemyTarget)
        {
            startingPosition = enemyTarget.transform.position;
        }
        else
        {
            Debug.LogError("Invalid target for knockback.");
            yield break;
        }

        // 부드러운 이동 처리
        while (elapsedTime < duration)
        {
            if (target is CharacterBase character)
            {
                character.transform.position = Vector3.Lerp(startingPosition, new Vector3(targetPosition.x, startingPosition.y, targetPosition.z), elapsedTime / duration);
                character.transform.rotation = Quaternion.LookRotation((transform.position - character.transform.position).normalized, Vector3.up);
            }
            else if (target is EnemyBase enemy)
            {
                enemy.transform.position = Vector3.Lerp(startingPosition, new Vector3(targetPosition.x, startingPosition.y, targetPosition.z), elapsedTime / duration);
                enemy.transform.rotation = Quaternion.LookRotation((transform.position - enemy.transform.position).normalized, Vector3.up);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 이동을 정확히 완료
        if (target is CharacterBase characterFinal)
        {
            characterFinal.transform.position = new Vector3(targetPosition.x, startingPosition.y, targetPosition.z);
        }
        else if (target is EnemyBase enemyFinal)
        {
            enemyFinal.transform.position = new Vector3(targetPosition.x, startingPosition.y, targetPosition.z);
        }

        // 충돌 대상 피해 적용
        foreach (var obj in collisionTargets)
        {
            if (obj is CharacterBase collidedCharacter)
            {
                collidedCharacter.TakeDamage(1);
                Debug.Log($"{collidedCharacter.name}이(가) 넉백 충돌로 인해 피해를 입었습니다.");
            }
            else if (obj is EnemyBase collidedEnemy)
            {
                collidedEnemy.TakeDamage(1);
                Debug.Log($"{collidedEnemy.name}이(가) 넉백 충돌로 인해 피해를 입었습니다.");
            }
        }

        // 충돌 리스트 초기화
        collisionTargets.Clear();
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

            if (gridManager.IsEnemyPosition(position) || gridManager.IsCharacterPosition(position) || gridManager.IsSylphPosition(position) || gridManager.IsObstaclePosition(position))
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
            else if (gridManager.IsEnemyPosition(gridPosition))
            {
                EnemyBase enemy = gridManager.GetEnemyAtPosition(gridPosition);
                if (enemy != null && enemy.HP > 0)
                {
                    enemyTargets.Add(enemy);
                    Debug.Log($"적 감지됨: {enemy.name}, 위치: ({gridPosition.x}, {gridPosition.y})");
                }
            }
        }

        // 각 캐릭터의 타겟 리스트를 TurnManager에 저장
        turnManager.SetCharacterTargets(this, characterTargets, enemyTargets);
        ClearIndicators(); // 기존 인디케이터 제거

        // 타겟 리스트의 모든 대상 넉백 시키기
        foreach (var target in characterTargets.Concat<object>(enemyTargets)) // 두 리스트를 통합
        {
            if (target is CharacterBase character)
            {
                // 워리어를 타겟의 앞칸으로 이동
                Vector2Int targetPosition = character.GetCurrentGridPosition(); // 타겟의 현재 위치
                Vector2Int direction = targetPosition - this.GetCurrentGridPosition(); // 워리어에서 타겟까지의 방향
                Vector2Int moveToPosition = targetPosition - new Vector2Int(
                    Mathf.Clamp(direction.x, -1, 1),
                    Mathf.Clamp(direction.y, -1, 1)); // 워리어의 이동 위치 계산 (타겟의 바로 앞)

                if (gridManager.IsWithinGridBounds(moveToPosition) &&
                    !gridManager.IsCharacterPosition(moveToPosition) &&
                    !gridManager.IsObstaclePosition(moveToPosition))
                {
                    gridManager.RemoveCharacterFromGrid(this);
                    this.SetGridPosition(moveToPosition);
                    gridManager.AddCharacterToGrid(moveToPosition, this);
                    Debug.Log($"{this.name}이(가) 타겟의 앞칸 ({moveToPosition.x}, {moveToPosition.y})으로 이동했습니다.");
                }
                else
                {
                    Debug.Log($"워리어가 이동할 위치 ({moveToPosition.x}, {moveToPosition.y})가 유효하지 않습니다.");
                }

                // 캐릭터 밀기(넉백)
                ApplyKnockback(character);
            }
            else if (target is EnemyBase enemy)
            {
                // 워리어를 적 타겟의 앞칸으로 이동
                Vector2Int targetPosition = enemy.CurrentGridPosition; // 타겟의 현재 위치
                Vector2Int direction = targetPosition - this.GetCurrentGridPosition(); // 워리어에서 타겟까지의 방향
                Vector2Int moveToPosition = targetPosition - new Vector2Int(
                    Mathf.Clamp(direction.x, -1, 1),
                    Mathf.Clamp(direction.y, -1, 1)); // 워리어의 이동 위치 계산 (타겟의 바로 앞)

                if (gridManager.IsWithinGridBounds(moveToPosition) &&
                    !gridManager.IsCharacterPosition(moveToPosition) &&
                    !gridManager.IsObstaclePosition(moveToPosition) &&
                    !gridManager.IsEnemyPosition(moveToPosition)) // 적 앞칸도 적이 없어야 이동 가능
                {
                    gridManager.RemoveCharacterFromGrid(this);
                    this.SetGridPosition(moveToPosition);
                    gridManager.AddCharacterToGrid(moveToPosition, this);
                    Debug.Log($"{this.name}이(가) 적 타겟의 앞칸 ({moveToPosition.x}, {moveToPosition.y})으로 이동했습니다.");
                }
                else
                {
                    Debug.Log($"워리어가 적 타겟의 앞칸으로 이동 실패: ({moveToPosition.x}, {moveToPosition.y})");
                }

                // 적 밀기(넉백)
                ApplyKnockback(enemy);
            }
        }
    }
}
