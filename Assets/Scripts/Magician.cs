using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magician : CharacterBase
{
    public GameObject skill1ProjectilePrefab; // 스킬 1의 투사체 프리팹
    public GameObject skill2ProjectilePrefab; // 스킬 2의 투사체 프리팹

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
            Magiciananimator.SetTrigger("firebomb"); // Firebomb 애니메이션 재생
            int damage = Damage + 1; // 스킬 2 데미지
            FireProjectile(GetGridPosition(transform.position), GetForwardDirection(), 4, skill2ProjectilePrefab, turnManager, damage);
        }
        else if (skillNumber == 1)
        {
            Magiciananimator.SetTrigger("fireball"); // Fireball 애니메이션 재생
            int damage = Damage; // 스킬 1 데미지
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

        // 투사체 생성 위치 설정
        Vector3 spawnPosition = gridOrigin + new Vector3(pathOrigin.x * cellWidth, pathIndicatorHeight, pathOrigin.y * cellHeight);
        Vector3 directionOffset = new Vector3(pathDirection.x * 0.5f, 0, pathDirection.y * 0.5f); // 방향으로 약간 앞 이동
        spawnPosition += directionOffset; // 보정된 위치
        spawnPosition.y = 1.5f; // Y 좌표 고정값

        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Projectile created at {spawnPosition} with offset {directionOffset}");

        // 최종 위치 계산
        Vector2Int targetGridPosition = CalculateProjectileTarget(pathOrigin, pathDirection, attackRange);
        Vector3 targetWorldPosition = gridOrigin + new Vector3(targetGridPosition.x * cellWidth, pathIndicatorHeight, targetGridPosition.y * cellHeight);

        // 투사체 이동 시작
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

            targetPosition = position; // 계속 업데이트
        }

        if (!targetPosition.HasValue)
        {
            targetPosition = pathOrigin + pathDirection * attackRange;
        }

        return targetPosition.Value;
    }

    private IEnumerator MoveProjectile(GameObject projectile, Vector3 targetPosition, TurnManager turnManager, int damage)
    {
        float speed = 5f; // 투사체 속도

        // Y 좌표 고정
        float fixedY = projectile.transform.position.y;
        targetPosition.y = fixedY; // 목표 위치의 Y 좌표 고정

        while (Vector3.Distance(projectile.transform.position, targetPosition) > 0.1f)
        {
            // 현재 Y 좌표를 유지하면서 이동
            Vector3 currentPosition = projectile.transform.position;
            currentPosition = Vector3.MoveTowards(currentPosition, targetPosition, speed * Time.deltaTime);
            currentPosition.y = fixedY; // Y 좌표 고정
            projectile.transform.position = currentPosition;

            yield return null;
        }

        // 투사체가 목표 위치에 도달한 후 데미지 적용
        ApplyDamageToTargets(damage, turnManager);

        // 투사체 파괴
        Destroy(projectile);
        Debug.Log($"Projectile reached target at {targetPosition} and was destroyed");
    }

    private Vector2Int GetForwardDirection()
    {
        // 캐릭터가 바라보는 전방 방향 계산
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
            if (gridManager.IsEnemyPosition(position) || gridManager.IsCharacterPosition(position) || gridManager.IsSylphPosition(position) || gridManager.IsObstaclePosition(position))
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

    public override IEnumerator StartMovement()
    {
        // 첫 번째 활성화된 셀을 건너뛰기 위해 i를 1로 설정
        for (int i = 1; i < activatedCells.Count; i++)
        {
            Vector2Int cellPosition = activatedCells[i];
            Vector3 targetPosition = gridOrigin + new Vector3(cellPosition.x * cellWidth, 0.55f, cellPosition.y * cellHeight);
            Vector3 direction = (targetPosition - transform.position).normalized;
            Quaternion targetRotation = Quaternion.Euler(0, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)).eulerAngles.y, 0);

            // 이동 애니메이션 시작
            Magiciananimator.SetBool("isMoving", true);
            yield return new WaitForSeconds(0.85f); // 애니메이션이 시작되도록 잠깐 대기

            // 회전
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 30);
                yield return null;
            }

            // 이동
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 7);
                yield return null;
            }

            yield return new WaitForSeconds(0.25f); // 각 위치에서 대기 시간
        }
        Magiciananimator.SetBool("isMoving", false); // 이동 애니메이션 종료
        ClearIndicators(); // 이동이 끝난 후 인디케이터 초기화
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
                    Debug.Log($"적 감지됨: {enemy.name}, 위치: ({gridPosition.x}, {gridPosition.y})");
                }
            }
            else if (gridManager.IsCharacterPosition(gridPosition))
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

    public override void TakeDamage(int damageAmount)
    {
        int damageToApply = damageAmount;

        if (Shield > 0)
        {
            int shieldAbsorbed = Mathf.Min(Shield, damageToApply);
            Shield -= shieldAbsorbed;
            damageToApply -= shieldAbsorbed;
            Debug.Log($"{name}의 쉴드가 {shieldAbsorbed}만큼 피해를 흡수했습니다. 남은 쉴드: {Shield}");
        }

        if (damageToApply > 0)
        {
            Magiciananimator.SetTrigger("hit"); // 피격 애니메이션 재생
            Health -= damageToApply;
            Debug.Log($"{name}이(가) {damageToApply}의 피해를 입었습니다. 남은 체력: {Health}");
        }

        if (Health <= 0)
        {
            Die();
        }
    }
    public override void Die()
    {
        Magiciananimator.SetTrigger("die"); // 사망 애니메이션 재생
        base.Die(); // 기본 사망 로직 실행
    }
}
