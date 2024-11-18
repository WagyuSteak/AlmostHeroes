using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

public class CharacterBase : MonoBehaviour
{
    protected Sylph sylph;
    public Sprite icon; // 캐릭터 아이콘
    public Sprite skillIcon1; // 첫 번째 스킬 아이콘
    public Sprite skillIcon2; // 두 번째 스킬 아이콘
    public Sprite Synergy; // 시너지 아이콘
    public GridManager gridManager; // 그리드 매니저 참조 - 그리드 셀을 관리
    public TurnManager turnManager; // 턴 매니저 참조 - 턴을 관리
    private UIManager uiManager;

    public bool hasMoved = false; // 캐릭터가 이동을 했는지 여부를 나타내는 플래그
    public int mana; // 현재 캐릭터의 마나 수치
    public int Health; // 현재 캐릭터의 체력
    public int MaxHealth; // 캐릭터의 최대 체력
    public int MoveCount = 10; // 이동 가능 횟수
    public int Shield;
    public int Damage;
    public int attackRange; // 사거리 설정
    public int Receivemana;
    public int ReceivemoveCount;


    public float cellWidth; // 그리드 셀의 너비
    public float cellHeight; // 그리드 셀의 높이
    
    
    public Vector3 gridOrigin; // 그리드의 월드 좌표 상의 원점 위치

    public GameObject indicatorPrefab; // 인디케이터 프리팹
    public Material enemyMaterial;
    public Material emptyMaterial;
    public Material BuffMaterial;
    public List<GameObject> indicators = new List<GameObject>();

    public bool IsRanged; //원거리인지 근거리인지를 판정

    // 이동 및 포인터 관련 변수들
    public GameObject pointerPrefab; // 마우스 포인터로 사용할 프리팹
    public GameObject pathIndicatorPrefab; // 경로를 표시하는 오브젝트 프리팹 
    public float pathIndicatorHeight = 1.5f; // 경로 표시 오브젝트의 Y축 높이

    public Camera mainCamera; // 메인 카메라 참조 - 마우스 위치를 화면 상에서 감지하기 위해 사용
    public LayerMask cellLayerMask; // 셀을 감지하기 위한 레이어 마스크

    protected bool isControllableCharacter = false; // 캐릭터가 현재 조작 가능한 상태인지 여부
    protected GameObject currentPointer; // 현재 마우스 위치를 나타내는 포인터 오브젝트

    public GameObject circlePrefab; // 동그란 프리팹
    public GameObject linePrefab;   // 직선 프리팹

    public List<GameObject> pathCircles = new List<GameObject>(); // 경로 동그란 오브젝트 리스트
    public List<GameObject> pathLines = new List<GameObject>(); // 경로 직선 오브젝트 리스트
    public List<Vector2Int> activatedCells = new List<Vector2Int>(); // 캐릭터가 활성화한 셀 리스트
    public Vector2Int currentGridPosition; // 현재 그리드 위치
    public Vector2Int startGridPosition; // 시작 그리드 위치
    public bool isMyTurn = false; // 현재 캐릭터의 턴 여부를 나타냄

    protected virtual void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        turnManager = FindObjectOfType<TurnManager>();

        mainCamera = Camera.main; // 메인 카메라 자동 할당
        Health = MaxHealth;

        // 그리드 매니저가 존재할 경우 그리드 기본 설정 가져옴
        if (gridManager != null)
        {
            gridOrigin = gridManager.gridOrigin;
            cellWidth = gridManager.cellWidth;
            cellHeight = gridManager.cellHeight;
        }
    }


    public virtual void SetGridPosition(Vector2Int position)
    {
        // 새로운 위치 등록
        currentGridPosition = position;
        gridManager.AddCharacterToGrid(position, this); // 마지막 위치만 등록
    }

    public virtual void ResetGridPosition()
    {
        currentGridPosition = Vector2Int.zero; // 또는 기본 위치로 초기화
    }


    public virtual Vector2Int GetCurrentGridPosition()
    {
        return currentGridPosition; // 현재 위치는 항상 finalGridPosition을 반환
    }

    public virtual void ReceiveMana(int amount)
    {
        mana += amount;
        Debug.Log($"{name}이(가) {amount}의 마나를 받았습니다. 현재 마나: {mana}");
    }

    // 캐릭터 조작 가능 상태 설정
    public virtual void SetControllable(bool canControl)
    {
        isControllableCharacter = canControl;
        if (!canControl)
        {
            hasMoved = false; // 턴 종료 시 이동 여부 초기화
        }
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellWidth); // 월드 좌표를 그리드 x좌표로 변환
        int z = Mathf.RoundToInt((worldPosition.z - gridOrigin.z) / cellHeight); // 월드 좌표를 그리드 z좌표로 변환
        return new Vector2Int(x, z); // 그리드 좌표 반환
    }

    protected virtual void Update()
    {
        if (isControllableCharacter) // 내 턴일 때만 입력을 처리
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClick(); // 마우스 클릭 처리
            }

            if (Input.GetMouseButton(0) && currentPointer != null)
            {
                HandlePointerMovement(); // 포인터 이동 처리
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (currentPointer != null)
                {
                    Destroy(currentPointer); // 포인터 오브젝트 삭제
                }
                if (activatedCells.Count > 1) // 조건에 따라 1보다 큰지 확인
                {
                    turnManager.EndCharacterTurn(); // EndCharacterTurn 호출
                }
            }
        }
    }
    public void UndoAllActivatedCells()
    {
        if (activatedCells.Count > 0)
        {
            // 모든 활성화된 셀 제거
            gridManager.DeactivateCellsForCharacter(activatedCells);
            gridManager.ClearActivatedCells();
            activatedCells.Clear();
            ClearIndicators();

            // 모든 경로와 인디케이터 제거
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

            mana = Receivemana; // 초기 마나 값으로 복원
            MoveCount = ReceivemoveCount; // 초기 이동 횟수로 복원

            currentGridPosition = startGridPosition;
            Debug.Log($"캐릭터 현재 위치: {currentGridPosition}");
            Debug.Log("모든 활성화된 셀을 취소하고 초기화했습니다.");
        }
    }
    protected virtual void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // 마우스 위치에서 레이 생성
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, cellLayerMask)) // 레이캐스트가 성공하면
        {
            Vector3 pointerPosition = hit.point; // 히트된 위치 가져오기
            Vector2Int targetGridPosition = GetGridPosition(pointerPosition); // 그리드 좌표로 변환

            if (targetGridPosition == currentGridPosition) // 현재 그리드 위치와 동일하다면
            {
                // 현재 위치와 동일하다
                currentPointer = Instantiate(pointerPrefab, pointerPosition, Quaternion.identity); // 포인터 프리팹 생성
                startGridPosition = currentGridPosition;

                if (!gridManager.IsCellActivatedByCharacter(targetGridPosition) && !activatedCells.Contains(targetGridPosition))
                {
                    ActivateCell(currentGridPosition);
                }
            }
            else
            {
                Debug.Log("그리드 경계를 벗어나 있거나 셀이 이미 점유되어 있어 이동할 수 없습니다."); // 다른 위치에 클릭 시
            }
        }
    }

    protected virtual void HandlePointerMovement()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // 마우스 위치에서 레이 생성
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, cellLayerMask)) // 레이캐스트 성공 시
        {
            Vector3 pointerPosition = hit.point;
            Vector2Int targetGridPosition = GetGridPosition(pointerPosition);

            // 새 조건: 마지막으로 활성화한 셀 기준으로 인접 여부, 활성화 여부, 캐릭터, 실프, 적 체크
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

    // 새로운 보조 함수: 마지막 활성화한 셀과 인접 여부를 계산
    protected bool IsOneTileAwayFromLastActivatedCell(Vector2Int targetGridPosition)
    {
        if (activatedCells.Count == 0) return false; // 활성화된 셀이 없으면 false 반환

        Vector2Int lastActivatedCell = activatedCells[activatedCells.Count - 1]; // 마지막 활성화 셀
        int deltaX = Mathf.Abs(targetGridPosition.x - lastActivatedCell.x);
        int deltaY = Mathf.Abs(targetGridPosition.y - lastActivatedCell.y);

        return (deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1); // 한 축으로 1칸 떨어졌는지 확인
    }

    protected virtual void ActivateCell(Vector2Int targetGridPosition)
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

            // 첫 셀의 경우 캐릭터 위치에 동그란 프리팹 생성 및 다음 위치와 직선 연결
            if (activatedCells.Count == 1)
            {
                MoveCount++;
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

    public virtual IEnumerator StartMovement()
    {
        // 첫 번째 활성화된 셀을 건너뛰기 위해 i를 1로 설정
        for (int i = 1; i < activatedCells.Count; i++)
        {
            Vector2Int cellPosition = activatedCells[i];
            Vector3 targetPosition = gridOrigin + new Vector3(cellPosition.x * cellWidth, 0.55f, cellPosition.y * cellHeight);
            Vector3 direction = (targetPosition - transform.position).normalized;
            Quaternion targetRotation = Quaternion.Euler(0, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)).eulerAngles.y, 0);

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

            yield return new WaitForSeconds(0.1f); // 각 위치에서 대기 시간
        }
        ClearIndicators(); // 이동이 끝난 후 인디케이터 초기화
        ResetCharacterSet();
    }


    // 마나에 따라 스킬을 자동으로 결정하여 사용하는 메서드
    public void UseSkillBasedOnMana(TurnManager turnManager)
    {
        if (mana <= 1)
        {
            Debug.Log("스킬 사용 불가: 마나가 부족합니다.");
        }
        else if (mana <= 5)
        {
            UseSkill(1, turnManager); // 1스킬 사용
            Debug.Log("1스킬 사용: 마나 2~5");
        }
        else
        {
            UseSkill(2, turnManager); // 2스킬 사용
            Debug.Log("2스킬 사용: 마나 6 이상");
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
                Debug.Log($"캐릭터 {target.name}에게 {damage}의 데미지 적용");
            }

            foreach (var enemy in enemyTargets)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"적 {enemy.name}에게 {damage}의 데미지 적용");
            }
        }
        else
        {
            Debug.LogWarning("타겟 리스트를 찾을 수 없습니다.");
        }
    }

    // 기본 스킬 사용 메서드 (상속 클래스에서 오버라이드 예정)
    public virtual void UseSkill(int skillNumber, TurnManager turnManager)
    {
        Debug.Log($"기본 스킬 {skillNumber} 사용");
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
        activatedCells.Clear(); // 활성화된 셀 리스트 초기화
    }

    public void ResetCharacterSet()
    {
        // 활성화된 셀을 비활성화하고 초기화
        gridManager.DeactivateCellsForCharacter(activatedCells);
        ClearActivatedCells();
        ClearPathIndicators(); // 경로 표시 오브젝트 초기화
    }

    public void ClearIndicators()
    {
        foreach (var indicator in indicators)
        {
          Destroy(indicator);
        }
        indicators.Clear(); // 리스트도 비워주기
    }

    public virtual void CharacterReturn()
    {
        mana = 0;
        MoveCount = 6;
        Shield = 0;
        Damage = 1;
    }

    // 공격 범위 내의 캐릭터와 적을 체크하고 턴 매니저에 리스트 전달
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

    public virtual void TakeDamage(int damageAmount)
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
            Health -= damageToApply;
            Debug.Log($"{name}이(가) {damageToApply}의 피해를 입었습니다. 남은 체력: {Health}");
        }

        // UIManager에 체력 변화 알림
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

        Destroy(gameObject); // 캐릭터 오브젝트 삭제
    }

    public int GetActivatedCellsCount()
    {
        return activatedCells.Count; // 활성화된 셀의 개수를 반환
    }

    public void HandleCollision(Vector2Int startPosition, Vector2Int collisionPosition)
    {
        // 충돌 위치의 대상 확인 (캐릭터 또는 적)
        CharacterBase collidedTarget = gridManager.GetCharacterAtPosition(collisionPosition);
        EnemyBase collidedEnemy = gridManager.GetEnemyAtPosition(collisionPosition);

        if (collidedTarget != null)
        {
            collidedTarget.TakeDamage(1); // 부딪힌 캐릭터 데미지
            Debug.Log($"{collidedTarget.name}이(가) {startPosition}에서 밀려와 충돌로 데미지 1을 입었습니다.");
        }
        else if (collidedEnemy != null)
        {
            collidedEnemy.TakeDamage(1); // 부딪힌 적 데미지
            Debug.Log($"{collidedEnemy.name}이(가) {startPosition}에서 밀려와 충돌로 데미지 1을 입었습니다.");
        }

        // 밀린 대상도 데미지 입음
        CharacterBase pushingTarget = gridManager.GetCharacterAtPosition(startPosition);
        EnemyBase pushingEnemy = gridManager.GetEnemyAtPosition(startPosition);

        if (pushingTarget != null)
        {
            pushingTarget.TakeDamage(1);
            Debug.Log($"{pushingTarget.name}이(가) 충돌로 인해 데미지 1을 입었습니다.");
        }
        else if (pushingEnemy != null)
        {
            pushingEnemy.TakeDamage(1);
            Debug.Log($"{pushingEnemy.name}이(가) 충돌로 인해 데미지 1을 입었습니다.");
        }
    }
}
