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
    public GameObject manaPrefab; // 마나를 나타낼 프리팹
    public Transform manaContainer; // 마나 프리팹을 담을 컨테이너
    public GridManager gridManager;
    private List<GameObject> manaIndicators = new List<GameObject>(); // 현재 표시된 마나 프리팹 리스트
    public TurnManager turnManager; // 턴 매니저 참조 - 턴을 관리
    public Button EndTurnButton;
    public Button CancelButton;

    public GameObject characterInfoPanel; // 전체 패널
    public Image characterIcon; // 현재 턴의 캐릭터 아이콘을 표시할 이미지
    public Image skillIcon1; // 첫 번째 스킬 아이콘
    public Image skillIcon2; // 두 번째 스킬 아이콘
    public Image Synergy; // 시너지 아이콘
    public TextMeshProUGUI moveCountText; // 남은 이동 횟수를 표시할 TextMeshPro 텍스트
    private CharacterBase currentCharacter; // 현재 턴을 가진 캐릭터

    public GameObject healthPrefab; // 체력을 나타낼 칸 프리팹
    public Transform healthContainer; // 체력 칸을 담을 컨테이너
    private List<GameObject> healthIndicators = new List<GameObject>(); // 현재 표시된 체력 칸 리스트

    public GameObject turnPrefab; // 턴을 나타낼 프리팹
    public Transform turnContainer; // 턴 프리팹을 담을 컨테이너
    private List<GameObject> turnIndicators = new List<GameObject>(); // 현재 표시된 턴 프리팹 리스트

    private bool isCharacterPlacementActive = false; // 캐릭터 배치 작업이 활성화되어 있는지 여부
    private PrefabSpawner prefabSpawner; // 캐릭터 배치를 관리하는 PrefabSpawner 참조

    // Start is called before the first frame update
    void Start()
    {
        sylph = FindObjectOfType<Sylph>();
        turnManager = FindObjectOfType<TurnManager>();
        gridManager = FindObjectOfType<GridManager>();
        prefabSpawner = FindObjectOfType<PrefabSpawner>(); // PrefabSpawner 참조 가져오기
        if (sylph != null)
        {
            UpdateManaUI(); // 초기 마나 표시
        }
        // EndTurnButton 클릭 이벤트 연결
        EndTurnButton.onClick.AddListener(EndTurnButtonClicked);
        CancelButton.onClick.AddListener(CancelButtonClicked);

        EndTurnButton.interactable = false; // 버튼은 보이지만 비활성화된 상태로 설정
        CancelButton.interactable = false; // 버튼 비활성화

        InitializeTurnUI(5); // 5개의 턴으로 시작
    }
    // Update is called once per frame
    void Update()
    {
        if (currentCharacter != null)
        {
            // 현재 턴 캐릭터의 이동 가능 횟수 실시간 표시
            moveCountText.text = currentCharacter.MoveCount.ToString();
        }

        // 마나가 변동되면 UI를 업데이트
        UpdateManaUI();
    }
    private void UpdateManaUI()
    {
        // 현재 마나 표시와 일치하도록 프리팹 갱신
        int currentManaCount = sylph.currentMana;

        // 부족한 마나 표시 제거
        while (manaIndicators.Count > currentManaCount)
        {
            Destroy(manaIndicators[manaIndicators.Count - 1]);
            manaIndicators.RemoveAt(manaIndicators.Count - 1);
        }

        // 부족한 마나 수만큼 프리팹 추가
        while (manaIndicators.Count < currentManaCount)
        {
            GameObject manaInstance = Instantiate(manaPrefab, manaContainer);
            manaIndicators.Add(manaInstance);
        }
    }
    public void UpdateHealthUI(CharacterBase character)
    {
        if (character == null) return;

        // 현재 체력에 맞게 프리팹 갱신
        int currentHealth = character.Health;
        int maxHealth = character.MaxHealth;

        // 기존 체력 칸 초기화
        foreach (var healthIndicator in healthIndicators)
        {
            Destroy(healthIndicator);
        }
        healthIndicators.Clear();

        // 최대 체력에 따라 칸 생성
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject healthInstance = Instantiate(healthPrefab, healthContainer);

            // 현재 체력을 초과한 칸은 비활성화 처리
            if (i >= currentHealth)
            {
                healthInstance.GetComponent<Image>().color = new Color(1, 0, 0, 0.5f); // 예: 투명도 있는 빨강
            }

            healthIndicators.Add(healthInstance);
        }
    }

    // 버튼이 눌러진 경우 실행될 함수
    public void CancelButtonClicked()
    {
        if (turnManager.isSylphTurn)
        {
            // 기존 기능: 실프 관련 취소 작업 수행
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
        CancelButton.interactable = true; // 버튼 활성화
    }

    public void DeactivateCancelButton()
    {
        CancelButton.interactable = false; // 버튼 비활성화
    }

    // EndTurnButton이 눌러졌을 때 호출되는 함수
    public void EndTurnButtonClicked()
    {
        if (turnManager.isSylphTurn) // 실프의 턴인 경우
        {
            StartCoroutine(sylph.StartMovement()); // 실프의 이동 시작 코루틴 호출
            Debug.Log("실프 이동 시작");
        }
        else if (turnManager.isCharacterTurn && turnManager.currentCharacterIndex >= turnManager.activeCharacters.Count) // 마지막 캐릭터의 턴 종료 후
        {
            StartCoroutine(turnManager.ExecuteTurn()); // 모든 캐릭터 이동 시작 코루틴 호출
            Debug.Log("모든 캐릭터 이동 시작");
        }

        negativeEndTurnButton(); // 버튼 비활성화
    }

    public void ActivateEndTurnButton()
    {
        EndTurnButton.interactable = true; // 
    }
    public void negativeEndTurnButton()
    {
        EndTurnButton.interactable = false; // 
    }

    // 초기 턴 UI 설정 메서드
    private void InitializeTurnUI(int initialTurns)
    {
        for (int i = 0; i < initialTurns; i++)
        {
            GameObject turnInstance = Instantiate(turnPrefab, turnContainer);
            turnIndicators.Add(turnInstance);
        }
    }

    // 턴이 지날 때마다 호출될 메서드: 프리팹을 하나 제거
    public void DecreaseTurn()
    {
        if (turnIndicators.Count > 0)
        {
            // 마지막 프리팹을 제거하여 턴이 줄어드는 효과를 줌
            Destroy(turnIndicators[turnIndicators.Count - 1]);
            turnIndicators.RemoveAt(turnIndicators.Count - 1);
        }
    }

    // 현재 턴 캐릭터 정보 UI 업데이트 메서드
    public void UpdateCharacterUI(CharacterBase character)
    {
        // 캐릭터 정보 패널을 활성화
        characterInfoPanel.SetActive(true);

        currentCharacter = character;
        // 캐릭터 아이콘 및 스킬 아이콘 설정
        characterIcon.sprite = character.icon;
        skillIcon1.sprite = character.skillIcon1;
        skillIcon2.sprite = character.skillIcon2;
        Synergy.sprite = character.Synergy;

        // 체력 UI 업데이트
        UpdateHealthUI(character);
    }
}
