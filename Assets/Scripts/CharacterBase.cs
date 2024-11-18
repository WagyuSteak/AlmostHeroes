using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

public class CharacterBase : MonoBehaviour
{
    protected Sylph sylph;
    public Sprite icon; // ĳ���� ������
    public Sprite skillIcon1; // ù ��° ��ų ������
    public Sprite skillIcon2; // �� ��° ��ų ������
    public Sprite Synergy; // �ó��� ������
    public GridManager gridManager; // �׸��� �Ŵ��� ���� - �׸��� ���� ����
    public TurnManager turnManager; // �� �Ŵ��� ���� - ���� ����
    private UIManager uiManager;

    public bool hasMoved = false; // ĳ���Ͱ� �̵��� �ߴ��� ���θ� ��Ÿ���� �÷���
    public int mana; // ���� ĳ������ ���� ��ġ
    public int Health; // ���� ĳ������ ü��
    public int MaxHealth; // ĳ������ �ִ� ü��
    public int MoveCount = 10; // �̵� ���� Ƚ��
    public int Shield;
    public int Damage;
    public int attackRange; // ��Ÿ� ����
    public int Receivemana;
    public int ReceivemoveCount;


    public float cellWidth; // �׸��� ���� �ʺ�
    public float cellHeight; // �׸��� ���� ����
    
    
    public Vector3 gridOrigin; // �׸����� ���� ��ǥ ���� ���� ��ġ

    public GameObject indicatorPrefab; // �ε������� ������
    public Material enemyMaterial;
    public Material emptyMaterial;
    public Material BuffMaterial;
    public List<GameObject> indicators = new List<GameObject>();

    public bool IsRanged; //���Ÿ����� �ٰŸ������� ����

    // �̵� �� ������ ���� ������
    public GameObject pointerPrefab; // ���콺 �����ͷ� ����� ������
    public GameObject pathIndicatorPrefab; // ��θ� ǥ���ϴ� ������Ʈ ������ 
    public float pathIndicatorHeight = 1.5f; // ��� ǥ�� ������Ʈ�� Y�� ����

    public Camera mainCamera; // ���� ī�޶� ���� - ���콺 ��ġ�� ȭ�� �󿡼� �����ϱ� ���� ���
    public LayerMask cellLayerMask; // ���� �����ϱ� ���� ���̾� ����ũ

    protected bool isControllableCharacter = false; // ĳ���Ͱ� ���� ���� ������ �������� ����
    protected GameObject currentPointer; // ���� ���콺 ��ġ�� ��Ÿ���� ������ ������Ʈ

    public GameObject circlePrefab; // ���׶� ������
    public GameObject linePrefab;   // ���� ������

    public List<GameObject> pathCircles = new List<GameObject>(); // ��� ���׶� ������Ʈ ����Ʈ
    public List<GameObject> pathLines = new List<GameObject>(); // ��� ���� ������Ʈ ����Ʈ
    public List<Vector2Int> activatedCells = new List<Vector2Int>(); // ĳ���Ͱ� Ȱ��ȭ�� �� ����Ʈ
    public Vector2Int currentGridPosition; // ���� �׸��� ��ġ
    public Vector2Int startGridPosition; // ���� �׸��� ��ġ
    public bool isMyTurn = false; // ���� ĳ������ �� ���θ� ��Ÿ��

    protected virtual void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        turnManager = FindObjectOfType<TurnManager>();

        mainCamera = Camera.main; // ���� ī�޶� �ڵ� �Ҵ�
        Health = MaxHealth;

        // �׸��� �Ŵ����� ������ ��� �׸��� �⺻ ���� ������
        if (gridManager != null)
        {
            gridOrigin = gridManager.gridOrigin;
            cellWidth = gridManager.cellWidth;
            cellHeight = gridManager.cellHeight;
        }
    }


    public virtual void SetGridPosition(Vector2Int position)
    {
        // ���ο� ��ġ ���
        currentGridPosition = position;
        gridManager.AddCharacterToGrid(position, this); // ������ ��ġ�� ���
    }

    public virtual void ResetGridPosition()
    {
        currentGridPosition = Vector2Int.zero; // �Ǵ� �⺻ ��ġ�� �ʱ�ȭ
    }


    public virtual Vector2Int GetCurrentGridPosition()
    {
        return currentGridPosition; // ���� ��ġ�� �׻� finalGridPosition�� ��ȯ
    }

    public virtual void ReceiveMana(int amount)
    {
        mana += amount;
        Debug.Log($"{name}��(��) {amount}�� ������ �޾ҽ��ϴ�. ���� ����: {mana}");
    }

    // ĳ���� ���� ���� ���� ����
    public virtual void SetControllable(bool canControl)
    {
        isControllableCharacter = canControl;
        if (!canControl)
        {
            hasMoved = false; // �� ���� �� �̵� ���� �ʱ�ȭ
        }
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellWidth); // ���� ��ǥ�� �׸��� x��ǥ�� ��ȯ
        int z = Mathf.RoundToInt((worldPosition.z - gridOrigin.z) / cellHeight); // ���� ��ǥ�� �׸��� z��ǥ�� ��ȯ
        return new Vector2Int(x, z); // �׸��� ��ǥ ��ȯ
    }

    protected virtual void Update()
    {
        if (isControllableCharacter) // �� ���� ���� �Է��� ó��
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClick(); // ���콺 Ŭ�� ó��
            }

            if (Input.GetMouseButton(0) && currentPointer != null)
            {
                HandlePointerMovement(); // ������ �̵� ó��
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (currentPointer != null)
                {
                    Destroy(currentPointer); // ������ ������Ʈ ����
                }
                if (activatedCells.Count > 1) // ���ǿ� ���� 1���� ū�� Ȯ��
                {
                    turnManager.EndCharacterTurn(); // EndCharacterTurn ȣ��
                }
            }
        }
    }
    public void UndoAllActivatedCells()
    {
        if (activatedCells.Count > 0)
        {
            // ��� Ȱ��ȭ�� �� ����
            gridManager.DeactivateCellsForCharacter(activatedCells);
            gridManager.ClearActivatedCells();
            activatedCells.Clear();
            ClearIndicators();

            // ��� ��ο� �ε������� ����
            foreach (var circle in pathCircles)
            {
                Destroy(circle);
            }
            pathCircles.Clear();

            foreach (var line in pathLines)
            {
                Destroy(line);
            }
            pathLines.Clear();

            mana = Receivemana; // �ʱ� ���� ������ ����
            MoveCount = ReceivemoveCount; // �ʱ� �̵� Ƚ���� ����

            currentGridPosition = startGridPosition;
            Debug.Log($"ĳ���� ���� ��ġ: {currentGridPosition}");
            Debug.Log("��� Ȱ��ȭ�� ���� ����ϰ� �ʱ�ȭ�߽��ϴ�.");
        }
    }
    protected virtual void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // ���콺 ��ġ���� ���� ����
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, cellLayerMask)) // ����ĳ��Ʈ�� �����ϸ�
        {
            Vector3 pointerPosition = hit.point; // ��Ʈ�� ��ġ ��������
            Vector2Int targetGridPosition = GetGridPosition(pointerPosition); // �׸��� ��ǥ�� ��ȯ

            if (targetGridPosition == currentGridPosition) // ���� �׸��� ��ġ�� �����ϴٸ�
            {
                // ���� ��ġ�� �����ϴ�
                currentPointer = Instantiate(pointerPrefab, pointerPosition, Quaternion.identity); // ������ ������ ����
                startGridPosition = currentGridPosition;

                if (!gridManager.IsCellActivatedByCharacter(targetGridPosition) && !activatedCells.Contains(targetGridPosition))
                {
                    ActivateCell(currentGridPosition);
                }
            }
            else
            {
                Debug.Log("�׸��� ��踦 ��� �ְų� ���� �̹� �����Ǿ� �־� �̵��� �� �����ϴ�."); // �ٸ� ��ġ�� Ŭ�� ��
            }
        }
    }

    protected virtual void HandlePointerMovement()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // ���콺 ��ġ���� ���� ����
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, cellLayerMask)) // ����ĳ��Ʈ ���� ��
        {
            Vector3 pointerPosition = hit.point;
            Vector2Int targetGridPosition = GetGridPosition(pointerPosition);

            // �� ����: ���������� Ȱ��ȭ�� �� �������� ���� ����, Ȱ��ȭ ����, ĳ����, ����, �� üũ
            if (IsOneTileAwayFromLastActivatedCell(targetGridPosition) &&
                !activatedCells.Contains(targetGridPosition) &&
                !gridManager.IsCharacterPosition(targetGridPosition) &&
                !gridManager.IsSylphPosition(targetGridPosition) &&
                !gridManager.IsEnemyPosition(targetGridPosition) &&
                gridManager.IsWithinGridBounds(targetGridPosition) &&
                !gridManager.IsObstaclePosition(targetGridPosition))
            {
                ActivateCell(targetGridPosition);
            }
        }
    }

    // ���ο� ���� �Լ�: ������ Ȱ��ȭ�� ���� ���� ���θ� ���
    protected bool IsOneTileAwayFromLastActivatedCell(Vector2Int targetGridPosition)
    {
        if (activatedCells.Count == 0) return false; // Ȱ��ȭ�� ���� ������ false ��ȯ

        Vector2Int lastActivatedCell = activatedCells[activatedCells.Count - 1]; // ������ Ȱ��ȭ ��
        int deltaX = Mathf.Abs(targetGridPosition.x - lastActivatedCell.x);
        int deltaY = Mathf.Abs(targetGridPosition.y - lastActivatedCell.y);

        return (deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1); // �� ������ 1ĭ ���������� Ȯ��
    }

    protected virtual void ActivateCell(Vector2Int targetGridPosition)
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

            // ù ���� ��� ĳ���� ��ġ�� ���׶� ������ ���� �� ���� ��ġ�� ���� ����
            if (activatedCells.Count == 1)
            {
                MoveCount++;
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

    public virtual IEnumerator StartMovement()
    {
        // ù ��° Ȱ��ȭ�� ���� �ǳʶٱ� ���� i�� 1�� ����
        for (int i = 1; i < activatedCells.Count; i++)
        {
            Vector2Int cellPosition = activatedCells[i];
            Vector3 targetPosition = gridOrigin + new Vector3(cellPosition.x * cellWidth, 0.55f, cellPosition.y * cellHeight);
            Vector3 direction = (targetPosition - transform.position).normalized;
            Quaternion targetRotation = Quaternion.Euler(0, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)).eulerAngles.y, 0);

            // ȸ��
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 30);
                yield return null;
            }

            // �̵�
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 7);
                yield return null;
            }

            yield return new WaitForSeconds(0.1f); // �� ��ġ���� ��� �ð�
        }
        ClearIndicators(); // �̵��� ���� �� �ε������� �ʱ�ȭ
        ResetCharacterSet();
    }


    // ������ ���� ��ų�� �ڵ����� �����Ͽ� ����ϴ� �޼���
    public void UseSkillBasedOnMana(TurnManager turnManager)
    {
        if (mana <= 1)
        {
            Debug.Log("��ų ��� �Ұ�: ������ �����մϴ�.");
        }
        else if (mana <= 5)
        {
            UseSkill(1, turnManager); // 1��ų ���
            Debug.Log("1��ų ���: ���� 2~5");
        }
        else
        {
            UseSkill(2, turnManager); // 2��ų ���
            Debug.Log("2��ų ���: ���� 6 �̻�");
        }
    }
    public void ApplyDamageToTargets(int damage, TurnManager turnManager)
    {
        if (turnManager.characterTargetMap.TryGetValue(this, out var targets))
        {
            var (characterTargets, enemyTargets) = targets;

            foreach (var target in characterTargets)
            {
                target.TakeDamage(damage);
                Debug.Log($"ĳ���� {target.name}���� {damage}�� ������ ����");
            }

            foreach (var enemy in enemyTargets)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"�� {enemy.name}���� {damage}�� ������ ����");
            }
        }
        else
        {
            Debug.LogWarning("Ÿ�� ����Ʈ�� ã�� �� �����ϴ�.");
        }
    }

    // �⺻ ��ų ��� �޼��� (��� Ŭ�������� �������̵� ����)
    public virtual void UseSkill(int skillNumber, TurnManager turnManager)
    {
        Debug.Log($"�⺻ ��ų {skillNumber} ���");
    }

    public void ClearPathIndicators()
    {
        foreach (GameObject circle in pathCircles)
        {
            Destroy(circle);
        }
        pathCircles.Clear();

        foreach (GameObject line in pathLines)
        {
            Destroy(line);
        }
        pathLines.Clear();
    }

    public void ClearActivatedCells()
    {
        activatedCells.Clear(); // Ȱ��ȭ�� �� ����Ʈ �ʱ�ȭ
    }

    public void ResetCharacterSet()
    {
        // Ȱ��ȭ�� ���� ��Ȱ��ȭ�ϰ� �ʱ�ȭ
        gridManager.DeactivateCellsForCharacter(activatedCells);
        ClearActivatedCells();
        ClearPathIndicators(); // ��� ǥ�� ������Ʈ �ʱ�ȭ
    }

    public void ClearIndicators()
    {
        foreach (var indicator in indicators)
        {
          Destroy(indicator);
        }
        indicators.Clear(); // ����Ʈ�� ����ֱ�
    }

    public virtual void CharacterReturn()
    {
        mana = 0;
        MoveCount = 6;
        Shield = 0;
        Damage = 1;
    }

    // ���� ���� ���� ĳ���Ϳ� ���� üũ�ϰ� �� �Ŵ����� ����Ʈ ����
    public virtual void CollectAttackTargets(TurnManager turnManager)
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
                    Debug.Log($"�� ������: {enemy.name}, ��ġ: ({gridPosition.x}, {gridPosition.y})");
                }
            }
            else if (gridManager.IsCharacterPosition(gridPosition))
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

    public virtual void TakeDamage(int damageAmount)
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
            Health -= damageToApply;
            Debug.Log($"{name}��(��) {damageToApply}�� ���ظ� �Ծ����ϴ�. ���� ü��: {Health}");
        }

        // UIManager�� ü�� ��ȭ �˸�
        if (uiManager != null && isMyTurn)
        {
            uiManager.UpdateHealthUI(this);
        }

        if (Health <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        activatedCells.Clear();
        foreach (var indicator in indicators)
        {
            Destroy(indicator);
        }
        foreach (var circle in pathCircles)
        {
            Destroy(circle);
        }
        pathCircles.Clear();

        foreach (var line in pathLines)
        {
            Destroy(line);
        }
        pathLines.Clear();
        indicators.Clear();
        ClearIndicators();
        ClearActivatedCells();
        gridManager.RemoveCharacterFromGrid(this);
        gridManager.characters.Remove(this);
        turnManager.characterTargetMap.Remove(this);

        Destroy(gameObject); // ĳ���� ������Ʈ ����
    }

    public int GetActivatedCellsCount()
    {
        return activatedCells.Count; // Ȱ��ȭ�� ���� ������ ��ȯ
    }

    public void HandleCollision(Vector2Int startPosition, Vector2Int collisionPosition)
    {
        // �浹 ��ġ�� ��� Ȯ�� (ĳ���� �Ǵ� ��)
        CharacterBase collidedTarget = gridManager.GetCharacterAtPosition(collisionPosition);
        EnemyBase collidedEnemy = gridManager.GetEnemyAtPosition(collisionPosition);

        if (collidedTarget != null)
        {
            collidedTarget.TakeDamage(1); // �ε��� ĳ���� ������
            Debug.Log($"{collidedTarget.name}��(��) {startPosition}���� �з��� �浹�� ������ 1�� �Ծ����ϴ�.");
        }
        else if (collidedEnemy != null)
        {
            collidedEnemy.TakeDamage(1); // �ε��� �� ������
            Debug.Log($"{collidedEnemy.name}��(��) {startPosition}���� �з��� �浹�� ������ 1�� �Ծ����ϴ�.");
        }

        // �и� ��� ������ ����
        CharacterBase pushingTarget = gridManager.GetCharacterAtPosition(startPosition);
        EnemyBase pushingEnemy = gridManager.GetEnemyAtPosition(startPosition);

        if (pushingTarget != null)
        {
            pushingTarget.TakeDamage(1);
            Debug.Log($"{pushingTarget.name}��(��) �浹�� ���� ������ 1�� �Ծ����ϴ�.");
        }
        else if (pushingEnemy != null)
        {
            pushingEnemy.TakeDamage(1);
            Debug.Log($"{pushingEnemy.name}��(��) �浹�� ���� ������ 1�� �Ծ����ϴ�.");
        }
    }
}
