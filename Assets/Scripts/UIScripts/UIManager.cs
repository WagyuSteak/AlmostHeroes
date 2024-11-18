using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Sylph sylph;
    public GameObject manaPrefab; // ������ ��Ÿ�� ������
    public Transform manaContainer; // ���� �������� ���� �����̳�
    public GridManager gridManager;
    private List<GameObject> manaIndicators = new List<GameObject>(); // ���� ǥ�õ� ���� ������ ����Ʈ
    public TurnManager turnManager; // �� �Ŵ��� ���� - ���� ����
    public Button EndTurnButton;
    public Button CancelButton;

    public GameObject characterInfoPanel; // ��ü �г�
    public Image characterIcon; // ���� ���� ĳ���� �������� ǥ���� �̹���
    public Image skillIcon1; // ù ��° ��ų ������
    public Image skillIcon2; // �� ��° ��ų ������
    public Image Synergy; // �ó��� ������
    public TextMeshProUGUI moveCountText; // ���� �̵� Ƚ���� ǥ���� TextMeshPro �ؽ�Ʈ
    private CharacterBase currentCharacter; // ���� ���� ���� ĳ����

    public GameObject healthPrefab; // ü���� ��Ÿ�� ĭ ������
    public Transform healthContainer; // ü�� ĭ�� ���� �����̳�
    private List<GameObject> healthIndicators = new List<GameObject>(); // ���� ǥ�õ� ü�� ĭ ����Ʈ

    public GameObject turnPrefab; // ���� ��Ÿ�� ������
    public Transform turnContainer; // �� �������� ���� �����̳�
    private List<GameObject> turnIndicators = new List<GameObject>(); // ���� ǥ�õ� �� ������ ����Ʈ

    private bool isCharacterPlacementActive = false; // ĳ���� ��ġ �۾��� Ȱ��ȭ�Ǿ� �ִ��� ����
    private PrefabSpawner prefabSpawner; // ĳ���� ��ġ�� �����ϴ� PrefabSpawner ����

    // Start is called before the first frame update
    void Start()
    {
        sylph = FindObjectOfType<Sylph>();
        turnManager = FindObjectOfType<TurnManager>();
        gridManager = FindObjectOfType<GridManager>();
        prefabSpawner = FindObjectOfType<PrefabSpawner>(); // PrefabSpawner ���� ��������
        if (sylph != null)
        {
            UpdateManaUI(); // �ʱ� ���� ǥ��
        }
        // EndTurnButton Ŭ�� �̺�Ʈ ����
        EndTurnButton.onClick.AddListener(EndTurnButtonClicked);
        CancelButton.onClick.AddListener(CancelButtonClicked);

        EndTurnButton.interactable = false; // ��ư�� �������� ��Ȱ��ȭ�� ���·� ����
        CancelButton.interactable = false; // ��ư ��Ȱ��ȭ

        InitializeTurnUI(5); // 5���� ������ ����
    }
    // Update is called once per frame
    void Update()
    {
        if (currentCharacter != null)
        {
            // ���� �� ĳ������ �̵� ���� Ƚ�� �ǽð� ǥ��
            moveCountText.text = currentCharacter.MoveCount.ToString();
        }

        // ������ �����Ǹ� UI�� ������Ʈ
        UpdateManaUI();
    }
    private void UpdateManaUI()
    {
        // ���� ���� ǥ�ÿ� ��ġ�ϵ��� ������ ����
        int currentManaCount = sylph.currentMana;

        // ������ ���� ǥ�� ����
        while (manaIndicators.Count > currentManaCount)
        {
            Destroy(manaIndicators[manaIndicators.Count - 1]);
            manaIndicators.RemoveAt(manaIndicators.Count - 1);
        }

        // ������ ���� ����ŭ ������ �߰�
        while (manaIndicators.Count < currentManaCount)
        {
            GameObject manaInstance = Instantiate(manaPrefab, manaContainer);
            manaIndicators.Add(manaInstance);
        }
    }
    public void UpdateHealthUI(CharacterBase character)
    {
        if (character == null) return;

        // ���� ü�¿� �°� ������ ����
        int currentHealth = character.Health;
        int maxHealth = character.MaxHealth;

        // ���� ü�� ĭ �ʱ�ȭ
        foreach (var healthIndicator in healthIndicators)
        {
            Destroy(healthIndicator);
        }
        healthIndicators.Clear();

        // �ִ� ü�¿� ���� ĭ ����
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject healthInstance = Instantiate(healthPrefab, healthContainer);

            // ���� ü���� �ʰ��� ĭ�� ��Ȱ��ȭ ó��
            if (i >= currentHealth)
            {
                healthInstance.GetComponent<Image>().color = new Color(1, 0, 0, 0.5f); // ��: ���� �ִ� ����
            }

            healthIndicators.Add(healthInstance);
        }
    }

    // ��ư�� ������ ��� ����� �Լ�
    public void CancelButtonClicked()
    {
        if (turnManager.isSylphTurn)
        {
            // ���� ���: ���� ���� ��� �۾� ����
            sylph.UndoAllActivatedCells();
        }
        else if (turnManager.isCharacterTurn)
        {
            turnManager.UndoAllCharacterPathsAndRestartTurn();
        }
        DeactivateCancelButton();
    }

    public void ActivateCancelButton()
    {
        CancelButton.interactable = true; // ��ư Ȱ��ȭ
    }

    public void DeactivateCancelButton()
    {
        CancelButton.interactable = false; // ��ư ��Ȱ��ȭ
    }

    // EndTurnButton�� �������� �� ȣ��Ǵ� �Լ�
    public void EndTurnButtonClicked()
    {
        if (turnManager.isSylphTurn) // ������ ���� ���
        {
            StartCoroutine(sylph.StartMovement()); // ������ �̵� ���� �ڷ�ƾ ȣ��
            Debug.Log("���� �̵� ����");
        }
        else if (turnManager.isCharacterTurn && turnManager.currentCharacterIndex >= turnManager.activeCharacters.Count) // ������ ĳ������ �� ���� ��
        {
            StartCoroutine(turnManager.ExecuteTurn()); // ��� ĳ���� �̵� ���� �ڷ�ƾ ȣ��
            Debug.Log("��� ĳ���� �̵� ����");
        }

        negativeEndTurnButton(); // ��ư ��Ȱ��ȭ
    }

    public void ActivateEndTurnButton()
    {
        EndTurnButton.interactable = true; // 
    }
    public void negativeEndTurnButton()
    {
        EndTurnButton.interactable = false; // 
    }

    // �ʱ� �� UI ���� �޼���
    private void InitializeTurnUI(int initialTurns)
    {
        for (int i = 0; i < initialTurns; i++)
        {
            GameObject turnInstance = Instantiate(turnPrefab, turnContainer);
            turnIndicators.Add(turnInstance);
        }
    }

    // ���� ���� ������ ȣ��� �޼���: �������� �ϳ� ����
    public void DecreaseTurn()
    {
        if (turnIndicators.Count > 0)
        {
            // ������ �������� �����Ͽ� ���� �پ��� ȿ���� ��
            Destroy(turnIndicators[turnIndicators.Count - 1]);
            turnIndicators.RemoveAt(turnIndicators.Count - 1);
        }
    }

    // ���� �� ĳ���� ���� UI ������Ʈ �޼���
    public void UpdateCharacterUI(CharacterBase character)
    {
        // ĳ���� ���� �г��� Ȱ��ȭ
        characterInfoPanel.SetActive(true);

        currentCharacter = character;
        // ĳ���� ������ �� ��ų ������ ����
        characterIcon.sprite = character.icon;
        skillIcon1.sprite = character.skillIcon1;
        skillIcon2.sprite = character.skillIcon2;
        Synergy.sprite = character.Synergy;

        // ü�� UI ������Ʈ
        UpdateHealthUI(character);
    }
}
