using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Priest : CharacterBase
{
    private Animator Priestanimator;
    private List<object> collisionTargets = new List<object>(); // 캐릭터와 적을 모두 저장
    public GameObject ProjectilePrefab;

    protected override void Start()
    {
        base.Start();
        IsRanged = true;

        // Animator 초기화
        Priestanimator = GetComponent<Animator>();
    }

    public override void UseSkill(int skillNumber, TurnManager turnManager)
    {
        if (skillNumber == 1)
        {
            Priestanimator.SetTrigger("skill1");
            int damage = Damage - 1; // 데미지를 기본 공격력보다 1 낮게 설정
            FireProjectile(GetGridPosition(transform.position), GetForwardDirection(), 4, ProjectilePrefab, turnManager, damage);

            // 넉백 후 이동 애니메이션 적용
            if (turnManager.characterTargetMap.TryGetValue(this, out var targets))
            {
                var (characterTargets, enemyTargets) = targets;
                foreach (var target in characterTargets)
                {
                    Vector3 knockbackWorldPosition = gridManager.GetWorldPositionFromGrid(target.GetCurrentGridPosition());
                    StartCoroutine(KnockbackCoroutine(target, knockbackWorldPosition, 0.5f)); // 넉백 애니메이션 실행 (0.5초 동안)
                }
                // 적 타겟 처리
                foreach (var target in enemyTargets)
                {
                    Vector3 knockbackWorldPosition = gridManager.GetWorldPositionFromGrid(target.CurrentGridPosition);
                    StartCoroutine(KnockbackCoroutine(target, knockbackWorldPosition, 0.5f));
                }
            }
        }
        else if (skillNumber == 2)
        {
            Priestanimator.SetTrigger("skill2"); 
            // else: 기본 회복 효과
            if (turnManager.characterTargetMap.TryGetValue(this, out var targets))
            {
                var (characterTargets, _) = targets;

                foreach (var ally in characterTargets)
                {
                    // 아군의 체력만 회복
                    ally.Health = Mathf.Min(ally.Health + 1, ally.MaxHealth); // 체력을 1 증가시키며 최대 체력을 초과하지 않음
                    Debug.Log($"{ally.name}의 체력 +1");
                }
            }
        }
    }

    private void FireProjectile(Vector2Int pathOrigin, Vector2Int pathDirection, int attackRange, GameObject projectilePrefab, TurnManager turnManager, int damage)
    {
        if (projectilePrefab == null)
        {
            return;
        }

        // 투사체 생성 위치 설정
        Vector3 spawnPosition = gridOrigin + new Vector3(pathOrigin.x * cellWidth, pathIndicatorHeight, pathOrigin.y * cellHeight);
        Vector3 directionOffset = new Vector3(pathDirection.x * 0.5f, 0, pathDirection.y * 0.5f); // 방향으로 약간 앞 이동
        spawnPosition += directionOffset; // 보정된 위치
        spawnPosition.y = 1.5f; // Y 좌표 고정값

        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

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
    }

    private Vector2Int GetForwardDirection()
    {
        // 캐릭터가 바라보는 전방 방향 계산
        Vector3 forward = transform.forward;
        int x = Mathf.RoundToInt(forward.x);
        int z = Mathf.RoundToInt(forward.z);
        return new Vector2Int(x, z);
    }

    private void ApplyKnockback(object target)
    {
        Vector2Int direction, knockbackDirection, knockbackPosition;

        if (target is CharacterBase characterTarget)
        {
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
                // 대상 자신을 collisionTargets에 추가
                if (!collisionTargets.Contains(target)) // 중복 추가 방지
                {
                    collisionTargets.Add(target);
                }

                // 넉백 위치에 존재하는 대상을 collisionTargets에 추가
                if (gridManager.IsCharacterPosition(knockbackPosition))
                {
                    var collidedCharacter = gridManager.GetCharacterAtPosition(knockbackPosition);
                    if (collidedCharacter != null && !collisionTargets.Contains(collidedCharacter))
                    {
                        collisionTargets.Add(collidedCharacter);
                        Debug.Log($"{collidedCharacter.name}이(가) 넉백 위치에서 충돌로 인해 추가되었습니다.");
                    }
                }

                if (gridManager.IsEnemyPosition(knockbackPosition))
                {
                    var collidedEnemy = gridManager.GetEnemyAtPosition(knockbackPosition);
                    if (collidedEnemy != null && !collisionTargets.Contains(collidedEnemy))
                    {
                        collisionTargets.Add(collidedEnemy);
                        Debug.Log($"{collidedEnemy.name}이(가) 넉백 위치에서 충돌로 인해 추가되었습니다.");
                    }
                }
            }
        }
        else if (target is EnemyBase enemyTarget)
        {
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
                // 대상 자신을 collisionTargets에 추가
                if (!collisionTargets.Contains(target)) // 중복 추가 방지
                {
                    collisionTargets.Add(target);
                }

                // 넉백 위치에 존재하는 대상을 collisionTargets에 추가
                if (gridManager.IsCharacterPosition(knockbackPosition))
                {
                    var collidedCharacter = gridManager.GetCharacterAtPosition(knockbackPosition);
                    if (collidedCharacter != null && !collisionTargets.Contains(collidedCharacter))
                    {
                        collisionTargets.Add(collidedCharacter);
                        Debug.Log($"{collidedCharacter.name}이(가) 넉백 위치에서 충돌로 인해 추가되었습니다.");
                    }
                }

                if (gridManager.IsEnemyPosition(knockbackPosition))
                {
                    var collidedEnemy = gridManager.GetEnemyAtPosition(knockbackPosition);
                    if (collidedEnemy != null && !collisionTargets.Contains(collidedEnemy))
                    {
                        collisionTargets.Add(collidedEnemy);
                        Debug.Log($"{collidedEnemy.name}이(가) 넉백 위치에서 충돌로 인해 추가되었습니다.");
                    }
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

        int effectiveRange = mana >= 6 ? 1 : attackRange;

        for (int i = 1; i <= effectiveRange; i++)
        {
            Vector2Int position = pathOrigin + pathDirection * i;

            if (!gridManager.IsWithinGridBounds(position))
            {
                primaryTargetPosition = pathOrigin + pathDirection * (i - 1);
                break;
            }

            if (gridManager.IsEnemyPosition(position) || gridManager.IsCharacterPosition(position) || gridManager.IsObstaclePosition(position))
            {
                primaryTargetPosition = position;
                break;
            }

            primaryTargetPosition = position;
        }

        if (!primaryTargetPosition.HasValue)
        {
            primaryTargetPosition = pathOrigin + pathDirection * effectiveRange;
        }

        List<Vector2Int> secondaryPositions;
        if (mana >= 6)
        {
            secondaryPositions = GetBehindPatternPositions(primaryTargetPosition.Value, pathDirection);
        }
        else
        {
            secondaryPositions = GetPiercingPositions(primaryTargetPosition.Value, pathDirection);
        }

        secondaryPositions.Add(primaryTargetPosition.Value);

        foreach (var pos in secondaryPositions)
        {
            if (gridManager.IsWithinGridBounds(pos))
            {
                Vector3 indicatorPos = gridOrigin + new Vector3(pos.x * cellWidth, pathIndicatorHeight, pos.y * cellHeight);
                GameObject secondaryIndicator = Instantiate(indicatorPrefab, indicatorPos, Quaternion.identity);

                if (mana >= 6)
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
                else if (mana <= 5)
                {
                    if (gridManager.IsEnemyPosition(pos) || gridManager.IsCharacterPosition(pos))
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
            Priestanimator.SetBool("isMoving", true);

            // 회전
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 50);
                yield return null;
            }

            // 이동
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 7);
                yield return null;
            }

            yield return new WaitForSeconds(0.15f); // 각 위치에서 대기 시간
        }
        Priestanimator.SetBool("isMoving", false); // 이동 애니메이션 종료
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

            if (gridManager.IsCharacterPosition(gridPosition))
            {
                CharacterBase character = gridManager.GetCharacterAtPosition(gridPosition);
                if (character != null && character.Health > 0 && character != this)
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


        // 마나 조건: 2 이상 6 미만일 때만 넉백 실행
        if (mana >= 2 && mana < 6)
        {
            foreach (var target in characterTargets.Concat<object>(enemyTargets)) // 두 리스트를 통합
            {
                if (target is CharacterBase character)
                {
                    // 캐릭터 밀기(넉백)
                    ApplyKnockback(character);
                }
                else if (target is EnemyBase enemy)
                {
                    // 적 밀기(넉백)
                    ApplyKnockback(enemy);
                }
            }
        }
        else
        {
            Debug.Log($"마나 조건 불충족: 현재 마나 {mana}, 넉백 실행 안 함.");
        }
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
            Priestanimator.SetTrigger("hit"); // 피격 애니메이션 재생
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
        Priestanimator.SetTrigger("die"); // 사망 애니메이션 재생
        base.Die(); // 기본 사망 로직 실행
    }
}
