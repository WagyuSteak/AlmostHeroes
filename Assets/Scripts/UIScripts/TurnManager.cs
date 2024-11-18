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

    public GameObject auraPrefab; // ���� ������
    public GameObject activeAura; // ���� Ȱ��ȭ�� ���� �ν��Ͻ�
    public float auraYPosition = 0.1f; // ������ Y ��ġ ���� ��

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

    private bool isGameOver = false; // ���� ���� ����

    public void EndGame()
    {
        isGameOver = true; // ���� ���� ���·� ����
    }

    void Awake()
    {
        gridManager.GenerateGrid();
        gridManager.SpawnSylph();
        StartStage();
        isSylphTurn = false;
        isCharacterTurn = false;

        uiManager = FindObjectOfType<UIManager>(); // UIManager ���� ȹ��
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
        if (isGameOver) return; // ���� ���� ���¸� ���� ����

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
        if (isGameOver) return; // ���� ���� ���¸� ���� ����

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

        // ��� ĳ���Ϳ� ���� CharacterReturn ȣ��
        foreach (var character in characters)
        {
            character.CharacterReturn();
        }

        // �� Ÿ�� ���� �� ��� ���
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

            // ù ��° ���� ĳ���� ������ ���� ������ ��ġ�� ����
            if (firstEncountered is Warrior)
            {
                foreach (var character in encounteredCharacters)
                {
                    character.Shield = Mathf.Min(character.Shield + 1, 1); // ���� 1 ���� ����
                }
                Debug.Log("ù ��°�� ���� ĳ���Ͱ� Warrior���� ���� +1 ����");
            }
            else if (firstEncountered is Archer)
            {
                foreach (var character in encounteredCharacters)
                {
                    character.MoveCount = Mathf.Min(character.MoveCount + 2, character.MoveCount + 2); // �̵� Ƚ�� 2 ���� ����
                }
                Debug.Log("ù ��°�� ���� ĳ���Ͱ� Archer���� MoveCount +1 ����");
            }
            else if (firstEncountered is Magician)
            {
                sylph.currentMana = Mathf.Min(sylph.currentMana + 1, sylph.maxMana); // ������ ���� +1 ���� = UI ǥ�� �뵵 ������ �ʱⰪ���� ���ư�
                foreach (var character in encounteredCharacters)
                {
                    character.mana = Mathf.Min(character.mana + 1, character.mana + 1); // ĳ���Ϳ��Ե� ����
                }
                Debug.Log("ù ��°�� ���� ĳ���Ͱ� Magician���� ������ ���� +1 ����");
            }
            else if (firstEncountered is Priest)
            {
                foreach (var character in encounteredCharacters)
                {
                    character.Damage += 1; // Damage ���� 1 ����
                }
                Debug.Log("ù ��°�� ���� ĳ���Ͱ� Priest���� Damage +1 ����");
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
        if (isGameOver) return; // ���� ���� ���¸� ���� ����

        if (currentCharacterIndex < activeCharacters.Count)
        {
            isCharacterTurn = true;
            CharacterBase currentCharacter = activeCharacters[currentCharacterIndex];
            // ���� ���� ĳ���� ������ UIManager�� �����Ͽ� UI ������Ʈ
            uiManager.UpdateCharacterUI(currentCharacter);
            currentCharacter.SetControllable(true); // ���� ĳ���� ���� ���� ����

            currentCharacter.Receivemana = currentCharacter.mana;
            currentCharacter.ReceivemoveCount = currentCharacter.MoveCount;

            // ���� ĳ������ ��ġ�� ���� ����
            if (auraPrefab != null)
            {
                // ���� ���� ������ ����
                if (activeAura != null)
                {
                    Destroy(activeAura);
                }

                // ���� �����ϰ� Y ��ġ ���� ����
                Vector3 auraPosition = currentCharacter.transform.position;
                auraPosition.y = auraYPosition; // Y ��ġ ����
                activeAura = Instantiate(auraPrefab, auraPosition, Quaternion.identity);
                activeAura.transform.SetParent(currentCharacter.transform); // ĳ������ �ڽ����� ����
            }
        }
        else
        {
            ResetTurn(); 
        }
    }

    public void EndCharacterTurn()
    {
        if (isGameOver) return; // ���� ���� ���¸� ���� ����

        if (isCharacterTurn)
        {
            CharacterBase currentCharacter = activeCharacters[currentCharacterIndex];
            currentCharacter.SetControllable(false); // ���� ĳ���� ���� ��Ȱ��ȭ

            // Ȱ��ȭ�� ���� ������ ����
            if (activeAura != null)
            {
                Destroy(activeAura);
                activeAura = null;
            }
            // �̵��� �����ϱ� ���� ���� ���� �� Ÿ���� �� ���� Ȯ��
            currentCharacter.CollectAttackTargets(this);

            currentCharacterIndex++; // ���� ĳ���ͷ� �̵�
            if (currentCharacterIndex < activeCharacters.Count)
            {
                StartCharacterTurn(); // ���� ĳ���� �� ����
            }
            else
            {
                // ��� ĳ������ ���� ����� ��� ��ư�� Ȱ��ȭ�Ͽ� �̵� ������ ���
                UIManager uiManager = FindObjectOfType<UIManager>();
                uiManager.ActivateEndTurnButton(); // ��ư Ȱ��ȭ
            }
        }
    }

    // ��� ����� ȣ���Ͽ� ��� ĳ������ ��θ� �ʱ�ȭ�ϰ� ù ��° ĳ���� �Ϻ��� �ٽ� ����
    public void UndoAllCharacterPathsAndRestartTurn()
    {
        foreach (CharacterBase character in activeCharacters)
        {
            character.UndoAllActivatedCells();
        }
        currentCharacterIndex = 0;
        StartCharacterTurn();
        Debug.Log("��� ĳ������ ��θ� �ʱ�ȭ�ϰ� ù ��° ĳ���� �Ϻ��� �ٽ� �����մϴ�.");
    }

    // Ÿ�� ����Ʈ ���� �޼���
    public void SetCharacterTargets(CharacterBase character, List<CharacterBase> characterTargets, List<EnemyBase> enemyTargets)
    {
        characterTargetMap[character] = (characterTargets, enemyTargets);
    }

    public IEnumerator ExecuteTurn()
    {
        foreach (CharacterBase character in activeCharacters)
        {
            // 1. �̵� ����
            yield return StartCoroutine(MoveCharacter(character));

            // 2. ���� ����
            yield return StartCoroutine(AttackCharacter(character));

            // 3. ������ �߰� (����)
            yield return new WaitForSeconds(0.5f);
        }

        isCharacterTurn = false;
        ResetTurn(); // �� �ʱ�ȭ �� �� �� ����
    }

    public IEnumerator MoveCharacter(CharacterBase character)
    {
        yield return StartCoroutine(character.StartMovement()); // ĳ������ �̵� ����
    }
    public IEnumerator AttackCharacter(CharacterBase character)
    {
        character.UseSkillBasedOnMana(this); // ĳ������ ��ų ���
        yield return new WaitForSeconds(0.5f); // ���� �� ��� �ð�
    }

    public void ResetTurn()
    {
        if (isGameOver) return; // ���� ���� ���¸� ���� ����

        StartCoroutine(ResetTurnWithDelay());
    }

    private IEnumerator ResetTurnWithDelay()
    {
        // �� ���� �۾� ����
        characterTargetMap.Clear();
        gridManager.ClearActivatedCells();

        yield return new WaitForSeconds(2f);

        turnIndicatorManager.ShowTurnIndicator(TurnIndicatorManager.TurnType.EnemyTurn);

        yield return new WaitForSeconds(1f);
        // �� �� ����
        EnemyTurn();
    }
    public void EnemyTurn()
    {
        if (isGameOver) return; // ���� ���� ���¸� ���� ����

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
