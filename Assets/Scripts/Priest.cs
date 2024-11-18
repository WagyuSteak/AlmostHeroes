using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Priest : CharacterBase
{
    private Animator Priestanimator;
    private List<object> collisionTargets = new List<object>(); // ĳ���Ϳ� ���� ��� ����
    public GameObject ProjectilePrefab;

    protected override void Start()
    {
        base.Start();
        IsRanged = true;

        // Animator �ʱ�ȭ
        Priestanimator = GetComponent<Animator>();
    }

    public override void UseSkill(int skillNumber, TurnManager turnManager)
    {
        if (skillNumber == 1)
        {
            Priestanimator.SetTrigger("skill1");
            int damage = Damage - 1; // �������� �⺻ ���ݷº��� 1 ���� ����
            FireProjectile(GetGridPosition(transform.position), GetForwardDirection(), 4, ProjectilePrefab, turnManager, damage);

            // �˹� �� �̵� �ִϸ��̼� ����
            if (turnManager.characterTargetMap.TryGetValue(this, out var targets))
            {
                var (characterTargets, enemyTargets) = targets;
                foreach (var target in characterTargets)
                {
                    Vector3 knockbackWorldPosition = gridManager.GetWorldPositionFromGrid(target.GetCurrentGridPosition());
                    StartCoroutine(KnockbackCoroutine(target, knockbackWorldPosition, 0.5f)); // �˹� �ִϸ��̼� ���� (0.5�� ����)
                }
                // �� Ÿ�� ó��
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
            // else: �⺻ ȸ�� ȿ��
            if (turnManager.characterTargetMap.TryGetValue(this, out var targets))
            {
                var (characterTargets, _) = targets;

                foreach (var ally in characterTargets)
                {
                    // �Ʊ��� ü�¸� ȸ��
                    ally.Health = Mathf.Min(ally.Health + 1, ally.MaxHealth); // ü���� 1 ������Ű�� �ִ� ü���� �ʰ����� ����
                    Debug.Log($"{ally.name}�� ü�� +1");
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

        // ����ü ���� ��ġ ����
        Vector3 spawnPosition = gridOrigin + new Vector3(pathOrigin.x * cellWidth, pathIndicatorHeight, pathOrigin.y * cellHeight);
        Vector3 directionOffset = new Vector3(pathDirection.x * 0.5f, 0, pathDirection.y * 0.5f); // �������� �ణ �� �̵�
        spawnPosition += directionOffset; // ������ ��ġ
        spawnPosition.y = 1.5f; // Y ��ǥ ������

        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

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
    }

    private Vector2Int GetForwardDirection()
    {
        // ĳ���Ͱ� �ٶ󺸴� ���� ���� ���
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
                Debug.Log($"{characterTarget.name}��(��) {knockbackDirection} �������� �� ĭ �˹�Ǿ����ϴ�. ���� ��ġ: {characterTarget.GetCurrentGridPosition()}");
            }
            else
            {
                // ��� �ڽ��� collisionTargets�� �߰�
                if (!collisionTargets.Contains(target)) // �ߺ� �߰� ����
                {
                    collisionTargets.Add(target);
                }

                // �˹� ��ġ�� �����ϴ� ����� collisionTargets�� �߰�
                if (gridManager.IsCharacterPosition(knockbackPosition))
                {
                    var collidedCharacter = gridManager.GetCharacterAtPosition(knockbackPosition);
                    if (collidedCharacter != null && !collisionTargets.Contains(collidedCharacter))
                    {
                        collisionTargets.Add(collidedCharacter);
                        Debug.Log($"{collidedCharacter.name}��(��) �˹� ��ġ���� �浹�� ���� �߰��Ǿ����ϴ�.");
                    }
                }

                if (gridManager.IsEnemyPosition(knockbackPosition))
                {
                    var collidedEnemy = gridManager.GetEnemyAtPosition(knockbackPosition);
                    if (collidedEnemy != null && !collisionTargets.Contains(collidedEnemy))
                    {
                        collisionTargets.Add(collidedEnemy);
                        Debug.Log($"{collidedEnemy.name}��(��) �˹� ��ġ���� �浹�� ���� �߰��Ǿ����ϴ�.");
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
                Debug.Log($"{enemyTarget.name}��(��) {knockbackDirection} �������� �� ĭ �˹�Ǿ����ϴ�. ���� ��ġ: {enemyTarget.CurrentGridPosition}");
            }
            else
            {
                // ��� �ڽ��� collisionTargets�� �߰�
                if (!collisionTargets.Contains(target)) // �ߺ� �߰� ����
                {
                    collisionTargets.Add(target);
                }

                // �˹� ��ġ�� �����ϴ� ����� collisionTargets�� �߰�
                if (gridManager.IsCharacterPosition(knockbackPosition))
                {
                    var collidedCharacter = gridManager.GetCharacterAtPosition(knockbackPosition);
                    if (collidedCharacter != null && !collisionTargets.Contains(collidedCharacter))
                    {
                        collisionTargets.Add(collidedCharacter);
                        Debug.Log($"{collidedCharacter.name}��(��) �˹� ��ġ���� �浹�� ���� �߰��Ǿ����ϴ�.");
                    }
                }

                if (gridManager.IsEnemyPosition(knockbackPosition))
                {
                    var collidedEnemy = gridManager.GetEnemyAtPosition(knockbackPosition);
                    if (collidedEnemy != null && !collisionTargets.Contains(collidedEnemy))
                    {
                        collisionTargets.Add(collidedEnemy);
                        Debug.Log($"{collidedEnemy.name}��(��) �˹� ��ġ���� �浹�� ���� �߰��Ǿ����ϴ�.");
                    }
                }
            }
        }
    }

    private IEnumerator KnockbackCoroutine(object target, Vector3 targetPosition, float duration)
    {
        float elapsedTime = 0;
        Vector3 startingPosition;

        // Ÿ���� ���� ��ġ�� ��������
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

        // �ε巯�� �̵� ó��
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

        // �̵��� ��Ȯ�� �Ϸ�
        if (target is CharacterBase characterFinal)
        {
            characterFinal.transform.position = new Vector3(targetPosition.x, startingPosition.y, targetPosition.z);
        }
        else if (target is EnemyBase enemyFinal)
        {
            enemyFinal.transform.position = new Vector3(targetPosition.x, startingPosition.y, targetPosition.z);
        }

        // �浹 ��� ���� ����
        foreach (var obj in collisionTargets)
        {
            if (obj is CharacterBase collidedCharacter)
            {
                collidedCharacter.TakeDamage(1);
                Debug.Log($"{collidedCharacter.name}��(��) �˹� �浹�� ���� ���ظ� �Ծ����ϴ�.");
            }
            else if (obj is EnemyBase collidedEnemy)
            {
                collidedEnemy.TakeDamage(1);
                Debug.Log($"{collidedEnemy.name}��(��) �˹� �浹�� ���� ���ظ� �Ծ����ϴ�.");
            }
        }

        // �浹 ����Ʈ �ʱ�ȭ
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
            Priestanimator.SetBool("isMoving", true);

            // ȸ��
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 50);
                yield return null;
            }

            // �̵�
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 7);
                yield return null;
            }

            yield return new WaitForSeconds(0.15f); // �� ��ġ���� ��� �ð�
        }
        Priestanimator.SetBool("isMoving", false); // �̵� �ִϸ��̼� ����
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

            if (gridManager.IsCharacterPosition(gridPosition))
            {
                CharacterBase character = gridManager.GetCharacterAtPosition(gridPosition);
                if (character != null && character.Health > 0 && character != this)
                {
                    characterTargets.Add(character);
                    Debug.Log($"ĳ���� ������: {character.name}, ��ġ: ({gridPosition.x}, {gridPosition.y})");
                }
            }
            else if (gridManager.IsEnemyPosition(gridPosition))
            {
                EnemyBase enemy = gridManager.GetEnemyAtPosition(gridPosition);
                if (enemy != null && enemy.HP > 0)
                {
                    enemyTargets.Add(enemy);
                    Debug.Log($"�� ������: {enemy.name}, ��ġ: ({gridPosition.x}, {gridPosition.y})");
                }
            }
        }

        // �� ĳ������ Ÿ�� ����Ʈ�� TurnManager�� ����
        turnManager.SetCharacterTargets(this, characterTargets, enemyTargets);
        ClearIndicators(); // ���� �ε������� ����


        // ���� ����: 2 �̻� 6 �̸��� ���� �˹� ����
        if (mana >= 2 && mana < 6)
        {
            foreach (var target in characterTargets.Concat<object>(enemyTargets)) // �� ����Ʈ�� ����
            {
                if (target is CharacterBase character)
                {
                    // ĳ���� �б�(�˹�)
                    ApplyKnockback(character);
                }
                else if (target is EnemyBase enemy)
                {
                    // �� �б�(�˹�)
                    ApplyKnockback(enemy);
                }
            }
        }
        else
        {
            Debug.Log($"���� ���� ������: ���� ���� {mana}, �˹� ���� �� ��.");
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
            Debug.Log($"{name}�� ���尡 {shieldAbsorbed}��ŭ ���ظ� ����߽��ϴ�. ���� ����: {Shield}");
        }

        if (damageToApply > 0)
        {
            Priestanimator.SetTrigger("hit"); // �ǰ� �ִϸ��̼� ���
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
        Priestanimator.SetTrigger("die"); // ��� �ִϸ��̼� ���
        base.Die(); // �⺻ ��� ���� ����
    }
}
