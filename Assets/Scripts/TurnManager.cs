using System.Collections;
using System.Collections.Generic;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Pathfinding.OffMeshLinks;
using static UnityEngine.GraphicsBuffer;

public class TurnManager : MonoBehaviour
{
    private UIManager uiManager;
    public GridManager gridManager;
    public PrefabSpawner prefabSpawner;
    public bool isSylphTurn;
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

    public List<int> activatedCellCounts = new List<int>(); // 각 캐릭터가 활성화한 셀의 개수를 저장하는 리스트

    // Add lists for each enemy type
    private List<SpecialEnt> specialEnts = new List<SpecialEnt>();
    private List<Ent> ents = new List<Ent>();
    private List<FireSlime> fireSlimes = new List<FireSlime>();
    private List<Slime> slimes = new List<Slime>();
    private List<Ghost> ghosts = new List<Ghost>();

    public CharacterBase character;

    void Awake()
    {
        gridManager.GenerateGrid();
        gridManager.SpawnSylph();
        StartStage();
        isSylphTurn = false;
        isCharacterTurn = false;

        uiManager = FindObjectOfType<UIManager>(); // UIManager 참조 획득
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
        sylph = FindObjectOfType<Sylph>();
    }

    private void Update()
    {
        // 추가된 로직: 'e' 키를 눌렀을 때 캐릭터의 최근 행동을 취소하고 다시 턴을 시작
        if (isCharacterTurn && Input.GetKeyDown(KeyCode.E))
        {
            UndoLastCharacterAction();
        }
    }

    public void OnCharacterPlaced(CharacterBase character)
    {
        charactersPlaced++;
        activeCharacters.Add(character);
        Debug.Log($"캐릭터 배치 완료: 현재 {charactersPlaced}명 배치됨");

        if (charactersPlaced >= 4)
        {
            Debug.Log("모든 캐릭터 배치 완료, 실프의 턴 시작");
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
        uiManager.characterInfoPanel.SetActive(false);

        characters.AddRange(FindObjectsOfType<CharacterBase>());
        Debug.Log("실프 이동 턴 시작");

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
        Debug.Log("실프 턴 종료");

        activeCharacters = sylph.GetEncounteredCharacters();
        if (activeCharacters.Count > 0)
        {
            currentCharacterIndex = 0;
            StartCharacterTurn();
        }
        else
        {
            Debug.Log("마주친 캐릭터가 없어 턴을 종료합니다.");
            ResetTurn();
        }
    }

    public void StartCharacterTurn()
    {
        if (currentCharacterIndex < activeCharacters.Count)
        {
            isCharacterTurn = true;
            CharacterBase currentCharacter = activeCharacters[currentCharacterIndex];
            // 현재 턴의 캐릭터 정보를 UIManager에 전달하여 UI 업데이트
            uiManager.UpdateCharacterUI(currentCharacter);
            Debug.Log($"{currentCharacter.name}의 턴 시작");
            currentCharacter.SetControllable(true); // 현재 캐릭터 조작 가능 설정

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
            Debug.Log("모든 캐릭터의 턴이 종료되었습니다.");
            StartCoroutine(MoveAllCharactersSequentially());
            ResetTurn(); 
        }
    }

    public void EndCharacterTurn()
    {
        if (isCharacterTurn)
        {
            CharacterBase currentCharacter = activeCharacters[currentCharacterIndex];
            currentCharacter.SetControllable(false); // 현재 캐릭터 조작 비활성화
            Debug.Log($"{currentCharacter.name}의 턴 종료");

            // 활성화된 오라가 있으면 삭제
            if (activeAura != null)
            {
                Destroy(activeAura);
                activeAura = null;
            }
            // 이동을 시작하기 전에 공격 범위 내 타겟을 한 번만 확인
            currentCharacter.CollectAttackTargets(this);

            activatedCellCounts.Add(currentCharacter.GetActivatedCellsCount());

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
                Debug.Log("모든 캐릭터의 턴이 종료되었습니다. 이동 버튼을 눌러서 이동을 시작하세요.");
            }
        }
    }

    // 타겟 리스트 저장 메서드
    public void SetCharacterTargets(CharacterBase character, List<CharacterBase> characterTargets, List<EnemyBase> enemyTargets)
    {
        characterTargetMap[character] = (characterTargets, enemyTargets);
        PrintCharacterTargets();
    }

    public void PrintCharacterTargets()
    {
        foreach (var entry in characterTargetMap)
        {
            CharacterBase character = entry.Key;
            List<CharacterBase> characterTargets = entry.Value.characterTargets;
            List<EnemyBase> enemyTargets = entry.Value.enemyTargets;

            //Debug.Log($"{character.name}의 캐릭터 타겟 리스트:");
            foreach (var target in characterTargets)
            {
                //Debug.Log($"- {target.name}");
            }

            //Debug.Log($"{character.name}의 적 타겟 리스트:");
            foreach (var target in enemyTargets)
            {
                //Debug.Log($"- {target.name}");
            }
        }
    }


    public IEnumerator MoveAllCharactersSequentially()
    {

        foreach (CharacterBase character in activeCharacters)
        {
            yield return StartCoroutine(character.StartMovement()); // 각 캐릭터의 이동 실행

            // 3. 이동 완료 후 타겟에게 데미지 적용
            character.UseSkillBasedOnMana(this); // 현재 마나에 따라 자동으로 스킬 사용

        }
        isCharacterTurn = false;
        Debug.Log("모든 캐릭터의 이동이 완료되었습니다.");
        ResetTurn();
    }

    public void ResetTurn()
    {
        characterTargetMap.Clear();
        gridManager.ClearActivatedCells();
        EnemyTurn();
    }
    public void EnemyTurn()
    {
        Debug.Log("적 턴이 시작됩니다.");
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

        if (uiManager != null)
        {
            uiManager.DecreaseTurn();
        }

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

    // 'e' 키를 눌렀을 때 캐릭터의 최근 활성화된 셀을 취소하는 메서드
    public void UndoLastCharacterAction()
    {
        if (currentCharacterIndex > 0 && currentCharacterIndex <= activatedCellCounts.Count)
        {
            CharacterBase currentCharacter = activeCharacters[currentCharacterIndex];
            currentCharacter.SetControllable(false);

            CharacterBase beforeCharacter = activeCharacters[currentCharacterIndex - 1]; // 최근 턴을 가진 캐릭터
            int cellsToDeactivate = activatedCellCounts[currentCharacterIndex - 1]; // 비활성화할 셀 수
            Debug.Log(cellsToDeactivate);

            int startIndex = beforeCharacter.GetActivatedCellsCount() - cellsToDeactivate; // 시작 인덱스 계산
            beforeCharacter.RemoveActivatedCells(startIndex, cellsToDeactivate); // 셀 비활성화

            activatedCellCounts.RemoveAt(currentCharacterIndex - 1);

            currentCharacterIndex--; // 이전 캐릭터로 인덱스를 되돌림

            StartCharacterTurn(); // 이전 캐릭터의 턴을 재시작

            Debug.Log("취소가 시작되었습니다.");
        }
    }
}
