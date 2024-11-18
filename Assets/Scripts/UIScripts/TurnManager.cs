using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Pathfinding.OffMeshLinks;

public class TurnManager : MonoBehaviour
{
    private UIManager uiManager;
    public GridManager gridManager;
    public PrefabSpawner prefabSpawner;
    public bool isSylphTurn;
    public bool FirstSylphTurn;
    public bool isCharacterTurn;

    public Sylph sylph;
    public int charactersPlaced = 0;
    public List<CharacterBase> characters = new List<CharacterBase>();
    public List<CharacterBase> activeCharacters = new List<CharacterBase>();
    public int currentCharacterIndex = 0;

    public GameObject auraPrefab; // 오라 프리팹
    public GameObject activeAura; // 현재 활성화된 오라 인스턴스
    public float auraYPosition = 0.1f; // 오라의 Y 위치 고정 값

    public List<CharacterBase> characterTargets = new List<CharacterBase>();
    public List<EnemyBase> enemyTargets = new List<EnemyBase>();
    public Dictionary<CharacterBase, (List<CharacterBase> characterTargets, List<EnemyBase> enemyTargets)> characterTargetMap = new Dictionary<CharacterBase, (List<CharacterBase>, List<EnemyBase>)>();

    // Add lists for each enemy type
    private List<SpecialEnt> specialEnts = new List<SpecialEnt>();
    private List<Ent> ents = new List<Ent>();
    private List<FireSlime> fireSlimes = new List<FireSlime>();
    private List<Slime> slimes = new List<Slime>();
    private List<Ghost> ghosts = new List<Ghost>();
    private TurnIndicatorManager turnIndicatorManager;

    public CharacterBase character;

    private bool isGameOver = false; // 게임 종료 상태

    public void EndGame()
    {
        isGameOver = true; // 게임 종료 상태로 설정
    }

    void Awake()
    {
        gridManager.GenerateGrid();
        gridManager.SpawnSylph();
        StartStage();
        isSylphTurn = false;
        isCharacterTurn = false;

        uiManager = FindObjectOfType<UIManager>(); // UIManager 참조 획득
        turnIndicatorManager = FindObjectOfType<TurnIndicatorManager>();
    }

    // Call this during enemy initialization or Start
    public void RegisterEnemy(EnemyBase enemy)
    {
        switch (enemy)
        {
            case SpecialEnt specialEnt:
                specialEnts.Add(specialEnt);
                break;
            case Ent ent:
                ents.Add(ent);
                break;
            case FireSlime fireSlime:
                fireSlimes.Add(fireSlime);
                break;
            case Slime slime:
                slimes.Add(slime);
                break;
            case Ghost ghost:
                ghosts.Add(ghost);
                break;
        }
    }

    void Start()
    {
        //sylph = FindObjectOfType<Sylph>();
        FirstSylphTurn = true;
    }

    public void Update()
    {
        if (isGameOver) return; // 게임 종료 상태면 동작 멈춤

        if (isCharacterTurn)
        {
            if (Input.GetMouseButtonUp(1))
            {
                UndoAllCharacterPathsAndRestartTurn();
            }
        }
    }

    public void OnCharacterPlaced(CharacterBase character)
    {
        charactersPlaced++;
        activeCharacters.Add(character);

        if (charactersPlaced >= 4)
        {
            gridManager.ResetGridColors();
            StartSylphTurn();
        }
    }

    private void StartStage()
    {
        sylph = gridManager.sylphInstance;
    }

    private void StartSylphTurn()
    {
        if (isGameOver) return; // 게임 종료 상태면 동작 멈춤

        turnIndicatorManager.ShowTurnIndicator(TurnIndicatorManager.TurnType.SylphTurn);

        if (uiManager != null)
        {
            if (!FirstSylphTurn)
            {
                sylph.sylphanimator.SetTrigger("moveStart");
                uiManager.DecreaseTurn();
            }
        }

        uiManager.characterInfoPanel.SetActive(false);

        characters.AddRange(FindObjectsOfType<CharacterBase>());

        sylph.ClearEncounteredCharacters();
        sylph.SetControllable(true);
        sylph.SylphTurn();
        isSylphTurn = true;

        List<EnemyBase> enemies = new List<EnemyBase>(FindObjectsOfType<EnemyBase>());

        // 모든 캐릭터에 대해 CharacterReturn 호출
        foreach (var character in characters)
        {
            character.CharacterReturn();
        }

        // 적 타겟 선택 및 경로 계산
        foreach (EnemyBase enemy in FindObjectsOfType<EnemyBase>())
        {
            enemy.SelectTarget(); // Select the target for each enemy
            Vector2Int intermediatePosition = enemy.CalculateIntermediatePosition(enemies); // Calculate intermediate position
            enemy.SetCalculatedIntermediatePosition(intermediatePosition); // Optionally store this position in the enemy
        }
    }

    public void EndSylphTurn(List<CharacterBase> encounteredCharacters)
    {
        FirstSylphTurn = false;

        if (encounteredCharacters.Count > 0)
        {
            CharacterBase firstEncountered = encounteredCharacters[0];

            // 첫 번째 만난 캐릭터 유형에 따라 고정된 수치를 적용
            if (firstEncountered is Warrior)
            {
                foreach (var character in encounteredCharacters)
                {
                    character.Shield = Mathf.Min(character.Shield + 1, 1); // 쉴드 1 고정 증가
                }
                Debug.Log("첫 번째로 만난 캐릭터가 Warrior여서 쉴드 +1 적용");
            }
            else if (firstEncountered is Archer)
            {
                foreach (var character in encounteredCharacters)
                {
                    character.MoveCount = Mathf.Min(character.MoveCount + 2, character.MoveCount + 2); // 이동 횟수 2 고정 증가
                }
                Debug.Log("첫 번째로 만난 캐릭터가 Archer여서 MoveCount +1 적용");
            }
            else if (firstEncountered is Magician)
            {
                sylph.currentMana = Mathf.Min(sylph.currentMana + 1, sylph.maxMana); // 실프의 마나 +1 증가 = UI 표시 용도 어차피 초기값으로 돌아감
                foreach (var character in encounteredCharacters)
                {
                    character.mana = Mathf.Min(character.mana + 1, character.mana + 1); // 캐릭터에게도 전달
                }
                Debug.Log("첫 번째로 만난 캐릭터가 Magician여서 실프의 마나 +1 적용");
            }
            else if (firstEncountered is Priest)
            {
                foreach (var character in encounteredCharacters)
                {
                    character.Damage += 1; // Damage 값을 1 증가
                }
                Debug.Log("첫 번째로 만난 캐릭터가 Priest여서 Damage +1 적용");
            }
        }
        sylph.SetControllable(false);
        isSylphTurn = false;

        activeCharacters = sylph.GetEncounteredCharacters();
        if (activeCharacters.Count > 0)
        {
            currentCharacterIndex = 0;
            StartCharacterTurn();
            turnIndicatorManager.ShowTurnIndicator(TurnIndicatorManager.TurnType.CharacterTurn);
        }
        else
        {
            ResetTurn();
        }
    }

    public void StartCharacterTurn()
    {
        if (isGameOver) return; // 게임 종료 상태면 동작 멈춤

        if (currentCharacterIndex < activeCharacters.Count)
        {
            isCharacterTurn = true;
            CharacterBase currentCharacter = activeCharacters[currentCharacterIndex];
            // 현재 턴의 캐릭터 정보를 UIManager에 전달하여 UI 업데이트
            uiManager.UpdateCharacterUI(currentCharacter);
            currentCharacter.SetControllable(true); // 현재 캐릭터 조작 가능 설정

            currentCharacter.Receivemana = currentCharacter.mana;
            currentCharacter.ReceivemoveCount = currentCharacter.MoveCount;

            // 현재 캐릭터의 위치에 오라 생성
            if (auraPrefab != null)
            {
                // 기존 오라가 있으면 삭제
                if (activeAura != null)
                {
                    Destroy(activeAura);
                }

                // 오라를 생성하고 Y 위치 고정 설정
                Vector3 auraPosition = currentCharacter.transform.position;
                auraPosition.y = auraYPosition; // Y 위치 고정
                activeAura = Instantiate(auraPrefab, auraPosition, Quaternion.identity);
                activeAura.transform.SetParent(currentCharacter.transform); // 캐릭터의 자식으로 설정
            }
        }
        else
        {
            ResetTurn(); 
        }
    }

    public void EndCharacterTurn()
    {
        if (isGameOver) return; // 게임 종료 상태면 동작 멈춤

        if (isCharacterTurn)
        {
            CharacterBase currentCharacter = activeCharacters[currentCharacterIndex];
            currentCharacter.SetControllable(false); // 현재 캐릭터 조작 비활성화

            // 활성화된 오라가 있으면 삭제
            if (activeAura != null)
            {
                Destroy(activeAura);
                activeAura = null;
            }
            // 이동을 시작하기 전에 공격 범위 내 타겟을 한 번만 확인
            currentCharacter.CollectAttackTargets(this);

            currentCharacterIndex++; // 다음 캐릭터로 이동
            if (currentCharacterIndex < activeCharacters.Count)
            {
                StartCharacterTurn(); // 다음 캐릭터 턴 시작
            }
            else
            {
                // 모든 캐릭터의 턴이 종료된 경우 버튼을 활성화하여 이동 시작을 대기
                UIManager uiManager = FindObjectOfType<UIManager>();
                uiManager.ActivateEndTurnButton(); // 버튼 활성화
            }
        }
    }

    // 언두 기능을 호출하여 모든 캐릭터의 경로를 초기화하고 첫 번째 캐릭터 턴부터 다시 시작
    public void UndoAllCharacterPathsAndRestartTurn()
    {
        foreach (CharacterBase character in activeCharacters)
        {
            character.UndoAllActivatedCells();
        }
        currentCharacterIndex = 0;
        StartCharacterTurn();
        Debug.Log("모든 캐릭터의 경로를 초기화하고 첫 번째 캐릭터 턴부터 다시 시작합니다.");
    }

    // 타겟 리스트 저장 메서드
    public void SetCharacterTargets(CharacterBase character, List<CharacterBase> characterTargets, List<EnemyBase> enemyTargets)
    {
        characterTargetMap[character] = (characterTargets, enemyTargets);
    }

    public IEnumerator ExecuteTurn()
    {
        foreach (CharacterBase character in activeCharacters)
        {
            // 1. 이동 실행
            yield return StartCoroutine(MoveCharacter(character));

            // 2. 공격 실행
            yield return StartCoroutine(AttackCharacter(character));

            // 3. 딜레이 추가 (선택)
            yield return new WaitForSeconds(0.5f);
        }

        isCharacterTurn = false;
        ResetTurn(); // 턴 초기화 및 적 턴 시작
    }

    public IEnumerator MoveCharacter(CharacterBase character)
    {
        yield return StartCoroutine(character.StartMovement()); // 캐릭터의 이동 실행
    }
    public IEnumerator AttackCharacter(CharacterBase character)
    {
        character.UseSkillBasedOnMana(this); // 캐릭터의 스킬 사용
        yield return new WaitForSeconds(0.5f); // 공격 후 대기 시간
    }

    public void ResetTurn()
    {
        if (isGameOver) return; // 게임 종료 상태면 동작 멈춤

        StartCoroutine(ResetTurnWithDelay());
    }

    private IEnumerator ResetTurnWithDelay()
    {
        // 턴 리셋 작업 수행
        characterTargetMap.Clear();
        gridManager.ClearActivatedCells();

        yield return new WaitForSeconds(2f);

        turnIndicatorManager.ShowTurnIndicator(TurnIndicatorManager.TurnType.EnemyTurn);

        yield return new WaitForSeconds(1f);
        // 적 턴 시작
        EnemyTurn();
    }
    public void EnemyTurn()
    {
        if (isGameOver) return; // 게임 종료 상태면 동작 멈춤

        StartCoroutine(HandleEnemyMovement());
    }

    private IEnumerator HandleEnemyMovement()
    {
        // Process enemies in the specified order
        yield return HandleEnemies(specialEnts);
        yield return HandleEnemies(ents);
        yield return HandleEnemies(fireSlimes);
        yield return HandleEnemies(slimes);
        yield return HandleEnemies(ghosts);

        StartSylphTurn();
    }

    private IEnumerator HandleEnemies<T>(List<T> enemies) where T : EnemyBase
    {
        foreach (T enemy in enemies)
        {
            Vector2Int targetPosition = enemy.GetCalculatedIntermediatePosition(); // Calculate intermediate position

            // Move to the calculated intermediate position
            yield return StartCoroutine(enemy.MoveToCalculatedPosition(targetPosition));

            // Handle attack if in range
            yield return StartCoroutine(enemy.HandleAttackIfInRange());
        }
    }

    public void DeregisterEnemy(EnemyBase enemy)
    {
        switch (enemy)
        {
            case SpecialEnt specialEnt:
                specialEnts.Remove(specialEnt);
                break;
            case Ent ent:
                ents.Remove(ent);
                break;
            case FireSlime fireSlime:
                fireSlimes.Remove(fireSlime);
                break;
            case Slime slime:
                slimes.Remove(slime);
                break;
            case Ghost ghost:
                ghosts.Remove(ghost);
                break;
        }
    }
}
