using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Warrior : CharacterBase
{
    private Animator Warrioranimator;

    private List<object> collisionTargets = new List<object>(); // ĳ���Ϳ� ���� ��� ����

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

            // �˹� ���� �ð������� ������ �̵�
            if (turnManager.characterTargetMap.TryGetValue(this, out var targets))
            {
                var (characterTargets, enemyTargets) = targets;

                // ĳ���� Ÿ�� ó��
                foreach (var target in characterTargets)
                {
                    // ������ �̵� �ð��� ó��
                    Vector3 warriorWorldPosition = gridManager.GetWorldPositionFromGrid(this.GetCurrentGridPosition());
                    StartCoroutine(MoveWarriorToPosition(warriorWorldPosition, 0.3f)); // 0.3�� ���� �̵� �ִϸ��̼�

                    // ĳ���� �˹� ó��
                    Vector3 knockbackWorldPosition = gridManager.GetWorldPositionFromGrid(target.GetCurrentGridPosition());
                    StartCoroutine(DelayedKnockback(target, knockbackWorldPosition, 0.3f, 0.5f)); // 0.3�� �� �˹� ����
                }

                // �� Ÿ�� ó��
                foreach (var target in enemyTargets)
                {
                    // ������ �̵� �ð��� ó��
                    Vector3 warriorWorldPosition = gridManager.GetWorldPositionFromGrid(this.GetCurrentGridPosition());
                    StartCoroutine(MoveWarriorToPosition(warriorWorldPosition, 0.3f)); // 0.3�� ���� �̵� �ִϸ��̼�

                    // �� �˹� ó��
                    Vector3 knockbackWorldPosition = gridManager.GetWorldPositionFromGrid(target.CurrentGridPosition);
                    StartCoroutine(DelayedKnockback(target, knockbackWorldPosition, 0.3f, 0.5f)); // 0.3�� �� �˹� ����
                }
            }
        }
        else if (skillNumber == 1)
        {
            // �⺻ ��� ȿ�� ����
            Shield += 1;
            Debug.Log("�������� ���� +1");
        }
    }
    private IEnumerator MoveWarriorToPosition(Vector3 targetPosition, float duration)
    {
        float elapsedTime = 0;
        Vector3 startingPosition = transform.position; // �������� ���� ��ġ ����
        float fixedY = startingPosition.y; // Y ��ǥ�� ����

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(
                startingPosition,
                new Vector3(targetPosition.x, fixedY, targetPosition.z), // Y ��ǥ ����
                elapsedTime / duration
            );
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = new Vector3(targetPosition.x, fixedY, targetPosition.z); // ���� ��ġ���� Y ��ǥ ����
    }

    private IEnumerator DelayedKnockback(object target, Vector3 knockbackPosition, float delay, float duration)
    {
        yield return new WaitForSeconds(delay); // ������ �̵� �ð� ���

        if (target is CharacterBase characterTarget)
        {
            // CharacterBase�� ���� �˹� ó��
            StartCoroutine(KnockbackCoroutine(characterTarget, knockbackPosition, duration));
        }
        else if (target is EnemyBase enemyTarget)
        {
            // EnemyBase�� ���� �˹� ó��
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
            // ĳ���� �˹� ó��
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
                // �˹� ��� �߰�
                collisionTargets.Add(target);

                // �浹 ��� ó��
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
                    Debug.Log($"��ֹ��� �浹: {target}");
                }
            }
        }
        else if (target is EnemyBase enemyTarget)
        {
            // �� �˹� ó��
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
                // �˹� ��� �߰�
                collisionTargets.Add(target);

                // �浹 ��� ó��
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
                    Debug.Log($"��ֹ��� �浹: {target}");
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
            Debug.LogError("Invalid target for knockback.");
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

            if (gridManager.IsEnemyPosition(position) || gridManager.IsCharacterPosition(position) || gridManager.IsSylphPosition(position) || gridManager.IsObstaclePosition(position))
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

        // Ÿ�� ����Ʈ�� ��� ��� �˹� ��Ű��
        foreach (var target in characterTargets.Concat<object>(enemyTargets)) // �� ����Ʈ�� ����
        {
            if (target is CharacterBase character)
            {
                // ����� Ÿ���� ��ĭ���� �̵�
                Vector2Int targetPosition = character.GetCurrentGridPosition(); // Ÿ���� ���� ��ġ
                Vector2Int direction = targetPosition - this.GetCurrentGridPosition(); // ������� Ÿ�ٱ����� ����
                Vector2Int moveToPosition = targetPosition - new Vector2Int(
                    Mathf.Clamp(direction.x, -1, 1),
                    Mathf.Clamp(direction.y, -1, 1)); // �������� �̵� ��ġ ��� (Ÿ���� �ٷ� ��)

                if (gridManager.IsWithinGridBounds(moveToPosition) &&
                    !gridManager.IsCharacterPosition(moveToPosition) &&
                    !gridManager.IsObstaclePosition(moveToPosition))
                {
                    gridManager.RemoveCharacterFromGrid(this);
                    this.SetGridPosition(moveToPosition);
                    gridManager.AddCharacterToGrid(moveToPosition, this);
                    Debug.Log($"{this.name}��(��) Ÿ���� ��ĭ ({moveToPosition.x}, {moveToPosition.y})���� �̵��߽��ϴ�.");
                }
                else
                {
                    Debug.Log($"����� �̵��� ��ġ ({moveToPosition.x}, {moveToPosition.y})�� ��ȿ���� �ʽ��ϴ�.");
                }

                // ĳ���� �б�(�˹�)
                ApplyKnockback(character);
            }
            else if (target is EnemyBase enemy)
            {
                // ����� �� Ÿ���� ��ĭ���� �̵�
                Vector2Int targetPosition = enemy.CurrentGridPosition; // Ÿ���� ���� ��ġ
                Vector2Int direction = targetPosition - this.GetCurrentGridPosition(); // ������� Ÿ�ٱ����� ����
                Vector2Int moveToPosition = targetPosition - new Vector2Int(
                    Mathf.Clamp(direction.x, -1, 1),
                    Mathf.Clamp(direction.y, -1, 1)); // �������� �̵� ��ġ ��� (Ÿ���� �ٷ� ��)

                if (gridManager.IsWithinGridBounds(moveToPosition) &&
                    !gridManager.IsCharacterPosition(moveToPosition) &&
                    !gridManager.IsObstaclePosition(moveToPosition) &&
                    !gridManager.IsEnemyPosition(moveToPosition)) // �� ��ĭ�� ���� ����� �̵� ����
                {
                    gridManager.RemoveCharacterFromGrid(this);
                    this.SetGridPosition(moveToPosition);
                    gridManager.AddCharacterToGrid(moveToPosition, this);
                    Debug.Log($"{this.name}��(��) �� Ÿ���� ��ĭ ({moveToPosition.x}, {moveToPosition.y})���� �̵��߽��ϴ�.");
                }
                else
                {
                    Debug.Log($"����� �� Ÿ���� ��ĭ���� �̵� ����: ({moveToPosition.x}, {moveToPosition.y})");
                }

                // �� �б�(�˹�)
                ApplyKnockback(enemy);
            }
        }
    }
}
