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

    public GameObject auraPrefab; // ���� ������
    public GameObject activeAura; // ���� Ȱ��ȭ�� ���� �ν��Ͻ�
    public float auraYPosition = 0.1f; // ������ Y ��ġ ���� ��

    public List<CharacterBase> characterTargets = new List<CharacterBase>();
    public List<EnemyBase> enemyTargets = new List<EnemyBase>();
    public Dictionary<CharacterBase, (List<CharacterBase> characterTargets, List<EnemyBase> enemyTargets)> characterTargetMap = new Dictionary<CharacterBase, (List<CharacterBase>, List<EnemyBase>)>();

    public List<int> activatedCellCounts = new List<int>(); // �� ĳ���Ͱ� Ȱ��ȭ�� ���� ������ �����ϴ� ����Ʈ

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

        uiManager = FindObjectOfType<UIManager>(); // UIManager ���� ȹ��
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
        // �߰��� ����: 'e' Ű�� ������ �� ĳ������ �ֱ� �ൿ�� ����ϰ� �ٽ� ���� ����
        if (isCharacterTurn && Input.GetKeyDown(KeyCode.E))
        {
            UndoLastCharacterAction();
        }
    }

    public void OnCharacterPlaced(CharacterBase character)
    {
        charactersPlaced++;
        activeCharacters.Add(character);
        Debug.Log($"ĳ���� ��ġ �Ϸ�: ���� {charactersPlaced}�� ��ġ��");

        if (charactersPlaced >= 4)
        {
            Debug.Log("��� ĳ���� ��ġ �Ϸ�, ������ �� ����");
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
        Debug.Log("���� �̵� �� ����");

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
        Debug.Log("���� �� ����");

        activeCharacters = sylph.GetEncounteredCharacters();
        if (activeCharacters.Count > 0)
        {
            currentCharacterIndex = 0;
            StartCharacterTurn();
        }
        else
        {
            Debug.Log("����ģ ĳ���Ͱ� ���� ���� �����մϴ�.");
            ResetTurn();
        }
    }

    public void StartCharacterTurn()
    {
        if (currentCharacterIndex < activeCharacters.Count)
        {
            isCharacterTurn = true;
            CharacterBase currentCharacter = activeCharacters[currentCharacterIndex];
            // ���� ���� ĳ���� ������ UIManager�� �����Ͽ� UI ������Ʈ
            uiManager.UpdateCharacterUI(currentCharacter);
            Debug.Log($"{currentCharacter.name}�� �� ����");
            currentCharacter.SetControllable(true); // ���� ĳ���� ���� ���� ����

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
            Debug.Log("��� ĳ������ ���� ����Ǿ����ϴ�.");
            StartCoroutine(MoveAllCharactersSequentially());
            ResetTurn(); 
        }
    }

    public void EndCharacterTurn()
    {
        if (isCharacterTurn)
        {
            CharacterBase currentCharacter = activeCharacters[currentCharacterIndex];
            currentCharacter.SetControllable(false); // ���� ĳ���� ���� ��Ȱ��ȭ
            Debug.Log($"{currentCharacter.name}�� �� ����");

            // Ȱ��ȭ�� ���� ������ ����
            if (activeAura != null)
            {
                Destroy(activeAura);
                activeAura = null;
            }
            // �̵��� �����ϱ� ���� ���� ���� �� Ÿ���� �� ���� Ȯ��
            currentCharacter.CollectAttackTargets(this);

            activatedCellCounts.Add(currentCharacter.GetActivatedCellsCount());

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
                Debug.Log("��� ĳ������ ���� ����Ǿ����ϴ�. �̵� ��ư�� ������ �̵��� �����ϼ���.");
            }
        }
    }

    // Ÿ�� ����Ʈ ���� �޼���
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

            //Debug.Log($"{character.name}�� ĳ���� Ÿ�� ����Ʈ:");
            foreach (var target in characterTargets)
            {
                //Debug.Log($"- {target.name}");
            }

            //Debug.Log($"{character.name}�� �� Ÿ�� ����Ʈ:");
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
            yield return StartCoroutine(character.StartMovement()); // �� ĳ������ �̵� ����

            // 3. �̵� �Ϸ� �� Ÿ�ٿ��� ������ ����
            character.UseSkillBasedOnMana(this); // ���� ������ ���� �ڵ����� ��ų ���

        }
        isCharacterTurn = false;
        Debug.Log("��� ĳ������ �̵��� �Ϸ�Ǿ����ϴ�.");
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
        Debug.Log("�� ���� ���۵˴ϴ�.");
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

    // 'e' Ű�� ������ �� ĳ������ �ֱ� Ȱ��ȭ�� ���� ����ϴ� �޼���
    public void UndoLastCharacterAction()
    {
        if (currentCharacterIndex > 0 && currentCharacterIndex <= activatedCellCounts.Count)
        {
            CharacterBase currentCharacter = activeCharacters[currentCharacterIndex];
            currentCharacter.SetControllable(false);

            CharacterBase beforeCharacter = activeCharacters[currentCharacterIndex - 1]; // �ֱ� ���� ���� ĳ����
            int cellsToDeactivate = activatedCellCounts[currentCharacterIndex - 1]; // ��Ȱ��ȭ�� �� ��
            Debug.Log(cellsToDeactivate);

            int startIndex = beforeCharacter.GetActivatedCellsCount() - cellsToDeactivate; // ���� �ε��� ���
            beforeCharacter.RemoveActivatedCells(startIndex, cellsToDeactivate); // �� ��Ȱ��ȭ

            activatedCellCounts.RemoveAt(currentCharacterIndex - 1);

            currentCharacterIndex--; // ���� ĳ���ͷ� �ε����� �ǵ���

            StartCharacterTurn(); // ���� ĳ������ ���� �����

            Debug.Log("��Ұ� ���۵Ǿ����ϴ�.");
        }
    }
}
